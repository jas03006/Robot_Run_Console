using MyAsset;
using MyObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMap
{
    class Map_Cell //맵의 한칸을 의미하는 객체
    {
        public List<Object_On_Map> steppers { get; protected set; } //맵 한칸에 올라와 있는 객체들을 담는 리스트
        public int[] pos { get; protected set; } //맵 상의 위치
        public Map_Cell(int x, int y)
        {
            steppers = new List<Object_On_Map>();
            pos = new int[2] { x, y };
        }
        public bool step_in(Object_On_Map obm) 
        {
            if (steppers.Count == 0 || get_highest().stepable)
            {
                steppers.Add(obm);
                //obm.set_pos(pos[0], pos[1]);
                return true;
            }
            return false;
        }
        public void step_out(Object_On_Map obm)
        {
            steppers.Remove(obm);
        }

        public void step_out_head()
        {
            steppers.RemoveAt(steppers.Count - 1);
        }
        public Object_On_Map get_highest() // 셀에서 가장 위쪽에 있는 객체 반환
        {
            return steppers[steppers.Count - 1];
        }

        public bool do_interaction(Player p) // 해당 셀에 있는 객체들과 플레이어 인터액션 진행
        {
            for (int i = 0; i < steppers.Count; i++)
            {
                Object_On_Map now_object = steppers[i];
                if (now_object.Equals(p))
                {
                    continue;
                }
                now_object.interact(p); // 객체 별 인터액션
                if (now_object.state_flag == object_state_flag.destroyed) // 해당 객체가 파괴된 경우 맵에서 삭제
                {
                    steppers.Remove(now_object);
                    i--;
                }
                if (p.state_flag == object_state_flag.destroyed) //플레이어가 파괴된 경우 false 반환하여 처리
                {
                    //steppers.Remove(p);                    
                    return false;
                }
            }
            return true;
        }
    }
    class Map
    {
        private Map_Cell[,] map; //실제 맵
        private char[,] map_char; // 맵 출력 버퍼

        public Asset asset { get; private set; } // 출력 시 사용할 string 에셋

        public int window_height { get; private set; } = 13; // 맵 일부 출력을 위한 윈도우 사이즈
        public int window_width { get; private set; } = 60;
        public Map()
        {
            int height = 13;
            int width = 1000;
            //Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);            
            map = new Map_Cell[height, width];
            map_char = new char[window_height, window_width];
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    map[i, j] = new Map_Cell(i, j);
                }
            }
            asset = new Asset();
        }
        public bool set_object_to(Object_On_Map obm, float x, float y) { // 맵에 객체 첫 배치
            int old_x = (int)obm.pos[0];
            int old_y = (int)obm.pos[1];
            int new_x = (int)x;
            int new_y = (int)y;
            if (!check_map_inside(new_x, new_y))
            {
                return false;
            }
            return map[new_x, new_y].step_in(obm);
        }
        public bool move_object_to(Object_On_Map obm, float x, float y) // 이미 배치된 객체 이동 (첫 배치 시에도 사용가능 하긴 하지만 구분하는 것이 좋음)
        {
            //update_map_char();
            int old_x = (int)obm.pos[0];
            int old_y = (int)obm.pos[1];
            int new_x =(int) x;
            int new_y = (int)y;
            if (!check_map_inside(obm, new_x,new_y)) {
                return false;
            }
            if (old_x == new_x && old_y == new_y)
            {
                return true;
            }
            if (map[new_x, new_y].step_in(obm))
            {
                if (old_x != new_x || old_y != new_y)
                {
                    map[old_x, old_y].step_out(obm);
                }
                return true;
            }            

            if (old_y != new_y && map[old_x, new_y].step_in(obm))
            {
                map[old_x, old_y].step_out(obm);
                return true;
            }

            if (old_x != new_x && map[new_x, old_y].step_in(obm))
            {
                //Console.WriteLine("amsdmaksdmmalk");
                map[old_x, old_y].step_out(obm);
                return true;
            }           

            return false;
        }
        public void remove_object_at(float x, float y) { // 맵에서 객체 지우기
            map[(int)x, (int)y].step_out_head();
        }

        public bool do_interaction(Player p) // 플레이어가 차지하는 범위에 있는 셀들과 플레이어 간의 인터액션 진행
        {
            for (int i =0; i < p.get_height(asset); i++) {
                for (int j = 0; j < p.get_width(asset); j++)
                {
                    if (!map[(int)p.pos[0] - i, (int)p.pos[1] - j].do_interaction(p)) {
                        return false;
                    }
                }
            }
            return true;//map[(int)p.pos[0], (int)p.pos[1]].do_interaction(p);
        }

        public int GetLength(int i)
        {
            if (i > 2 || i < 0)
            {
                return -1;
            }
            return map.GetLength(i);
        }

        public icon get_icon(float x, float y) // 해당 위치의 가장 위쪽에있는 객체의 아이콘 반환 (출력 시 이용)
        {
            return map[(int)x, (int)y].get_highest().icon;
        }
        public move_state_flag get_move_state(float x, float y) //해당 위치의 가장 위쪽에 있는 객체의 move state 반환 (출력 시 이용)
        {
            return map[(int)x, (int)y].get_highest().move_state;
        }
        public bool is_stepable(float x, float y) //해당 위치의 가장 위쪽에 있는 객체의 stepable 반환
        {
            return map[(int)x, (int)y].get_highest().stepable;
        }

        public void update_map_char(Player player) // 맵 출력 버퍼 업데이트
        {
            int width = map.GetLength(1);
            int height = map.GetLength(0);
            List<string> now_str;
            int j_start = Math.Min(Math.Max(0, player.get_left(asset)-3), width - window_width);
            int j_end = Math.Min(width, player.get_left(asset)-3 + window_width);
            int i_start = height - window_height;
            //맵셀을 모두 순회하며 그리기
            for (int i = i_start; i < height; i++)
            {
                for (int j = j_start; j < j_end; j++)
                {
                    now_str = asset.image_str[(int)this.get_icon(i, j)][(int)this.get_move_state(i,j)];  // 맵셀의 가장 최상단 객체의 image str 가져옴

                    if (now_str == null || (this.get_icon(i, j) == icon.player && player.invincible_time%200 > 100)) { // 타격을 당해 잠시 무적이 된 경우 깜빡임
                        continue;
                    }

                    for (int k = 0; k < now_str.Count; k++) // 객체의 string image가 차지하는 범위 만큼 버퍼 갱신
                    {
                        for (int v = 0; v < now_str[k].Length; v++) {
                            map_char[i - i_start - (now_str.Count - k - 1), j - j_start - now_str[k].Length + 1 + v] = now_str[k][v];   
                            
                        }
                    }
                }
            }

            // 플레이어 체력 표시를 위한 버퍼 갱신 (좌상단)
            for (int i = 0; i  < player.life; i++) { 
                map_char[0, i] = '♥';
            }
            for (int i = player.life; i < player.max_life; i++)
            {
                map_char[0, i] = '♡';
            }

            // 플레이어 슈퍼무적 버프 표시를 위한 버퍼 갱신 (좌상단)
            if (player.super_invincible_time > 2000 || player.super_invincible_time%200 > 100) {
                map_char[1, 0] = asset.image_str[(int)icon.buff_item][(int)move_state_flag.stop][0][0];
            }
            else{
                map_char[1, 0] = '\u3000';
            }
            // 플레이어 자석 버프 표시를 위한 버퍼 갱신 (좌상단)
            if (player.magnet_time > 2000 || player.magnet_time % 200 > 100)
            {
                map_char[1, 1] = asset.image_str[(int)icon.buff_item][(int)move_state_flag.jumping][0][0];
            }
            else{
                map_char[1, 1] = '\u3000';
            }
            map_char[1, 3] = '\u3000';



            //실제 출력
            Console.WriteLine("".PadRight(window_width*2,'─'));
            for (int i = 0; i < height-i_start; i++)
            {
                for (int j = 0; j < j_end-j_start; j++)
                {
                    Console.Write(map_char[i, j]);
                }
                Console.WriteLine();
            }
            Console.WriteLine("".PadRight(window_width*2, '─'));
        }


        public bool check_map_inside(float new_x, float new_y) //해당 좌표가 맵 내부인지 체크
        {
            return !(new_x < 0 || new_x >= map.GetLength(0) || new_y < 0 || new_y >= map.GetLength(1));
        }

        public bool check_map_inside(Object_On_Map obm, float new_x, float new_y) //해당 좌표에 위치한 객체의 출력범위가 맵을 벗어나지 않는지 체크
        {
            for (int i = 0; i < 2; i++) {
                for (int j = 0; j < 2; j++)
                {
                    if ((new_x - i* (obm.get_height(asset)-1) < 0 || new_x-i * (obm.get_height(asset) - 1) >= map.GetLength(0) || new_y-j * (obm.get_width(asset) - 1) < 0 || new_y-j * (obm.get_width(asset) - 1) >= map.GetLength(1))) {
                        return false;
                    }
                }
            }
            return true;
        }



    }
}
