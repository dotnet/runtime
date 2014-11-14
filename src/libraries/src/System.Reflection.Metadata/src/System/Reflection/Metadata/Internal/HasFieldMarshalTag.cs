// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335
{
    internal static class HasFieldMarshalTag
    {
        internal const int NumberOfBits = 1;
        internal const uint LargeRowSize = 0x00000001 << (16 - NumberOfBits);
        internal const uint Field = 0x00000000;
        internal const uint Param = 0x00000001;
        internal const uint TagMask = 0x00000001;
        internal const TableMask TablesReferenced =
          TableMask.Field
          | TableMask.Param;
        internal const uint TagToTokenTypeByteVector = TokenTypeIds.FieldDef >> 24 | TokenTypeIds.ParamDef >> 16;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Handle ConvertToToken(uint hasFieldMarshal)
        {
            uint tokenType = (TagToTokenTypeByteVector >> ((int)(hasFieldMarshal & TagMask) << 3)) << TokenTypeIds.RowIdBitCount;
            uint rowId = (hasFieldMarshal >> NumberOfBits);

            if ((rowId & ~TokenTypeIds.RIDMask) != 0)
            {
                Handle.ThrowInvalidCodedIndex();
            }

            return new Handle(tokenType | rowId);
        }

        internal static uint ConvertToTag(Handle handle)
        {
            if (handle.TokenType == TokenTypeIds.FieldDef)
            {
                return handle.RowId << NumberOfBits | Field;
            }
            else if (handle.TokenType == TokenTypeIds.ParamDef)
            {
                return handle.RowId << NumberOfBits | Param;
            }

            return 0;
        }
    }
}