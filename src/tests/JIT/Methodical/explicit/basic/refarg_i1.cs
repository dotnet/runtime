// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Test
{
    internal class AA
    {
        private AA _self = null;
        private byte _pad1 = 191;
        public byte mm = 11;

        public AA(int reclevel) { if (reclevel < 100) _self = new AA(reclevel + 1); }

        ~AA()
        {
            try
            {
                if (_pad1 != 191 ||
                    mm != 11 ||
                    _self._pad1 != 191 ||
                    _self.mm != 11)
                {
                    App.exitCode = 1;
                    throw new Exception();
                }
            }
            catch (NullReferenceException)
            {
                App.exitCode = 100;
                Console.WriteLine("NullReferenceException caught in Finalizer as expected");
            }
        }
    }

    internal class App
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

        private static void Test(ref byte n)
        {
            s_aa = null;
            Litter();
            if (n != 11)
                exitCode = 1;
            exitCode = 100;
        }

        private static int Main()
        {
            Test(ref s_aa.mm);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return exitCode;
        }
    }
}
