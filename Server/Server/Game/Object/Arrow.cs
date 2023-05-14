using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;

namespace Server.Game
{
    public class Arrow : Projectile
    {
        public GameObject Owner { get; set; }

        long _nextMoveTick = 0;
        public override void Update()
        {
            if (Data == null || Data.projectile == null || Owner == null || Room == null)
                return;

            if (_nextMoveTick >= Environment.TickCount64)
                return;

            long tick = (long)(1000 / Data.projectile.speed);
            _nextMoveTick = Environment.TickCount64 + tick;

            Vector2Int destPos = GetFrontCellPos();

            if(Room.Map.CanGo(destPos))
            {
                CellPos = destPos;

                S_Move movepacket = new S_Move();
                movepacket.ObjectId = Id;
                movepacket.PosInfo = PosInfo;
                Room.Broadcast(movepacket);

                Console.WriteLine("Move Arrow");
            }
            else
            {
                GameObject target = Room.Map.Find(destPos);
                if(target != null)
                {

                }

                Room.LeaveGame(Id);
            }
        }
    }
}
