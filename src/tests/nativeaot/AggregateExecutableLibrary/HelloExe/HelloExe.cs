// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

if (OperatingSystem.IsWindows())
{
    Win32Resources.Validate();
}

Console.WriteLine("Hello from HelloExe");

unsafe static class Win32Resources
{
    public static void Validate()
    {
        nint lib = 0;

        int resourceValue = GetIntValueFromResource(lib, (ushort*)(nuint)(ushort)10, 0x041B);
        if (resourceValue != 3)
            throw new Exception($"Expected resource 10 to have value 3, but got {resourceValue}");

        ReadOnlySpan<char> resName = "funny";
        fixed (char* pResName = resName)
        {
            resourceValue = GetIntValueFromResource(lib, (ushort*)pResName, 0x041B);
            if (resourceValue != 1)
                throw new Exception($"Expected resource 'funny' to have value 1, but got {resourceValue}");
        }
    }

    private static int GetIntValueFromResource(nint hModule, ushort* lpName, ushort wLanguage)
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
    private static extern nint FindResourceExW(nint hModule, ushort* lpType, ushort* lpName, ushort wLanguage);

    [DllImport("kernel32")]
    private static extern nint LoadResource(nint hModule, nint hResInfo);

    [DllImport("kernel32")]
    private static extern void* LockResource(nint hResData);

    [DllImport("kernel32")]
    private static extern uint SizeofResource(nint hModule, nint hResInfo);
}
