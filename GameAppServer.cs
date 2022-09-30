using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;

namespace FastBurgerMaker_GameServer
{
    public class GameAppSession: AppSession<GameAppSession, GameRequestInfo>
    {

    }

    public class GameAppServer: AppServer<GameAppSession, GameRequestInfo>
    {
        public GameAppServer(): 
            base(new DefaultReceiveFilterFactory<GameReceiveFilter, GameRequestInfo>())
        {

        }
    }
}
