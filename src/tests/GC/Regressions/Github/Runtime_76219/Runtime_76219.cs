// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;
using TestLibrary;

public class Runtime_76219
{
    [MethodImpl(MethodImplOptions.Synchronized)]
    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 100; i++)
        {
            string alloc = i.ToString();

            Test();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    [Theory]
    [InlineData(16)]
    [InlineData(256)]
    public static void CollectEmptySegments(int objectCount)
    {
        for (int i = 0; i < 8; i++)
        {
            AllocateAndCollect(objectCount);
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AllocateAndCollect(int objectCount)
    {
        var objects = new byte[objectCount][];
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i] = new byte[32 * 1024];
            objects[i][0] = (byte)i;
        }

        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        for (int i = 0; i < objects.Length; i++)
        {
            Assert.Equal((byte)i, objects[i][0]);
        }
        GC.KeepAlive(objects);
    }

    [MethodImpl(MethodImplOptions.Synchronized | MethodImplOptions.NoInlining)]
    static void Test()
    {
        CallConsume("hello");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CallConsume(string str)
    {
        lock (str)
        {
            Consume(str);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(string str)
    {
    }
}
