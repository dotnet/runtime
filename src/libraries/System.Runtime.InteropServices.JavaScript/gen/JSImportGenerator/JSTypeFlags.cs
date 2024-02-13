// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    // please keep in sync with src\mono\browser\runtime\marshal.ts
    [Flags]
    internal enum JSTypeFlags : int
    {
        None = 0x0,
        Void = 0x1,
        Boolean = 0x2,
        Number = 0x4, // max 52 integral bits
        BigInt = 0x8,
        Date = 0x10,
        String = 0x20,
        Function = 0x40,
        Array = 0x80,
        Object = 0x100,
        Promise = 0x200,
        Error = 0x400,
        MemoryView = 0x800,
        Any = 0x1000,
        Discard = 0x2000,
        Missing = 0x4000_0000,
    }
}
