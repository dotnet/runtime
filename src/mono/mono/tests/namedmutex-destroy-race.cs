
/* test for https://bugzilla.xamarin.com/show_bug.cgi?id=41914 */

using System;
using System.Threading;

namespace Crasher
{
	class Program
	{
		public static void Main (string[] args)
		{
			Thread[] threads = new Thread[100];

			DateTime start = DateTime.Now;

			for (int i = 0; i < threads.Length; ++i) {
				threads [i] = new Thread (() => {
					var rnd = new Random();
					do {
						using (var mutex = new Mutex(false, "Global\\TEST")) {
							var owner = false;
							try {
								owner = mutex.WaitOne(TimeSpan.FromMinutes(1));
							} finally {
								if (owner)
									mutex.ReleaseMutex();
							}
						}
						Thread.Sleep(rnd.Next(100, 1000));
					} while ((DateTime.Now - start) < TimeSpan.FromSeconds (10));
				});
			}

			for (int i = 0; i < threads.Length; ++i)
				threads [i].Start ();

			for (int i = 0; i < threads.Length; ++i)
				threads [i].Join ();
		}

		private static void Crasher(){
		}
	}
}