// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_refarg_i4_cs
{
    internal class AA
    {
        public AA self3 = null;
        public AA self4 = null;
        private int _pad1 = 191;
        public int mm = 11;
        public AA self1 = null;
        public AA self2 = null;

        public AA(int reclevel)
        {
            self1 = self2 = self4 = this;
            if (reclevel < 100)
                self3 = new AA(reclevel + 1);
        }

        protected void CheckFields()
        {
            if (this == null)
            {
                App.exitCode = 1;
                throw new Exception();
            }
            if (_pad1 != 191 || mm != 11)
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
            if (self3 != null) self3.CheckFields();
            if (self4 != null) self4.CheckFields();
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

        private static void Test(ref int n)
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
            Test(ref s_aa.mm);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return exitCode;
        }
    }
}
