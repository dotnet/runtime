// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_64657
{
    [DllImport("kernel32")]
    public static extern byte* VirtualAlloc(IntPtr lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Validate<T>(T* c, int x) where T : unmanaged
    {
        // this nullcheck should not read more than requested
        T implicitNullcheck = c[x];
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (!OperatingSystem.IsWindows())
            return 100; // VirtualAlloc is only for Windows

        uint length = (uint)Environment.SystemPageSize;
        byte* ptr = VirtualAlloc(IntPtr.Zero, length, 0x1000 | 0x2000 /* reserve commit */, 0x04 /*readonly guard*/);

        Validate((byte*)(ptr + length - sizeof(byte)), 0);
        Validate((sbyte*)(ptr + length - sizeof(sbyte)), 0);
        Validate((bool*)(ptr + length - sizeof(bool)), 0);
        Validate((ushort*)(ptr + length - sizeof(ushort)), 0);
        Validate((short*)(ptr + length - sizeof(short)), 0);
        Validate((uint*)(ptr + length - sizeof(uint)), 0);
        Validate((int*)(ptr + length - sizeof(int)), 0);
        Validate((ulong*)(ptr + length - sizeof(ulong)), 0);
        Validate((long*)(ptr + length - sizeof(long)), 0);
        Validate((nint*)(ptr + length - sizeof(nint)), 0);
        Validate((nuint*)(ptr + length - sizeof(nuint)), 0);

        Validate((S1*)(ptr + length - sizeof(S1)), 0);
        Validate((S2*)(ptr + length - sizeof(S2)), 0);
        Validate((S3*)(ptr + length - sizeof(S3)), 0);
        Validate((S4*)(ptr + length - sizeof(S4)), 0);

        TestStructures();

        return 100;
    }

    private static void TestStructures()
    {
        S1 s1 = new S1();
        TestS1_1(ref s1);
        TestS1_2(ref s1);

        S2 s2 = new S2();
        TestS2_1(ref s2);
        TestS2_2(ref s2);

        S3 s3 = new S3();
        TestS3_1(ref s3);
        TestS3_2(ref s3);

        S4 s4 = new S4();
        TestS4_1(ref s4);
        TestS4_2(ref s4);

        S5 s5 = new S5 { a1 = "1", a2 = "2" };
        TestS5_1(ref s5);
        TestS5_2(ref s5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)] static void TestS1_1(ref S1 s) { var _ = s.a1; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void TestS1_2(ref S1 s) { var _ = s.a2; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void TestS2_1(ref S2 s) { var _ = s.a1; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void TestS2_2(ref S2 s) { var _ = s.a2; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void TestS3_1(ref S3 s) { var _ = s.a1; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void TestS3_2(ref S3 s) { var _ = s.a2; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void TestS4_1(ref S4 s) { var _ = s.a1; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void TestS4_2(ref S4 s) { var _ = s.a2; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void TestS5_1(ref S5 s) { var _ = s.a1; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void TestS5_2(ref S5 s) { var _ = s.a2; }

    public struct S1
    {
        public byte a1;
        public byte a2;
    }

    public struct S2
    {
        public short a1;
        public short a2;
    }

    public struct S3
    {
        public int a1;
        public int a2;
    }

    public struct S4
    {
        public long a1;
        public long a2;
    }

    public struct S5
    {
        public string a1;
        public string a2;
    }
}
