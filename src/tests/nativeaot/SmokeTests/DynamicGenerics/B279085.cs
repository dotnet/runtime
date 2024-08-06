// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using RuntimeLibrariesTest;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

using TypeOfRepo;

public class B279085
{
    public struct Pair<T, U>
    {
        internal T m_first;
        internal U m_second;

        public Pair(T first, U second)
        {
            m_first = first;
            m_second = second;
        }

        public T First
        {
            get { return m_first; }
            set { m_first = value; }
        }

        public U Second
        {
            get { return m_second; }
            set { m_second = value; }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [TestMethod]
    public static void TestB279085Repro()
    {
        var templateType = typeof(Pair<int, IEnumerable<short>>[]);

        int[] firsts = new int[100];
        List<int>[] seconds = new List<int>[100];
        for (int i = 0; i < 100; i++)
        {
            firsts[i] = i;
            seconds[i] = new List<int>();
            for (int j = 0; j < i; j++)
                seconds[i].Add(j);
        }

        MethodInfo testMethod = typeof(B279085).GetTypeInfo().GetDeclaredMethod("TestB279085Repro_Inner").MakeGenericMethod(TypeOf.Int32, typeof(IEnumerable<int>));
        Pair<int, IEnumerable<int>>[] pairs = (Pair<int, IEnumerable<int>>[])testMethod.Invoke(null, new object[] { firsts, seconds });

        firsts = null;
        seconds = null;
        for (int i = 0; i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        
        // The bug was a malformed GCDesc for an array of generic valuetypes.
        // The code that follows will crash when trying to access the Second property if the GCDesc was wrong (gc collects it).

        for (int i = 0; i < pairs.Length; i++)
        {
            int first = pairs[i].First;
            Assert.AreEqual(i, first);

            int count = 0;
            IEnumerable<int> second = pairs[i].Second;
            foreach (int item in second)
            {
                Assert.AreEqual(count, item);
                count++;
            }
            Assert.AreEqual(i, count);
        }
    }

    public static Pair<T, U>[] TestB279085Repro_Inner<T, U>(T[] firsts, U[] seconds)
    {
        Pair<T, U>[] arrayOfPairs = new Pair<T, U>[100];

        for (int i = 0; i < 100; i++)
        {
            arrayOfPairs[i] = new Pair<T, U>(firsts[i], seconds[i]);
        }

        return arrayOfPairs;
    }
}

