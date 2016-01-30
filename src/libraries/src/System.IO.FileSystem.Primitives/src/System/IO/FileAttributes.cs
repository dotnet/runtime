// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.IO
{
    [Flags]
    public enum FileAttributes
    {
        ReadOnly = 0x0001,
        Hidden = 0x0002,
        System = 0x0004,
        Directory = 0x0010,
        Archive = 0x0020,
        Device = 0x0040,
        Normal = 0x0080,
        Temporary = 0x0100,
        SparseFile = 0x0200,
        ReparsePoint = 0x0400,
        Compressed = 0x0800,
        Offline = 0x1000,
        NotContentIndexed = 0x2000,
        Encrypted = 0x4000,
        IntegrityStream = 0x8000,
        NoScrubData = 0x20000
    }
}
