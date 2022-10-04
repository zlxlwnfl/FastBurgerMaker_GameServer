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
        static public ConcurrentDictionary<string, int> GameTimeEndMap = new ConcurrentDictionary<string, int>();

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

        static public byte[] makeResponse(RequestKey requestKey, byte[] bodyArray)
        {
            var sizeBytes = BitConverter.GetBytes(sizeof(int) + bodyArray.Length);
            var requestKeyBytes = BitConverter.GetBytes((int)RequestKey.OTHER_PLAYER_BURGER_COUNT);

            var sizeAndRequestKeyBytes = sizeBytes.Concat(requestKeyBytes);

            if(bodyArray == null)
            {
                return sizeAndRequestKeyBytes.ToArray();
            }
            else
            {
                return sizeAndRequestKeyBytes.Concat(bodyArray).ToArray();
            }
        }
    }

    public class USER_READY : CommandBase<GameAppSession, GameRequestInfo>
    {
        public override void ExecuteCommand(GameAppSession session, GameRequestInfo requestInfo)
        {
            user_ready_dto userReadyDto = user_ready_dto.GetRootAsuser_ready_dto(
                new ByteBuffer(requestInfo.Body)
                );

            var acceptPlayerSessionResult = GameLiftServerAPI.AcceptPlayerSession(userReadyDto.PlayerSessionId);
            if(!acceptPlayerSessionResult.Success)
            {
                var sendMessage = Program.makeResponse(
                    RequestKey.SUCESS,
                    Encoding.UTF8.GetBytes(acceptPlayerSessionResult.Error.ToString())
                    );

                session.Send(sendMessage.ToString());
            }

            var describePlayerSessionsRequestWithPlayerSessionId = new DescribePlayerSessionsRequest();
            describePlayerSessionsRequestWithPlayerSessionId.PlayerSessionId = userReadyDto.PlayerSessionId;

            var describePlayerSessionsResultWithPlayerSessionId = GameLiftServerAPI.DescribePlayerSessions(describePlayerSessionsRequestWithPlayerSessionId);
            var gameSessionId = describePlayerSessionsResultWithPlayerSessionId.Result.PlayerSessions[0].GameSessionId;

            var playerList = Program.GamePlayersMap[gameSessionId];
            playerList.Add(userReadyDto.PlayerSessionId);

            if(playerList.Count >= Program.MaximumPlayerCount)
            {
                List<string> burgerListToComplete = makeBurgerListToComplete(100);

                playerList.ForEach(playerSessionId =>
                {
                    GameAppSession playerAppSession = Program.gameAppServer.GetSessionByID(
                        Program.PlayerSessionMap[playerSessionId]
                        );

                    FlatBufferBuilder flatBufferBuilder = new FlatBufferBuilder(1);
                    game_start_dto.Startgame_start_dto(flatBufferBuilder);
                    game_start_dto.CreateBurgerListToCompleteVector(flatBufferBuilder,
                        burgerListToComplete.ConvertAll<StringOffset>(burger =>
                        {
                            return flatBufferBuilder.CreateString(burger);
                        }).ToArray()
                        );
                    var encodeResult = game_start_dto.Endgame_start_dto(flatBufferBuilder);
                    flatBufferBuilder.Finish(encodeResult.Value);

                    var sendMessage = Program.makeResponse(
                        RequestKey.GAME_START,
                        flatBufferBuilder.SizedByteArray()
                        );

                    playerAppSession.Send(sendMessage.ToString());
                });
            }
        }

        private List<string> makeBurgerListToComplete(int size)
        {
            List<string> burgerListToComplete = new List<string>();

            for(int i = 0; i < size; i++)
            {
                burgerListToComplete.Add(makeBurgerToComplete());
            }

            return burgerListToComplete;
        }

        private string makeBurgerToComplete()
        {
            // 1: 윗빵
            // 2~6: 재료
            // 7: 아랫빵

            Random random = new Random();

            string burgerToComplete = "1";

            for(int i = 0; i < 5; i++)
            {
                burgerToComplete += random.Next(2, 7);
            }

            burgerToComplete += "7";

            return burgerToComplete;
        }
    }

    public class TIME_END : CommandBase<GameAppSession, GameRequestInfo>
    {
        public override void ExecuteCommand(GameAppSession session, GameRequestInfo requestInfo)
        {
            time_end_dto timeEndDto = time_end_dto.GetRootAstime_end_dto(
                new ByteBuffer(requestInfo.Body)
                );

            string currentPlayerSessionId = timeEndDto.PlayerSessionId;
            string gameSessionId = Program.PlayerGameMap[currentPlayerSessionId];

            int timeEndCount = Program.GameTimeEndMap[gameSessionId]++;
            if(timeEndCount >= Program.MaximumPlayerCount)
            {
                // 게임 종료

                List<string> playerSessions = Program.GamePlayersMap[gameSessionId];

                List<KeyValuePair<string, int>> playerBurgerCountRank = new List<KeyValuePair<string, int>>();

                playerSessions.ForEach(playerSessionId =>
                {
                    playerBurgerCountRank.Add(
                        new KeyValuePair<string, int>(playerSessionId, Program.PlayerBurgerCountMap[playerSessionId])
                        );
                });

                playerBurgerCountRank.Sort((x, y) => y.Value.CompareTo(x.Value));

                List<string> playerRank = new List<string>();

                playerBurgerCountRank.ForEach(playerBurgerCount =>
                {
                    playerRank.Add(playerBurgerCount.Key);
                });

                playerSessions.ForEach(playerSessionId =>
                {
                    GameAppSession playerAppSession = Program.gameAppServer.GetSessionByID(
                        Program.PlayerSessionMap[playerSessionId]
                        );

                    FlatBufferBuilder flatBufferBuilder = new FlatBufferBuilder(1);
                    game_end_dto.Startgame_end_dto(flatBufferBuilder);
                    game_end_dto.CreateRankVector(flatBufferBuilder,
                        playerRank.ConvertAll<StringOffset>(player =>
                        {
                            return flatBufferBuilder.CreateString(player);
                        }).ToArray()
                        );
                    var encodeResult = game_end_dto.Endgame_end_dto(flatBufferBuilder);
                    flatBufferBuilder.Finish(encodeResult.Value);

                    var sendMessage = Program.makeResponse(
                        RequestKey.GAME_END,
                        flatBufferBuilder.SizedByteArray()
                        );

                    playerAppSession.Send(sendMessage.ToString());
                });
            }
            else
            {
                var sendMessage = Program.makeResponse(
                    RequestKey.SUCESS,
                    null
                    );

                session.Send(sendMessage.ToString());
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

                GameAppSession otherPlayerAppSession = Program.gameAppServer.GetSessionByID(
                    Program.PlayerSessionMap[playerSessionId]
                    );

                FlatBufferBuilder flatBufferBuilder = new FlatBufferBuilder(1);
                other_player_burger_count_dto.Startother_player_burger_count_dto(flatBufferBuilder);
                other_player_burger_count_dto.AddOtherPlayerSessionId(flatBufferBuilder, flatBufferBuilder.CreateString(currentPlayerSessionId));
                other_player_burger_count_dto.AddOtherPlayerBurgerCount(flatBufferBuilder, currentPlayerBurgerCount);
                var encodeResult = other_player_burger_count_dto.Endother_player_burger_count_dto(flatBufferBuilder);
                flatBufferBuilder.Finish(encodeResult.Value);

                var sendMessage = Program.makeResponse(
                    RequestKey.OTHER_PLAYER_BURGER_COUNT, 
                    flatBufferBuilder.SizedByteArray()
                    );

                otherPlayerAppSession.Send(sendMessage.ToString());
            });
        }
    }
}