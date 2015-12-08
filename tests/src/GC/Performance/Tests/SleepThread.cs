// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace EEGC{
	using System;
	using System.Threading;
	
	public class SleepThread{
		private static int m_sleepTime;
		public static bool shouldContinue; 
	
		public SleepThread(int time){
			m_sleepTime = time;
		}
		
		public static void ThreadStart(){
			run();
		}
		
		public static void run(){
			long tElapsed, t1, t2;
			int cIteration;
			cIteration = 0;
			
			while(Volatile.Read(ref shouldContinue))
			{
				cIteration++;
				
				t1 = Environment.TickCount;
				
				Thread.Sleep(m_sleepTime);
			
				t2 = Environment.TickCount;
				
				if(t2 - t1 > m_sleepTime * 1.4)
				{
					tElapsed = t2 - t1;
#if VERBOSE
					Console.WriteLine("Thread 2. Iteration " + cIteration + ". " + tElapsed + "ms elapsed");
#endif
				}
			}
		}
	}
}
			
