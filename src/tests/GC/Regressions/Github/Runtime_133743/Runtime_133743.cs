// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Runtime_133743
{
    private const int AllocationWaveSize = 128 * 1024 * 1024;
    private const int SmallObjectSize = 32 * 1024;
    private const int Rounds = 12;

    [Fact]
    public static void TestEntryPoint()
    {
        long initialBackgroundIndex = GC.GetGCMemoryInfo(GCKind.Background).Index;
        for (int round = 0; round < Rounds; round++)
        {
            byte[]?[] objects = AllocateWave();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.Collect(2, GCCollectionMode.Forced, blocking: false, compacting: false);
            GC.WaitForPendingFinalizers();

            for (int i = 0; i < objects.Length; i++)
            {
                if (i % 8 != round % 8)
                {
                    objects[i] = null;
                }
            }

            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.KeepAlive(objects);
        }

        long finalBackgroundIndex = GC.GetGCMemoryInfo(GCKind.Background).Index;
        Assert.True(finalBackgroundIndex > initialBackgroundIndex, "Background GC did not run.");
    }

    private static byte[]?[] AllocateWave()
    {
        var objects = new byte[]?[AllocationWaveSize / SmallObjectSize];
        for (int i = 0; i < objects.Length; i++)
        {
            byte[] allocation = new byte[SmallObjectSize];
            allocation[0] = (byte)i;
            allocation[^1] = (byte)(i >> 8);
            objects[i] = allocation;
        }

        return objects;
    }
}
