using System;
using System.Threading;
public class DifferentialOperator
{
        static void Main (string[] args)
	{
		WeakReference weakref = null;
		Swap (ref weakref, new object ());
		if (weakref == null)
			throw new Exception ();
	}
	
	static void Swap(ref WeakReference refNmsp, object o)
	{
		WeakReference wref = refNmsp;
		if (wref != null)
		{
			Console.WriteLine ("Need this to make it pass");
		}
		Interlocked.CompareExchange<WeakReference>(ref refNmsp, new WeakReference(o), wref);
	}
}
