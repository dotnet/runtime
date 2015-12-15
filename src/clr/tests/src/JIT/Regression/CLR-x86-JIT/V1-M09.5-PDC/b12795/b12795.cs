// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DefaultNamespace
{
    using System;

    internal class NStructTun
    {
        public static int Main()
        {
            Mainy();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Console.Out.WriteLine(STRMAP.icFinal + " finalized.");
            return 100;
        }

        public static void Mainy()
        {
            try
            {
            }
            catch (Exception)
            {
            }

            STRMAP Strmap;
            Strmap = new STRMAP();
            Strmap = new STRMAP();
            Strmap = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Console.Out.WriteLine(STRMAP.icFinal + " finalized.");
        }
    }

    class STRMAP
    {
        internal static int icFinal = 0;
        ~STRMAP()
        {
            STRMAP.icFinal++;
        }
    }
}
