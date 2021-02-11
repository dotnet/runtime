using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace GetStackTrace
{
    class Gen<T> {}

	class C<T>
	{
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public int foo () {
			return new StackTrace ().GetFrame (0).GetMethod ().DeclaringType.IsGenericTypeDefinition ? 1 : 0;
		}
	}

	class D : C<string>
	{
	}

    class Program
    {
        static int Main (string[] args)
        {
            Thread t = new Thread (new ParameterizedThreadStart (Test<string>));
            t.Start (null);
            t.Join ();

			if (test_0_nongeneric_subclass () != 0)
				return 1;
			return 0;
        }

		/* Test for gshared methods declared in a generic subclass of a nongeneric class */
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static int test_0_nongeneric_subclass () {
			return new D ().foo ();
		}

        static void Test<TT> (object test)
        {
	    Console.WriteLine (typeof (Gen<TT>).ToString ());
            Console.WriteLine (System.Environment.StackTrace);
        }
    }
}
