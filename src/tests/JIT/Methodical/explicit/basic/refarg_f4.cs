// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_refarg_f4_cs
{
    internal class AA
    {
        public float mm2 = 12.0f;
        public AA self1 = null;
        public float mm1 = 11.314f;
        public AA self2 = null;

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
            if (mm2 != 12.0f || mm1 != 11.314f)
                throw new Exception();
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

        private static void Litter()
        {
            GC.Collect();
            for (int i = 0; i < 1000; i++)
            {
                int[] p = new int[1000];
            }
            GC.Collect();
        }

        private static int Test(ref float n)
        {
            s_aa = null;
            Litter();
            if (n != 12.0f)
            {
                Console.WriteLine("*** failed ***");
                return 1;
            }
            Console.WriteLine("*** passed ***");
            return 100;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int exitCode = Test(ref s_aa.mm2);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return exitCode;
        }
    }
}
