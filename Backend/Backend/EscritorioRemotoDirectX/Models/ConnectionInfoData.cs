using WebSocketSharp.Server;

namespace EscritorioRemotoDirectX.Models
{
    public class ConnectionInfoData
    {
        public string RemoteIp { get; set; }
        public int RemotePort { get; set; }
        public WebSocketSessionManager Session { get; set; }
    }
}
