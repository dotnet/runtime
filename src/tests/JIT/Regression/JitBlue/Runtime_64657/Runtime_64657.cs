// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class Runtime_64657
{
    [DllImport("kernel32")]
    public static extern byte* VirtualAlloc(IntPtr lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Validate<T>(T* c, int x) where T : unmanaged
    {
        // this nullcheck should not read more than requested
        T implicitNullcheck = c[x];
    }

    public static int Main()
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

        return 100;
    }
}
