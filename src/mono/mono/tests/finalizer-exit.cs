
using System; 
using System.Collections; 
using System.Threading;

public class foo  { 
	public static LocalDataStoreSlot dataslot = Thread.AllocateDataSlot();

	~foo() { 
		string ID=(string)Thread.GetData(dataslot);
		if(ID==null) {
			Console.WriteLine("Set ID: foo");
			Thread.SetData(dataslot, "foo");
		}
		Console.WriteLine("finalizer thread ID: {0}", (string)Thread.GetData(dataslot));
		Environment.Exit(0);
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

