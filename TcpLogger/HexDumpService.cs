using DotNetty.Buffers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace TcpLogger
{
    public class HexDumpService
    {
        private bool type;
        private ILogger log;
        private IByteBuffer buf;
        private DateTime last = DateTime.Now.AddDays(-1);
        private Timer timer;

        public HexDumpService(ILogger<HexDumpService> log)
        {
            this.log = log;
            buf = PooledByteBufferAllocator.Default.Buffer();
            timer = new Timer(TimerCallback, null, 1000 * 2, 1000 * 2);
        }

        public void Add(byte[] msg, bool type)
        {
            lock (buf)
            {
                if (this.type != type)
                {
                    Print();
                    this.type = type;
                }

                buf.WriteBytes(msg);
                last = DateTime.Now;
            }
        }

        private void Print()
        {
            if (buf.ReadableBytes > 0)
            {
                var t = type ? "OUT" : "IN ";
                log.LogInformation($"{t} - {last:yyyy-MM-dd HH:mm:ss} {Environment.NewLine}{ByteBufferUtil.PrettyHexDump(buf)}");
                buf.ResetReaderIndex();
                buf.ResetWriterIndex();
            }
        }

        private void TimerCallback(object state)
        {
            if ((DateTime.Now - last).TotalSeconds < 2 || buf.ReadableBytes == 0)
                return;

            lock (buf)
                Print();
        }
    }
}