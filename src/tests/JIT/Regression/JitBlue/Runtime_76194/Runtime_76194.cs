// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    static T Read<T>(byte* location) => Unsafe.ReadUnaligned<T>(location);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Write<T>(byte* location, T t) => Unsafe.WriteUnaligned(location, t);
}

// Cross-platform implementation of VirtualAlloc that is focused on reproducing problems
// where JIT emits code that reads/writes more than requested
public unsafe class CrossVirtualAlloc : IDisposable
{
    private readonly byte* _ptr;

    public CrossVirtualAlloc()
    {
        if (OperatingSystem.IsWindows())
        {
            const int MEM_COMMIT = 0x1000;
            const int MEM_RESERVE = 0x2000;
            const int PAGE_READWRITE = 0x04;
            const int MEM_RELEASE = 0x8000;

            byte* reservePtr = VirtualAlloc(null, PageSize * 2, MEM_RESERVE, PAGE_READWRITE);
            if (reservePtr != null)
            {
                _ptr = VirtualAlloc(reservePtr, PageSize, MEM_COMMIT, PAGE_READWRITE);
                if (_ptr == null)
                {
                    VirtualFree(reservePtr, 0, MEM_RELEASE);
                }
            }
        }
        else
        {
            const int PROT_NONE = 0x0;
            const int PROT_READ = 0x1;
            const int PROT_WRITE = 0x2;
            const int MAP_PRIVATE = 0x02;
            const int MAP_ANONYMOUS = 0x20;

            _ptr = mmap(null, 2 * PageSize, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
            if (_ptr != null && mprotect(_ptr + PageSize, PageSize, PROT_NONE) != 0)
            {
                munmap(_ptr, 2 * PageSize);
            }
        }
    }

    public bool IsFailedToCommit => _ptr == null;

    public nuint PageSize => (nuint)Environment.SystemPageSize;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public byte* GetPointerNearPageEndFor<T>() => _ptr + PageSize - Unsafe.SizeOf<T>();

    public void Dispose()
    {
        if (OperatingSystem.IsWindows())
        {
            const int MEM_RELEASE = 0x8000;
            VirtualFree(_ptr, 0, MEM_RELEASE);
        }
        else
        {
            munmap(_ptr, (nuint)Environment.SystemPageSize * 2);
        }
    }

    [DllImport("kernel32")]
    static extern byte* VirtualAlloc(byte* lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32")]
    static extern int VirtualFree(byte* lpAddress, nuint dwSize, uint dwFreeType);

    [DllImport("libc")]
    static extern byte* mmap(byte* addr, nuint length, int prot, int flags, int fd, nuint offset);

    [DllImport("libc")]
    static extern int mprotect(byte* addr, nuint len, int prot);

    [DllImport("libc")]
    static extern int munmap(byte* addr, nuint length);
}
