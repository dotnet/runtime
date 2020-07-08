// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace DefaultNamespace {
    using System.Threading;
    using System;
    using System.IO;

    public class TreeThread {

        internal int[] mA_Count;
        internal int m_id = 0;
        internal BinTree m_BinTree;
        internal Thread Mv_Thread;

        public TreeThread(int ThreadId, TreeType treeType, int[] count)
        {
            // attempt to synchronize the console output
            //Console.SetOut(TextWriter.Synchronized(Console.Out));

            mA_Count = count;
            m_BinTree = new BinTree(ThreadId, treeType);
            m_id = ThreadId;
            Mv_Thread = new Thread( new ThreadStart(this.ThreadStart));
            Mv_Thread.Start( );
            Console.Out.WriteLine("Started Thread: " + m_id);
        }

        public void ThreadStart()
        {                                           //All threads start here
            for (int i = 0; i < mA_Count.Length; i++)
            {
                if (mA_Count[i] == 0)
                {
                    m_BinTree.Empty(m_id);
                }
                else if (mA_Count[i] > 0 )
                {
                    m_BinTree.AddNodes(mA_Count[i], m_id);
                }
                else
                {
                    m_BinTree.DeleteNodes((mA_Count[i] * -1), m_id);
                }
            }
        }

    }
}
