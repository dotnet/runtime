// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Helpers;

internal struct EcmaMetadataCursor
{
    internal ReadOnlyMemory<byte> TableData;
    internal MetadataTable Table;
    internal uint Rid;
    internal int RowSize;

    public ReadOnlySpan<byte> Row
    {
        get
        {
            return TableData.Span.Slice((int)(RowSize * (Rid - 1)), (int)RowSize);
        }
    }
}

internal partial class EcmaMetadataReader
{
    private EcmaMetadata _ecmaMetadata;
    private int[] rowSize;
    private int[] columnSize;
    private int[] columnOffset;
    private Func<uint, uint>[] columnTokenDecode;


    public EcmaMetadataReader(ReadOnlyMemory<byte> imageMemory)
    {
        columnSize = new int[(int)MetadataColumnIndex.Count];
        columnOffset = new int[(int)MetadataColumnIndex.Count];
        rowSize = new int[(int)MetadataTable.Count];
        columnTokenDecode = Array.Empty<Func<uint, uint>>();

        ReadOnlySpan<byte> image = imageMemory.Span;
        int magic = ReadLittleEndian<int>(image);
        if (magic != 0x424A5342)
            throw new ArgumentOutOfRangeException(nameof(imageMemory));

        int versionSize = ReadLittleEndian<int>(image.Slice(12, 4));
        versionSize = AlignUp(versionSize, 4);

        ReadOnlySpan<byte> versionName = image.Slice(16, versionSize);
        int nullTerminatorIndex = versionName.IndexOf((byte)0);

        if ((nullTerminatorIndex == -1) || (nullTerminatorIndex == 0))
        {
            // VersionName isn't null terminated
            throw new ArgumentException(nameof(imageMemory));
        }

        string metadataVersion = Encoding.UTF8.GetString(versionName.Slice(0, nullTerminatorIndex));

        int currentOffset = 16 + versionSize;

        currentOffset += 2; // Flags ... unused in this implementation
        ushort streams = ReadLittleEndian<ushort>(image.Slice(currentOffset));
        currentOffset += 2;

        ReadOnlyMemory<byte> StringHeap = null;
        ReadOnlyMemory<byte> UserStringHeap = null;
        ReadOnlyMemory<byte> BlobHeap = null;
        ReadOnlyMemory<byte> GuidHeap = null;
        ReadOnlyMemory<byte> TablesHeap = null;

        for (ushort iStream = 0; iStream < streams; iStream++)
        {
            var stream = ReadStream(ref image);
            if (stream.Name == "#Strings")
            {
                StringHeap = stream.Data;
            }
            else if (stream.Name == "#US")
            {
                UserStringHeap = stream.Data;
            }
            else if (stream.Name == "#Blob")
            {
                BlobHeap = stream.Data;
            }
            else if (stream.Name == "#GUID")
            {
                GuidHeap = stream.Data;
            }
            else if (stream.Name == "#~")
            {
                TablesHeap = stream.Data;
            }
            else if (stream.Name == "#-")
            {
                TablesHeap = stream.Data;
            }
        }

        if (TablesHeap.Length == 0)
        {
            throw new ArgumentException(nameof(imageMemory));
        }
        ReadOnlySpan<byte> tables = TablesHeap.Span;

        byte heapSizes = ReadLittleEndian<byte>(tables.Slice(6, 1));
        ulong validTables = ReadLittleEndian<ulong>(tables.Slice(8, 8));
        ulong sortedTables = ReadLittleEndian<ulong>(tables.Slice(16, 8));

        int[] tableRowCounts = new int[(int)MetadataTable.Count];
        bool[] isSorted = new bool[(int)MetadataTable.Count];
        int currentTablesOffset = 24;
        for (int i = 0; i < (int)MetadataTable.Count; i++)
        {
            if ((validTables & ((ulong)1 << i)) != 0)
            {
                tableRowCounts[i] = ReadLittleEndian<int>(tables.Slice(currentTablesOffset));
                currentTablesOffset += 4;
            }
            if ((sortedTables & ((ulong)1 << i)) != 0)
            {
                isSorted[i] = true;
            }
        }

        // There is an undocumented flag "extra_data" which adds a 4 byte pad here.
        if ((heapSizes & 0x40) != 0)
        {
            currentTablesOffset += 4;
        }

        EcmaMetadataSchema schema = new EcmaMetadataSchema(metadataVersion,
            largeStringHeap: (heapSizes & 1) != 0,
            largeGuidHeap: (heapSizes & 2) != 0,
            largeBlobHeap: (heapSizes & 4) != 0,
            rowCount: tableRowCounts,
            isSorted: isSorted,
            variableSizedColumnsAre4BytesLong: false
            );

        ReadOnlyMemory<byte>[] tableData = new ReadOnlyMemory<byte>[(int)MetadataTable.Count];

        _ecmaMetadata = new EcmaMetadata(schema, tableData, StringHeap, UserStringHeap, BlobHeap, GuidHeap);

        Init();

        // Init will compute row sizes, which is necessary for actually computing the tableData

        for (int i = 0; i < (int)MetadataTable.Count; i++)
        {
            checked
            {
                if ((validTables & ((ulong)1 << i)) != 0)
                {
                    int tableSize = checked(rowSize![i] * _ecmaMetadata.Schema.RowCount[i]);
                    tableData[i] = TablesHeap.Slice(currentTablesOffset, tableSize);
                    currentTablesOffset += tableSize;
                }
            }
        }

        (string Name, ReadOnlyMemory<byte> Data) ReadStream(ref ReadOnlySpan<byte> image)
        {
            int offset = ReadLittleEndian<int>(image.Slice(currentOffset));
            currentOffset += 4;
            int size = ReadLittleEndian<int>(image.Slice(currentOffset));
            currentOffset += 4;
            int nameStartOffset = currentOffset;
            int nameLen = 0;
            while (image[currentOffset++] != 0)
            {
                nameLen++;
                if (nameLen > 31) throw new ArgumentException(nameof(imageMemory));
            }

            if (nameLen == 0) throw new ArgumentException(nameof(imageMemory));

            currentOffset = AlignUp(currentOffset, 4);
            return (Encoding.ASCII.GetString(image.Slice(nameStartOffset, nameLen)), imageMemory.Slice(offset, size));
        }
    }

    private static int AlignUp(int input, int alignment)
    {
        return input + (alignment - 1) & ~(alignment - 1);
    }

    public EcmaMetadataReader(EcmaMetadata ecmaMetadata)
    {
        columnTokenDecode = Array.Empty<Func<uint, uint>>();
        columnSize = new int[(int)MetadataColumnIndex.Count];
        columnOffset = new int[(int)MetadataColumnIndex.Count];
        rowSize = new int[(int)MetadataTable.Count];

        _ecmaMetadata = ecmaMetadata;
        Init();
    }

    private void Init()
    {
        PtrTablesPresent ptrTable = PtrTablesPresent.None;
        if (RowCount(MetadataTable.MethodPtr) != 0)
            ptrTable |= PtrTablesPresent.Method;
        if (RowCount(MetadataTable.FieldPtr) != 0)
            ptrTable |= PtrTablesPresent.Field;
        if (RowCount(MetadataTable.ParamPtr) != 0)
            ptrTable |= PtrTablesPresent.Param;
        if (RowCount(MetadataTable.EventPtr) != 0)
            ptrTable |= PtrTablesPresent.Event;
        if (RowCount(MetadataTable.PropertyPtr) != 0)
            ptrTable |= PtrTablesPresent.Property;

        columnTokenDecode = columnTokenDecoders[(int)ptrTable];

        ComputeColumnSizesAndOffsets();

        void ComputeColumnSizesAndOffsets()
        {
            MetadataTable currentTable = MetadataTable.Unused;
            MetadataColumnIndex? prevColumn = null;

            for (int i = 0; i < (int)MetadataColumnIndex.Count; i++)
            {
                MetadataColumnIndex column = (MetadataColumnIndex)i;
                MetadataTable newColumnTable = ColumnTable(column);
                if (currentTable != newColumnTable)
                {
                    if (prevColumn.HasValue)
                        rowSize[(int)currentTable] = ComputeColumnEnd(prevColumn.Value);
                    currentTable = newColumnTable;
                    columnOffset[i] = 0;
                }
                else
                {
                    columnOffset[i] = ComputeColumnEnd(prevColumn!.Value);
                }
                prevColumn = column;

                columnSize[i] = columnTypes[i] switch
                {
                    ColumnType.TwoByteConstant => 2,
                    ColumnType.FourByteConstant => 4,
                    ColumnType.Utf8String => _ecmaMetadata.Schema.LargeStringHeap ? 4 : 2,
                    ColumnType.Blob => _ecmaMetadata.Schema.LargeBlobHeap ? 4 : 2,
                    ColumnType.Guid => _ecmaMetadata.Schema.LargeGuidHeap ? 4 : 2,
                    ColumnType.Token => _ecmaMetadata.Schema.VariableSizedColumnsAreAll4BytesLong ? 4 : CodedIndexEncodingBytes(codedIndexDecoderRing[i]),
                    _ => throw new System.Exception()
                };
            }

            rowSize[(int)ColumnTable(prevColumn!.Value)] = ComputeColumnEnd(prevColumn!.Value);
        }

        int ComputeColumnEnd(MetadataColumnIndex column)
        {
            return ColumnOffset(column) + ColumnSize(column);
        }
    }

    public EcmaMetadata UnderlyingMetadata => _ecmaMetadata;

    public uint GetColumnAsConstant(EcmaMetadataCursor c, MetadataColumnIndex col_idx)
    {
        if (columnTypes[(int)col_idx] != ColumnType.TwoByteConstant && columnTypes[(int)col_idx] != ColumnType.FourByteConstant)
            throw new ArgumentOutOfRangeException(nameof(col_idx));
        return GetColumnRaw(c, col_idx);
    }

    public System.ReadOnlySpan<byte> GetColumnAsBlob(EcmaMetadataCursor c, MetadataColumnIndex col_idx)
    {
        throw new NotImplementedException();
    }

    public uint GetColumnAsToken(EcmaMetadataCursor c, MetadataColumnIndex col_idx)
    {
        Func<uint, uint> decoder = columnTokenDecode[(int)col_idx];
        if (decoder == null)
        {
            throw new ArgumentOutOfRangeException(nameof(col_idx));
        }
        uint rawResult = GetColumnRaw(c, col_idx);
        uint result = decoder(rawResult);
        return result;
    }

    public System.ReadOnlySpan<byte> GetColumnAsUtf8(EcmaMetadataCursor c, MetadataColumnIndex col_idx)
    {
        if (columnTypes[(int)col_idx] != ColumnType.Utf8String)
            throw new ArgumentOutOfRangeException(nameof(col_idx));
        int initialOffset = (int)GetColumnRaw(c, col_idx);

        if (initialOffset == 0)
            return default(ReadOnlySpan<byte>);

        checked
        {
            ReadOnlySpan<byte> stringHeap = _ecmaMetadata.StringHeap.Span;
            int curOffset = initialOffset;
            while (stringHeap[curOffset] != '\0')
            {
                curOffset++;
            }
            return stringHeap.Slice(initialOffset, curOffset - initialOffset);
        }
    }

    public bool TryGetCursor(uint token, out EcmaMetadataCursor cursor)
    {
        cursor = default;
        MetadataTable table = TokenToTable(token);
        if (table == MetadataTable.Unused)
            return false;

        if (RowCount(table) < RidFromToken(token))
            return false;

        cursor.Rid = RidFromToken(token);
        cursor.TableData = _ecmaMetadata.Tables[(int)table];
        cursor.RowSize = rowSize[(int)table];
        cursor.Table = table;
        return true;
    }

    public bool TryGetCursorToFirstEntryInTable(MetadataTable table, out EcmaMetadataCursor cursor)
    {
        cursor = default;
        if (RowCount(table) > 0)
        {
            cursor.Rid = 1;
            cursor.TableData = _ecmaMetadata.Tables[(int)table];
            cursor.RowSize = rowSize[(int)table];
            cursor.Table = table;
            return true;
        }
        return false;
    }

    public bool TryFindRowFromCursor(EcmaMetadataCursor tableCursor, MetadataColumnIndex col_idx, uint searchToken, out EcmaMetadataCursor foundRow)
    {
        foundRow = tableCursor;

/*        if (_ecmaMetadata.Schema.IsSorted[(int)tableCursor.Table])
        {
            // TODO(cdac) implement sorted searching in metadata
        }
        else*/
        {
            while (foundRow.Rid <= RowCount(tableCursor.Table))
            {
                if (GetColumnAsToken(foundRow, col_idx) == searchToken)
                {
                    return true;
                }
                foundRow.Rid += 1;
            }
        }
        return false;
    }

    public EcmaMetadataCursor GetCursor(uint token)
    {
        if (!TryGetCursor(token, out EcmaMetadataCursor cursor))
        {
            throw new ArgumentOutOfRangeException(nameof(token));
        }
        return cursor;
    }

    public static uint GetToken(EcmaMetadataCursor c)
    {
        return CreateToken(c.Table, c.Rid);
    }

    private static MetadataTable ColumnTable(MetadataColumnIndex column)
    {
        return columnTable[(int)column];
    }

    public virtual string GetColumnAsUtf8String(EcmaMetadataCursor c, MetadataColumnIndex col_idx)
    {
        ReadOnlySpan<byte> utf8Data = GetColumnAsUtf8(c, col_idx);
        string str = string.Empty;
        if (utf8Data.Length > 0)
        {
            str = System.Text.Encoding.UTF8.GetString(utf8Data);
        }
        return str;
    }
}
