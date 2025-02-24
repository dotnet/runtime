// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Microsoft.Diagnostics.DataContractReader.Decoder;

internal static class BinaryReaderExtensions
{
    /// <summary>
    /// Reads a C-style, zero-terminated string from memory.
    /// </summary>
    public static string ReadZString(this BinaryReader reader)
    {
        var sb = new StringBuilder();
        byte nextByte = reader.ReadByte();
        while (nextByte != 0)
        {
            sb.Append((char)nextByte);
            nextByte = reader.ReadByte();
        }
        return sb.ToString();
    }

    public static unsafe T Read<T>(this BinaryReader reader)
     where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        T value = default;
        Span<byte> buffer = stackalloc byte[sizeof(T)];

        if (reader.Read(buffer) != buffer.Length)
        {
            throw new IOException();
        }
        if (!T.TryReadLittleEndian(buffer, !IsSigned<T>(), out value))
        {
            throw new InvalidOperationException("Unable to convert to type.");
        }
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSigned<T>() where T : struct, INumberBase<T>, IMinMaxValue<T>
    {
        return T.IsNegative(T.MinValue);
    }
}
