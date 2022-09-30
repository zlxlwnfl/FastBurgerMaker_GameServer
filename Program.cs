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
using FlatBuffers;

namespace FastBurgerMaker_GameServer
{
    internal class Program
    {
        public const int ServerPort = 9090;
        public const int MaximumPlayerCount = 4;

        static void Main(string[] args)
        {
            var gameAppServer = new GameAppServer();

            if (!gameAppServer.Setup(ServerPort))
            {
                return;
            }

            if (!gameAppServer.Start())
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
                    gameSession.MaximumPlayerSessionCount = MaximumPlayerCount;

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

            var processReadyOutcome = GameLiftServerAPI.ProcessReady(processParameters);
            if (processReadyOutcome.Success)
            {
                Console.WriteLine("ProcessReady success.");
            }
            else
            {
                Console.WriteLine("ProcessReady failure : " + processReadyOutcome.Error.ToString());
            }

            gameAppServer.NewSessionConnected += new SessionHandler<GameAppSession>(appServer_NewSessionConnected);
            gameAppServer.SessionClosed += new SessionHandler<GameAppSession, CloseReason>(appServer_SessionClosed);
        }

        static void appServer_NewSessionConnected(GameAppSession session)
        {
            session.Send("Hello new session!");
        }

        static void appServer_SessionClosed(GameAppSession session, CloseReason closeReason)
        {
            session.Send("Bye session!");
        }
    }

    public class USER_READY : CommandBase<GameAppSession, GameRequestInfo>
    {
        public override void ExecuteCommand(GameAppSession session, GameRequestInfo requestInfo)
        {
            var describePlayerSessionsRequest = new DescribePlayerSessionsRequest()
            {
                GameSessionId = GameLiftServerAPI.GetGameSessionId().Result,
                Limit = 1,
                PlayerSessionStatusFilter = PlayerSessionStatusMapper.GetNameForPlayerSessionStatus(PlayerSessionStatus.NOT_SET)
            };

            var playerSessionsResult = GameLiftServerAPI.DescribePlayerSessions(describePlayerSessionsRequest).Result;

            var playerSessionId = playerSessionsResult.PlayerSessions[0].PlayerSessionId;

            var acceptPlayerSessionOutcome = GameLiftServerAPI.AcceptPlayerSession(playerSessionId);
            if(acceptPlayerSessionOutcome.Success)
            {
                session.Send(playerSessionId);
            }
            else
            {
                // 에러처리
            }
        }
    }

    public class BURGER_NUMBER_COMPLETED : CommandBase<GameAppSession, GameRequestInfo>
    {
        public override void ExecuteCommand(GameAppSession session, GameRequestInfo requestInfo)
        {
            //
        }
    }
}