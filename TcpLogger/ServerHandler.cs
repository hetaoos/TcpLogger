using DotNetty.Transport.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace TcpLogger
{
    /// <summary>
    /// 包处理
    /// </summary>
    public class ServerHandler : SimpleChannelInboundHandler<byte[]>, IHandler
    {
        private readonly IServiceProvider serviceProvider;
        private readonly HexDumpService hexDumpService;
        private readonly ILogger log;

        /// <summary>
        ///
        /// </summary>
        public IHandler Peer { get; set; }

        /// <summary>
        /// Gets the channel handler context.
        /// </summary>
        private IChannelHandlerContext ctx;

        /// <summary>
        ///
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="db"></param>
        /// <param name="smartDoorLockStorageService"></param>
        /// <param name="log"></param>
        public ServerHandler(IServiceProvider serviceProvider, HexDumpService hexDumpService, ILogger<ServerHandler> log)
        {
            this.serviceProvider = serviceProvider;
            this.hexDumpService = hexDumpService;
            this.log = log;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ctx"></param>
        public override void ChannelReadComplete(IChannelHandlerContext ctx)
            => ctx.Flush();

        /// <summary>
        ///
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="exception"></param>
        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
        {
            log.LogError($"{ctx.Channel.Id} {exception.ToString()}");
            base.ExceptionCaught(ctx, exception);
            ctx.CloseAsync();
            Peer?.CloseAsync();
        }

        /// <summary>
        /// 通道激活
        /// </summary>
        /// <param name="context"></param>
        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);
            Peer = null;
            ctx = context;

            var server = serviceProvider.GetService<Server>();
            var r = Task.Run(() => server.CreateClientHandler(this)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 通道关闭
        /// </summary>
        /// <param name="context"></param>
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
            Peer?.CloseAsync();
            Peer = null;
        }

        /// <summary>
        /// 接收到一个包
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="msg"></param>
        protected override void ChannelRead0(IChannelHandlerContext ctx, byte[] msg)
        {
            Peer?.SendAsync(msg);
            //log.LogInformation($"{nameof(ChannelRead0)}: {BitConverter.ToString(msg).Replace("-"," ")}");

            hexDumpService.Add(msg, true);
        }

        public Task SendAsync(byte[] msg)
            => ctx.WriteAndFlushAsync(msg);

        public Task CloseAsync()
            => ctx.CloseAsync();
    }
}