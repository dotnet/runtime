// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public static class Runtime_100220
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/100220", TestRuntimes.Mono)]
    public static int TestEntryPoint()
    {
        if (IntPtr.Size == 8)
        {
            // Classes
            Assert.Equal(32, GetManagedSize(() => new MyClassAuto()));
            Assert.Equal(32, GetManagedSize(() => new MyClass1000Exp()));
            Assert.Equal(32, GetManagedSize(() => new MyClass1000Seq()));
            Assert.Equal(32, GetManagedSize(() => new MyClass1000NoGcExp()));
            Assert.Equal(1016, GetManagedSize(() => new MyClass1000NoGcSeq()));
            Assert.Equal(40, GetManagedSize(() => new BaseClassSeq()));
            Assert.Equal(56, GetManagedSize(() => new SubclassSeq()));
            Assert.Equal(64, GetManagedSize(() => new SubclassSubclassSeq()));
            Assert.Equal(48, GetManagedSize(() => new SubclassWithGcSeq()));
            Assert.Equal(32, GetManagedSize(() => new SubclassOfBaseWithGcSeq()));

            // Structs
            Assert.Equal(16, Unsafe.SizeOf<MyStructAuto>());
            Assert.Equal(16, Unsafe.SizeOf<MyStruct9Seq>());
            Assert.Equal(16, Unsafe.SizeOf<MyStruct9Exp>());
            Assert.Equal(16, Unsafe.SizeOf<MyStruct1000Seq>());
            Assert.Equal(1000, Unsafe.SizeOf<MyStruct1000Exp>());
        }
        else
        {
            // Classes
            Assert.Equal(16, GetManagedSize(() => new MyClassAuto()));
            Assert.Equal(20, GetManagedSize(() => new MyClass1000Exp()));
            Assert.Equal(16, GetManagedSize(() => new MyClass1000Seq()));
            Assert.Equal(20, GetManagedSize(() => new MyClass1000NoGcExp()));
            Assert.Equal(1008, GetManagedSize(() => new MyClass1000NoGcSeq()));
            Assert.Equal(28, GetManagedSize(() => new BaseClassSeq()));
            Assert.Equal(44, GetManagedSize(() => new SubclassSeq()));
            Assert.Equal(52, GetManagedSize(() => new SubclassSubclassSeq()));
            Assert.Equal(36, GetManagedSize(() => new SubclassWithGcSeq()));
            Assert.Equal(16, GetManagedSize(() => new SubclassOfBaseWithGcSeq()));

            // Structs
            Assert.Equal(8, Unsafe.SizeOf<MyStructAuto>());
            Assert.Equal(8, Unsafe.SizeOf<MyStruct9Seq>());
            Assert.Equal(12, Unsafe.SizeOf<MyStruct9Exp>());
            Assert.Equal(8, Unsafe.SizeOf<MyStruct1000Seq>());
            Assert.Equal(1000, Unsafe.SizeOf<MyStruct1000Exp>());
        }

        // Field offsets:
        Assert.Equal("(5, 3, 4, 1, 2, 6)", FieldOffsets(new SubclassSubclassSeq { A = 1, B = 2, C = 3, D = 4, E = 5, F = 6 }).ToString());
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static object FieldOffsets(SubclassSubclassSeq f) => (f.E, f.C, f.D, f.A, f.B, f.F);

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    public class BaseClassSeq
    {
        public byte A;
        public nint B;
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    public class BaseClassWithGcSeq
    {
        public byte A;
        public string B;
    }

    public class SubclassOfBaseWithGcSeq : BaseClassWithGcSeq
    {
        public byte C;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public class SubclassSeq : BaseClassSeq
    {
        public byte C;
        public nint D;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public class SubclassWithGcSeq : BaseClassSeq
    {
        public byte C;
        public object D;
    }

    public class SubclassSubclassSeq : SubclassSeq
    {
        public byte E;
        public nint F;
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
    private class MyClass1000Exp
    {
        [FieldOffset(0)]
        private byte[] a;
        [FieldOffset(8)]
        private byte b;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1000)]
    private class MyClass1000Seq
    {
        private byte[] a;
        private byte b;
    }

    [StructLayout(LayoutKind.Explicit, Size = 1000)]
    private class MyClass1000NoGcExp
    {
        [FieldOffset(0)]
        private nint a;
        [FieldOffset(8)]
        private byte b;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1000)]
    private class MyClass1000NoGcSeq
    {
        private nint a;
        private byte b;
    }

    private struct MyStructAuto
    {
        private byte[] a;
        private byte b;
    }

    [StructLayout(LayoutKind.Explicit, Size = 1000)]
    private struct MyStruct1000Exp
    {
        [FieldOffset(0)]
        private byte[] a;
        [FieldOffset(8)]
        private byte b;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1000)]
    private struct MyStruct1000Seq
    {
        private byte[] a;
        private byte b;
    }

    [StructLayout(LayoutKind.Explicit, Size = 9)]
    private struct MyStruct9Exp
    {
        [FieldOffset(0)]
        private byte[] a;
        [FieldOffset(8)]
        private byte b;
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    private struct MyStruct9Seq
    {
        private byte[] a;
        private byte b;
    }
}
