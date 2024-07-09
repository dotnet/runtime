// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Metadata_1 : IMetadata
{
    private readonly Target _target;

    internal Metadata_1(Target target)
    {
        _target = target;
    }

    public MetadataReader GetMetadataReader(ModuleHandle module) => throw new System.NotImplementedException();

    private class SystemReflectionMetadataBasedReader : MetadataReader
    {
        private enum ColumnType
        {
            Unknown,
            TwoByteConstant,
            FourByteConstant,
            Utf8String,
            UserString,
            Blob,
            Token
        }
        private byte[] _bytes;
        private System.Reflection.Metadata.MetadataReader mr;
        private int[] tableRowCount;
        private int[] columnSize;
        private int[] columnOffset;
        private int stringHeapSize;
        private int userStringHeapSize;
        private int blobHeapSize;
        private static readonly MetadataTable[] columnTable = GetColumnTables();
        private static readonly ColumnType[] columnTypes = GetColumnTypes();
        private static readonly Func<uint, uint>[] columnTokenDecode = GetColumnTokenDecode();
        private static readonly MetadataTable[][] codedIndexDecoderRing = GetCodedIndexDecoderRing();

        private static readonly MetadataTable[] TypeOrMethodDef = { MetadataTable.TypeDef, MetadataTable.MethodDef };
        private static readonly MetadataTable[] TypeDefOrRef = { MetadataTable.TypeDef, MetadataTable.TypeRef, MetadataTable.TypeSpec };

        private static ColumnType[] GetColumnTypes()
        {
            ColumnType[] columnTypes = new ColumnType[(int)MetadataColumnIndex.Count];

            columnTypes[(int)MetadataColumnIndex.Assembly_HashAlgId] = ColumnType.FourByteConstant;
            columnTypes[(int)MetadataColumnIndex.Assembly_MajorVersion] = ColumnType.TwoByteConstant;
            columnTypes[(int)MetadataColumnIndex.Assembly_MinorVersion] = ColumnType.TwoByteConstant;
            columnTypes[(int)MetadataColumnIndex.Assembly_BuildNumber] = ColumnType.TwoByteConstant;
            columnTypes[(int)MetadataColumnIndex.Assembly_RevisionNumber] = ColumnType.TwoByteConstant;
            columnTypes[(int)MetadataColumnIndex.Assembly_Flags] = ColumnType.FourByteConstant;
            columnTypes[(int)MetadataColumnIndex.Assembly_PublicKey] = ColumnType.Blob;
            columnTypes[(int)MetadataColumnIndex.Assembly_Name] = ColumnType.Utf8String;
            columnTypes[(int)MetadataColumnIndex.Assembly_Culture] = ColumnType.Utf8String;

            columnTypes[(int)MetadataColumnIndex.GenericParam_Number] = ColumnType.TwoByteConstant;
            columnTypes[(int)MetadataColumnIndex.GenericParam_Flags] = ColumnType.TwoByteConstant;
            columnTypes[(int)MetadataColumnIndex.GenericParam_Owner] = ColumnType.Token;
            columnTypes[(int)MetadataColumnIndex.GenericParam_Name] = ColumnType.Utf8String;

            columnTypes[(int)MetadataColumnIndex.NestedClass_NestedClass] = ColumnType.Token;
            columnTypes[(int)MetadataColumnIndex.NestedClass_EnclosingClass] = ColumnType.Token;

            columnTypes[(int)MetadataColumnIndex.TypeDef_Flags] = ColumnType.FourByteConstant;
            columnTypes[(int)MetadataColumnIndex.TypeDef_TypeName] = ColumnType.Utf8String;
            columnTypes[(int)MetadataColumnIndex.TypeDef_TypeNamespace] = ColumnType.Utf8String;
            columnTypes[(int)MetadataColumnIndex.TypeDef_Extends] = ColumnType.Token;
            columnTypes[(int)MetadataColumnIndex.TypeDef_FieldList] = ColumnType.Token;
            columnTypes[(int)MetadataColumnIndex.TypeDef_MethodList] = ColumnType.Token;

            return columnTypes;
        }

        private static MetadataTable[] GetColumnTables()
        {
            MetadataTable[] metadataTables = new MetadataTable[(int)MetadataColumnIndex.Count];

            metadataTables[(int)MetadataColumnIndex.Assembly_HashAlgId] = MetadataTable.Assembly;
            metadataTables[(int)MetadataColumnIndex.Assembly_MajorVersion] = MetadataTable.Assembly;
            metadataTables[(int)MetadataColumnIndex.Assembly_MinorVersion] = MetadataTable.Assembly;
            metadataTables[(int)MetadataColumnIndex.Assembly_BuildNumber] = MetadataTable.Assembly;
            metadataTables[(int)MetadataColumnIndex.Assembly_RevisionNumber] = MetadataTable.Assembly;
            metadataTables[(int)MetadataColumnIndex.Assembly_Flags] = MetadataTable.Assembly;
            metadataTables[(int)MetadataColumnIndex.Assembly_PublicKey] = MetadataTable.Assembly;
            metadataTables[(int)MetadataColumnIndex.Assembly_Name] = MetadataTable.Assembly;
            metadataTables[(int)MetadataColumnIndex.Assembly_Culture] = MetadataTable.Assembly;

            metadataTables[(int)MetadataColumnIndex.GenericParam_Number] = MetadataTable.GenericParam;
            metadataTables[(int)MetadataColumnIndex.GenericParam_Flags] = MetadataTable.GenericParam;
            metadataTables[(int)MetadataColumnIndex.GenericParam_Owner] = MetadataTable.GenericParam;
            metadataTables[(int)MetadataColumnIndex.GenericParam_Name] = MetadataTable.GenericParam;

            metadataTables[(int)MetadataColumnIndex.NestedClass_NestedClass] = MetadataTable.NestedClass;
            metadataTables[(int)MetadataColumnIndex.NestedClass_EnclosingClass] = MetadataTable.NestedClass;

            metadataTables[(int)MetadataColumnIndex.TypeDef_Flags] = MetadataTable.TypeDef;
            metadataTables[(int)MetadataColumnIndex.TypeDef_TypeName] = MetadataTable.TypeDef;
            metadataTables[(int)MetadataColumnIndex.TypeDef_TypeNamespace] = MetadataTable.TypeDef;
            metadataTables[(int)MetadataColumnIndex.TypeDef_Extends] = MetadataTable.TypeDef;
            metadataTables[(int)MetadataColumnIndex.TypeDef_FieldList] = MetadataTable.TypeDef;
            metadataTables[(int)MetadataColumnIndex.TypeDef_MethodList] = MetadataTable.TypeDef;

            return metadataTables;
        }

        private static MetadataTable[][] GetCodedIndexDecoderRing()
        {
            MetadataTable[][] decoderRing = new MetadataTable[(int)MetadataColumnIndex.Count][];

            decoderRing[(int)MetadataColumnIndex.GenericParam_Owner] = TypeOrMethodDef;

            decoderRing[(int)MetadataColumnIndex.NestedClass_NestedClass] = new[] { MetadataTable.TypeDef };
            decoderRing[(int)MetadataColumnIndex.NestedClass_EnclosingClass] = new[] { MetadataTable.TypeDef };

            decoderRing[(int)MetadataColumnIndex.TypeDef_Extends] = TypeDefOrRef;
            decoderRing[(int)MetadataColumnIndex.TypeDef_FieldList] = new[] { MetadataTable.Field };
            decoderRing[(int)MetadataColumnIndex.TypeDef_MethodList] = new[] { MetadataTable.MethodDef };

            return decoderRing;
        }
        private static Func<uint, uint>[] GetColumnTokenDecode()
        {
            Func<uint, uint>[] columnTokenDecode = new Func<uint, uint>[(int)MetadataColumnIndex.Count];
            MetadataTable[][] decoderRing = GetCodedIndexDecoderRing();
            for (int i = 0; i < decoderRing.Length; i++)
            {
                if (decoderRing[i] != null)
                {
                    columnTokenDecode[i] = ComputeDecoder(decoderRing[i]);
                }
            }

            return columnTokenDecode;

            Func<uint, uint> ComputeDecoder(MetadataTable[] decoderData)
            {
                Func<uint, uint> result;

                if (decoderData.Length == 1)
                {
                    MetadataTable metadataTable = decoderData[0];
                    result = delegate (uint input) { return MetadataReader.CreateToken(metadataTable, input); };
                }
                else
                {
                    result = delegate (uint input) { return DecodeCodedIndex(input, decoderData); };
                }

                return result;
            }
        }


        public unsafe SystemReflectionMetadataBasedReader(Target target, TargetPointer metadataLocation, int metadataSize)
        {
            _bytes = GC.AllocateArray<byte>(metadataSize, pinned: true);
            target.ReadBuffer(metadataLocation, _bytes);
            fixed (byte* actualPtr = _bytes)
            {
                mr = new(actualPtr, _bytes.Length);
            }

            columnSize = new int[(int)MetadataColumnIndex.Count];
            columnOffset = new int[(int)MetadataColumnIndex.Count];

            tableRowCount = new int[(int)MetadataTable.MaxValue + 1];
            tableRowCount[(int)MetadataTable.TypeDef] = MetadataReaderExtensions.GetTableRowCount(mr, TableIndex.TypeDef);
            tableRowCount[(int)MetadataTable.MethodDef] = MetadataReaderExtensions.GetTableRowCount(mr, TableIndex.MethodDef);
            tableRowCount[(int)MetadataTable.Field] = MetadataReaderExtensions.GetTableRowCount(mr, TableIndex.Field);
            tableRowCount[(int)MetadataTable.TypeRef] = MetadataReaderExtensions.GetTableRowCount(mr, TableIndex.TypeRef);
            tableRowCount[(int)MetadataTable.TypeSpec] = MetadataReaderExtensions.GetTableRowCount(mr, TableIndex.TypeSpec);

            blobHeapSize = MetadataReaderExtensions.GetHeapSize(mr, HeapIndex.Blob);
            stringHeapSize = MetadataReaderExtensions.GetHeapSize(mr, HeapIndex.String);
            userStringHeapSize = MetadataReaderExtensions.GetHeapSize(mr, HeapIndex.UserString);

            ComputeColumnSizesAndOffsets();

            void ComputeColumnSizesAndOffsets()
            {
                MetadataTable currentTable = MetadataTable.Unused;
                MetadataColumnIndex? prevColumn = null;

                for (int i = 0; i < (int)MetadataColumnIndex.Count; i++)
                {
                    MetadataColumnIndex column = (MetadataColumnIndex)i;
                    if (currentTable != ColumnTable(column))
                    {
                        columnOffset[i] = 0;
                        prevColumn = null;
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
                        ColumnType.Utf8String => StringEncodingBytes,
                        ColumnType.UserString => UserStringEncodingBytes,
                        ColumnType.Blob => BlobEncodingBytes,
                        ColumnType.Token => CodedIndexEncodingBytes(codedIndexDecoderRing[i]),
                        _ => throw new System.Exception()
                    };
                }
            }

            int ComputeColumnEnd(MetadataColumnIndex column)
            {
                return ColumnOffset(column) + ColumnSize(column);
            }
        }

        private int ColumnSize(MetadataColumnIndex column)
        {
            return columnSize[(int)column];
        }

        private int ColumnOffset(MetadataColumnIndex column)
        {
            return columnOffset[(int)column];
        }

        private static MetadataTable ColumnTable(MetadataColumnIndex column)
        {
            return columnTable[(int)column];
        }

        private int StringEncodingBytes => stringHeapSize > 0xFFFF ? 4 : 2;
        private int UserStringEncodingBytes => userStringHeapSize > 0xFFFF ? 4 : 2;
        private int BlobEncodingBytes => blobHeapSize > 0xFFFF ? 4 : 2;

        private int RidEncodingBits(MetadataTable table)
        {
            if (table == MetadataTable.Unused)
                return 0;

            int countInTable = tableRowCount[(int)table];

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
            foreach (MetadataTable table in tablesEncoded)
            {
                if ((RidEncodingBits(table) + bitsForTableEncoding) > 16)
                    return 4;
            }
            return 2;
        }

        private static uint DecodeCodedIndex(uint input, ReadOnlySpan<MetadataTable> tablesEncoded)
        {
            uint encodingMask = BitOperations.RoundUpToPowerOf2((uint)tablesEncoded.Length) - 1;
            int bitsForTableEncoding = 32 - BitOperations.LeadingZeroCount(BitOperations.RoundUpToPowerOf2((uint)tablesEncoded.Length) - 1);
            MetadataTable table = tablesEncoded[(int)(input & encodingMask)];
            uint rid = input >> bitsForTableEncoding;
            return MetadataReader.CreateToken(table, rid);
        }

        private ReadOnlySpan<byte> TokenToRow(MetadataTable table, uint rid)
        {
            if (table == MetadataTable.Unused)
                throw new ArgumentOutOfRangeException(nameof(table));

            if (MetadataReader.RidFromToken(rid) <= 0)
                throw new ArgumentOutOfRangeException(nameof(rid));

            TableIndex tableIndex = (TableIndex)table;

            int offset = MetadataReaderExtensions.GetTableMetadataOffset(mr, tableIndex);
            int tableRowSize = MetadataReaderExtensions.GetTableRowSize(mr, tableIndex);
            int tableEntryCount = MetadataReaderExtensions.GetTableRowCount(mr, tableIndex);

            ReadOnlySpan<byte> tableBytes = _bytes.AsSpan().Slice(offset, tableRowSize * tableEntryCount);
            return tableBytes.Slice(0, tableRowSize * (int)(rid - 1));
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
        private bool TryReadTableEntry(ReadOnlySpan<byte> bytes, MetadataColumnIndex column, out uint value)
        {
            if (ColumnOffset(column) == 0)
                throw new ArgumentOutOfRangeException(nameof(column));

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

        private uint GetColumnRaw(MetadataCursor c, MetadataColumnIndex col_idx)
        {
            MetadataTable tableEnumValue = MetadataReader.TokenToTable((uint)c.reserved1);
            if (tableEnumValue != ColumnTable(col_idx))
                throw new ArgumentOutOfRangeException(nameof(col_idx));

            ReadOnlySpan<byte> row = TokenToRow(tableEnumValue, MetadataReader.RidFromToken((uint)c.reserved1));
            if (!TryReadTableEntry(row, col_idx, out uint rawResult))
            {
                throw new ArgumentOutOfRangeException(nameof(col_idx));
            }
            return rawResult;
        }

        public override System.ReadOnlySpan<byte> GetColumnAsBlob(MetadataCursor c, MetadataColumnIndex col_idx)
        {
            throw new NotImplementedException();
        }
        public override uint GetColumnAsConstant(MetadataCursor c, MetadataColumnIndex col_idx)
        {
            if (columnTypes[(int)col_idx] != ColumnType.TwoByteConstant && columnTypes[(int)col_idx] != ColumnType.FourByteConstant)
                throw new ArgumentOutOfRangeException(nameof(col_idx));
            return GetColumnRaw(c, col_idx);
        }

        public override MetadataCursor GetColumnAsCursor(MetadataCursor c, MetadataColumnIndex col_idx) => throw new System.NotImplementedException();
        public override System.Guid GetColumnAsGuid(MetadataCursor c, MetadataColumnIndex col_idx) => throw new System.NotImplementedException();
        public override void GetColumnAsRange(MetadataCursor c, MetadataColumnIndex col_idx, out MetadataCursor cursor, out int count) => throw new System.NotImplementedException();
        public override uint GetColumnAsToken(MetadataCursor c, MetadataColumnIndex col_idx)
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
        public override System.ReadOnlySpan<char> GetColumnAsUserstring(MetadataCursor c, MetadataColumnIndex col_idx) => throw new System.NotImplementedException();

        public override System.ReadOnlySpan<byte> GetColumnAsUtf8(MetadataCursor c, MetadataColumnIndex col_idx)
        {
            if (columnTypes[(int)col_idx] != ColumnType.Utf8String)
                throw new ArgumentOutOfRangeException(nameof(col_idx));
            uint rawResult = GetColumnRaw(c, col_idx);

            if (rawResult == 0)
                return default(ReadOnlySpan<byte>);

            checked
            {
                int initialOffset = MetadataReaderExtensions.GetHeapMetadataOffset(mr, HeapIndex.String);
                initialOffset += (int)rawResult;
                int curOffset = initialOffset;
                while (_bytes[curOffset] != '\0')
                {
                    curOffset++;
                }
                return _bytes.AsSpan().Slice(initialOffset, curOffset - initialOffset);
            }
        }
        public override MetadataCursor GetCursor(uint token)
        {
            if (!TryGetCursor(token, out MetadataCursor cursor))
            {
                throw new ArgumentOutOfRangeException(nameof(token));
            }
            return cursor;
        }
        public override uint GetToken(MetadataCursor c)
        {
            return (uint)c.reserved1;
        }
        public override bool TryFindRowFromCursor(MetadataCursor begin, MetadataColumnIndex col_idx, uint value, out MetadataCursor foundCursor) => throw new System.NotImplementedException();
        public override bool TryGetCursor(uint token, out MetadataCursor cursor)
        {
            cursor = default;
            MetadataTable tableEnumValue = MetadataReader.TokenToTable(token);
            if (tableEnumValue == MetadataTable.Unused)
                return false;

            TableIndex table = (TableIndex)tableEnumValue;

            if (MetadataReaderExtensions.GetTableRowCount(mr, table) < MetadataReader.RidFromToken(token))
                return false;

            cursor.reserved1 = token;
            cursor.reserved2 = this;
            return true;
        }
        public override bool TryGetCursorToFirstEntryInTable(MetadataTable table, out MetadataCursor cursor)
        {
            cursor = default;
            if (MetadataReaderExtensions.GetTableRowCount(mr, (TableIndex)table) > 0)
            {
                cursor.reserved1 = MetadataReader.CreateToken(table, 1);
                cursor.reserved2 = this;
                return true;
            }
            return false;
        }
    }
}
