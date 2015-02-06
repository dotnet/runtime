// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

public class GCTests
{
    [Fact]
    public static void ValidCollectionGenerations()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GC.Collect(-1));

        for (int i = 0; i < GC.MaxGeneration + 10; i++)
        {
            GC.Collect(i);
        }
    }

    [Fact]
    public static void CollectionCountDefault()
    {
        VerifyCollectionCount(GCCollectionMode.Default);
    }

    [Fact]
    public static void CollectionCountForced()
    {
        VerifyCollectionCount(GCCollectionMode.Forced);
    }

    private static void VerifyCollectionCount(GCCollectionMode mode)
    {
        for (int gen = 0; gen <= 2; gen++)
        {
            byte[] b = new byte[1024 * 1024 * 10];
            int oldCollectionCount = GC.CollectionCount(gen);
            b = null;

            GC.Collect(gen, GCCollectionMode.Default);

            Assert.True(GC.CollectionCount(gen) > oldCollectionCount);
        }
    }

    [Fact]
    public static void InvalidCollectionModes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GC.Collect(2, (GCCollectionMode)(GCCollectionMode.Default - 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => GC.Collect(2, (GCCollectionMode)(GCCollectionMode.Optimized + 1)));
    }

    [Fact]
    [ActiveIssue(659)]
    public static void TotalMemory()
    {
        GC.Collect();
        GC.Collect();

        int[] array1 = new int[20000];
        int memOld = (int)GC.GetTotalMemory(false);

        array1 = null;
        GC.Collect();

        int[] array2 = new int[40000];
        int memNew = (int)GC.GetTotalMemory(false);

        Assert.True(memNew >= memOld);
    }

    [Fact]
    public static void Finalizer()
    {
        FinalizerTest.Run();
    }

    private class FinalizerTest
    {
        public static void Run()
        {
            var obj = new TestObject();
            obj = null;
            GC.Collect();

            // Make sure Finalize() is called
            GC.WaitForPendingFinalizers();

            Assert.True(TestObject.Finalized);
        }

        private class TestObject
        {
            public static bool Finalized { get; private set; }

            ~TestObject()
            {
                Finalized = true;
            }
        }
    }

    [Fact]
    public static void KeepAlive()
    {
        KeepAliveTest.Run();
    }
    
    private class KeepAliveTest
    {
        public static void Run()
        {
            RunWorker();
            Assert.False(KeepAliveObject.Finalized);
            Assert.True(DoNotKeepAliveObject.Finalized);
        }

        private static void RunWorker()
        {
            var keepAlive = new KeepAliveObject();
            var doNotKeepAlive = new DoNotKeepAliveObject();

            doNotKeepAlive = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            GC.KeepAlive(keepAlive);
        }

        private class KeepAliveObject
        {
            public static bool Finalized { get; private set; }

            ~KeepAliveObject()
            {
                Finalized = true;
            }
        }

        private class DoNotKeepAliveObject
        {
            public static bool Finalized { get; private set; }

            ~DoNotKeepAliveObject()
            {
                Finalized = true;
            }
        }
    }

    [Fact]
    public static void KeepAliveNull()
    {
        KeepAliveNullTest.Run();
    }

    private class KeepAliveNullTest
    {
        public static void Run()
        {
            var obj = new TestObject();
            obj = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.KeepAlive(obj);

            Assert.True(TestObject.Finalized);
        }

        private class TestObject
        {
            public static bool Finalized { get; private set; }

            ~TestObject()
            {
                Finalized = true;
            }
        }
    }

    [Fact]
    public static void KeepAliveRecursive()
    {
        KeepAliveRecursiveTest.Run();
    }

    private class KeepAliveRecursiveTest
    {
        public static void Run()
        {
            var obj = new TestObject();
            int recursionCount = 0;

            RunWorker(obj, ref recursionCount);

            Assert.False(TestObject.Finalized);
        }

        private static void RunWorker(object obj, ref int recursionCount)
        {
            if (recursionCount++ == 10)
                return;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            RunWorker(obj, ref recursionCount);

            GC.KeepAlive(obj);
        }

        private class TestObject
        {
            public static bool Finalized { get; private set; }

            ~TestObject()
            {
                Finalized = true;
            }
        }
    }

    [Fact]
    public static void SuppressFinalizer()
    {
        SuppressFinalizerTest.Run();
    }

    private class SuppressFinalizerTest
    {
        public static void Run()
        {
            var obj = new TestObject();
            GC.SuppressFinalize(obj);

            obj = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.False(TestObject.Finalized);
        }

        private class TestObject
        {
            public static bool Finalized { get; private set; }

            ~TestObject()
            {
                Finalized = true;
            }
        }
    }
}