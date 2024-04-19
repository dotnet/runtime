// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public unsafe class Program
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct GUID
    {
        private int align;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Guid initialGuid = Guid.Parse("E6218D43-3C16-48BF-9C3C-8076FF5AFCD0");
        GUID g = default;
        Test(initialGuid, &g);
        return Unsafe.As<GUID, Guid>(ref g) == initialGuid ? 100 : 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static GUID GetGUID(ref Guid guid) => Unsafe.As<Guid, GUID>(ref guid);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void Test(Guid initialGuid, GUID* result)
    {
        Guid g = initialGuid;
        GUID guid = GetGUID(ref g);
        Unsafe.CopyBlock(result, &guid, (uint)sizeof(GUID));
    }
}
