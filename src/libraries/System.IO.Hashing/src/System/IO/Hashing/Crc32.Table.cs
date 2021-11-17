// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System.IO.Hashing
{
    public sealed partial class Crc32 : NonCryptographicHashAlgorithm
    {
        // Pre-computed CRC-32 transition table.
        // While this implementation is based on the standard CRC-32 polynomial,
        // x32 + x26 + x23 + x22 + x16 + x12 + x11 + x10 + x8 + x7 + x5 + x4 + x2 + x1 + x0,
        // this version uses reflected bit ordering, so 0x04C11DB7 becomes 0xEDB88320
        private static readonly uint[] s_crcLookup = Crc32ReflectedTable.Generate(0xEDB88320u);
    }
}
