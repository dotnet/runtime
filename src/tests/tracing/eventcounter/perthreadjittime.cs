// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PerTHreadJitTime
{
    public class MyClass
    {
        public int MyMethod(int n)
        {
            int ret = 0;
            for (int i = 0; i < n; i++)
                ret += i;
            return ret;
        }
    }

    public class MyOtherClass
    {
        public int MyOtherMethod(int n)
        {
            int ret = 1;
            for (int i = 0; i < n; i++)
                ret *= i;
            return ret;
        }
    }

    public class Program
    {
        public static int Main(string[] args)
        {
            long threadOneJitTime = 0;
            long threadTwoJitTime = 0;

            MethodInfo getNanosecondsInJitForThread = typeof(RuntimeHelpers).GetMethod("GetNanosecondsInJitForThread", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (getNanosecondsInJitForThread is null)
            {
                foreach (var m in typeof(RuntimeHelpers).GetMembers(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                    Console.WriteLine($"\t{m}");
            }

            Thread[] threads = new Thread[2];
            threads[0] = new Thread(() => {
                var mc = new MyClass();
                int n = mc.MyMethod(100);
                threadOneJitTime = (long)getNanosecondsInJitForThread.Invoke(null, null);
            });
            threads[1] = new Thread(() => {
                var moc = new MyOtherClass();
                int n = moc.MyOtherMethod(10);
                threadTwoJitTime = (long)getNanosecondsInJitForThread.Invoke(null, null);
            });

            foreach (Thread t in threads)
                t.Start();

            foreach (Thread t in threads)
                t.Join();

            Console.WriteLine($"Thread One JIT Time: {threadOneJitTime}");
            Console.WriteLine($"Thread Two JIT Time: {threadTwoJitTime}");

            return (threadOneJitTime > 0 && threadTwoJitTime > 0) ? 100 : -1;
        }
    }
}