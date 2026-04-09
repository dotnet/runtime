using System;
using System.Threading;

class Driver
{

	static readonly Mutex[] mutexes = new Mutex[2];

	public static void Main(string[] args)
	{
		for (int i = 0; i < mutexes.Length; i++) {
			mutexes [i] = new Mutex();
		}

		Thread thread1 = new Thread(() => {
			for (int i = 0; i < 1; i++) {
				int idx = -1;
				try {
					idx = WaitHandle.WaitAny (mutexes);
					Console.WriteLine($"Thread 1 iter: {i} with mutex: {idx}");
				} finally {
					if (idx != -1)
						mutexes [idx].ReleaseMutex();
				}
			}

			Console.WriteLine("Thread 1 ended");
		});

		thread1.Start();
		thread1.Join();

		Thread thread2 = new Thread(() => {
			for (int i = 0; i < 1000; i++) {
				int idx = -1;
				try {
					idx = WaitHandle.WaitAny (mutexes);
					Console.WriteLine($"Thread 2 iter: {i} with mutex: {idx}");
				} finally {
					if (idx != -1)
						mutexes [idx].ReleaseMutex();
				}
			}

			Console.WriteLine("Thread 2 ended");
		});

		thread2.Start();
		thread2.Join();
	}
}