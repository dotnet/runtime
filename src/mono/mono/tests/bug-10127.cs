using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WeakReferenceTest
{
	public static class Cache {
		static GCHandle[] table = new GCHandle[1024 * 1024];

		public static T Probe<T>(int hc) where T : class
		{
			int index = hc & (table.Length - 1);
			lock (table)
			{
				var wr = table[index];
				if (!wr.IsAllocated)
					return null;
				return wr.Target as T;
			}
		}

		public static T Add<T>(T obj, int hc) where T : class 
		{
			int index = hc & (table.Length - 1);
			lock (table)
			{
				table[index] = GCHandle.Alloc (obj, GCHandleType.Weak);
			}
			return obj;
		}

	}

	public class Tester {
		public static readonly int seed = unchecked(DateTime.Now.Ticks.GetHashCode());

		Random rand = new Random(seed);

		bool alive;
		Thread thread;

		public void Start()
		{
			alive = true;
			thread = new Thread(new ThreadStart(Work));
			thread.Start();
		}

		void Work()
		{
			do {

				var item = rand.Next ();
				var probed = Cache.Probe<object>(item.GetHashCode());

				if (probed == null) {
					Cache.Add<object>(item, item.GetHashCode());
				}

				if (rand.NextDouble() <= 0.1) {
					GC.Collect();
				}

			} while (alive);
		}

		public void Stop ()
		{
			alive = false;
		}

	}

	static class RandHelper {
		public static string RandString(this Random rand, int len)
		{
			char[] table = new char[len];
			for (int idx = 0; idx < len; idx++) {
				table[idx] = (char) ('a' + idx);
			}
			return new string(table, 0, len);
		}
	}

	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine("Starting cache testers");
			Console.WriteLine("Thread seed: " + Tester.seed);
			List<Tester> testers = new List<Tester>();
			for (int count = 0; count < 10; count++) {
				testers.Add(new Tester());
			}

			foreach (var tester in testers) {
				tester.Start();
			}

			for (int i = 0; i < 4; ++i)
			{
				Thread.Sleep(TimeSpan.FromSeconds(1));
			}

			foreach (var tester in testers)
			{
				tester.Stop ();
			}
		}
	}
}
