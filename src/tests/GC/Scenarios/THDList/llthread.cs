// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ThdList {
    using System.Threading;
    using System;
    using System.IO;

    public class LLThread {

        internal int [] mA_Count = {10000, -5000, -15000, 3000, -6000, 0, 15000, 0, 10000,0,100,100}; //Action Array +ve add, -ve delete, 0 empty
        internal int m_id = 0;
        internal LinkedList m_LinkedList;
        internal Thread Mv_Thread;

        public LLThread(int ThreadId)
        {
            // console synchronization Console.SetOut(TextWriter.Synchronized(Console.Out));
            m_LinkedList = new LinkedList(ThreadId);
            m_id = ThreadId;
            Mv_Thread = new Thread( new ThreadStart (this.ThreadStart) );
            Mv_Thread.Start( );
            Console.Out.WriteLine("Started Thread: " + m_id);
        }

        public void ThreadStart()
        {
            for (int i = 0; i < mA_Count.Length; i++)
            {
                if (mA_Count[i] == 0)
                {
                    m_LinkedList.Empty(m_id);
                }
                else if (mA_Count[i] > 0 )
                {
                    m_LinkedList.AddNodes(mA_Count[i], m_id);
                }
                else
                {
                    m_LinkedList.DeleteNodes((mA_Count[i] * -1), m_id);
                }
            }
        }

    }
}
