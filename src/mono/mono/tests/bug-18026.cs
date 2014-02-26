using System;
using System.Threading;

public class Test
{
	static bool done = false;

	static void Allocator (int n)
	{
		//Console.WriteLine (n);
		if (n < 1)
		{
			done = true;
			return;
		}

		for (int i = 0; i < 10000; ++i)
		{
			var o = new object [12];
			o = null;
		}
		ThreadPool.QueueUserWorkItem (_ => Allocator (n - 1));
	}

	static void LowLimits ()
	{
		ThreadPool.SetMinThreads (1, 1);
		ThreadPool.SetMaxThreads (1, 1);
	}

	static void HighLimits ()
	{
		ThreadPool.SetMaxThreads (1000, 1000);
		ThreadPool.SetMinThreads (100, 100);
	}

	public static void Main ()
	{
		var N = 10;
		var dones = new bool [N];
		var low = false;

		ThreadPool.QueueUserWorkItem (_ => Allocator (10000));
		while (!done)
		{
			//Console.WriteLine ("new");
			if (low)
				LowLimits ();
			else
				HighLimits ();
			low = !low;

			for (int i = 0; i < N; ++i)
			{
				var j = i;
				dones [j] = false;
				ThreadPool.QueueUserWorkItem (_ => {
						//Console.WriteLine ("done " + j);
						Thread.Sleep (1);
						dones [j] = true;
					});
			}

			bool all_done;
			do
			{
				all_done = true;
				for (int i = 0; i < N; ++i)
				{
					if (!dones [i])
					{
						all_done = false;
						Thread.Sleep (1);
						break;
					}
				}
			} while (!all_done);
		}
	}
}
