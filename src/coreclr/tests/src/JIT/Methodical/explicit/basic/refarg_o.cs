// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Test
{
    internal class AA
    {
        protected char pad1 = 'z';
        public int mm = 11;

        public AA(AA aa) { m_aa = aa; }

        public AA m_aa = null;

        ~AA()
        {
            if (pad1 != 'z' || mm != 11)
                throw new Exception();
            if (m_aa != null && (m_aa.pad1 != 'z' || m_aa.mm != 11))
                throw new Exception();
        }
    }

    internal class App
    {
        private static AA s_aa = new AA(new AA(null));

        private static void Litter()
        {
            GC.Collect();
            for (int i = 0; i < 1000; i++)
            {
                int[] p = new int[1000];
            }
            GC.Collect();
        }

        private static int Test(ref AA aa)
        {
            s_aa = null;
            Litter();
            if (aa.mm != 11)
            {
                Console.WriteLine("*** failed ***");
                return 1;
            }
            Console.WriteLine("*** passed ***");
            return 100;
        }

        private static int Main()
        {
            int exitCode = Test(ref s_aa.m_aa);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return exitCode;
        }
    }
}
