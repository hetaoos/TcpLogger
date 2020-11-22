using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace TcpLogger
{
    /// <summary>
    /// 包处理
    /// </summary>
    public class ClientHandler : SimpleChannelInboundHandler<byte[]>, IHandler
    {
        private readonly IServiceProvider serviceProvider;
        private readonly HexDumpService hexDumpService;
        private readonly ILogger log;

        /// <summary>
        /// Gets the channel handler context.
        /// </summary>
        private IChannelHandlerContext ctx;

        public IHandler Peer { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="clientSettings"></param>
        /// <param name="storageService"></param>
        /// <param name="log"></param>
        public ClientHandler(IServiceProvider serviceProvider, HexDumpService hexDumpService, ILogger<ClientHandler> log)
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
            log.LogError($"{ctx.Channel.Id} {exception}");
            ctx.CloseAsync();
            Peer?.CloseAsync();
        }

        /// <summary>
        /// 通道激活
        /// </summary>
        /// <param name="context"></param>
        public override void ChannelActive(IChannelHandlerContext context)
        {
            ctx = context;
            base.ChannelActive(context);
        }

        /// <summary>
        /// 通道关闭
        /// </summary>
        /// <param name="context"></param>
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
            ctx = null;
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
            if (Peer == null)
                ctx.CloseAsync();
            else
                Peer.SendAsync(msg);
            hexDumpService.Add(msg,false);
        }

        public Task SendAsync(byte[] msg)
            => ctx.WriteAndFlushAsync(msg);

        public Task CloseAsync()
            => ctx.CloseAsync();
    }
}