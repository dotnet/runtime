// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections;
using System.Threading.Tasks;
using Xunit;

public class Queue_Synchronized
{
    public Queue m_Queue;
    public Int32 iCountTestcases = 0;
    public Int32 iCountErrors = 0;
    public Int32 m_ThreadsToUse = 8;
    public Int32 m_ThreadAge = 5; //5000;

    public Int32 m_ThreadCount;

    public void StartEnThread()
    {
        Int32 t_age = m_ThreadAge;
        while (t_age > 0)
        {      // closer to dead
            try
            {
                m_Queue.Enqueue(t_age);
                iCountTestcases++;
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_EnThread : " + e.GetType().FullName);
            }
            Interlocked.Decrement(ref t_age);
        }
        Interlocked.Decrement(ref m_ThreadCount);
    }


    public void StartDeThread()
    {
        Int32 t_age = m_ThreadAge;
        while (t_age > 0)
        {      // closer to dead
            try
            {
                m_Queue.Dequeue();
                iCountTestcases++;
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_DeThread : " + e.GetType().FullName);
            }
            Interlocked.Decrement(ref t_age);
        }
        Interlocked.Decrement(ref m_ThreadCount);
    }

    public void StartDeEnThread()
    {
        Int32 t_age = m_ThreadAge;
        while (t_age > 0)
        {      // closer to dead
            try
            {
                m_Queue.Dequeue();
                m_Queue.Enqueue("DeEn");
                iCountTestcases++;
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_DeEnThread : " + e.GetType().FullName);
            }
            Interlocked.Decrement(ref t_age);
        }
        Interlocked.Decrement(ref m_ThreadCount);
    }

    public void StartEnDeThread()
    {
        Int32 t_age = m_ThreadAge;
        while (t_age > 0)
        {      // closer to dead
            try
            {
                m_Queue.Dequeue();
                m_Queue.Enqueue("EnDe");
                iCountTestcases++;
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_EnDeThread : " + e.GetType().FullName);
            }
            Interlocked.Decrement(ref t_age);
        }
        Interlocked.Decrement(ref m_ThreadCount);
    }

    public Boolean runTest()
    {
        Queue q1;
        Queue q2;
        Queue q3;
        Boolean fPass;
        String[] strArr;
        Object[] oArr;
        IEnumerator ienm1;
        Object oValue;
        Int32 iCount;

        /////////////////////////////////////////////////////////////////////
        // Begin Testing run
        /////////////////////////////////////////////////////////////////////

        // Setup Queue Test
        //[]Testing Method: Queue.Synchronized( Queue )
        try
        {
            // Exception Test Cases
            String[] expectedExceptions = {
                "System.ArgumentNullException",
            };
            Queue[] errorValues = {
                null,
            };
            for (int i = 0; i < expectedExceptions.Length; i++)
            {
                iCountTestcases++;
                try
                {
                    Queue result = Queue.Synchronized(errorValues[i]);
                    iCountErrors++;
                }
                catch (Exception e)
                {
                    if (!e.GetType().FullName.Equals(expectedExceptions[i]))
                    {
                        iCountErrors++;
                        Console.WriteLine(" Wrong Exception Thrown " + e.GetType().FullName);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Uncaught Exception in Queue Queue.Synchronized( Queue )");
            Console.WriteLine("Exception->" + e.GetType().FullName);
        }
        ////////////////////////////////////////////////////////////////////////////////////////
        // Thread testing...
        // Enqueue
        m_Queue = new Queue();
        m_Queue = Queue.Synchronized(m_Queue);

        Task[] ths = new Task[m_ThreadsToUse];

        try
        {
            for (int i = 0; i < m_ThreadsToUse; i++)
            {
                ths[i] = Task.Factory.StartNew(new Action(this.StartEnThread), TaskCreationOptions.LongRunning);
            }
            m_ThreadCount = m_ThreadsToUse;
            Task.WaitAll(ths);

            Int32 expected = m_ThreadsToUse * m_ThreadAge;
            Int32 result = m_Queue.Count;

            if (!expected.Equals(result))
            {
                iCountErrors++;
                Console.WriteLine("Err_ENQUEUE Race.  Expected = " + expected + " Result = " + result);
            }
        }
        catch (Exception e)
        {
            iCountErrors++;
            Console.WriteLine("Err_THREAD threw " + e.GetType().FullName);
        }
        /////////////////// DeQueue
        // Less Threads.
        m_ThreadsToUse = (m_ThreadsToUse - 2);     // now we can see differences better (2000 entry overhead)

        try
        {
            m_ThreadCount = m_ThreadsToUse;
            for (int i = 0; i < m_ThreadsToUse; i++)
            {
                ths[i] = Task.Factory.StartNew(new Action(this.StartDeThread), TaskCreationOptions.LongRunning);
            }
            Task.WaitAll(ths);

            Int32 expected = 2 * m_ThreadAge;
            Int32 result = m_Queue.Count;

            if (!expected.Equals(result))
            {
                iCountErrors++;
                Console.WriteLine("Err_DEQUEUE Race.  Expected = " + expected + " Result = " + result);
            }
        }
        catch (Exception e)
        {
            iCountErrors++;
            Console.WriteLine("Err_THREAD threw " + e.GetType().FullName);
        }

        /////////////////// Dequeue + Enqueue
        // Less Threads.
        m_ThreadsToUse = (m_ThreadsToUse + 2);     // now we can see differences better (2000 entry overhead)

        try
        {
            m_ThreadCount = m_ThreadsToUse;
            for (int i = 0; i < m_ThreadsToUse; i++)
            {
                ths[i] = Task.Factory.StartNew(new Action(this.StartDeEnThread), TaskCreationOptions.LongRunning);
            }
            Task.WaitAll(ths);

            Int32 expected = 2 * m_ThreadAge;
            Int32 result = m_Queue.Count;

            if (!expected.Equals(result))
            {
                iCountErrors++;
                Console.WriteLine("Err_DEQUEUE Race.  Expected = " + expected + " Result = " + result);
            }
        }
        catch (Exception e)
        {
            iCountErrors++;
            Console.WriteLine("Err_THREAD threw " + e.GetType().FullName);
        }

        /////////////////// Enqueue + Dequeue
        // Less Threads.

        try
        {
            m_ThreadCount = m_ThreadsToUse;
            for (int i = 0; i < m_ThreadsToUse; i++)
            {
                ths[i] = Task.Factory.StartNew(new Action(this.StartEnDeThread), TaskCreationOptions.LongRunning);
            }
            Task.WaitAll(ths);

            Int32 expected = 2 * m_ThreadAge;
            Int32 result = m_Queue.Count;

            if (!expected.Equals(result))
            {
                iCountErrors++;
                Console.WriteLine("Err_DEQUEUE Race.  Expected = " + expected + " Result = " + result);
            }
        }
        catch (Exception e)
        {
            iCountErrors++;
            Console.WriteLine("Err_THREAD threw " + e.GetType().FullName);
        }


        //[] Synchronized returns a wrapper. We will test all the methods here!!!

        iCountTestcases++;

        fPass = true;
        q1 = new Queue();
        for (int i = 0; i < 10; i++)
            q1.Enqueue("String_" + i);
        q2 = Queue.Synchronized(q1);
        if (q2.Count != q1.Count)
            fPass = false;
        q2.Clear();
        if (q2.Count != 0)
            fPass = false;
        for (int i = 0; i < 10; i++)
            q2.Enqueue("String_" + i);

        for (int i = 0; i < 10; i++)
        {
            if (!((String)q2.Peek()).Equals("String_" + i))
            {
                Console.WriteLine(q2.Peek());
                fPass = false;
            }
            q2.Dequeue();
        }

        if (q2.Count != 0)
            fPass = false;

        if (!q2.IsSynchronized)
            fPass = false;

        for (int i = 0; i < 10; i++)
            q2.Enqueue("String_" + i);
        q3 = Queue.Synchronized(q2);

        if (!q3.IsSynchronized || (q2.Count != q3.Count))
            fPass = false;

        strArr = new String[10];
        q2.CopyTo(strArr, 0);
        for (int i = 0; i < 10; i++)
        {
            if (!strArr[i].Equals("String_" + i))
                fPass = false;
        }

        strArr = new String[10 + 10];
        q2.CopyTo(strArr, 10);
        for (int i = 0; i < 10; i++)
        {
            if (!strArr[i + 10].Equals("String_" + i))
                fPass = false;
        }

        try
        {
            q2.CopyTo(null, 0);
            fPass = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception)
        {
            fPass = false;
        }

        oArr = q2.ToArray();
        for (int i = 0; i < 10; i++)
        {
            if (!((String)oArr[i]).Equals("String_" + i))
            {
                fPass = false;
            }
        }

        ienm1 = q2.GetEnumerator();
        try
        {
            oValue = ienm1.Current;
            fPass = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception)
        {
            fPass = false;
        }

        iCount = 0;
        while (ienm1.MoveNext())
        {
            if (!((String)ienm1.Current).Equals("String_" + iCount))
            {
                fPass = false;
            }
            iCount++;
        }

        ienm1.Reset();
        iCount = 0;
        while (ienm1.MoveNext())
        {
            if (!((String)ienm1.Current).Equals("String_" + iCount))
            {
                fPass = false;
            }
            iCount++;
        }


        ienm1.Reset();
        q2.Dequeue();

        try
        {
            oValue = ienm1.Current;
            fPass = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception)
        {
            fPass = false;
        }

        try
        {
            ienm1.MoveNext();
            fPass = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception)
        {
            fPass = false;
        }

        try
        {
            ienm1.Reset();
            fPass = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception)
        {
            fPass = false;
        }

        if (!fPass)
        {
            iCountErrors++;
            Console.WriteLine("Unexpect result returned!");
        }

        /////////////////////////////////////////////////////////////////////
        // Diagnostics and reporting of results
        /////////////////////////////////////////////////////////////////////
        return iCountErrors == 0;
    }

    [Fact]
    public static void ExecuteQueue_Synchronized()
    {
        bool bResult = false;
        var test = new Queue_Synchronized();

        try
        {
            bResult = test.runTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine("Fail! Error Err_main! Uncaught Exception in main(), exc_main==" + exc_main);
        }

        Assert.Equal(true, bResult);
    }


}