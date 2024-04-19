// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
namespace DebuggerTests
{
    public class SetVariableLocals
    {
        public static void run()
        {
            sbyte a = 1;
            byte a1 = 1;
            short b = 2;
            ushort b2 = 3;
            int d = 5;
            uint d2 = 6;
            long e = 7;
            ulong e2 = 8;
            float f = 9;
            double g = 10;
            System.Console.WriteLine(g);
            System.Console.WriteLine(f);
            System.Console.WriteLine(e2);
        }
    }
}