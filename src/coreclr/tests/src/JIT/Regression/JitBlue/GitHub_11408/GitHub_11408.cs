// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

class GitHub_11408
{
    const int Pass = 100;
    const int Fail = -1;

    unsafe class Program
    {
        static int save = 7;

        static void foo(IntPtr x)
        {
            save = *(int*)x;
            Console.WriteLine(*(int*)x);
        }

        static void bar()
        {
            int x = 42;
            foo((IntPtr)(&x));
        }

        public static int Main(string[] args)
        {
            bar();

            if (save == 42)
            {
                Console.WriteLine("Pass");
                return Pass;
            }
            else
            {
                Console.WriteLine("Fail");
                return Fail;
            }
        }
    }
}
