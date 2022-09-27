using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model;

namespace FastBurgerMaker_GameServer
{
    internal class Program
    {
        public const int ServerPort = 9090;

        static void Main(string[] args)
        {
            var appServer = new AppServer();

            if (!appServer.Setup(ServerPort))
            {
                return;
            }

            if (!appServer.Start())
            {
                return;
            }

            var initSDKOutcome = GameLiftServerAPI.InitSDK();
            if (!initSDKOutcome.Success)
            {
                Console.WriteLine("GameLift init fail : " + initSDKOutcome.Error.ToString());
                return;
            }

            ProcessParameters processParameters = new ProcessParameters(
                (gameSession) =>
                {
                    gameSession.MaximumPlayerSessionCount = 4;

                    GameLiftServerAPI.ActivateGameSession();
                },
                (updateGameSession) =>
                {
                    var describePlayerSessionsRequest = new DescribePlayerSessionsRequest();
                    describePlayerSessionsRequest.GameSessionId = updateGameSession.GameSession.GameSessionId;

                    var describePlayerSessionsOutcome = GameLiftServerAPI.DescribePlayerSessions(describePlayerSessionsRequest);
                    if(describePlayerSessionsOutcome.Result.PlayerSessions.Count == 4)
                    {
                        //게임 시작
                    }
                },
                () =>
                {
                    GameLiftServerAPI.ProcessEnding();
                },
                () =>
                {
                    return true;
                },
                ServerPort,
                new LogParameters(new List<string>()
                {
                    "/local/game/logs/myserver.log"
                }
                )
            );

            appServer.NewSessionConnected += new SessionHandler<AppSession>(appServer_NewSessionConnected);
            appServer.SessionClosed += new SessionHandler<AppSession, CloseReason>(appServer_SessionClosed);
        }

        static void appServer_NewSessionConnected(AppSession session)
        {
            session.Send("Hello new session!");
        }

        static void appServer_SessionClosed(AppSession session, CloseReason closeReason)
        {
            session.Send("Bye session!");
        }
    }
}

public class ADD : CommandBase<AppSession, StringRequestInfo>
{
    public override void ExecuteCommand(AppSession session, StringRequestInfo requestInfo)
    {
        session.Send(requestInfo.Parameters.Select(p => Convert.ToInt32(p)).Sum().ToString());
    }
}

public class MULT : CommandBase<AppSession, StringRequestInfo>
{
    public override void ExecuteCommand(AppSession session, StringRequestInfo requestInfo)
    {
        var result = 1;

        foreach (var factor in requestInfo.Parameters.Select(p => Convert.ToInt32(p)))
        {
            result *= factor;
        }

        session.Send(result.ToString());
    }
}