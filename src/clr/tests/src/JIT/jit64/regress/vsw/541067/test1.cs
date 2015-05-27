// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////
//
// Description 
// ____________
// Access violation in JIT when range check is statically 
// determined to fail at compile time
//
// Right Behavior
// ________________
// No Exception
//
// Wrong Behavior
// ________________
// Unhandled Exception
//
// Commands to issue
// __________________
// > test1.exe
//
// External files 
// _______________
// None
////////////////////////////////////////////////////////////////

using System;

namespace AutoGen
{
    public class Program
    {
        static public void Test()
        {
            int[] a = new int[1];
            a[0] = 0;

            int i;
            for (i = 0; i <= a[i]; i++)
            {
                for (i = 0; i <= a[i]; i++)
                {
                    for (i = 0; i <= a[i]; i++)
                    {
                        goto L1;
                    }
                }
            }

        L1:
            return;
        }


        public static int Main()
        {
            try
            {
                Test();
            }
            catch (System.Exception exp)
            {
                System.Console.WriteLine("Unexpected Exception!");
                System.Console.WriteLine(exp);
                return 1;
            }
            return 100;
        }
    }
}
