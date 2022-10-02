using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
    public class Program
    {
        public const int ServerPort = 9090;
        public const int MaximumPlayerCount = 4;

        static public ConcurrentDictionary<string, string> PlayerSessionMap = new ConcurrentDictionary<string, string>();
        static public ConcurrentDictionary<string, string> PlayerGameMap = new ConcurrentDictionary<string, string>();
        static public ConcurrentDictionary<string, List<string>> GamePlayersMap = new ConcurrentDictionary<string, List<string>>();
        static public ConcurrentDictionary<string, int> PlayerBurgerCountMap = new ConcurrentDictionary<string, int>();

        static public GameAppServer gameAppServer = new GameAppServer();

        static void Main(string[] args)
        {
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
                    //gameSession.MaximumPlayerSessionCount = MaximumPlayerCount;

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
                    "/local/game/logs/FastBurgerMaker_GameServer.log"
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
            user_ready_dto userReadyDto = user_ready_dto.GetRootAsuser_ready_dto(
                new ByteBuffer(requestInfo.Body)
                );

            var result = GameLiftServerAPI.AcceptPlayerSession(userReadyDto.PlayerSessionId);
            if(result.Success)
            {
                session.Send("sucess");
            }
            else
            {
                session.Send("fail: " + result.Error.ToString());
            }
        }
    }

    public class BURGER_COMPLETED : CommandBase<GameAppSession, GameRequestInfo>
    {
        public override void ExecuteCommand(GameAppSession session, GameRequestInfo requestInfo)
        {
            burger_completed_dto burgerCompletedDto = burger_completed_dto.GetRootAsburger_completed_dto(
                new ByteBuffer(requestInfo.Body)
                );

            string currentPlayerSessionId = burgerCompletedDto.PlayerSessionId;

            int currentPlayerBurgerCount = Program.PlayerBurgerCountMap[currentPlayerSessionId]++;

            string gameSessionId = Program.PlayerGameMap[currentPlayerSessionId];
            List<string> playerSessions = Program.GamePlayersMap[gameSessionId];

            playerSessions.ForEach(playerSessionId =>
            {
                if(playerSessionId == currentPlayerSessionId)
                {
                    return;
                }

                GameAppSession otherPlayerSession = Program.gameAppServer.GetSessionByID(
                    Program.PlayerSessionMap[playerSessionId]
                    );

                FlatBufferBuilder flatBufferBuilder = new FlatBufferBuilder(1);
                other_player_burger_count_dto.Startother_player_burger_count_dto(flatBufferBuilder);
                other_player_burger_count_dto.AddKey(flatBufferBuilder, (int)RequestKey.OTHER_PLAYER_BURGER_COUNT);
                other_player_burger_count_dto.AddOtherPlayerSessionId(flatBufferBuilder, flatBufferBuilder.CreateString(currentPlayerSessionId));
                other_player_burger_count_dto.AddOtherPlayerBurgerCount(flatBufferBuilder, currentPlayerBurgerCount);
                var encodeResult = other_player_burger_count_dto.Endother_player_burger_count_dto(flatBufferBuilder);
                flatBufferBuilder.Finish(encodeResult.Value);

                otherPlayerSession.Send(flatBufferBuilder.SizedByteArray().ToString());
            });
        }
    }
}