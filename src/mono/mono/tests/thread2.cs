
using System;
using System.Threading;

public class Test {
	static LocalDataStoreSlot slot;
	
	private void Thread_func() {
		//Throws undocumented exception :-(
		//LocalDataStoreSlot namedslot=Thread.AllocateNamedDataSlot("data-slot");
		LocalDataStoreSlot namedslot=Thread.GetNamedDataSlot("data-slot");
		
		Console.WriteLine("In a thread!");
		
		Thread thr=Thread.CurrentThread;
		Console.WriteLine("Found thread!");
		thr.Name="wobble";
		Thread otherthr=Thread.CurrentThread;
		Console.WriteLine("Other subthread is " + otherthr.Name);

		Thread.SetData(slot, thr);
		Thread storedthr=(Thread)Thread.GetData(slot);
		Console.WriteLine("Stored subthread is " + storedthr.Name);

		Thread.SetData(namedslot, thr);
		storedthr=(Thread)Thread.GetData(namedslot);
		Console.WriteLine("Stored subthread is " + storedthr.Name);
		
		Console.WriteLine("Locking thr for 1.5s");
		lock(thr) {
			Thread.Sleep(1500);
		}
		
		Console.WriteLine("Waiting for signal");
		lock(thr) {
			Monitor.Wait(thr);
			Console.WriteLine("Thread signalled!");
		}
		
		Console.WriteLine("Sleeping for 10s");
		Thread.Sleep(10000);

		Thread storedthr2=(Thread)Thread.GetData(slot);
		Console.WriteLine("Stored subthread is still " + storedthr2.Name);
	}
	
	public static int Main () {
		Console.WriteLine ("Hello, World!");
		slot=Thread.AllocateDataSlot();
		LocalDataStoreSlot namedslot=Thread.AllocateNamedDataSlot("data-slot");

		Test test = new Test();
		Thread thr=new Thread(new ThreadStart(test.Thread_func));
		thr.Start();
		Thread.Sleep(1000);
		Thread main=Thread.CurrentThread;
		main.Name="wibble";
		Thread othermain=Thread.CurrentThread;
		Console.WriteLine("Other name " + othermain.Name);
		Thread.Sleep(0);
		
		Console.WriteLine("In the main line!");

		Console.WriteLine("Trying to enter lock");
		if(Monitor.TryEnter(thr, 100)==true) {
			Console.WriteLine("Returned lock");
			Monitor.Exit(thr);
		} else {
			Console.WriteLine("Didn't get lock");
		}

		Thread.SetData(slot, main);
		Thread storedthr=(Thread)Thread.GetData(slot);
		Console.WriteLine("Stored subthread is " + storedthr.Name);

		Thread.SetData(namedslot, main);
		storedthr=(Thread)Thread.GetData(namedslot);
		Console.WriteLine("Stored subthread is " + storedthr.Name);
		
		if(thr.Join(5000)) {
			Console.WriteLine("Joined thread");
		} else {
			Console.WriteLine("Didn't join thread");
		}
		
		lock(thr) {
			Monitor.Pulse(thr);
			Console.WriteLine("Signalled thread");
		}
		
		thr.Join();
		
		return 0;
	}
}

