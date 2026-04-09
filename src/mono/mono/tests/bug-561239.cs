using System;
using System.IO;
using System.Threading;

public class PulseTest
{
	private bool   startedUp = false;
	private int    threadNum = 0;
	private object theLock   = new object ();

	public static void Main (string[] args)
	{
		int lastThreadNum = 0;

		for (int i = 0; i < 100; ++i) {
			/*
			 * Start a thread going.
			 */
			PulseTest pulseTest = new PulseTest ();
			pulseTest.threadNum = ++ lastThreadNum;
			Thread sysThread = new Thread (pulseTest.ThreadMain);

			/*
			 * Block thread from doing anything.
			 */
			Monitor.Enter (pulseTest.theLock);

			/*
			 * Now start it.
			 */
			sysThread.Start ();

			/*
			 * Wait for pulsetest thread to call Monitor.Wait().
			 */
			while (!pulseTest.startedUp) {
				pulseTest.Message ("Main", "waiting");
				Monitor.Wait (pulseTest.theLock);
				pulseTest.Message ("Main", "woken");
			}
			Monitor.Exit (pulseTest.theLock);

			/*
			 * Whilst it is sitting in Monitor.Wait, kill it off.
			 *
			 * Without the patch, the wait event sits in mon->wait_list,
			 * even as the mon struct gets recycled onto monitor_freelist.
			 *
			 * With the patch, the event is unlinked when the mon struct
			 * gets recycled.
			 */
			pulseTest.Message ("Main", "disposing");
			sysThread.Abort ();
			sysThread.Join ();
			pulseTest.Message ("Main", "disposed");
		}
	}

	private void ThreadMain ()
	{
		Monitor.Enter (theLock);
		startedUp = true;
		Monitor.Pulse (theLock);
		while (true) {
			Message ("ThreadMain", "waiting");

			/*
			 * This puts an event onto mon->wait_list.
			 * Then Main() does a sysThread.Abort() and
			 * the event is left on mon->wait_list.
			 */
			Monitor.Wait (theLock);
			Message ("ThreadMain", "woken");
		}
	}

	private void Message (string func, string msg)
	{
		Console.WriteLine ("{0}[{1}]*: {2}", func, threadNum, msg);
	}
}
