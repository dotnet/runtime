using System;
using System.Reflection;

public class Program
{
    static void Main()
    {
	    //Console.WriteLine ("start");
	    Test(""); // this one works.
	    //Console.WriteLine ("halfway");
	    Test(""); // this one throws.
	    //Console.WriteLine ("done");
    }

    static public void Test<T>(T a)
    {
	    Func<T> func = () => a;
	    //MethodInfo mi = func.Method;
	    //Console.WriteLine (mi.ToString ());
	    func.DynamicInvoke();
    }
}
