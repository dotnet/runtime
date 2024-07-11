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
    private int ColumnSize(MetadataColumnIndex column)
    {
        return columnSize[(int)column];
    }

    private int ColumnOffset(MetadataColumnIndex column)
    {
        return columnOffset[(int)column];
    }

    private int RowCount(MetadataTable table)
    {
        return _ecmaMetadata.Schema.RowCount[(int)table];
    }

    private bool TryReadTableEntry(ReadOnlySpan<byte> bytes, MetadataColumnIndex column, out uint value)
    {
        int size = ColumnSize(column);
        ReadOnlySpan<byte> singleColumn = bytes.Slice(ColumnOffset(column), size);
        if (size == 2)
        {
            if (TryReadCore<ushort>(singleColumn, out ushort valueAsShort))
            {
                value = valueAsShort;
                return true;
            }
            value = 0;
            return false;
        }
        if (size != 4)
            throw new ArgumentOutOfRangeException(nameof(column));

        return TryReadCore(singleColumn, out value);
    }

    private uint GetColumnRaw(EcmaMetadataCursor c, MetadataColumnIndex col_idx)
    {
        if (c.Table != ColumnTable(col_idx))
            throw new ArgumentOutOfRangeException(nameof(col_idx));

        if (!TryReadTableEntry(c.Row, col_idx, out uint rawResult))
        {
            throw new ArgumentOutOfRangeException(nameof(col_idx));
        }
        return rawResult;
    }

    private int RidEncodingBits(MetadataTable table)
    {
        if (table == MetadataTable.Unused)
            return 0;

        int countInTable = RowCount(table);

        // Tables start at 1
        countInTable++;
        return 32 - BitOperations.LeadingZeroCount((uint)countInTable);
    }

    private int RidEncodingBytes(MetadataTable table)
    {
        if (RidEncodingBits(table) > 16)
            return 4;
        else
            return 2;
    }

    private int CodedIndexEncodingBytes(ReadOnlySpan<MetadataTable> tablesEncoded)
    {
        uint encodingMask = BitOperations.RoundUpToPowerOf2((uint)tablesEncoded.Length) - 1;
        int bitsForTableEncoding = 32 - BitOperations.LeadingZeroCount(encodingMask);
        if (tablesEncoded.Length == 1)
        {
            Debug.Assert(bitsForTableEncoding == 0); // This is just a rid to token conversion, no extra bits.
        }
        if (tablesEncoded.Length == 3 && tablesEncoded[0] == (MetadataTable)(-2))
        {
            // Ptr scenario
            return RidEncodingBytes(tablesEncoded[2]);
        }

        foreach (MetadataTable table in tablesEncoded)
        {
            if ((RidEncodingBits(table) + bitsForTableEncoding) > 16)
                return 4;
        }
        return 2;
    }
}
