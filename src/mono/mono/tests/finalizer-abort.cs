
using System; 
using System.Collections; 
using System.Threading;

public class foo  { 
	public static LocalDataStoreSlot dataslot = Thread.AllocateDataSlot();
	public static int final_count=0;

	~foo() { 
		// Demonstrate that this is still the same thread
		string ID=(string)Thread.GetData(dataslot);
		if(ID==null) {
			Console.WriteLine("Set ID: foo");
			Thread.SetData(dataslot, "foo");
		}

		// Don't run forever
		if(final_count++>10) {
			Environment.Exit(0);
		}

		Console.WriteLine("finalizer thread ID: {0}", (string)Thread.GetData(dataslot));
		try {
			Thread.CurrentThread.Abort();
		} catch(ThreadAbortException) {
			Console.WriteLine("Aborted!");
			// No ResetAbort()!
		}
	} 

	public static int Main() { 
		ArrayList list = new ArrayList (); 
		Thread.SetData(dataslot, "ID is wibble");
		Environment.ExitCode = 2;
		while(true) { 
			foo instance = new foo(); 
			list.Add (new WeakReference(instance)); 
			Thread.Sleep (0);
		}
		return 1;
	} 
} 

