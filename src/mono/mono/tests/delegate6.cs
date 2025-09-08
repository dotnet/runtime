using System;
using System.Reflection;

public class T
{
	public void Test ()
	{
		Console.WriteLine ("CreateDelegate success!");
	}

	public delegate void blah ();

	public static void Main ()
	{
		T t = new T();
		Delegate o = Delegate.CreateDelegate (typeof (T.blah), t, "Test");
		o.DynamicInvoke (new Object[] {});
	}
}


