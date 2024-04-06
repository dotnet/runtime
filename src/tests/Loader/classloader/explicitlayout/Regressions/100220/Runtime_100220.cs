// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public static class Runtime_100220
{
    [Fact]
    public static int TestEntryPoint()
    {
        // Classes
        Assert.Equal(IntPtr.Size * 4, GetManagedSize(() => new MyClassAuto()));
        Assert.Equal(16 + IntPtr.Size * 2, GetManagedSize(() => new MyClass1000()));
        Assert.Equal(16 + IntPtr.Size * 2, GetManagedSize(() => new MyClass1000NoGc()));

        // Structs
        Assert.Equal(IntPtr.Size * 2, Unsafe.SizeOf<MyStructAuto>());
        Assert.Equal(16, Unsafe.SizeOf<MyStruct9>());
        Assert.Equal(1000, Unsafe.SizeOf<MyStruct1000>());

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int GetManagedSize(Func<object> allocator)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        allocator();
        long after = GC.GetAllocatedBytesForCurrentThread();
        return checked((int)(after - before));
    }

    private class MyClassAuto
    {
        private byte[] a;
        private byte b;
    }

    [StructLayout(LayoutKind.Explicit, Size = 1000)]
    private class MyClass1000
    {
        [FieldOffset(0)]
        private byte[] a;
        [FieldOffset(8)]
        private byte b;
    }

    [StructLayout(LayoutKind.Explicit, Size = 1000)]
    private class MyClass1000NoGc
    {
        [FieldOffset(0)]
        private nint a;
        [FieldOffset(8)]
        private byte b;
    }

    private struct MyStructAuto
    {
        private byte[] a;
        private byte b;
    }

    [StructLayout(LayoutKind.Explicit, Size = 1000)]
    private struct MyStruct1000
    {
        [FieldOffset(0)]
        private byte[] a;
        [FieldOffset(8)]
        private byte b;
    }

    [StructLayout(LayoutKind.Explicit, Size = 9)]
    private struct MyStruct9
    {
        [FieldOffset(0)]
        private byte[] a;
        [FieldOffset(8)]
        private byte b;
    }
}
