// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public unsafe class Runtime_76194
{
    [StructLayout(LayoutKind.Explicit, Size = 5)]
    public struct Data1
    {
        [FieldOffset(0)] public byte Byte;
        [FieldOffset(1)] public int Int;
    }

    [StructLayout(LayoutKind.Explicit, Size = 7)]
    public struct Data2
    {
        [FieldOffset(0)] public short A1;
        [FieldOffset(2)] public short A2;
    }

    [StructLayout(LayoutKind.Explicit, Size = 7)]
    public struct Data3
    {
        [FieldOffset(0)] public int Int1;
        [FieldOffset(3)] public int Int2;
    }

    public struct Data4
    {
        public short A1;
        public short A2;
    }

    public struct Data5
    {
        public byte Byte;
        public int Int;
    }

    [MethodImpl(MethodImplOptions.NoInlining)] static Data1 Read1(byte* location) => Unsafe.ReadUnaligned<Data1>(location);
    [MethodImpl(MethodImplOptions.NoInlining)] static Data2 Read2(byte* location) => Unsafe.ReadUnaligned<Data2>(location);
    [MethodImpl(MethodImplOptions.NoInlining)] static Data3 Read3(byte* location) => Unsafe.ReadUnaligned<Data3>(location);
    [MethodImpl(MethodImplOptions.NoInlining)] static Data4 Read4(byte* location) => Unsafe.ReadUnaligned<Data4>(location);
    [MethodImpl(MethodImplOptions.NoInlining)] static Data5 Read5(byte* location) => Unsafe.ReadUnaligned<Data5>(location);

    [MethodImpl(MethodImplOptions.NoInlining)] static void Write1(byte* location, Data1 d) => Unsafe.WriteUnaligned(location, d);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Write2(byte* location, Data2 d) => Unsafe.WriteUnaligned(location, d);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Write3(byte* location, Data3 d) => Unsafe.WriteUnaligned(location, d);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Write4(byte* location, Data4 d) => Unsafe.WriteUnaligned(location, d);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Write5(byte* location, Data5 d) => Unsafe.WriteUnaligned(location, d);


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte* GetPointerNearPageEndFor<T>(byte* ptr, nuint pageSize) => ptr + pageSize - Unsafe.SizeOf<T>();


    [MethodImpl(MethodImplOptions.NoInlining)]
    [Fact]
    public static int TestEntryPoint()
    {
        nuint pageSize = (nuint)Environment.SystemPageSize;
        for (int i = 0; i < 100; i++)
        {
            byte* ptr = CrossplatVirtualAlloc.AllocWithGuard(pageSize);
            if (ptr == null)
            {
                throw new InvalidOperationException($"CrossplatVirtualAlloc.Alloc returned null at {i}th iteration");
            }

            Read1(GetPointerNearPageEndFor<Data1>(ptr, pageSize));
            Write1(GetPointerNearPageEndFor<Data1>(ptr, pageSize), default);
            Read2(GetPointerNearPageEndFor<Data2>(ptr, pageSize));
            Write2(GetPointerNearPageEndFor<Data2>(ptr, pageSize), default);
            Read3(GetPointerNearPageEndFor<Data3>(ptr, pageSize));
            Write3(GetPointerNearPageEndFor<Data3>(ptr, pageSize), default);
            Read4(GetPointerNearPageEndFor<Data4>(ptr, pageSize));
            Write4(GetPointerNearPageEndFor<Data4>(ptr, pageSize), default);
            Read5(GetPointerNearPageEndFor<Data5>(ptr, pageSize));
            Write5(GetPointerNearPageEndFor<Data5>(ptr, pageSize), default);

            CrossplatVirtualAlloc.Free(ptr, pageSize);
        }
        return 100;
    }
}

// Cross-platform implementation of VirtualAlloc that is focused on reproducing problems
// where JIT emits code that reads/writes more than requested
internal static unsafe class CrossplatVirtualAlloc
{
    [DllImport(nameof(CrossplatVirtualAlloc))]
    public static extern byte* AllocWithGuard(nuint size);

    [DllImport(nameof(CrossplatVirtualAlloc))]
    public static extern void Free(byte* ptr, nuint size);
}
