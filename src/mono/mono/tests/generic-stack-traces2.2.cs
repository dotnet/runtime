using System;
using System.Threading;

namespace GetStackTrace
{
    class Gen<T> {}

    class Program
    {
        static void Main (string[] args)
        {
            Thread t = new Thread (new ParameterizedThreadStart (Test<string>));
            t.Start (null);
            t.Join ();
        }

        static void Test<TT> (object test)
        {
	    Console.WriteLine (typeof (Gen<TT>).ToString ());
            Console.WriteLine (System.Environment.StackTrace);
        }
    }
}
