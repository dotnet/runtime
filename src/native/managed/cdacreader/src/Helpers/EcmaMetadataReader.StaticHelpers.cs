// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Helpers;

internal partial class EcmaMetadataReader
{
    public static MetadataTable TokenToTable(uint token)
    {
        byte tableIndex = (byte)(token >> 24);
        if (tableIndex > (uint)MetadataTable.GenericParamConstraint)
        {
            return MetadataTable.Unused;
        }
        else
        {
            return (MetadataTable)tableIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSigned<T>() where T : struct, INumberBase<T>, IMinMaxValue<T>
    {
        return T.IsNegative(T.MinValue);
    }
    private static bool TryReadCore<T>(ReadOnlySpan<byte> bytes, out T value) where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
    {
        return T.TryReadLittleEndian(bytes, IsSigned<T>(), out value);
    }

    private static T ReadLittleEndian<T>(ReadOnlySpan<byte> bytes) where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
    {
        if (!T.TryReadLittleEndian(bytes, IsSigned<T>(), out T value))
            throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }

    public static uint RidFromToken(uint token)
    {
        return token & 0xFFFFFF;
    }
    public static uint CreateToken(MetadataTable table, uint rid)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan<uint>(rid, 0xFFFFFF, nameof(rid));
        ArgumentOutOfRangeException.ThrowIfGreaterThan<int>((int)table, (int)MetadataTable.GenericParamConstraint, nameof(table));
        return ((uint)table << 24) | rid;
    }

}
