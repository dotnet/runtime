// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Test
{
    internal class AA
    {
        protected char pad1 = 'z';
        public String mm = "aha";

        public AA() { _self = this; }

        private AA _self = null;

        ~AA()
        {
            if (pad1 != 'z' || mm != "aha")
                throw new Exception();
            if (_self != null && (pad1 != 'z' || mm != "aha"))
                throw new Exception();
        }
    }

    internal class App
    {
        private static AA s_aa = new AA();

        private static void Litter()
        {
            GC.Collect();
            for (int i = 0; i < 1000; i++)
            {
                int[] p = new int[1000];
            }
            GC.Collect();
        }

        private static int Test(ref String n)
        {
            s_aa = null;
            Litter();
            if (n != "aha")
            {
                Console.WriteLine("*** failed ***");
                return 1;
            }
            Console.WriteLine("*** passed ***");
            return 100;
        }

        private static int Main()
        {
            int exitCode = Test(ref s_aa.mm);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return exitCode;
        }
    }
}
