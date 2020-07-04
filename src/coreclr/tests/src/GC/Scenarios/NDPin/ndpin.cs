// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DefaultNamespace {
    using System;
    using System.Runtime.InteropServices;

    internal class NDPin
    {

        internal Object p;
        internal static NDPin m_n;
        internal static Object m_o;

        internal NDPin (Object p)
        {
            this.p = p;
        }

        public static int Main(String [] args)
        {
            Console.WriteLine("Test should return with ExitCode 100 ...");

            m_o = new int[10];
            GCHandle h = GCHandle.Alloc(m_o, GCHandleType.Pinned);

            for (int i = 0; i < 100000; i++)
            {

                m_o = new int[10];
                m_n = new NDPin (m_o);
                h.Free();
                h = GCHandle.Alloc(m_o, GCHandleType.Pinned);
            }

            GC.Collect();

            bool result = (m_o == m_n.p);
            h.Free();

            if (result)
            {
                Console.WriteLine ("Test Passed");
                return 100;
            }
            Console.WriteLine ("Test Failed");
            return 1;
        }

    }

}
