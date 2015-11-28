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
            var keepAlive = new KeepAliveObject();

            var doNotKeepAlive = new DoNotKeepAliveObject();
            doNotKeepAlive = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.True(DoNotKeepAliveObject.Finalized);
            Assert.False(KeepAliveObject.Finalized);

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
            int recursionCount = 0;
            RunWorker(new TestObject(), ref recursionCount);
        }

        private static void RunWorker(object obj, ref int recursionCount)
        {
            if (recursionCount++ == 10)
                return;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            RunWorker(obj, ref recursionCount);

            Assert.False(TestObject.Finalized);
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

    [Fact]
    public static void ReRegisterForFinalize()
    {
        ReRegisterForFinalizeTest.Run();
    }

    private class ReRegisterForFinalizeTest
    {
        public static void Run()
        {
            TestObject.Finalized = false;
            CreateObject();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.True(TestObject.Finalized);
        }

        private static void CreateObject()
        {
            using (var obj = new TestObject())
            {
                GC.SuppressFinalize(obj);
            }
        }

        private class TestObject : IDisposable
        {
            public static bool Finalized { get; set; }

            ~TestObject()
            {
                Finalized = true;
            }

            public void Dispose()
            {
                GC.ReRegisterForFinalize(this);
            }
        }
    }

    [Fact]
    public static void GetTotalMemoryTest_ForceCollection()
    {
        GC.Collect();

        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);

        Assert.InRange(GC.GetTotalMemory(true), 1, long.MaxValue);

        Assert.InRange(GC.CollectionCount(0), gen0 + 1, int.MaxValue);
        Assert.InRange(GC.CollectionCount(1), gen1 + 1, int.MaxValue);
        Assert.InRange(GC.CollectionCount(2), gen2 + 1, int.MaxValue);

        // We don't test GetTotalMemory(false) at all because a collection
        // could still occur even if not due to the GetTotalMemory call,
        // and as such there's no way to validate the behavior.  We also
        // don't verify a tighter bound for the result of GetTotalMemory
        // because collections could cause significant fluctuations.
    }

    [Fact]
    public static void GetGenerationTest()
    {
        GC.Collect();
        object obj = new object();

        for (int i = 0; i <= GC.MaxGeneration + 1; i++)
        {
            Assert.InRange(GC.GetGeneration(obj), 0, GC.MaxGeneration);
            GC.Collect();
        }

        // We don't test a tighter bound on GetGeneration as objects
        // can actually get demoted or stay in the same generation
        // across collections.
    }
}
