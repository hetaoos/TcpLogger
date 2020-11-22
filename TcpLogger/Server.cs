using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TcpLogger
{
    /// <summary>
    /// 服务器
    /// </summary>
    public class Server
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<Server> log;
        private Bootstrap clientBootstrap;

        /// <summary>
        ///
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="db"></param>
        /// <param name="cache"></param>
        /// <param name="serverSettings"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="log"></param>
        public Server(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, ILogger<Server> log)
        {
            this.serviceProvider = serviceProvider;
            this.loggerFactory = loggerFactory;
            this.log = log;
            InternalLoggerFactory.DefaultFactory = loggerFactory;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public async Task Start(int port, IPEndPoint endPoint, CancellationToken stoppingToken)
        {
            IEventLoopGroup bossGroup;
            IEventLoopGroup workerGroup;
            IChannel boundChannel = null;

            bossGroup = new MultithreadEventLoopGroup();
            workerGroup = new MultithreadEventLoopGroup();

            var clientBossGroup = new MultithreadEventLoopGroup();

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap.Group(bossGroup, workerGroup);

                bootstrap.Channel<TcpServerSocketChannel>();

                var logEncoder = loggerFactory.CreateLogger<Encoder>();
                var logDecoder = loggerFactory.CreateLogger<Decoder>();
                bootstrap
                    .Option(ChannelOption.SoBacklog, 100)
                    .Handler(new LoggingHandler("SRV-LSTN"))
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        //pipeline.AddLast(new ReadTimeoutHandler(TimeSpan.FromSeconds(120)));
                        pipeline.AddLast(new LoggingHandler("SRV-CONN"));
                        pipeline.AddLast("framing-enc", new Encoder(logEncoder));
                        pipeline.AddLast("framing-dec", new Decoder(logDecoder));
                        pipeline.AddLast("echo", serviceProvider.GetService<ServerHandler>());
                    }));

                boundChannel = await bootstrap.BindAsync(port);
                log.LogInformation($"Bind: {port}");
                clientBootstrap = new Bootstrap();
                clientBootstrap.Group(clientBossGroup);
                clientBootstrap.Channel<TcpSocketChannel>();

                clientBootstrap
                    .Option(ChannelOption.TcpNodelay, true)
                    .RemoteAddress(endPoint)
                    .Handler(new LoggingHandler("SRV-LSTN"))
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        pipeline.AddLast(new LoggingHandler("SRV-CONN"));
                        pipeline.AddLast("framing-enc", new Encoder(logEncoder));
                        pipeline.AddLast("framing-dec", new Decoder(logDecoder));
                        pipeline.AddLast("echo", serviceProvider.GetService<ClientHandler>());
                    }));

                log.LogInformation("waiting...");
                Console.ReadLine();
                await boundChannel.CloseAsync();
            }
            finally
            {
                try
                {
                    if (boundChannel != null)
                        await boundChannel.CloseAsync();
                }
                catch { }

                try
                {
                    if (clientBossGroup != null)
                        await clientBossGroup.ShutdownGracefullyAsync();
                }
                catch { }
                await Task.WhenAll(
                    bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                    workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
            }
        }

        public async Task<IHandler> CreateClientHandler(IHandler serverHandler)
        {
            if (clientBootstrap == null)
                return null;

            var channel = await clientBootstrap.ConnectAsync();
            var handler = channel.Pipeline.LastOrDefault(o => o is ClientHandler) as ClientHandler;
            handler.Peer = serverHandler;
            serverHandler.Peer = handler;
            return handler;
        }
    }
}