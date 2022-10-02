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
        GAME_START = 000,
        GAME_END = 001,
        
        // USER: 100
        USER_READY = 100,
        
        // BURGER: 200
        BURGER_LIST_TO_MAKE = 200,
        BURGER_COMPLETED = 201,
        OTHER_PLAYER_BURGER_COUNT = 202,
    }
}
