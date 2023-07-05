// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_refarg_c_cs
{
    internal class AA
    {
        protected char pad1 = 'z';
        private AA _self = null;
        public char mm = 'Q';

        public AA() { _self = this; }

        ~AA()
        {
            if (pad1 != 'z' || mm != 'Q')
            {
                App.exitCode = 1;
                throw new Exception();
            }
            if (_self != null && (pad1 != 'z' || mm != 'Q'))
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

        private static void Test(ref char n)
        {
            s_aa = null;
            Litter();
            if (n != 'Q')
                exitCode = 1;
            exitCode = 100;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Test(ref s_aa.mm);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return exitCode;
        }
    }
}
