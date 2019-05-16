// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public static partial class GCTests
    {
        [Theory]
        [InlineData(1000)]
        [InlineData(100000)]
        public static void GetAllocatedBytesForCurrentThread(int size)
        {
            long start = GC.GetAllocatedBytesForCurrentThread();

            GC.KeepAlive(new string('a', size));

            long end = GC.GetAllocatedBytesForCurrentThread();

            Assert.True((end - start) > size, $"Allocated too little: start: {start} end: {end} size: {size}");
            Assert.True((end - start) < 5 * size, $"Allocated too much: start: {start} end: {end} size: {size}");
        }

        [Fact]
        public static void GetGCMemoryInfo()
        {
            RemoteExecutor.Invoke(() =>
            {
                // Allows to update the value returned by GC.GetGCMemoryInfo
                GC.Collect();

                GCMemoryInfo memoryInfo1 = GC.GetGCMemoryInfo();

                Assert.InRange(memoryInfo1.HighMemoryLoadThresholdBytes, 1, long.MaxValue);
                Assert.InRange(memoryInfo1.MemoryLoadBytes, 1, long.MaxValue);
                Assert.InRange(memoryInfo1.TotalAvailableMemoryBytes, 1, long.MaxValue);
                Assert.InRange(memoryInfo1.HeapSizeBytes, 1, long.MaxValue);
                Assert.InRange(memoryInfo1.FragmentedBytes, 0, long.MaxValue);

                GCHandle[] gch = new GCHandle[64 * 1024];
                for (int i = 0; i < gch.Length * 2; ++i)
                {
                    byte[] arr = new byte[64];
                    if (i % 2 == 0)
                    {
                        gch[i / 2] = GCHandle.Alloc(arr, GCHandleType.Pinned);
                    }
                }

                // Allows to update the value returned by GC.GetGCMemoryInfo
                GC.Collect();

                GCMemoryInfo memoryInfo2 = GC.GetGCMemoryInfo();

                Assert.Equal(memoryInfo2.HighMemoryLoadThresholdBytes, memoryInfo1.HighMemoryLoadThresholdBytes);
                Assert.InRange(memoryInfo2.MemoryLoadBytes, memoryInfo1.MemoryLoadBytes, long.MaxValue);
                Assert.Equal(memoryInfo2.TotalAvailableMemoryBytes, memoryInfo1.TotalAvailableMemoryBytes);
                Assert.InRange(memoryInfo2.HeapSizeBytes, memoryInfo1.HeapSizeBytes + 1, long.MaxValue);
                Assert.InRange(memoryInfo2.FragmentedBytes, memoryInfo1.FragmentedBytes + 1, long.MaxValue);
            }).Dispose();
        }

        [Fact]
        public static void GetTotalAllocatedBytes()
        {
            byte[] stash;

            long CallGetTotalAllocatedBytesAndCheck(long previous, out long differenceBetweenPreciseAndImprecise)
            {
                long precise = GC.GetTotalAllocatedBytes(true);
                long imprecise = GC.GetTotalAllocatedBytes(false);

                if (precise <= 0)
                {
                    throw new Exception($"Bytes allocated is not positive, this is unlikely. precise = {precise}");
                }

                if (imprecise < precise)
                {
                    throw new Exception($"Imprecise total bytes allocated less than precise, imprecise is required to be a conservative estimate (that estimates high). imprecise = {imprecise}, precise = {precise}");
                }

                if (previous > precise)
                {
                    throw new Exception($"Expected more memory to be allocated. previous = {previous}, precise = {precise}, difference = {previous - precise}");
                }

                differenceBetweenPreciseAndImprecise = imprecise - precise;
                return precise;
            }

            long CallGetTotalAllocatedBytes(long previous)
            {
                long differenceBetweenPreciseAndImprecise;
                previous = CallGetTotalAllocatedBytesAndCheck(previous, out differenceBetweenPreciseAndImprecise);
                stash = new byte[differenceBetweenPreciseAndImprecise];
                previous = CallGetTotalAllocatedBytesAndCheck(previous, out differenceBetweenPreciseAndImprecise);
                return previous;
            }

            long previous = 0;

            for (int i = 0; i < 1000; ++i)
            {
                stash = new byte[1234];
                previous = CallGetTotalAllocatedBytes(previous);
            }
        }
    }
}
