// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335
{
    internal static class HasFieldMarshalTag
    {
        internal const int NumberOfBits = 1;
        internal const int LargeRowSize = 0x00000001 << (16 - NumberOfBits);
        internal const uint Field = 0x00000000;
        internal const uint Param = 0x00000001;
        internal const uint TagMask = 0x00000001;
        internal const TableMask TablesReferenced =
          TableMask.Field
          | TableMask.Param;
        internal const uint TagToTokenTypeByteVector = TokenTypeIds.FieldDef >> 24 | TokenTypeIds.ParamDef >> 16;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EntityHandle ConvertToHandle(uint hasFieldMarshal)
        {
            uint tokenType = (TagToTokenTypeByteVector >> ((int)(hasFieldMarshal & TagMask) << 3)) << TokenTypeIds.RowIdBitCount;
            uint rowId = (hasFieldMarshal >> NumberOfBits);

            if ((rowId & ~TokenTypeIds.RIDMask) != 0)
            {
                Throw.InvalidCodedIndex();
            }

            return new EntityHandle(tokenType | rowId);
        }

        internal static uint ConvertToTag(EntityHandle handle)
        {
            if (handle.Type == TokenTypeIds.FieldDef)
            {
                return (uint)handle.RowId << NumberOfBits | Field;
            }
            else if (handle.Type == TokenTypeIds.ParamDef)
            {
                return (uint)handle.RowId << NumberOfBits | Param;
            }

            return 0;
        }
    }
}
