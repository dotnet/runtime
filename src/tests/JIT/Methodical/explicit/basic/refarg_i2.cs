// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_refarg_i2_cs
{
    internal class AA
    {
        private short _pad1 = 191;
        public short mm = 11;
        public AA self = null;

        ~AA()
        {
            if (_pad1 != 191 ||
                mm != 11)
            {
                App.exitCode = 1;
                throw new Exception();
            }
            if (self != null && (_pad1 != 191 || mm != 11))
            {
                App.exitCode = 1;
                throw new Exception();
            }
        }
    }

    public class App
    {
        private static AA s_aa = new AA();
        public static int exitCode = 1;
        private static void Litter()
        {
            GC.Collect();
            for (int i = 0; i < 1000; i++)
            {
                int[] p = new int[1000];
            }
            GC.Collect();
        }

        private static void Test(ref short n)
        {
            s_aa = null;
            Litter();
            if (n != 11)
                exitCode = 1;
            exitCode = 100;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            s_aa.self = new AA();
            Test(ref s_aa.mm);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return exitCode;
        }
    }
}
