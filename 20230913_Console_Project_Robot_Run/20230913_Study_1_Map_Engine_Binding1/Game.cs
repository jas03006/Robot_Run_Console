using MyAsset;
using MyMap;
using MyObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyGame
{
    public enum Display_State { // 게임 출력 상태를 구분하기 위한 enum
        intro =0,
        game,
        explain,
        exit
        
    }
    class Game
    {
        private Map map;
        public Player player { get; private set; }
        public List<Object_On_Map> moving_objects { get; private set; } //움직일 수 있는 객체들을 한번에 관리라기 위한 리스트
        public Random rand { get; private set; }
        public float gravity_coefficient { get; private set; } = 9.8f; //중력 가속도 계수

        public List<String[]> score_board { get; private set; }

        public Display_State display_state { get; private set; } = Display_State.intro;

        public int[] display_pos = { 0, 0 };

        public Game()
        {
            rand = new Random();
            score_board = new List<string[]>();
            map = new Map();
        }

        public void init_game(bool is_restart) // 재시작 시에도 사용하기 위해 초기화 분리
        {
            string pname;
            Console.Write("플레이어의 이름을 입력하세요: ");
            pname = Console.ReadLine();
            player = new Player(pname, 3);

            moving_objects = new List<Object_On_Map>();
            if (is_restart) {
                map = new Map();
            }
            init_map();
        }

        public void generate_items(int score) // 벽이 미리 생성된 맵에 자동으로 아이템 생성
        {
            int r = 0;
            int half_jump_len = (int)(player.speed * player.jump_momentum / player.mass / gravity_coefficient);
            int half_jump_len_right = (int)(player.speed * Math.Sqrt(player.jump_momentum * player.jump_momentum / player.mass / gravity_coefficient / gravity_coefficient));
            int h = map.GetLength(0);
            int w = map.GetLength(1);
            int [] past_wall = new int[2] { h-1,0}; // 왼쪽 가장 가까운 벽
            int[] next_wall = new int[2] { h-1,0}; // 오른쪽 가장 가까운 벽
            bool wall_flag = true;
            
            for (int i = player.get_width(map.asset)+2; i < w; i+=1)
            {
                r = h-1;               
                if (i > next_wall[1] && wall_flag) //최근 찾은벽을 지나쳤고 예전에 벽을 찾은 상태라면 (벽을 못찾았던 적이 있다면 진행X)
                {
                    wall_flag = false;
                    for (int wall_ind = i; wall_ind < w;  wall_ind++) {
                        for (int j = h - 1; j >= 3; j--)
                        {
                            if (map.get_icon(j, wall_ind) == icon.wall && map.get_icon(j - 1, wall_ind) == icon.empty 
                                && map.get_icon(j - 2, wall_ind) == icon.empty && map.get_icon(j - 3, wall_ind) == icon.empty) //아래에서 솟은 벽 탐색
                            {
                                past_wall[0] = next_wall[0];
                                past_wall[1] = next_wall[1];
                                next_wall[0] = j - 3;
                                next_wall[1] = wall_ind;
                                wall_flag = true;
                                break;
                            } else if (map.get_icon(j, wall_ind) == icon.empty && map.get_icon(j - 1, wall_ind) == icon.empty 
                                && map.get_icon(j - 2, wall_ind) == icon.empty && map.get_icon(j - 3, wall_ind) == icon.wall) { // 위에서 자란 벽 탐색
                                past_wall[0] = next_wall[0];
                                past_wall[1] = next_wall[1];
                                next_wall[0] = h-1;
                                next_wall[1] = wall_ind;
                                wall_flag = true;
                                break;
                            }
                        }
                        if (wall_flag) { 
                            break; 
                        }
                    }
                    if (!wall_flag)
                    {
                        past_wall[0] = next_wall[0];
                        past_wall[1] = next_wall[1];
                        next_wall[0] = h - 1;
                        next_wall[1] = w-1;
                    }
                }

                //작은 점수 아이템 생성
                if (i%2 == 0) {
                    if (next_wall[1] >= i)
                    {
                        r -= (h - 1 - past_wall[0]) * (half_jump_len_right - (i - past_wall[1])) / half_jump_len_right;
                        r = Math.Min(r,  h - 1 - (h - 1 - next_wall[0]) * (half_jump_len - (next_wall[1] - i)) / half_jump_len);
                        if (r-1 >= h - 1)
                        {
                            r = h - 1;
                        }
                        else {
                            r = Math.Max(0,r-1);
                        }                        
                    }
                    
                    Item now_item = new Item(r, i, score);
                    now_item.set_on_map(map, r, i);
                    moving_objects.Add(now_item);
                }
               
                //버프 아이템, 큰 점수 아이템 덩어리 생성
                //버프 아이템 생성을 위한 매개 변수 (버프 아이템 생성자에서 처리되게 구현하여 동등한 확률로 버프 생성 되는 것을 기대)
                int heal = rand.Next()%2;
                float magnet_time = 0f;
                if (rand.Next(0, 3) == 0f) {
                    magnet_time = 10000f;
                }
                int is_buff = rand.Next() % 10;
                if (i % ((int)player.speed * 3) == (int)player.speed) // 벽 생성 간격인 player.speed*3 사이에 하나씩 버프 혹은 큰점수덩어리가 생성되게 함
                {
                    int x = h / 2;
                    if (is_buff <= 1 ) //버프 아이템 생성
                    {
                        Buff_Item now_buff = new Buff_Item(x, i, 10000, heal, magnet_time, player.speed/3f);
                        now_buff.set_on_map(map, x, i);
                        moving_objects.Add(now_buff);
                    }
                    else { //십자 모양으로 큰점수 덩어리 생성
                        
                        Item now_item = new Item(x, i, score*10);
                        now_item.set_on_map(map, x, i);
                        moving_objects.Add(now_item);

                        now_item = new Item(x, i-1, score * 10);
                        now_item.set_on_map(map, x, i-1);
                        moving_objects.Add(now_item);
                        
                        now_item = new Item(x, i+1, score * 10);
                        now_item.set_on_map(map, x, i+1);
                        moving_objects.Add(now_item);

                        now_item = new Item(x-1, i, score * 10);
                        now_item.set_on_map(map, x-1, i);
                        moving_objects.Add(now_item);

                        now_item = new Item(x+1, i, score * 10);
                        now_item.set_on_map(map, x+1, i);
                        moving_objects.Add(now_item);
                    }
                }

            }
        }

        public void generate_walls(int distance, int wall_height) {  //벽 생성
            int r, j_start, j_end;
            for (int i = distance; i < map.GetLength(1); i+=distance)
            {
                r = rand.Next() % 3; //랜덤으로 3가지 종류의 벽 중 하나씩 생성
                if (r == 0)
                {
                    j_start = map.GetLength(0) - wall_height; //아래에서 자라나는 벽
                    j_end = map.GetLength(0);
                }
                else if (r == 1)
                {
                    j_start = 0;
                    j_end = map.GetLength(0) - wall_height; //위에서 내려오는벽
                }
                else {
                    j_start = map.GetLength(0)/2+1;
                    j_end = map.GetLength(0) - wall_height; // 약간 떠있는 벽 (더블 점프/슬라이딩으로 회피 가능)
                }
                for (int j = j_start; j < j_end; j++) {
                    if (map.get_icon(j, i) != icon.empty || !map.is_stepable(j, i))
                    {
                        continue;
                    }
                    else {
                        Wall now_wall = new Wall("wall",j, i);
                        now_wall.set_on_map(map, j, i);
                    }
                }                        
                              
            }
        }

        public void generate_monsters(int cnt) // 몬스터 사용을 대비한 몬스터 자동 생성 (현재 몬스터는 적용하지 않음)
        {
            int r, c;
            for (int i = 0; i <cnt; i++)
            {
                r = rand.Next() % map.GetLength(0);
                c = rand.Next() % map.GetLength(1);
                while (map.get_icon(r, c) == icon.player || !map.is_stepable(r, c))
                {
                    r = rand.Next() % map.GetLength(0);
                    c = rand.Next() % map.GetLength(1);
                }
                moving_objects.Add(new Monster($"Monster{i}", r, c));
            }
        }

        public void init_map() //맵의 첫 초기화
        {         
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    Object_On_Map es = new Object_On_Map();; // 맵 전체를 icon.empty 속성의 oom으로 채움
                    es.set_on_map(map, i, j);
                }
            }

            player.set_on_map(map, map.GetLength(0)-1, player.pos[1]); // 캐릭터

            generate_walls((int)(player.speed * 3), 2);
            generate_items(100);
            //generate_monsters();
           
        }
        public void change_map() { // 맵하나를 다 주파한 경우 맵 새로 생성 //생성 속도가 느리다면 캐시 기능 추가할 계획 
            player.get_speed (player.speed*1.1f); //맵 하나 클리어 마다 속도 증가
            player.set_velocity(player.now_velocity[0], player.speed * player.speed_coeff);

            for (int i = 0; i < map.GetLength(0); i++) //맵위의 벽과 아이템 제거
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    while (map.get_icon(i,j)!=icon.empty) {
                        map.remove_object_at(i,j);
                    }                    
                }
            }
            moving_objects.Clear(); 
            //벽과 아이템 새로 생성
            generate_walls((int)(player.speed*3)- (int)(player.speed * 3)%2, 2);
            generate_items(100);
        }

        public bool move_user(Char keyInput) //사용자 키입력 적용
        {
            switch (keyInput)
            {
                /* case 'w':
                     dx = -1;
                     break;
                 case 's':
                     dx = 1;
                     break;
                case 'a':
                     dy = -1;
                     break;
                 case 'd':
                     dy = 1;
                     break;*/
                case ' ':
                    player.jump(map);
                    return true;
                case 'ㄴ':
                case 'S':
                case 's':
                    player.slide();
                    return true;
                default:
                    return true;
            }
            /*
            player.get_momentum(10, dx, dy);
            return true;*/
        }

        public bool move_objects_auto() //몬스터 적용을 대비한 자동 움직이기 (현재 미적용)
        {
            Object_On_Map oom;
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };
            for (int i = 0; i < moving_objects.Count; i++)
            {
                oom = moving_objects[i];
                if (oom.state_flag == object_state_flag.destroyed)
                {
                    moving_objects.Remove(oom);
                    i--;
                    continue;
                }
                int d = rand.Next() % dx.Length;
                float x = oom.pos[0] + dx[d];
                float y = oom.pos[1] + dy[d];
                oom.get_momentum(10,dx[d], dy[d]);
            }
            return true;
        }

        public void force_gravity(float delta_t) { //중력 적용 (현재 플레이어에게만 적용)
            //Object_On_Map now_o;
            float gravity = delta_t * gravity_coefficient;
              /*  for (int i = 0; i < moving_objects.Count; i++)
                {
                    now_o = moving_objects[i];
                    if (now_o.is_on_something(map))
                    {
                        now_o.set_pos((float)(int)now_o.pos[0], now_o.pos[1]);                       
                    }
                    else {
                        now_o.get_accel(gravity, 1, 0);
                    }
                }*/
            if (player.is_on_something(map)) 
            {
                player.set_pos((float)(int)player.pos[0], player.pos[1]);
            }
            else { //공중에 떠 있을 경우에만 중력 가속도 적용
                player.get_accel(gravity, 1, 0);
            }
        }

        // 자석 버프를 먹었을 경우 자기력 적용
        public void force_magnet(float delta_t) { //밀리초단위
            if (!player.lose_magnet(delta_t)) {                
                return;
            }
            Object_On_Map now_o;
            float gravity = delta_t * gravity_coefficient;
            for (int i = 0; i < moving_objects.Count; i++) // 모든 움직일수 있는 객체 리스트를 순회하며 적용 // 모든 객체를 순회하지말고 캐릭터 주변만 순회한다면 더 빨라질수 있음
            {
                now_o = moving_objects[i];
                player.give_magnet(now_o,delta_t,map.asset);
            }            
        }

        // 프레임 업데이트 마다 흐른 시간(delta_t)을 기반으로 게임 상황 업데이트 적용
        public bool animate(float delta_t) { //초단위 
            //force_user_move(delta_t);
            force_gravity(delta_t); //중력 적용
            force_magnet(delta_t*1000); // 자석 버프 적용
            player.back_from_invincible(delta_t * 1000); // 플레이어 충돌 시 무적 시간

            Object_On_Map now_o;
            for (int i = 0; i < moving_objects.Count; i++)
            {
                now_o = moving_objects[i]; 
                now_o.moving(map, delta_t); // 객체 실제 이동 (상호작용 시에는 속도 정보만을 변경해 놓았고, 실제 이동은 이곳에서 진행) 
            }

            if (player.pos[1] + delta_t * player.now_velocity[1] > (float)map.GetLength(1) - 1f) // 현재 맵의 우측 끝에 도달한지 체크 후 맵 새로 생성
            {
                change_map();
                player.move_on_map(map, player.pos[0], player.get_width(map.asset)); 
            }
            else {
                player.moving(map, delta_t); //플레이어 이동
            }            

            map.update_map_char(player); // 출력버퍼 업데이트 후 출력
            
            return player.check_map_and_interact(map); // 플레이어와 객체간 충돌과 인터액션 체크           
        }


        private bool process_map_round()
        {
            Console.SetCursorPosition(display_pos[0], display_pos[1]);
            Console.WriteLine("SpaceBar: 점프 / s: 슬라이드".PadRight(map.window_width, ' '));
            Console.WriteLine($"<< Score: {player.score} >>".PadLeft(map.window_width+10, ' '));
            map.update_map_char(player);
            print_press_any_key(30);
            Console.Clear();
            DateTime timer_frame = DateTime.Now;
            DateTime temp_timer = DateTime.Now;

            Char key ;
            bool is_end = false;

            Task.Factory.StartNew(() =>  // 비동기로 사용자 입력 받음
            {
                while (!is_end)
                {
                    while (!Console.KeyAvailable) { }
                    key = Console.ReadKey(true).KeyChar;
                    move_user(key);
                }
            });

            float delta_t;            
            while (!is_end)
            {     
                temp_timer = DateTime.Now;
                delta_t = (float)((temp_timer - timer_frame).TotalMilliseconds);//밀리초 단위
                if (delta_t>10)
                {                    
                    timer_frame = temp_timer;
                    
                    Console.SetCursorPosition(display_pos[0], display_pos[1]);
                    Console.WriteLine("SpaceBar: 점프 / s: 슬라이드".PadRight(map.window_width, ' '));
                    Console.WriteLine($"<< Score: {player.score} >>".PadLeft(map.window_width, ' '));

                    is_end = !animate(delta_t*2f / 1000f);    // 흐른 시간 측정 후 실제 애니메이션 출력                     
                    
                    Console.WriteLine($"velocity: {player.now_velocity[0]}, {player.now_velocity[1]}".PadRight(80));
                    Console.WriteLine($"position: {player.pos[0]}, {player.pos[1]}");
                    Console.WriteLine($"invincible: {player.invincible_time}");
                    Console.WriteLine($"super invincible: {player.super_invincible_time}");
                    Console.WriteLine($"magnet: {player.magnet_time}");
                    Console.WriteLine($"fps: {(1000/delta_t)}");
                    Console.WriteLine($"state: {player.move_state}".PadRight(80));
                }

            }            
            return true;
        }

        public void print_press_any_key(int time_limit)
        {
            Console.WriteLine();
            Console.WriteLine("아무 키나 눌러 진행해주세요.");
            DateTime start_t = DateTime.Now;
            TimeSpan delta_t = TimeSpan.FromSeconds(0);
            uint check = 0;
            uint max_t = (uint)time_limit;
            while (!Console.KeyAvailable && (int)(delta_t.TotalSeconds) < max_t)
            {
                delta_t = DateTime.Now - start_t;
                if ((int)(delta_t.TotalSeconds) > check)
                {
                    Console.Write(max_t - check + " ");
                    check += 1;
                }
            }
            if ((int)(delta_t.TotalSeconds) >= max_t)
            {
                Console.WriteLine();
                return;
            }
            Console.ReadKey(true);
            Console.WriteLine();
        }

        public void show_intro() {            
            int option = 1;
            int len = System.Enum.GetValues(typeof(Display_State)).Length;
            int[] arrow_pos = { display_pos[0]+30, display_pos[1]+12 };
            while (display_state == Display_State.intro)
            {
                Console.SetCursorPosition(display_pos[0], display_pos[1]);
                Console.WriteLine("방향키와 엔터키를 사용하여 선택");
                Console.WriteLine(map.asset.str_intro);
                Console.SetCursorPosition(arrow_pos[0], arrow_pos[1] + option);
                Console.Write("▶");
                while (!Console.KeyAvailable)
                {
                }
                switch (Console.ReadKey(true).Key) {
                    case ConsoleKey.UpArrow:
                        option = option - 1 + Convert.ToInt32(option - 1 == 0)*(len-1);                        
                        break;
                    case ConsoleKey.DownArrow:
                        option = (option + 1)%len + Convert.ToInt32(option+1 == len);
                        break;
                    case ConsoleKey.Enter:
                        display_state = (Display_State)option;
                        break;
                    default:
                        break;
                }

                switch (display_state)
                {
                    case Display_State.game:
                        Console.Clear();
                        break;
                    case Display_State.explain:
                        Console.Clear();
                        show_explain();
                        Console.Clear();
                        display_state = Display_State.intro;
                        break;
                    case Display_State.exit:
                        Console.Clear();
                        Environment.Exit(0);
                        break;
                    default:
                        break;
                }
            }
        }
        public void show_explain()
        {
            Console.Clear();
            Console.WriteLine("엔터를 눌러 뒤로 가기");
            Console.SetCursorPosition(display_pos[0],display_pos[1]);
            Console.WriteLine(map.asset.str_intro_explain);
                       
            while (!Console.KeyAvailable && display_state == Display_State.explain)
            {
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.Enter:
                        display_state = Display_State.intro;
                        break;
                    default:
                        break;
                }
            }
        }
        public void show_score_board() {
        score_board.Sort((string []x,string[] y) => Convert.ToInt32(y[1]).CompareTo(Convert.ToInt32(x[1])));
        for (int i =0; i <score_board.Count; i++) {
            Console.WriteLine($"{i+1}. {score_board[i][0].PadLeft(10)}: {score_board[i][1].PadLeft(10)}점");
        }
        Console.WriteLine();
        }

        public void play_map()
        {
            int flag = 0;
            bool is_end = false;
            show_intro();
            while (!is_end)
            {               
                Console.Clear();
                
                init_game(flag == 1);                
                process_map_round();

                Console.Clear();
                Console.SetCursorPosition(display_pos[0], display_pos[1]);
                Console.WriteLine("SpaceBar: 점프 / s: 슬라이드".PadRight(map.window_width, ' '));
                Console.WriteLine($"<< Score: {player.score} >>".PadLeft(map.window_width, ' '));
                map.update_map_char(player);
                Console.WriteLine();
                Console.WriteLine("사망하였습니다.");
                score_board.Add(new string[]{player.name, Convert.ToString(player.score)});
                show_score_board();

                int left = Console.CursorLeft;
                int top = Console.CursorTop;
                while (true) {
                    Console.SetCursorPosition(left, top);
                    Console.WriteLine("".PadRight(80));
                    Console.WriteLine("".PadRight(80));
                    Console.WriteLine("".PadRight(80));
                    Console.SetCursorPosition(left, top);
                    Console.WriteLine("1: 재시작, 2: 게임 종료");
                    try
                    {
                        flag = Convert.ToInt32( Console.ReadLine());
                        if (flag == 2)
                        {
                            is_end = true;

                            break;
                        }
                        else if(flag == 1){
                            break;
                        }
                    }
                    catch {
                        Console.WriteLine("잘못된 입력입니다.");
                    }
                    Console.WriteLine("잘못된 입력입니다.");
                }
            }
            Console.WriteLine("게임이 종료됩니다.");
        }
    }
}
