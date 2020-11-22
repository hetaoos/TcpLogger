using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace TcpLogger
{
    public class Decoder : ByteToMessageDecoder
    {
        ILogger log;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="log"></param>
        public Decoder(ILogger log)
        {
            this.log = log;
        }
        protected override void Decode(IChannelHandlerContext context, IByteBuffer message, List<object> output)
        {
            if (message.ReadableBytes > 0)
            {
                var bytes = new byte[message.ReadableBytes];
                var msg = message.ReadBytes(bytes);
                output.Add(bytes);
            }
        }
    }
}