// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Shared helpers for reading and writing a scalar value from a fixed-stride floating-point / SIMD
/// register file (the V/XMM/F/D areas) laid out contiguously within a platform CONTEXT buffer.
/// </summary>
internal static class SimdRegisterAccess
{
    /// <summary>
    /// Reads the low 64 bits (interpreted as a <see cref="double"/>) of the <paramref name="index"/>-th
    /// register in the file that starts at <paramref name="fileOffset"/> with a <paramref name="registerStride"/>
    /// byte stride. Returns false when <paramref name="index"/> is outside the register file.
    /// </summary>
    public static bool TryReadRegister(ReadOnlySpan<byte> context, int fileOffset, int registerStride, int registerCount, int index, out double value)
    {
        value = 0.0;
        if ((uint)index >= (uint)registerCount)
            return false;
        int offset = fileOffset + (index * registerStride);
        if (offset + sizeof(ulong) <= context.Length)
            value = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(context.Slice(offset)));
        return true;
    }

    /// <summary>
    /// Writes <paramref name="value"/> (4 or 8 bytes) into the low bytes of the <paramref name="index"/>-th
    /// register in the file that starts at <paramref name="fileOffset"/> with a <paramref name="registerStride"/>
    /// byte stride. Returns false when <paramref name="index"/> is outside the register file.
    /// </summary>
    public static bool TryWriteRegister(Span<byte> context, int fileOffset, int registerStride, int registerCount, int index, ReadOnlySpan<byte> value)
    {
        if ((uint)index >= (uint)registerCount)
            return false;
        int offset = fileOffset + (index * registerStride);
        if (offset + value.Length > context.Length)
            return false;
        value.CopyTo(context.Slice(offset, value.Length));
        return true;
    }
}
