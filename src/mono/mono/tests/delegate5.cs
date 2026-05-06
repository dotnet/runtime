using System;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;

public class T21
{
	public delegate void TestCallback (int i);
	
	public static void Test (int i)
	{
		Console.WriteLine (i);
	}
	
	public static void Main ()
	{
		TestCallback cb = new TestCallback (Test);
		IAsyncResult ar = cb.BeginInvoke (100, null, null);
		ar.AsyncWaitHandle.WaitOne ();
		cb.EndInvoke (ar);
	}
}


