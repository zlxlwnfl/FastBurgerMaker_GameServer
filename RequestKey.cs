using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastBurgerMaker_GameServer
{
    public enum RequestKey
    {
        // GAME CORE: 000
        SUCESS = 000,
        FAIL = 001,
        GAME_START = 002,
        GAME_END = 003,
        TIME_END = 004,
        
        // USER: 100
        USER_READY = 100,
        
        // BURGER: 200
        BURGER_COMPLETED = 200,
        OTHER_PLAYER_BURGER_COUNT = 201,
    }
}
