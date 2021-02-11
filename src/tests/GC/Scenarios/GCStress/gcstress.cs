// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DefaultNamespace {
    using System;

    internal class GCStress
    {
        internal GCStress next;
        internal byte[] data;

        public static int Main(String [] args)
        {
            Console.WriteLine("Test should return with ExitCode 100 ...");
            GCStress obj= new GCStress();
            if (obj.RunTest())
            {
                Console.WriteLine("Test Passed");
                return 100;
            }

            Console.WriteLine("Test Failed");
            return 1;
        }


        public bool RunTest()
        {
            GCStress garbage;
            GCStress head = null;
            GCStress tail = null;
            GCStress walker;
            int stressCount = 0;
            int stressCount2 = 0;

            for (int i=0; i<1500000; i++)
            {
                byte[] x = new byte [(i %1111)];

                if ((i%100) == 0)
                {
                    garbage = new GCStress();
                    garbage.data = x;
                    stressCount += x.Length;
                    if (head == null)
                    {
                        head = garbage;
                    }
                    else
                    {
                        tail.next = garbage;
                    }

                    tail = garbage;
                }
            }


            walker = head;
            while (walker != null)
            {
                if (walker.data != null)
                {
                    stressCount2 += walker.data.Length;
                }
                walker = walker.next;
            }

            Console.WriteLine ("Stress count: {0} {1}", stressCount, stressCount2);
            return (stressCount == stressCount2);
        }
    }

}
