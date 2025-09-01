// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

class Ordering
{
    internal static unsafe int Run()
    {
        // Method addresses are not observable in WASM
        if (OperatingSystem.IsWasi() || OperatingSystem.IsBrowser())
            return 100;

        var keys = new nint[]
        {
            (nint)(delegate*<Guid>)&Method4,
            (nint)(delegate*<Guid>)&Method3,
            (nint)(delegate*<Guid>)&Method2,
            (nint)(delegate*<Guid>)&Method1,
            (nint)(delegate*<Guid>)&Method0,
        };

        var items = new int[] { 4, 3, 2, 1, 0 };

        Array.Sort(keys, items);

        // the order specified in the order.txt file
        var expectedOrder = new int[] { 2, 1, 3, 0, 4 };

        for (int i = 0; i < items.Length; i++)
            if (items[i] != expectedOrder[i])
                throw new Exception(i.ToString());

        return 100;
    }

    static Guid Method0() => new Guid(0xb20e3a5f, 0x4aad, 0x4225, 0x98, 0x26, 0xac, 0xe8, 0xe0, 0xf7, 0x64, 0x56);
    static Guid Method1() => new Guid(0x13464316, 0xb19c, 0x4e1c, 0x95, 0x2e, 0x22, 0xb4, 0xa, 0x21, 0x7d, 0xd5);
    static Guid Method2() => new Guid(0x510f2c1e, 0x7715, 0x4aee, 0x8d, 0xd5, 0x16, 0xf8, 0xd5, 0x70, 0x5, 0x90);
    static Guid Method3() => new Guid(0x4cc6e597, 0x875e, 0x4cb0, 0x90, 0x88, 0xcb, 0x4e, 0xd8, 0x8, 0x91, 0xb8);
    static Guid Method4() => new Guid(0x2d2e2b87, 0x75f5, 0x4c16, 0x93, 0xa9, 0xbe, 0xbd, 0x6b, 0x58, 0xbd, 0xd6);
}
