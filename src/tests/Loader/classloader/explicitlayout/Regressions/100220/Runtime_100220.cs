// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public static class Runtime_100220
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/100220", TestRuntimes.Mono)]
    // Also, Mono needs RuntimeHelpers.GetRawObjectDataSize or equivalent to get an object size
    public static void TestEntryPoint()
    {
        if (IntPtr.Size == 8)
        {
            // Classes
            Assert.Equal(16, ObjectSize<MyClassAuto>());
            Assert.Equal(16, ObjectSize<MyClass1000Exp>());
            Assert.Equal(16, ObjectSize<MyClass1000Seq>());
            Assert.Equal(16, ObjectSize<MyClass1000NoGcExp>());
            Assert.Equal(1000, ObjectSize<MyClass1000NoGcSeq>());
            Assert.Equal(24, ObjectSize<BaseClassSeq>());
            Assert.Equal(40, ObjectSize<SubclassSeq>());
            Assert.Equal(48, ObjectSize<SubclassSubclassSeq>());
            Assert.Equal(32, ObjectSize<SubclassWithGcSeq>());
            Assert.Equal(16, ObjectSize<SubclassOfBaseWithGcSeq>());

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
            Assert.Equal(8, ObjectSize<MyClassAuto>());
            Assert.Equal(12, ObjectSize<MyClass1000Exp>());
            Assert.Equal(8, ObjectSize<MyClass1000Seq>());
            Assert.Equal(12, ObjectSize<MyClass1000NoGcExp>());
            Assert.Equal(1000, ObjectSize<MyClass1000NoGcSeq>());
            Assert.Equal(20, ObjectSize<BaseClassSeq>());
            Assert.Equal(36, ObjectSize<SubclassSeq>());
            Assert.Equal(44, ObjectSize<SubclassSubclassSeq>());
            Assert.Equal(28, ObjectSize<SubclassWithGcSeq>());
            Assert.Equal(8, ObjectSize<SubclassOfBaseWithGcSeq>());

            // Structs
            Assert.Equal(8, Unsafe.SizeOf<MyStructAuto>());
            Assert.Equal(8, Unsafe.SizeOf<MyStruct9Seq>());
            Assert.Equal(12, Unsafe.SizeOf<MyStruct9Exp>());
            Assert.Equal(8, Unsafe.SizeOf<MyStruct1000Seq>());
            Assert.Equal(1000, Unsafe.SizeOf<MyStruct1000Exp>());
        }

        // Field offsets:
        Assert.Equal("(5, 3, 4, 1, 2, 6)", FieldOffsets(new SubclassSubclassSeq { A = 1, B = 2, C = 3, D = 4, E = 5, F = 6 }).ToString());
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
    private static int ObjectSize<T>() where T : new()
    {
        return (int)(nuint)typeof(RuntimeHelpers)
            .GetMethod("GetRawObjectDataSize", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, [new T()]);
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
