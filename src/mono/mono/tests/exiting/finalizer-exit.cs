
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
		Environment.Exit(42);
	} 

	public static void Main() { 
		ArrayList list = new ArrayList (); 
		Thread.SetData(dataslot, "ID is wibble");
		while(true) { 
			foo instance = new foo(); 
			list.Add (new WeakReference(instance)); 
		} 
	} 
} 

