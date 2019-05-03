using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// The test returns 2*pointer size struct where the second pointer is a managed object
// and need a gc reference. On amd64 Unix and arm64 such structs are returned via registers.
// The was a problem in GCStress infrastracture where it did not mark this pointer alive.

namespace GitHub_23199
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Test1()
        {
            ProcessStartInfo pi = new ProcessStartInfo();
            // pi.Environment calls crossgened HashtableEnumerator::get_Entry returning struct that we need.
            Console.WriteLine(pi.Environment.Count);
        }

        struct A
        {
            public String a;
            public String b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static A GetA()
        {
            A a = new A();
            a.a = new String("Hello");
            a.b = new string("World!");
            return a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Test2()
        {
            A a = GetA();
            Console.WriteLine(a.a + " " + a.b);
        }

        static int Main(string[] args)
        {
            Test1();
            Test2();
            return 100;
        }
    }
}
