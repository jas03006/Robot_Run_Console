using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAsset
{
    enum icon
    {
        error = -1,
        empty = 0,
        wall,
        monster,
        npc,
        player,
        item,
        buff_item
    }
    class Asset
    {
        public string[] icon_str = 
                    {"\u3000"//빈공간 0
                    ,"■"//벽 1
                    ,"▨"//몬스터 2
                    ,"◎" // NPC 3
                    ,"●" // 캐릭터 4
                    ,"ο" // 아이템 5
                ,"★"
                    };

        public List<List<string>>[] image_str = 
                    {new List<List<string>>(){//빈공간 0
                        new List<string>()
                    { "\u3000" }}
                    ,new List<List<string>>(){//벽 1
                        new List<string>()
                    {"■"}}
                    ,new List<List<string>>(){new List<string>()//몬스터2
                    {"★" } }
                    ,new List<List<string>>(){new List<string>()// NPC 3
                    {"◎"}} 
                    ,new List<List<string>>(){new List<string>()// 캐릭터 4
                    {"\u3000■■\u3000",
                        "■■■■",
                    "\u3000■■\u3000",
                    "\u3000■■\u3000"}, //stop
                        new List<string>()
                    {"\u3000■■\u3000", 
                        "■■■■",
                    "\u3000■■\u3000",
                    "\u3000■■\u3000"}, //running
                        new List<string>()
                    {"\u3000■■\u3000",
                        "■■■■",
                    "\u3000■■\u3000",
                    "\u3000▼▼\u3000"}, //jumping
                        new List<string>()
                    {"\u3000■■\u3000",
                        "■■■■",
                    "\u3000■■\u3000",
                    "■\u3000\u3000■"}, //falling
                        new List<string>()
                    {"\u3000■■\u3000",
                        "■■■■"}, // sliding
                         new List<string>()
                   {"\u3000\u3000◀■■\u3000\u3000",
                    "\u3000\u3000\u3000\u3000∑■\u3000\u3000",
                    "◀■■■■■■",
                    "◀■■■■■■",
                    "\u3000\u3000∑■\u3000\u3000",
                    "\u3000\u3000◀■■\u3000\u3000"} // super_invincible
                        
                    }
                    ,new List<List<string>>(){// 아이템 5
                        new List<string>()
                    {"ο" } ,
                        new List<string>()
                    {"○" }
                    },new List<List<string>>(){// 버프 아이템 6
                        new List<string>()
                    {"★" },
                        new List<string>()
                    {"♥" },
                        new List<string>()
                    {"ⓜ" }
                    } };

        public string str_intro = @"


          ,------.        ,--.            ,--.      ,------.                  
          |  .--. ' ,---. |  |-.  ,---. ,-'  '-.    |  .--. ',--.,--.,--,--,
          |  '--'.'| .-. || .-. '| .-. |'-.  .-'    |  '--'.'|  ||  ||      \ 
          |  |\  \ ' '-' '| `-' |' '-' '  |  |      |  |\  \ '  ''  '|  ||  | 
          `--' '--' `---'  `---'  `---'   `--'      `--' '--' `----' `--''--' 
          
          
          
          
                                  게임 시작
                                  게임 설명
                                   나가기";
        public string str_intro_explain = $@"


          
                         슬라이딩: s 
                         점프:     space bar
                         더블점프: 점프 도중 space bar
                         
                         <아이템>
                         ο: 100점 획득
                         ○: 1000점 획득
                         
                         <버프 아이템>
                         ★: 무적 모드 돌입
                         ♥: 생명력 1 획득 (최대 3)
                         ⓜ: 자석 효과 획득
                        
                         ※단계별로 속도가 점점 빨라집니다";



    }
}
