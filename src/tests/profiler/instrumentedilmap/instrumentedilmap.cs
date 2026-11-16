// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Profiler.Tests;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

public struct W<T>
{
    public T Value;
}

public struct X<T>
{
    public T Value;
}

public struct Y<T>
{
    public T Value;
}

public struct Z<T>
{
    public T Value;
}

public static class InstrumentedILMap
{
    private static readonly Guid ProfilerGuid = new("4F7C2B1E-8D3A-4E59-B6A1-2C9E5D7F3A10");

    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
        {
            return RunTest();
        }

        return ProfilerTestRunner.Run(
            Assembly.GetExecutingAssembly().Location,
            testName: "InstrumentedILMap",
            profilerClsid: ProfilerGuid,
            envVars: new Dictionary<string, string>
            {
                // glibc fills freed memory with 0xA5, so a read of a freed instrumented map
                // returns IL offsets the profiler detects. Other C runtimes ignore it.
                ["MALLOC_PERTURB_"] = "165"
            });
    }

    // The profiler replaces this method's instrumented IL map each time an instantiation is JIT-compiled.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Target<T>(T value, int x)
    {
        int sum = x;
        for (int i = 0; i < 4; i++)
        {
            sum += i * x;
        }

        return sum;
    }

    private static int RunTest()
    {
        MethodInfo target = typeof(InstrumentedILMap).GetMethod(nameof(Target));

        // Value-type instantiations are compiled separately but share Target's method token, and so share
        // one instrumented map in the runtime.
        Type[] bases = { typeof(int), typeof(long), typeof(short), typeof(byte), typeof(uint), typeof(ulong),
                         typeof(ushort), typeof(sbyte), typeof(double), typeof(float), typeof(char), typeof(bool) };
        Type[] wrappers = { typeof(W<>), typeof(X<>), typeof(Y<>), typeof(Z<>) };
        List<Type> types = new();
        foreach (Type b in bases)
        {
            types.Add(b);
            foreach (Type wrapper in wrappers)
            {
                Type t = b;
                for (int depth = 1; depth < 64; depth++)
                {
                    t = wrapper.MakeGenericType(t);
                    types.Add(t);
                }
            }
        }

        const int ThreadCount = 8;
        using ManualResetEventSlim go = new();
        Thread[] threads = new Thread[ThreadCount];
        int done = 0;
        for (int k = 0; k < ThreadCount; k++)
        {
            int first = k;
            threads[k] = new Thread(() =>
            {
                go.Wait();
                for (int i = first; i < types.Count; i += ThreadCount)
                {
                    Type t = types[i];
                    target.MakeGenericMethod(t).Invoke(null, new object[] { Activator.CreateInstance(t), 1 });
                    Interlocked.Increment(ref done);
                }
            });
            threads[k].Start();
        }

        go.Set();
        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        return done == types.Count ? 100 : 1;
    }
}
