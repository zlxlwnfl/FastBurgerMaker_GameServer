using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase.Protocol;

namespace FastBurgerMaker_GameServer
{
    public class GameRequestInfo : IRequestInfo
    {
        public string Key { get; set; }
        public string Body { get; set; }

        public GameRequestInfo(string key, string body)
        {
            this.Key = key;
            this.Body = body;
        }
    }
}
