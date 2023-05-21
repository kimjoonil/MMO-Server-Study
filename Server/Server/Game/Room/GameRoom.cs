using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class GameRoom : JobSerializer
    {
        public int RoomId { get; set; }

        // 게임 방안에 생성될 게임 오브젝트들 플레이어, 몬스터, 발사체 등이 있다.
        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();
        Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();
        public Map Map { get; private set; } = new Map();

        //
        public void Init(int mapId)
        {
            // 생성된 방에 mapId에 해당하는 맵을 로드하고 테스트용으로 몬스터 객체를 하나 생성 스폰 위치는 임시로 고정한다.
            Map.LoadMap(mapId);
            Monster monster = ObjectrManager.Instance.Add<Monster>();
            monster.CellPos = new Vector2Int(5, 5);

            //생성된 몬스터 객체를 방안에 생성
            EnterGame(monster);
        }

        // 어디선가 주기적으로 호출해줘야 한다.
        public void Update()
        {
            // 월드에 있는 몬스터와 발사체를 주기적으로 업데이트하여 상태를 변화시킨다.
            foreach(Monster monster in _monsters.Values)
                monster.Update();
            
            foreach(Projectile projectile in _projectiles.Values)
                projectile.Update();

            // 상속받고 있는 JobSerializer의 Flush를 호출해 작업 처리 큐에
            Flush();
        }

        // 매개변수로 받은 게임 오브젝트의 타입을 구분해 spawn하고 월드 모두에게 알린다.
        public void EnterGame(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            //GetObjectTypeById를 통해 게임 오브젝트의 타입을 얻는다.
            GameObjectType type = ObjectrManager.GetObjectTypeById(gameObject.Id);

            //게임 오브젝트의 타입에 따라 다르게 처리하여 spawn한다.
            if (type == GameObjectType.Player)
            {
                // Player 객체가 게임방에 입장할 때 가져야 할 값을 대입한다.
                Player player = gameObject as Player;
                _players.Add(gameObject.Id, player);
                player.Room = this;

                // 플레이어의 실제 이동 처리 부분
                Map.ApplyMove(player, new Vector2Int(player.CellPos.x, player.CellPos.y));

                // 플레이어 본인클라이언트에 전송
                {
                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.Player = player.Info;
                    player.Session.Send(enterPacket);

                    //플레이어 스폰 처리
                    S_Spawn spawnPacket = new S_Spawn();

                    // 월드에 있는 모든 객체들을 spawn해서 패킷에 추가한다.
                    foreach (Player p in _players.Values)
                    {
                        // 만약 _players에서 가져온 플레이어가 자신이 아니라면 다른 플레이어를 생성하도록 처리한다.
                        if (player != p)
                            spawnPacket.Objects.Add(p.Info);
                    }
                    foreach(Monster m in _monsters.Values)
                        spawnPacket.Objects.Add(m.Info);
                        
                    foreach (Projectile p in _projectiles.Values)
                        spawnPacket.Objects.Add(p.Info);
                        
                    player.Session.Send(spawnPacket);
                }
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = gameObject as Monster;
                _monsters.Add(gameObject.Id, monster);
                monster.Room = this;

                Map.ApplyMove(monster, new Vector2Int(monster.CellPos.x, monster.CellPos.y));
            }
            else if(type == GameObjectType.Projectile)
            {
                Projectile projectile = gameObject as Projectile;
                _projectiles.Add(gameObject.Id, projectile);
                projectile.Room = this;
            }
            // 타 플레이어들에게 전송
            {
                S_Spawn spawnPacket = new S_Spawn();
                spawnPacket.Objects.Add(gameObject.Info);

                // 자신이 아닌 다른 플레이어들에게 어떠한 객체가 생성되었다고 알린다.
                foreach(Player p in _players.Values)
                {
                    if (p.Id != gameObject.Id)
                        p.Session.Send(spawnPacket);
                }
            }
        }

        // 연결이 끊기거나 방에서 퇴장하였을 때 실행되는 부분을 처리한다.
        public void LeaveGame(int objectId)
        {
            //GetObjectTypeById를 통해 게임 오브젝트의 타입을 얻는다.
            GameObjectType type = ObjectrManager.GetObjectTypeById(objectId);

            //게임 오브젝트의 타입에 따라 다르게 처리하여 삭제한다.
            if (type == GameObjectType.Player)
            {
                Player player = null;
                if (_players.Remove(objectId, out player) == false)
                    return;

                // 맵에서 플레이어를 삭제 처리하는 부분
                Map.ApplyLeave(player);
                player.Room = null;

                // 플레이어 본인클라이언트에 전송
                {
                    S_LeaveGame leavePacket = new S_LeaveGame();
                    player.Session.Send(leavePacket);
                }
            }
            else if(type == GameObjectType.Monster)
            {
                Monster monster = null;
                if (_monsters.Remove(objectId, out monster) == false)
                    return;
                
                Map.ApplyLeave(monster);
                monster.Room = null;
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = null;
                if (_projectiles.Remove(objectId, out projectile) == false)
                    return;
                projectile.Room = null;
            }


            // 타 플레이어들에게 전송
            {
                S_Despawn despawnPacket = new S_Despawn();
                despawnPacket.ObjectIds.Add(objectId);

                // 자신이 아닌 다른 플레이어들에게 어떠한 객체가 삭제되었다고 알린다.
                foreach (Player p in _players.Values)
                {
                    if(p.Id != objectId)
                    p.Session.Send(despawnPacket);
                }
            }            
        }

        // 클라이언트에서 발송한 패킷으로 플레이어의 이동을 처리한다.
        public void HandleMove(Player player, C_Move movePacket)
        {
            if (player == null)
                return;

            //플레이어의 현재 위치와 이동하려는 위치를 생성한다.
            PositionInfo movePosInfo = movePacket.PosInfo;
            ObjectInfo info = player.Info;

            //다른 좌표로 이동할 때 갈 수 있는지 체크하는 부분
            if(movePosInfo.PosX != info.PosInfo.PosX || movePosInfo.PosY != info.PosInfo.PosY)
            {
                if (Map.CanGo(new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)) == false)
                    return;
            }

            //갈 수 있는 좌표라고 판단되면 플레이어의 상태와 보는 방향을 수정하고 이동한다.
            info.PosInfo.State = movePosInfo.State;
            info.PosInfo.MoveDir = movePosInfo.MoveDir;
            Map.ApplyMove(player, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY));


            //클라이언트에게 플레이어를 움직였다고 전송
            S_Move resMovePacket = new S_Move();
            resMovePacket.ObjectId = player.Info.ObjectId;
            resMovePacket.PosInfo = movePacket.PosInfo;
            Broadcast(resMovePacket);
        }

        // 클라이언트에서 발송한 패킷으로 플레이어의 스킬을 처리한다.
        public void HandleSkill(Player player, C_Skill skillPacket)
        {
            if (player == null)
                return;

            ObjectInfo info = player.Info;
            
            //플레이어의 상태가 Idle이 아니라면 스킬을 쓸 수 없게 처리한다.
            if (info.PosInfo.State != CreatureState.Idle)
                return;

            // 플레이어의 상태를 Skill로 바꾸고 패킷에 스킬 사용자ID와 스킬Id를 넣고 broadcast
            info.PosInfo.State = CreatureState.Skill;
            S_Skill skill = new S_Skill() { Info = new SkillInfo() };
            skill.ObjectId = info.ObjectId;
            skill.Info.SkillId = skillPacket.Info.SkillId;
            Broadcast(skill);

            // DataManager에서 스킬데이터를 받아온다.
            Data.Skill skillData = null;
            if (DataManager.SkillDict.TryGetValue(skillPacket.Info.SkillId, out skillData) == false)
                return;

            // 스킬 타입에 따라 처리
            switch(skillData.skillType)
            {
                // 근접 공격
                case SkillType.SkillAuto:
                    {
                        // GetFrontCellPos를 통해 스킬의 적용 범위를 가져오고 Map.Find를 통해 타겟을 지정
                        Vector2Int skillPos = player.GetFrontCellPos(info.PosInfo.MoveDir);
                        GameObject target = Map.Find(skillPos);
                        if (target != null)
                        {
                            Console.WriteLine("Hit GameObject!");
                        }
                    }
                    break;
                    // 화살 공격
                case SkillType.SkillProjectile:
                    {
                        // 화살 인스턴스를 생성
                        Arrow arrow = ObjectrManager.Instance.Add<Arrow>();
                        if (arrow == null)
                            return;

                        // 화살의 기본정보를 skillData와 player를 통해 초기화하고 월드에 추가
                        arrow.Owner = player;
                        arrow.Data = skillData;
                        arrow.PosInfo.State = CreatureState.Moving;
                        arrow.PosInfo.MoveDir = player.PosInfo.MoveDir;
                        arrow.PosInfo.PosX = player.PosInfo.PosX;
                        arrow.PosInfo.PosY = player.PosInfo.PosY;
                        arrow.Speed = skillData.projectile.speed;
                        Push(EnterGame, arrow);
                    }
                    break;
            }
            
        }

        // 인식 범위 내에 들어온 플레이어를 반환하기 위해 Invoke 람다 식을 사용하는 함수를 구현
        public Player FindPlayer(Func<GameObject, bool> condition)
        {
            foreach(Player player in _players.Values)
            {
                if (condition.Invoke(player))
                    return player;
            }
            return null;
        }

        // 모든 플레이어에게 패킷 전송
        public void Broadcast(IMessage packet)
        {

            foreach(Player p in _players.Values)
            {
                p.Session.Send(packet);
            }
            
        }
    }
}
