// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public unsafe class Runtime_65937
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Main()
    {
        if (!OperatingSystem.IsLinux())
        {
            return 100;
        }

        const int PROT_NONE = 0x0;
        const int PROT_READ = 0x1;
        const int PROT_WRITE = 0x2;
        const int MAP_PRIVATE = 0x02;
        const int MAP_ANONYMOUS = 0x20;
        const int PAGE_SIZE = 0x1000;

        byte* pages = (byte*)mmap(null, 2 * PAGE_SIZE, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);

        if (pages == (byte*)-1)
        {
            Console.WriteLine("Failed to allocate two pages, errno is {0}, giving up on the test", Marshal.GetLastSystemError());
            return 100;
        }

        if (mprotect(pages + PAGE_SIZE, PAGE_SIZE, PROT_NONE) != 0)
        {
            Console.WriteLine("Failed to protect the second page, errno is {0}, giving up on the test", Marshal.GetLastSystemError());
            munmap(pages, 2 * PAGE_SIZE);
            return 100;
        }

        CallWithStkArg(0, 0, 0, 0, 0, 0, *(StructWithNineBytes*)(pages + PAGE_SIZE - sizeof(StructWithNineBytes)));

        munmap(pages, 2 * PAGE_SIZE);

        return 100;
    }

    struct StructWithNineBytes
    {
        byte ByteOne;
        byte ByteTwo;
        byte ByteThree;
        byte ByteFour;
        byte ByteFive;
        byte ByteSix;
        byte ByteSeven;
        byte ByteEight;
        byte ByteNine;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallWithStkArg(int a, int b, int c, int d, int e, int f, StructWithNineBytes stkArg) { }

    [DllImport("libc")]
    private static extern void* mmap(void* addr, nuint length, int prot, int flags, int fd, nuint offset);

    [DllImport("libc")]
    private static extern int mprotect(void* addr, nuint len, int prot);

    [DllImport("libc")]
    private static extern int munmap(void* addr, nuint length);
}
