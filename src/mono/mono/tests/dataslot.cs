
using System;
using System.Threading;

public class Test : MarshalByRefObject {
	static LocalDataStoreSlot[] slot = new LocalDataStoreSlot[102400];
	
	private void Thread_func() {
		Console.WriteLine("In a thread!");

		for(int i=51200; i<102400; i++) {
			slot[i]=Thread.AllocateDataSlot();
			Thread.SetData(slot[i], i);
		}

		Thread.Sleep(5000);

		Thread.SetData(slot[11111], 42);
		Thread.SetData(slot[76801], 42);

		Thread.Sleep(20000);
		
		Console.WriteLine("Subthread done");
		Console.WriteLine("slot 11111 contains " + Thread.GetData(slot[11111]));
		Console.WriteLine("slot 26801 contains " + Thread.GetData(slot[26801]));
		Console.WriteLine("slot 76801 contains " + Thread.GetData(slot[76801]));
		Console.WriteLine("slot 96801 contains " + Thread.GetData(slot[96801]));
	}

	static LocalDataStoreSlot slot2;
	static string slot_value;
	
	public void Foo (AppDomain ad)
	{
		ad.DoCallBack (new CrossAppDomainDelegate (Bar));
	}
	
	public static void Bar ()
	{
		 slot_value = (string)Thread.GetData (slot2);
	}

	public static int Main () {
		Console.WriteLine ("Hello, World!");
		Test test=new Test();
		Thread thr=new Thread(new ThreadStart(test.Thread_func));
		thr.Start();

		for(int i=0; i<51200; i++) {
			slot[i]=Thread.AllocateDataSlot();
			Thread.SetData(slot[i], i);
		}
		Thread.SetData(slot[11111], 69);
		Thread.SetData(slot[26801], 69);

		Thread.Sleep(10000);
		
		Console.WriteLine("Main thread done");
		Console.WriteLine("slot 11111 contains " + Thread.GetData(slot[11111]));
		Console.WriteLine("slot 16801 contains " + Thread.GetData(slot[16801]));
		Console.WriteLine("slot 26801 contains " + Thread.GetData(slot[26801]));
		Console.WriteLine("slot 76801 contains " + Thread.GetData(slot[76801]));

		// Test appdomain transitions
		AppDomain ad = AppDomain.CreateDomain ("MyFriendlyDomain");
		Test o = (Test) ad.CreateInstanceFromAndUnwrap ("dataslot.exe", "Test");
		
		slot2 = Thread.AllocateDataSlot ();
		Thread.SetData (slot2, "hello");
		
		o.Foo (AppDomain.CurrentDomain);

		if (Test.slot_value != "hello")
			return 1;

		return 0;
	}
}

