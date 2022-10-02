using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase.Protocol;

namespace FastBurgerMaker_GameServer
{
    public class GameReceiveFilter : IReceiveFilter<GameRequestInfo>
    {
        const int BODY_LENGTH_KEY_SIZE = sizeof(int);
        const int REQUEST_KEY_SIZE = sizeof(int);

        public int LeftBufferSize { get; set; }

        public IReceiveFilter<GameRequestInfo> NextReceiveFilter { get; set; }

        public FilterState State { get; set; }

        public GameRequestInfo Filter(byte[] readBuffer, int offset, int length, bool toBeCopied, out int rest)
        {
            if(length < (BODY_LENGTH_KEY_SIZE + REQUEST_KEY_SIZE))
            {
                // 에러처리
            }

            int bodyLength = BitConverter.ToInt32(
                readBuffer.Take(BODY_LENGTH_KEY_SIZE).ToArray(),
                0
                );
            RequestKey requestKey = (RequestKey)BitConverter.ToInt32(
                readBuffer.Skip(BODY_LENGTH_KEY_SIZE).Take(REQUEST_KEY_SIZE).ToArray(),
                0
                );

            byte[] bodyArray = readBuffer.Skip(BODY_LENGTH_KEY_SIZE + REQUEST_KEY_SIZE).Take(bodyLength).ToArray();
            
            if(bodyLength < bodyArray.Length)
            {
                rest = bodyArray.Length - bodyLength;
            }
            else
            {
                rest = 0;
            }

            return new GameRequestInfo(
                requestKey.ToString(),
                bodyArray
                );
        }

        public void Reset()
        {
            return;
        }
    }
}
