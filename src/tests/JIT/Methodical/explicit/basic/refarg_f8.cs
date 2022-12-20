// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_refarg_f8_cs
{
    internal class AA
    {
        public double mm1 = 11.314d;
        public AA self1 = null;
        public AA self2 = null;
        public double mm2 = 12.0d;

        public AA(int reclevel)
        {
            if (reclevel < 100)
            {
                self1 = new AA(reclevel + 1);
                self2 = self1.self1;
            }
            else
            {
                self1 = this;
                self2 = null;
            }
        }

        protected void CheckFields()
        {
            if (mm2 != 12.0d || mm1 != 11.314d)
            {
                App.exitCode = 1;
                throw new Exception();
            }
        }

        ~AA()
        {
            CheckFields();
            if (self1 != null) self1.CheckFields();
            if (self2 != null) self2.CheckFields();
        }
    }

    public class App
    {
        private static AA s_aa = new AA(0);
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

        private static void Test(ref double n)
        {
            s_aa = null;
            Litter();
            if (n != 11.314d)
                exitCode = 1;
            exitCode = 100;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Test(ref s_aa.mm1);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return exitCode;
        }
    }
}
