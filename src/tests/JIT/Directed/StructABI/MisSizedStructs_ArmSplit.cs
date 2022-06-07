// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public unsafe class MisSizedStructs_ArmSplit
{
    public const byte ByteValue = 0xC1;

    public static int Main()
    {
        if (ProblemWithOutOfBoundsLoads(out int result))
        {
            return result;
        }

        return 100;
    }

    // Test that we do not load of bounds for split arguments on ARM.
    //
    static bool ProblemWithOutOfBoundsLoads(out int result)
    {
        result = 100;

        // TODO: enable for x64 once https://github.com/dotnet/runtime/issues/65937 has been fixed.
        if (!OperatingSystem.IsLinux() || (RuntimeInformation.ProcessArchitecture == Architecture.X64))
        {
            return false;
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
            return false;
        }

        if (mprotect(pages + PAGE_SIZE, PAGE_SIZE, PROT_NONE) != 0)
        {
            Console.WriteLine("Failed to protect the second page, errno is {0}, giving up on the test", Marshal.GetLastSystemError());
            munmap(pages, 2 * PAGE_SIZE);
            return false;
        }

        pages[PAGE_SIZE - 1] = ByteValue;

        if (CallForSplitStructWithSixteenBytes(0, *(StructWithSixteenBytes*)(pages + PAGE_SIZE - sizeof(StructWithSixteenBytes))) != ByteValue)
        {
            result = 200;
            return true;
        }
        if (CallForSplitStructWithSeventeenBytes(0, *(StructWithSeventeenBytes*)(pages + PAGE_SIZE - sizeof(StructWithSeventeenBytes))) != ByteValue)
        {
            result = 201;
            return true;
        }
        if (CallForSplitStructWithEighteenBytes(0, *(StructWithEighteenBytes*)(pages + PAGE_SIZE - sizeof(StructWithEighteenBytes))) != ByteValue)
        {
            result = 202;
            return true;
        }
        if (CallForSplitStructWithNineteenBytes(0, *(StructWithNineteenBytes*)(pages + PAGE_SIZE - sizeof(StructWithNineteenBytes))) != ByteValue)
        {
            result = 203;
            return true;
        }

        munmap(pages, 2 * PAGE_SIZE);

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForSplitStructWithSixteenBytes(long arg0, StructWithSixteenBytes splitArg) => splitArg.Bytes[15];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForSplitStructWithSeventeenBytes(long arg0, StructWithSeventeenBytes splitArg) => splitArg.Bytes[16];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForSplitStructWithEighteenBytes(long arg0, StructWithEighteenBytes splitArg) => splitArg.Bytes[17];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForSplitStructWithNineteenBytes(long arg0, StructWithNineteenBytes splitArg) => splitArg.Bytes[18];

    [DllImport("libc")]
    private static extern void* mmap(void* addr, nuint length, int prot, int flags, int fd, nuint offset);

    [DllImport("libc")]
    private static extern int mprotect(void* addr, nuint len, int prot);

    [DllImport("libc")]
    private static extern int munmap(void* addr, nuint length);

    struct StructWithSixteenBytes
    {
        public fixed byte Bytes[16];
    }

    struct StructWithSeventeenBytes
    {
        public fixed byte Bytes[17];
    }

    struct StructWithEighteenBytes
    {
        public fixed byte Bytes[18];
    }

    struct StructWithNineteenBytes
    {
        public fixed byte Bytes[19];
    }
}
