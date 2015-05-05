// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335
{
    internal static class HasConstantTag
    {
        internal const int NumberOfBits = 2;
        internal const int LargeRowSize = 0x00000001 << (16 - NumberOfBits);
        internal const uint Field = 0x00000000;
        internal const uint Param = 0x00000001;
        internal const uint Property = 0x00000002;
        internal const uint TagMask = 0x00000003;
        internal const TableMask TablesReferenced =
          TableMask.Field
          | TableMask.Param
          | TableMask.Property;
        internal const uint TagToTokenTypeByteVector = TokenTypeIds.FieldDef >> 24 | TokenTypeIds.ParamDef >> 16 | TokenTypeIds.Property >> 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EntityHandle ConvertToHandle(uint hasConstant)
        {
            uint tokenType = (TagToTokenTypeByteVector >> ((int)(hasConstant & TagMask) << 3)) << TokenTypeIds.RowIdBitCount;
            uint rowId = (hasConstant >> NumberOfBits);

            if (tokenType == 0 || (rowId & ~TokenTypeIds.RIDMask) != 0)
            {
                Handle.ThrowInvalidCodedIndex();
            }

            return new EntityHandle(tokenType | rowId);
        }

        internal static uint ConvertToTag(EntityHandle token)
        {
            HandleKind tokenKind = token.Kind;
            uint rowId = (uint)token.RowId;
            if (tokenKind == HandleKind.FieldDefinition)
            {
                return rowId << NumberOfBits | Field;
            }
            else if (tokenKind == HandleKind.Parameter)
            {
                return rowId << NumberOfBits | Param;
            }
            else if (tokenKind == HandleKind.PropertyDefinition)
            {
                return rowId << NumberOfBits | Property;
            }

            return 0;
        }
    }
}