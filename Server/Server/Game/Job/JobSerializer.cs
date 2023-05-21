using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class JobSerializer
    {
		JobTimer _timer = new JobTimer();
		Queue<IJob> _jobQueue = new Queue<IJob>();
		object _lock = new object();
		bool _flush = false;

		public void PushAfter(int tickAfter , Action action) { PushAfter(tickAfter, new Job(action)); }
		public void PushAfter<T1>(int tickAfter, Action<T1> action, T1 t1) { PushAfter(tickAfter, new Job<T1>(action, t1)); }
		public void PushAfter<T1, T2>(int tickAfter, Action<T1, T2> action, T1 t1, T2 t2) { PushAfter(tickAfter, new Job<T1, T2>(action, t1, t2)); }
		public void PushAfter<T1, T2, T3>(int tickAfter, Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) { PushAfter(tickAfter, new Job<T1, T2, T3>(action, t1, t2, t3)); }

		public void PushAfter(int tickAfter, IJob job)
        {
			_timer.Push(job, tickAfter);
        }

		public void Push(Action action) { Push(new Job(action)); }
		public void Push<T1>(Action<T1> action, T1 t1) { Push(new Job<T1>(action, t1)); }
		public void Push<T1, T2>(Action<T1 , T2> action, T1 t1 , T2 t2) { Push(new Job<T1, T2>(action, t1 , t2)); }
		public void Push<T1, T2, T3>(Action<T1, T2, T3> action, T1 t1, T2 t2 , T3 t3) { Push(new Job<T1, T2, T3>(action, t1 , t2 , t3)); }

		public void Push(IJob job)
		{

			lock (_lock)
			{
				_jobQueue.Enqueue(job);
			}
		}

		public void Flush()
		{
			// 타이머를 플러시 합니다.
			_timer.Flush();

			while (true)
			{
				// pop을 통해 작업을 가져옵니다.
				IJob job = Pop();   
				if (job == null)
					return;

				// 작업을 실행합니다.
				job.Execute();       
			}
		}

		IJob Pop()
		{
			//동기화를 위해 lock을 겁니다.
			lock (_lock)
			{
				if (_jobQueue.Count == 0)
				{
					// 작업 큐가 비어있다면 null을 반환합니다.
					_flush = false;
					return null;
				}
				// 작업 큐에서 작업을 가져옵니다.
				return _jobQueue.Dequeue();     
			}
		}
	}
}
