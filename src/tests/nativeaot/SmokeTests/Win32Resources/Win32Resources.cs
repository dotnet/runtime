// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

public unsafe class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        nint lib = 0;

        if (GetIntValueFromResource(lib, (ushort*)(nuint)(ushort)10, 0x041B) != 3)
            throw new Exception();

        ReadOnlySpan<char> resName = "funny";
        fixed (char* pResName = resName)
            if (GetIntValueFromResource(lib, (ushort*)pResName, 0x041B) != 1)
                throw new Exception();

        return 100;
    }

    static int GetIntValueFromResource(nint hModule, ushort* lpName, ushort wLanguage)
    {
        ushort* RT_RCDATA = (ushort*)(nuint)(ushort)10;

        nint hResInfo = FindResourceExW(hModule, RT_RCDATA, lpName, wLanguage);
        if (hResInfo == 0)
            throw new Exception("Resource not found");

        if (SizeofResource(hModule, hResInfo) != 4)
            throw new Exception("Wrong size of resource");

        nint hResData = LoadResource(hModule, hResInfo);
        int val = *(int*)LockResource(hResData);

        return val;
    }

    [DllImport("kernel32")]
    static extern nint FindResourceExW(nint hModule, ushort* lpType, ushort* lpName, ushort wLanguage);

    [DllImport("kernel32")]
    static extern nint LoadResource(nint hModule, nint hResInfo);

    [DllImport("kernel32")]
    static extern void* LockResource(nint hResData);

    [DllImport("kernel32")]
    static extern uint SizeofResource(nint hModule, nint hResInfo);
}
