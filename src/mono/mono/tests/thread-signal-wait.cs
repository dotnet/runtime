
using System;
using System.Threading;

public class Test {
	static Object mon = new Object();
	static Object reply = new Object();
	static Object bogus = new Object();
	
	private void Thread_func() {
		Console.WriteLine("thread: In a thread!");

		Thread.Sleep(10000);

		Console.WriteLine("thread: Waiting for mon");

		Monitor.Enter(mon);
		Monitor.Pulse(mon);
		Console.WriteLine("thread: Pulsed mon");
		Monitor.Exit(mon);

		Thread.Sleep(10000);

		Monitor.Enter(reply);
		Console.WriteLine("thread: Got reply mutex");
		Monitor.Pulse(reply);
		Console.WriteLine("thread: Pulsed reply");
		Monitor.Exit(reply);
	}
	
	public static int Main () {
		Console.WriteLine ("main: Hello, World!");

		Test test = new Test();
		Thread thr=new Thread(new ThreadStart(test.Thread_func));
		thr.Start();

		Monitor.Enter(mon);
		Console.WriteLine("main: mon lock 1");

		Monitor.Enter(mon);
		Console.WriteLine("main: mon lock 2");

		Console.WriteLine("main: Waiting for mon");
		Monitor.Wait(mon);
		Console.WriteLine("main: mon waited");
		Monitor.Exit(mon);

		Monitor.Enter(reply);
		Console.WriteLine("main: reply locked");
		Monitor.Enter(reply);
		Console.WriteLine("main: reply locked");
		Monitor.Enter(reply);
		Console.WriteLine("main: reply locked");
		Monitor.Enter(reply);
		Console.WriteLine("main: reply locked");
		Monitor.Wait(reply);
		Console.WriteLine("main: reply waited");
		Monitor.Exit(reply);

		Console.WriteLine("Seeing how many locks we have...");
		Monitor.Exit(reply);
		Console.WriteLine("main: Exit reply");
		Monitor.Exit(reply);
		Console.WriteLine("main: Exit reply");
		Monitor.Exit(reply);
		Console.WriteLine("main: Exit reply");
		Monitor.Exit(reply);
		Console.WriteLine("main: Exit reply");
		Monitor.Exit(reply);
		Console.WriteLine("main: Exit reply");
		Monitor.Exit(reply);
		Console.WriteLine("main: Exit reply");
		Monitor.Exit(reply);
		Console.WriteLine("main: Exit reply");

		Monitor.Exit(bogus);
		Console.WriteLine("main: Exit bogus");
		
		return 0;
	}
}

