using MyAsset;
using MyMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyObject
{
    enum object_state_flag // 파괴 확인을 위한 enum
    {
        exist = 0,
        destroyed = 1
    }

    enum move_state_flag {  // 플레이어 움직임 상태마다 출력을 바꾸기 위한 enum //일반 객체들도 인덱스로 활용하여 출력 변경
        stop = 0,
        running, //1 
        jumping, //2
        falling, //3
        sliding, //4
        super_invincible
    }

    interface Move // 이동 가능한 객체를 위한 인터페이스 // 현재는 맵 상의 모든 객체가 이동 가능하다고 가정하여 필수적인 구현은 아님
    {
        bool move_on_map(Map map, float x, float y);
    }

    interface fight // 전투 구현을 대비한 인터페이스
    {

    }


    class Object_On_Map : Move // 맵 상의 오브젝트 (이동가능)
    {
        public float[] pos { get; protected set; } // 위치 좌표 (맵 좌표는 소수점을 빼고 사용)
        public icon icon { get; protected set; } // 표시할 아이콘 (객체의 종류를 나타냄)
        public bool stepable { get; protected set; } // 밟을수있는지 여부 (해당 객체가 존재하는 맵셀에 다른 객체가 추가로 올 수 있는지)
        public object_state_flag state_flag { get; protected set; } 
        public float jump_momentum { get; protected set; } // 점프 시 가해지는 운동량
        public float[] now_velocity { get; protected set; } = { 0f,0f}; //각각 x,y 방향의 속력(unit/sec)
        public float mass { get; protected set; } // 질량 (점프 가속도 계산 시 사용)

        public int jump_cnt_limit{ get; protected set; } = 1; // 점프 도중 추가 점프 가능 횟수
        public int jump_cnt { get; protected set; } = 0; // 현재 연속 점프 횟수 (땅에 닿으면 0으로 초기화)
        public move_state_flag move_state { get; protected set; } = move_state_flag.stop; //상태 별로 다른 출력을 위한 enum  

        public TimeSpan slide_time { get; protected set; } // 슬라이딩 1회시 슬라이딩 시간
        public DateTime last_slide { get; protected set; } // 슬라이딩 시간 적용울 위한 슬라이딩 시작 시간 기록
        public Object_On_Map()
        {
            pos = new float[2] { 0, 0 };
            icon = icon.empty; ;
            stepable = true;
            state_flag = object_state_flag.exist;
        }
        public Object_On_Map(icon icon_)
        {
            pos = new float[2] { 0, 0 };
            icon = icon_;
            stepable = true;
            state_flag = object_state_flag.exist;
        }
        public Object_On_Map(int x, int y, icon icon_, bool stepable_)
        {
            pos = new float[2] { x, y };
            icon = icon_;
            stepable = stepable_;
            state_flag = object_state_flag.exist;
        }
        public void set_pos(float x, float y)
        {
            pos[0] = x;
            pos[1] = y;
        }
        public bool set_on_map(Map map, float x, float y) // 맵에 배치해달라고 요청
        {
            if (map.set_object_to(this, x, y))
            {
                set_pos(x, y);
                return true;
            }
            return false;

        }
        public bool move_on_map(Map map, float x, float y) // 맵에서 해당 좌표로 이동시켜달라고 요청
        {
            int p0 = (int)pos[0];
            int p1 = (int)pos[1];
            if (p0 == (int)x && p1 == (int)y) // 맵 상의 좌표(int)가 같을 경우 실제 좌표(float)만 바꿔주고 맵에서 이동시키진 않음
            {
                set_pos(x, y);
                return false;
            }

            if (map.move_object_to(this, x, y))
            {
                set_pos(x, y);
                if (is_on_something(map))
                {
                    set_velocity(0, now_velocity[1]);
                    jump_cnt = 0;

                    if (icon == icon.player && move_state != move_state_flag.super_invincible && (move_state == move_state_flag.falling || !is_sliding()))
                    {
                        move_state = move_state_flag.running; //플레이어 인 경우 falling 상태에서 땅에 닿을때 running 상태로 변경
                    }
                }
                return true;
            }
            
            /*if (p0 == (int)x)
            {
                now_velocity[1] = 0f;
            }
            else if (p1 == (int)y)
            {
                now_velocity[0] = 1f;
            }*/

            return false;

        }
        public virtual void interact(Player p) // 플레이어와 인터액션을 위한 함수 (객체마다 인터액션이 상이하여 virtual로 구현)
        {

        }
        public void jump(Map map) //점프
        {
            if (jump_cnt_limit <= jump_cnt) // 연속 점프횟수 초과시 리턴
            {
                return;
            }
            float correction = -1 *jump_momentum * (float)jump_cnt / 2f; // 연속 점프 시 운동량 조정
            if (now_velocity[0] >= 0) {
                correction = jump_momentum * (float)jump_cnt / 3f;
            }
            get_momentum(jump_momentum + correction ,-1,0); // 운동량만을 가해주어 속도 정보만 바꿔주고 실제 이동은 Game.animate()에서 진행
            jump_cnt++;
            if ( move_state != move_state_flag.super_invincible) { // 슈퍼 무적 상태에서는 점프 폼으로 따로 출력하지 않음
                move_state = move_state_flag.jumping;
            }
            //Console.WriteLine("jump");
        }

        public void slide() { //슬라이딩
            if (move_state == move_state_flag.running) // running 상태에서만 슬라이딩 가능
            {
                last_slide = DateTime.Now;
                move_state = move_state_flag.sliding;
            }                      
        }
        public bool is_sliding() { //현재 슬라이딩 중인지 시간 기반으로 체크
            return DateTime.Now - last_slide < slide_time;
        }
        public bool is_on_something(Map map) { // 2D 맵 좌표상 아래쪽에 무언가를 밟고 서있는지 체크
            if (map.check_map_inside(pos[0] + 1, pos[1]) && map.is_stepable(pos[0] + 1, pos[1]))
            {                
                return false;
            }
            return true;
        }

        public void get_momentum(float mag, float x, float y)//크기(F*t = m*v), 방향 //운동량을 받은 경우 속도 증감
        {
            float temp = (float)Math.Sqrt(x * x + y * y);
            float temp_v = mag / mass;
            now_velocity[0] += temp_v * (x / temp);
            now_velocity[1] += temp_v * (y / temp);
        }
        public void get_accel(float accel, float x, float y)//크기(F*t = m*v), 방향 // 가속도를 인자로 받은 경우 속도 증감
        {
            float temp = (float)Math.Sqrt(x * x + y * y);           
            now_velocity[0] += accel * (x / temp);
            now_velocity[1] += accel * (y / temp);
        }
        public void get_accel(float x_accel, float y_accel)//방향 별 가속도 크기
        {
            now_velocity[0] += x_accel;
            now_velocity[1] += y_accel;
        }

        public void set_velocity(float x, float y) { // 속도 변경
            now_velocity[0] = x;
            now_velocity[1] = y;
        }

        public void moving(Map map, float delta_t) { //맵에 움직여 달라고 요청
            if (now_velocity[0] ==0f && now_velocity[1] ==0f) { //속력이 0이라면 이동 요청 자체를 하지 않음
                return;
            }
            
            if (icon == icon.player && now_velocity[0] > 0 && move_state != move_state_flag.super_invincible) { // 2d 맵 상에서 아래로가는 속도를 가지고 있다면 falling 상태로 전환
                move_state = move_state_flag.falling;
            }
            move_on_map(map, pos[0]+delta_t*now_velocity[0], pos[1] + delta_t * now_velocity[1]); //속도와 경과 시간으로 이동하게 될 좌표 계산 후 이동 요청
        }

        // 위치좌표 보다 실제 차지하는 좌표범위가 더 크기 때문에 만든 메소드
        // 좌우가 y좌표, 상하가 x좌표
        // 실제 차지하는 범위의 우측하단 꼭지점을 위치 좌표로 정함
        public int get_left(Asset asset) {  // 가장 좌측의 y좌표 반환
            return (int)pos[1] - asset.image_str[(int)icon][(int)move_state][0].Length;
        }
        public int get_top(Asset asset) //가장 상단의 x좌표 반환
        {
            return (int)pos[0] - asset.image_str[(int)icon][(int)move_state].Count;
        }
        public int get_right(Asset asset) 
        {
            return (int)pos[1];
        }
        public int get_bottom(Asset asset)
        {
            return (int)pos[0];
        }

        public int get_width(Asset asset) { //차지하는 범위의 좌우폭 반환
            return asset.image_str[(int)icon][(int)move_state][0].Length;
        }
        public int get_height(Asset asset) //차지하는 범위의 상하 높이 반환
        {
            return asset.image_str[(int)icon][(int)move_state].Count;
        }
        public void die() // 맵 상에서 파괴를 위해 상태 저장 (실제 파괴는 Mapcell의 do_interaction에서 진행)
        {
            state_flag = object_state_flag.destroyed;
        }
    }
    class Player : Object_On_Map
    {
        public string name { get; private set; }
        public int score { get; private set; }
        public int life { get; private set; } // 생명력 (한번 벽 충돌 시 마다 1회 차감)
        public int max_life { get; private set; } //최대 생명력
        public float invincible_time { get; private set; } // 밀리초 단위 // 충돌 시 무적 시간
        public float super_invincible_time { get; private set; } // 밀리초 단위 //버프 획득 시 슈퍼 무적 시간
        public float speed_coeff { get; private set; } // 버프 혹은 각종 단기 효과로 인한 속력 증감 계수
        public float speed { get; private set; } = 20f; //기본 속력

        public float magnet { get; private set; } = 0f; //자석 가속도
        public float magnet_time { get; private set; } = 0f; // 자석 버프 유지 시간 

        public Player(string name_, int life_)
        {
            name = name_;
            pos = new float[2];
            set_pos(4, 4);
            icon = icon.player;
            stepable = true;
            score = 0;
            state_flag = object_state_flag.exist;
            jump_momentum = 85;
            mass = 10;
            jump_cnt_limit  = 2;
            jump_cnt= 0;
            move_state = move_state_flag.running;
            slide_time = new TimeSpan(0, 0, 0, 0, 500);
            life = life_;
            max_life = life_;
            invincible_time = 0f;
            super_invincible_time = 0f;
            speed_coeff = 1f;
            set_velocity(0, speed * speed_coeff);
        }

        public void init()
        {
            set_pos(4, 4);
            score = 0;
            state_flag = object_state_flag.exist;
            move_state = move_state_flag.running;
            life = 3;
            invincible_time = 0f;
            super_invincible_time = 0f;
            speed_coeff = 1f;
        }

        public bool check_map_and_interact(Map map) // 맵에 인터액션 요청 (현재 플레이어랑만 인터액션하기 때문에 플레이어에 구현)
        {
            return map.do_interaction(this);
        }

        public void get_score(int score_) // 점수 획득 (점수 아이템 획득 시 사용)
        {
            score += score_;
        }
        
        public void get_attacked(Object_On_Map oom) { // 충돌 시 생명력 1회 차감
            if (super_invincible_time > 0) { // 슈퍼 무적일 경우 충돌한 객체 파괴 
                oom.die();                
                return;
            }
            if (invincible_time <= 0) { // 무적이 아닐 시에만 차감
                //Console.Clear();
                life--;
                invincible_time = 1000f;
                if (life <= 0) {
                    die();
                }
            }
        }

        public void back_from_invincible(float delta_t) { // 무적/슈퍼무적 시간 자동 차감
            super_invincible_time -= delta_t;
            invincible_time -= delta_t;
            if (super_invincible_time <= 0f)
            {
                super_invincible_time = 0f;
                if (move_state == move_state_flag.super_invincible) {
                    move_state = move_state_flag.running;
                    speed_coeff = 1f;
                    set_velocity(now_velocity[0], speed * speed_coeff);
                    invincible_time = 1000f;
                }
            }
            if (invincible_time <= 0f)
            {
                invincible_time = 0f;
            }
        }

        public void get_super_invincible(float t) { // 슈퍼 무적 버프 획득
            super_invincible_time += t;
            if (super_invincible_time>0) {
                move_state = move_state_flag.super_invincible;
                speed_coeff = 2f;
                set_velocity(now_velocity[0], speed * speed_coeff);
            }
        }

        public void get_healed(int heal) { // 생명력 획득
            life += heal;
            if (life > max_life) {
                life = max_life;
            }
        }

        public void get_magnet(float magnet_, float time_) { //자석 버프 획득
            magnet = magnet_;
            magnet_time = time_;
        }

        public bool lose_magnet(float delta_t) { //밀리초 단위 // 자석 버프 시간 자동 차감
            magnet_time -= delta_t;
            if (magnet_time < 0f) {
                magnet_time = 0f;
                magnet = 0f;
                return false;
            }
            return true;
        }

        public void give_magnet(Object_On_Map oom, float delta_t, Asset asset) { // 맵위의 객체 들에게 자길력 적용
            float x = oom.pos[0];
            float y = oom.pos[1];
            float x_dis = (pos[0] - get_height(asset) / 2 +1 - x );
            float y_dis = (pos[1]+1 - y );
            if ( y_dis > 2 || y_dis < -14) { // 범위 밖의 객체들은 속력 0으로 설정 (현재 평소에는 플레이어만 움직이는 상태이므로 이렇게 구현, 다른 객체도 움직인다면 수정 필요) 
                oom.set_velocity(0, 0);
                return;
            }
            float x_dir = 1f;
            if (x_dis < 0) {
                x_dir = -1f;
            }
            float y_dir = 0f;
            //if (y_dis >-2) {
               // y_dir = speed*0.7f;
            //}
            oom.set_velocity( magnet * x_dir, y_dir); // 적용 객체의 속도 변경
        }

        public void get_speed(float speed_) { //기본 속도 설정 
            if (speed_ <0) {
                return;
            }
            speed = speed_;
        }
    }
    class Wall : Object_On_Map //벽
    {
        public string name { get; private set; }

        public Wall(string name_)
        {
            name = name_;
            pos = new float[2];
            set_pos(0, 0);
            icon = icon.wall;
            stepable = true;
            state_flag = object_state_flag.exist;
            jump_momentum = 0;
            mass = 10;
        }
        public Wall(string name_, float x, float y)
        {
            name = name_;
            pos = new float[2] { x, y };
            icon = icon.wall;
            stepable = true;
            state_flag = object_state_flag.exist;
            jump_momentum = 0;
            mass = 10;
        }
        /// <summary>
        /// <para>플레이어와 인터액션</para>
        /// </summary>
        /// <param name="p"></param>
        public override void interact(Player p)
        {
            base.interact(p);
            p.get_attacked(this); // 벽과 충돌 시 생명력
        }
    }
    class Monster : Object_On_Map // 미사용
    {
        public string name { get; private set; }
        public Monster(string name_)
        {
            name = name_;
            pos = new float[2];
            set_pos(0, 0);
            icon = icon.monster;
            stepable = true;
            state_flag = object_state_flag.exist;
            jump_momentum = 30;
            mass = 10;
        }
        public Monster(string name_, float x, float y)
        {
            name = name_;
            pos = new float[2] { x, y };
            icon = icon.monster;
            stepable = true;
            state_flag = object_state_flag.exist;
            jump_momentum = 30;
            mass = 10;
        }
        public override void interact(Player p)
        {
            base.interact(p);
            p.die();
        }
    }

    class Npc : Object_On_Map // 미사용
    {

        public override void interact(Player p)
        {
            base.interact(p);
        }
    }
    /// <summary>
    /// 점수 획득 아이템 
    /// </summary>
    class Item : Object_On_Map 
    {
        int score;
        public Item(float x, float y)
        {
            pos = new float[2] { x, y };
            score = 100;
            icon = icon.item;
            stepable = true;
            state_flag = object_state_flag.exist;

        }
        public Item(float x, float y, int score_)
        {
            pos = new float[2] { x, y };
            score = score_;
            icon = icon.item;
            stepable = true;
            state_flag = object_state_flag.exist;
            if (score_ > 100) {
                move_state = move_state_flag.running;
            }
        }
        public override void interact(Player p)
        {
            base.interact(p);
            state_flag = object_state_flag.destroyed;
            p.get_score(score);
        }
    }

    class Buff_Item : Object_On_Map
    {
        public float super_invincible_time { get; private set; }
        /// <summary>
        /// 힐
        /// </summary>
        public int heal { get; private set; }
        public float magnet { get; private set; }
        public float magnet_time { get; private set; } //밀리초단위
        public Buff_Item(float x, float y)
        {
            pos = new float[2] { x, y };
            icon = icon.buff_item;
            stepable = true;
            state_flag = object_state_flag.exist;
            super_invincible_time = 7000;
            heal = 0;
        }
        public Buff_Item(float x, float y, float super_invincible_time_, int heal_, float magnet_time_, float magnet_)
        {
            pos = new float[2] { x, y };
            icon = icon.buff_item;
            stepable = true;
            state_flag = object_state_flag.exist;
            super_invincible_time = super_invincible_time_;

            heal = heal_; // 힐량이 있다면 슈퍼무적 효과 삭제
            if (heal >0) {
                super_invincible_time = 0;
                move_state = move_state_flag.running;//1
            }
            magnet_time = magnet_time_; // 자석효과가 있다면 힐과 슈퍼무적 효과 삭제
            if (magnet_time > 0) {
                move_state = move_state_flag.jumping; //2
                magnet = magnet_;
                heal = 0;
                super_invincible_time = 0;
            }
        }
        public override void interact(Player p)
        {
            base.interact(p);
            state_flag = object_state_flag.destroyed;
            switch (move_state) {
                case move_state_flag.stop:
                    p.get_super_invincible(super_invincible_time);
                    break;
                case move_state_flag.running:
                    p.get_healed(heal);
                    break;
                case move_state_flag.jumping:
                    p.get_magnet(magnet, magnet_time);
                    break;
                default:
                    break;
            }          
        }
    }
}
