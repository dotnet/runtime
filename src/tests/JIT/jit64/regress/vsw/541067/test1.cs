// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using Xunit;

namespace AutoGen
{
    public class Program
    {
        internal static void Test()
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


        [Fact]
        public static int TestEntryPoint()
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
