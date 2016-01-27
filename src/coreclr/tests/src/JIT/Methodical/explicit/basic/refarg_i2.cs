// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Test
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

    internal class App
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

        private static int Main()
        {
            s_aa.self = new AA();
            Test(ref s_aa.mm);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return exitCode;
        }
    }
}
