using System; 
using System.Collections; 
using System.Threading;
using System.Collections.Generic;

public class Foo  {
	static Foo resurrect;
	static Foo reference;
	static List<Foo> list = new List<Foo> ();

	~Foo() {
		resurrect = this;
	}

	public static void EnterMonitor (object obj)
	{
		for (int i = 0; i < 257; i++)
			Monitor.Enter (obj);
	}

	public static void ExitMonitor (object obj)
	{
		for (int i = 0; i < 257; i++)
			Monitor.Exit (obj);
	}

	public static void CreateFoo (int level)
	{
		if (level == 0) {
			reference = new Foo ();

			/* Allocate a MonoThreadsSync for the object */
			EnterMonitor (reference);
			ExitMonitor (reference);
			reference = null;
		} else {
			CreateFoo (level - 1);
		}
	}

	public static int Main() {
		/* Allocate an object down the stack so it doesn't get pinned */
		typeof (Foo).GetMethod ("CreateFoo").Invoke (null, new object [] { 100 });

		Console.WriteLine (".");

		/* resurrect obj */
		GC.Collect ();
		GC.WaitForPendingFinalizers ();

		/* Allocate MonoThreadsSyncs for another thread that are locked */
		Thread t = new Thread (new ThreadStart (resurrect.Func));
		t.Start ();
		t.Join ();

		/* Make sure that none of the other structures overlap with the original one */
		Monitor.Enter (resurrect);
		return 0;
	}

	public void Func () {
		for (int i = 0; i < 100; i++) {
			Foo foo = new Foo ();
			/* Make sure these are not collected */
			list.Add (foo);

			EnterMonitor (foo);
		}
	}
} 

