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

        if (!OperatingSystem.IsLinux())
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

        // Split args on ARM.
        //
        if (CallForSplitStructWithSixteenBytes_Arm(0, *(StructWithSixteenBytes*)(pages + PAGE_SIZE - sizeof(StructWithSixteenBytes))) != ByteValue)
        {
            result = 216;
            return true;
        }
        if (CallForSplitStructWithSeventeenBytes_Arm(0, *(StructWithSeventeenBytes*)(pages + PAGE_SIZE - sizeof(StructWithSeventeenBytes))) != ByteValue)
        {
            result = 217;
            return true;
        }
        if (CallForSplitStructWithEighteenBytes_Arm(0, *(StructWithEighteenBytes*)(pages + PAGE_SIZE - sizeof(StructWithEighteenBytes))) != ByteValue)
        {
            result = 218;
            return true;
        }
        if (CallForSplitStructWithNineteenBytes_Arm(0, *(StructWithNineteenBytes*)(pages + PAGE_SIZE - sizeof(StructWithNineteenBytes))) != ByteValue)
        {
            result = 219;
            return true;
        }

        // Stack args on ARM64.
        //
        if (CallForStkStructWithOneByte_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithOneByte*)(pages + PAGE_SIZE - sizeof(StructWithOneByte))) != ByteValue)
        {
            result = 301;
            return true;
        }
        if (CallForStkStructWithTwoBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithTwoBytes*)(pages + PAGE_SIZE - sizeof(StructWithTwoBytes))) != ByteValue)
        {
            result = 302;
            return true;
        }
        if (CallForStkStructWithThreeBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithThreeBytes*)(pages + PAGE_SIZE - sizeof(StructWithThreeBytes))) != ByteValue)
        {
            result = 303;
            return true;
        }
        if (CallForStkStructWithFourBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithFourBytes*)(pages + PAGE_SIZE - sizeof(StructWithFourBytes))) != ByteValue)
        {
            result = 304;
            return true;
        }
        if (CallForStkStructWithFiveBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithFiveBytes*)(pages + PAGE_SIZE - sizeof(StructWithFiveBytes))) != ByteValue)
        {
            result = 305;
            return true;
        }
        if (CallForStkStructWithSixBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithSixBytes*)(pages + PAGE_SIZE - sizeof(StructWithSixBytes))) != ByteValue)
        {
            result = 306;
            return true;
        }
        if (CallForStkStructWithSevenBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithSevenBytes*)(pages + PAGE_SIZE - sizeof(StructWithSevenBytes))) != ByteValue)
        {
            result = 307;
            return true;
        }
        if (CallForStkStructWithEightBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithEightBytes*)(pages + PAGE_SIZE - sizeof(StructWithEightBytes))) != ByteValue)
        {
            result = 308;
            return true;
        }
        if (CallForStkStructWithNineBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithNineBytes*)(pages + PAGE_SIZE - sizeof(StructWithNineBytes))) != ByteValue)
        {
            result = 309;
            return true;
        }
        if (CallForStkStructWithTenBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithTenBytes*)(pages + PAGE_SIZE - sizeof(StructWithTenBytes))) != ByteValue)
        {
            result = 310;
            return true;
        }
        if (CallForStkStructWithElevenBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithElevenBytes*)(pages + PAGE_SIZE - sizeof(StructWithElevenBytes))) != ByteValue)
        {
            result = 311;
            return true;
        }
        if (CallForStkStructWithTwelveBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithTwelveBytes*)(pages + PAGE_SIZE - sizeof(StructWithTwelveBytes))) != ByteValue)
        {
            result = 312;
            return true;
        }
        if (CallForStkStructWithThirteenBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithThirteenBytes*)(pages + PAGE_SIZE - sizeof(StructWithThirteenBytes))) != ByteValue)
        {
            result = 313;
            return true;
        }
        if (CallForStkStructWithFourteenBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithFourteenBytes*)(pages + PAGE_SIZE - sizeof(StructWithFourteenBytes))) != ByteValue)
        {
            result = 314;
            return true;
        }
        if (CallForStkStructWithFifteenBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithFifteenBytes*)(pages + PAGE_SIZE - sizeof(StructWithFifteenBytes))) != ByteValue)
        {
            result = 315;
            return true;
        }
        if (CallForStkStructWithSixteenBytes_Arm64(0, 0, 0, 0, 0, 0, 0, *(StructWithSixteenBytes*)(pages + PAGE_SIZE - sizeof(StructWithSixteenBytes))) != ByteValue)
        {
            result = 316;
            return true;
        }

        munmap(pages, 2 * PAGE_SIZE);

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForSplitStructWithSixteenBytes_Arm(long arg0, StructWithSixteenBytes splitArg) => splitArg.Bytes[15];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForSplitStructWithSeventeenBytes_Arm(long arg0, StructWithSeventeenBytes splitArg) => splitArg.Bytes[16];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForSplitStructWithEighteenBytes_Arm(long arg0, StructWithEighteenBytes splitArg) => splitArg.Bytes[17];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForSplitStructWithNineteenBytes_Arm(long arg0, StructWithNineteenBytes splitArg) => splitArg.Bytes[18];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithOneByte_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithOneByte stkArg) => stkArg.Bytes[0];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithTwoBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithTwoBytes stkArg) => stkArg.Bytes[1];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithThreeBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithThreeBytes stkArg) => stkArg.Bytes[2];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithFourBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithFourBytes stkArg) => stkArg.Bytes[3];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithFiveBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithFiveBytes stkArg) => stkArg.Bytes[4];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithSixBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithSixBytes stkArg) => stkArg.Bytes[5];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithSevenBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithSevenBytes stkArg) => stkArg.Bytes[6];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithEightBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithEightBytes stkArg) => stkArg.Bytes[7];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithNineBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithNineBytes stkArg) => stkArg.Bytes[8];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithTenBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithTenBytes stkArg) => stkArg.Bytes[9];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithElevenBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithElevenBytes stkArg) => stkArg.Bytes[10];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithTwelveBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithTwelveBytes stkArg) => stkArg.Bytes[11];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithThirteenBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithThirteenBytes stkArg) => stkArg.Bytes[12];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithFourteenBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithFourteenBytes stkArg) => stkArg.Bytes[13];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithFifteenBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithFifteenBytes stkArg) => stkArg.Bytes[14];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStkStructWithSixteenBytes_Arm64(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, StructWithSixteenBytes stkArg) => stkArg.Bytes[15];

    [DllImport("libc")]
    private static extern void* mmap(void* addr, nuint length, int prot, int flags, int fd, nuint offset);

    [DllImport("libc")]
    private static extern int mprotect(void* addr, nuint len, int prot);

    [DllImport("libc")]
    private static extern int munmap(void* addr, nuint length);

    struct StructWithOneByte
    {
        public fixed byte Bytes[1];
    }

    struct StructWithTwoBytes
    {
        public fixed byte Bytes[2];
    }

    struct StructWithThreeBytes
    {
        public fixed byte Bytes[3];
    }

    struct StructWithFourBytes
    {
        public fixed byte Bytes[4];
    }

    struct StructWithFiveBytes
    {
        public fixed byte Bytes[5];
    }

    struct StructWithSixBytes
    {
        public fixed byte Bytes[6];
    }

    struct StructWithSevenBytes
    {
        public fixed byte Bytes[7];
    }

    struct StructWithEightBytes
    {
        public fixed byte Bytes[8];
    }

    struct StructWithNineBytes
    {
        public fixed byte Bytes[9];
    }

    struct StructWithTenBytes
    {
        public fixed byte Bytes[10];
    }

    struct StructWithElevenBytes
    {
        public fixed byte Bytes[11];
    }

    struct StructWithTwelveBytes
    {
        public fixed byte Bytes[12];
    }

    struct StructWithThirteenBytes
    {
        public fixed byte Bytes[13];
    }

    struct StructWithFourteenBytes
    {
        public fixed byte Bytes[14];
    }

    struct StructWithFifteenBytes
    {
        public fixed byte Bytes[15];
    }

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
