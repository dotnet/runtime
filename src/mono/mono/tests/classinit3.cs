using System;
using System.Collections.Generic;
using System.Threading;
namespace integer_test
{
	class MainClass
	{
		// #23242
		public static void Main (string[] args)
		{
			var _trigger = new ManualResetEvent (false);

			var testThreads = new Thread[100];
			for (Int16 i = 0; i < testThreads.Length; ++i)
			{
				testThreads [i] = new Thread ( () => 
					{
						_trigger.WaitOne();
						for (Int16 index = 0; index < 1000; ++index)
						{
							var val = index.ToString();
							GC.KeepAlive(val);
						}
					});
				testThreads [i].Start ();
			}
			Console.WriteLine ("setting event");
			_trigger.Set ();
			foreach (var thread in testThreads)
			{
				thread.Join ();
			}
		}
	}
}
