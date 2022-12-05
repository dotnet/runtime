// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    public static int Main()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
        {
            return 100;
        }

        for (int i = 0; i < 100; i++)
        {
            using CrossVirtualAlloc alloc = new();
            if (alloc.IsFailedToCommit)
            {
                Console.WriteLine("VirtualAlloc/mmap/mprotect failed, giving up on test.");
                break;
            }
            Read1(alloc.GetPointerNearPageEndFor<Data1>());
            Write1(alloc.GetPointerNearPageEndFor<Data1>(), default);
            Read2(alloc.GetPointerNearPageEndFor<Data2>());
            Write2(alloc.GetPointerNearPageEndFor<Data2>(), default);
            Read3(alloc.GetPointerNearPageEndFor<Data3>());
            Write3(alloc.GetPointerNearPageEndFor<Data3>(), default);
            Read4(alloc.GetPointerNearPageEndFor<Data4>());
            Write4(alloc.GetPointerNearPageEndFor<Data4>(), default);
            Read5(alloc.GetPointerNearPageEndFor<Data5>());
            Write5(alloc.GetPointerNearPageEndFor<Data5>(), default);
        }
        return 100;
    }
}

internal unsafe static class CrossplatVirtualAlloc
{
    [DllImport(nameof(CrossplatVirtualAlloc))]
    public static extern byte* Alloc(nuint size);

    [DllImport(nameof(CrossplatVirtualAlloc))]
    public static extern void Free(byte* ptr, nuint size);
}

// Cross-platform implementation of VirtualAlloc that is focused on reproducing problems
// where JIT emits code that reads/writes more than requested
internal unsafe class CrossVirtualAlloc : IDisposable
{
    private readonly byte* _ptr;

    public CrossVirtualAlloc()
    {
        _ptr = CrossplatVirtualAlloc.Alloc(PageSize);
    }

    public bool IsFailedToCommit => _ptr == null;

    public static readonly nuint PageSize = (nuint)Environment.SystemPageSize;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public byte* GetPointerNearPageEndFor<T>() => _ptr + PageSize - Unsafe.SizeOf<T>();

    public void Dispose()
    {
        if (IsFailedToCommit)
            return;
        CrossplatVirtualAlloc.Free(_ptr, PageSize);
    }
}
