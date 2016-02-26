// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/**
 * Description:
 *      Mainly stresses the GC by creating n threads each manipulating its own local Linked List.
 *      Each thread in turn adds and deletes thousands of nodes from the linked list.
 */


namespace ThdList {
    using System.Threading;
    using System;
    using System.IO;

    public class ThdList
    {

        public static int Main (System.String[] Args)
        {

            Console.Out.WriteLine("Test should return with ExitCode 100 ...");
            // console synchronization Console.SetOut(TextWriter.Synchronized(Console.Out));

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


            LLThread Mv_LLThread;

            //Creates m_iNofThreads LLThread objects
            //Each LLThread then launches a thread in its constructor
            for (int i = 0; i < iNofThread; i++)
            {
                Mv_LLThread = new LLThread(i);
            }
            return 100;
        }

    }
}
