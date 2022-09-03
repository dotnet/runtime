// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace ConsoleApplication1
{
    class Program
    {
        public struct A
        {
        }

        public struct B
        {
            A a;
        }

        public struct C
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public B[] b;
        }

        static int Main(string[] args)
        {
            try
            {
                M();
                Console.WriteLine("PASS");
                return 100;
            }
            catch(TypeLoadException)
            {
                Console.WriteLine("Caught TypeLoadException, FAIL");
                return 99;
            }
            catch(Exception e)
            {
                Console.WriteLine("Caught unexpected exception");
                Console.WriteLine(e);
                Console.WriteLine("\nFAIL");
                return 99;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void M()
        {
            C obj = new C(); // exception occurs in the initializing this member
        }
    }
}
