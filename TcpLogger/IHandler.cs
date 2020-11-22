using System.Threading.Tasks;

namespace TcpLogger
{
    /// <summary>
    ///
    /// </summary>
    public interface IHandler
    {
        /// <summary>
        /// 对方
        /// </summary>
        IHandler Peer { get; set; }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Task SendAsync(byte[] msg);

        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns></returns>
        public Task CloseAsync();
    }
}