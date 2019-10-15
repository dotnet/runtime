// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
