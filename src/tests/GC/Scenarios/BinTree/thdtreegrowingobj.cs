// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * Description:
 *      Mainly stresses the GC by creating n threads each manipulating its own local binary tree.
 *      Differs from thdtree in a way that the nodes of the binary trees grow during the lifetime.
 */



namespace DefaultNamespace {
    using System.Threading;
    using System;
    using System.IO;

    public class ThdTreeGrowingObj
    {

        public static int Main (System.String[] Args)
        {
            Console.Out.WriteLine("Test should return with ExitCode 100 ...");
            // console sync Console.SetOut(TextWriter.Synchronized(Console.Out));

            int iNofThread = 0;

            if (Args.Length == 1)
            {
                if (!Int32.TryParse( Args[0], out iNofThread ))
                {
                    iNofThread = 2;
                }
            }
            else
            {
                iNofThread = 2;
            }

            int[] count = {300, 1000, -350, 0, 71, 200};
            TreeThread Mv_TreeThread;
            for (int i = 0; i < iNofThread; i++)
            {
                Mv_TreeThread = new TreeThread(i, TreeType.Growing, count);              //Each treethread object launches a thread
            }
            return 100;
        }

    }
}
