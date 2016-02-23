// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

public static class GCTests
{
    private static bool s_is32Bits = IntPtrTests.TestSize == 4; // Skip IntPtr tests on 32-bit platforms

    [Fact]
    public static void TestAddMemoryPressure_Invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("bytesAllocated", () => GC.AddMemoryPressure(-1)); // Bytes allocated < 0

        if (s_is32Bits)
        {
            Assert.Throws<ArgumentOutOfRangeException>("bytesAllocated", () => GC.AddMemoryPressure((long)int.MaxValue + 1)); // Bytes allocated > int.MaxValue on 32 bit platforms
        }
    }

    [Fact]
    public static void TestCollect_Int()
    {
        for (int i = 0; i < GC.MaxGeneration + 10; i++)
        {
            GC.Collect(i);
        }
    }

    [Fact]
    public static void TestCollect_Int_Invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("generation", () => GC.Collect(-1)); // Generation < 0
    }

    [Theory]
    [InlineData(GCCollectionMode.Default)]
    [InlineData(GCCollectionMode.Forced)]
    public static void TestCollect_Int_GCCollectionMode(GCCollectionMode mode)
    {
        for (int gen = 0; gen <= 2; gen++)
        {
            var b = new byte[1024 * 1024 * 10];
            int oldCollectionCount = GC.CollectionCount(gen);
            b = null;

            GC.Collect(gen, GCCollectionMode.Default);

            Assert.True(GC.CollectionCount(gen) > oldCollectionCount);
        }
    }

    [Fact]
    public static void TestCollect_Int_GCCollectionMode_Invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("generation", () => GC.Collect(-1, GCCollectionMode.Default)); // Generation < 0

        Assert.Throws<ArgumentOutOfRangeException>(() => GC.Collect(2, GCCollectionMode.Default - 1)); // Invalid collection mode
        Assert.Throws<ArgumentOutOfRangeException>(() => GC.Collect(2, GCCollectionMode.Optimized + 1)); // Invalid collection mode
    }

    [Fact]
    public static void TestCollect_Int_GCCollectionMode_Bool_Invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("generation", () => GC.Collect(-1, GCCollectionMode.Default, false)); // Generation < 0

        Assert.Throws<ArgumentOutOfRangeException>(() => GC.Collect(2, GCCollectionMode.Default - 1, false)); // Invalid collection mode
        Assert.Throws<ArgumentOutOfRangeException>(() => GC.Collect(2, GCCollectionMode.Optimized + 1, false)); // Invalid collection mode
    }

    [Fact]
    public static void TestCollect_CallsFinalizer()
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
    public static void KeepAlive_Null()
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
    public static void TestSuppressFinalizer()
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
    public static void TestSuppressFinalizer_Invalid()
    {
        Assert.Throws<ArgumentNullException>("obj", () => GC.SuppressFinalize(null)); // Obj is null
    }

    [Fact]
    public static void TestReRegisterForFinalize()
    {
        ReRegisterForFinalizeTest.Run();
    }

    [Fact]
    public static void TestReRegisterFoFinalize()
    {
        Assert.Throws<ArgumentNullException>("obj", () => GC.ReRegisterForFinalize(null)); // Obj is null
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
    public static void TestCollectionCount_Invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("generation", () => GC.CollectionCount(-1)); // Generation < 0
    }

    [Fact]
    public static void TestRemoveMemoryPressure_Invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("bytesAllocated", () => GC.RemoveMemoryPressure(-1)); // Bytes allocated < 0

        if (s_is32Bits)
        {
            Assert.Throws<ArgumentOutOfRangeException>("bytesAllocated", () => GC.RemoveMemoryPressure((long)int.MaxValue + 1)); // Bytes allocated > int.MaxValue on 32 bit platforms
        }
    }

    [Fact]
    public static void TestGetTotalMemoryTest_ForceCollection()
    {
        // We don't test GetTotalMemory(false) at all because a collection
        // could still occur even if not due to the GetTotalMemory call,
        // and as such there's no way to validate the behavior.  We also
        // don't verify a tighter bound for the result of GetTotalMemory
        // because collections could cause significant fluctuations.

        GC.Collect();

        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);

        Assert.InRange(GC.GetTotalMemory(true), 1, long.MaxValue);

        Assert.InRange(GC.CollectionCount(0), gen0 + 1, int.MaxValue);
        Assert.InRange(GC.CollectionCount(1), gen1 + 1, int.MaxValue);
        Assert.InRange(GC.CollectionCount(2), gen2 + 1, int.MaxValue);

        Assert.InRange(GC.GetTotalMemory(false), 1, long.MaxValue);
    }

    [Fact]
    public static void TestGetGeneration()
    {
        // We don't test a tighter bound on GetGeneration as objects
        // can actually get demoted or stay in the same generation
        // across collections.

        GC.Collect();
        var obj = new object();

        for (int i = 0; i <= GC.MaxGeneration + 1; i++)
        {
            Assert.InRange(GC.GetGeneration(obj), 0, GC.MaxGeneration);
            GC.Collect();
        }
    }
}
