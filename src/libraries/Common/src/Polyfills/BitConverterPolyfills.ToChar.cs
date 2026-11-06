// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System;

internal static class BitConverterCharPolyfills
{
    extension(BitConverter)
    {
        public static char ToChar(ReadOnlySpan<byte> value) => MemoryMarshal.Read<char>(value);
    }
}
