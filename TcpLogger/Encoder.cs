using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace TcpLogger
{
    public class Encoder : MessageToByteEncoder<byte[]>
    {
        private ILogger log;

        /// <summary>
        ///
        /// </summary>
        /// <param name="log"></param>
        public Encoder(ILogger log)
        {
            this.log = log;
        }

        protected override void Encode(IChannelHandlerContext context, byte[] message, IByteBuffer output)
        {
            if (message?.Any() == true)
                output.WriteBytes(message);
        }
    }
}