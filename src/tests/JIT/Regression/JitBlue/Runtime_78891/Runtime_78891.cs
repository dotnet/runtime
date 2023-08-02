// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// This test is to ensure that an assertion does not occur in the JIT.
public class Runtime_78891
{
    public class C0
    {
        public long F1;
    }

    public struct S5
    {
        public bool F1;
        public int F2;
        public C0 F4;
        public short F5;
        public ulong F6;
        public uint F7;
    }

    public class Program
    {
        public static S5 s_48;
        public static int Start()
        {
            var vr2 = new S5();
            var vr3 = new S5();
            try
            {
                M59(vr2, vr3);
                return 0;
            }
            catch (NullReferenceException)
            {
                // We expect a null reference exception to occur, return 100.
                return 100;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Consume(short x)
        {

        }

        public static void M59(S5 arg0, S5 arg1)
        {
            try
            {
                arg0 = arg1;
                arg0 = arg1;
                short var3 = arg1.F5;
                Consume(var3);
            }
            finally
            {
                if (s_48.F4.F1 > arg0.F7)
                {
                    arg0.F1 |= false;
                }
            }
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return Program.Start();
    }
}
