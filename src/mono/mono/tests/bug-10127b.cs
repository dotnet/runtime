// This is a reduced form of bug-10127.cs that stresses multi-threaded GC.Collect.
// It hangs often on Windows.

using System;
using System.Threading;
using System.Collections.Generic;

namespace WeakReferenceTest
{
	public class Tester {
		volatile bool alive;
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
				GC.Collect();
			} while (alive);
		}

		public void Stop ()
		{
			alive = false;
		}

	}

	class MainClass
	{
		public static void Main (string[] args)
		{
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
