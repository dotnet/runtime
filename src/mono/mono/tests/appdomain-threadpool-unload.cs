
using System;
using System.Linq;
using System.Threading;

class Driver
{
	class ThreadPoolLauncherObject
	{
		public volatile int i = 0;

		public ThreadPoolLauncherObject ()
		{
			ThreadPool.QueueUserWorkItem (_ => { for (int i = 0; i < 10 * 1000 * 1000; ++i); }, null);
		}
	}

	public static void Main ()
	{
		int count = 0;
		object o = new object ();

		foreach (var i in
			Enumerable.Range (0, 100)
				.AsParallel ().WithDegreeOfParallelism (Environment.ProcessorCount)
				.Select (i => {
					AppDomain ad;

					ad = AppDomain.CreateDomain ("testdomain" + i);
					ad.CreateInstance (typeof (ThreadPoolLauncherObject).Assembly.FullName, typeof (ThreadPoolLauncherObject).FullName);

					Thread.Sleep (10);

					AppDomain.Unload (ad);

					return i;
				})
				.Select (i => {
					lock (o) {
						count += 1;

						Console.Write (".");
						if (count % 25 == 0)
							Console.WriteLine ();
					}

					return i;
				})
		) {
		}
	}
}
