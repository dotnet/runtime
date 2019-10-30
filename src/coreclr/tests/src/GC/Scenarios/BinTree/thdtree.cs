// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/**
 * Description:
 *      Mainly stresses the GC by creating n threads each manipulating its own local binary tree structure.
 *      Each thread in turn adds and deletes thousands of nodes from the binary tree.
 */

namespace DefaultNamespace {
    using System.Threading;
    using System;
    using System.IO;

    public class ThdTree
    {

        public static int Main (System.String[] Args)
        {

            Console.Out.WriteLine("Test should return with ExitCode 100 ...");
            // sync console output Console.SetOut(TextWriter.Synchronized(Console.Out));

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

            TreeThread Mv_LLTree;

            int[] count = {10000, -5000, 3000, -6000, 0, 15000, 0, 10000,0,100,100};
            for (int i = 0; i < iNofThread; i++)
            {
                Mv_LLTree = new TreeThread(i, TreeType.Normal, count);
            }
            return 100;
        }

    }
}
