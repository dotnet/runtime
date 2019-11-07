// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335
{
    internal static class MemberForwardedTag
    {
        internal const int NumberOfBits = 1;
        internal const int LargeRowSize = 0x00000001 << (16 - NumberOfBits);
        internal const uint Field = 0x00000000;
        internal const uint MethodDef = 0x00000001;
        internal const uint TagMask = 0x00000001;
        internal const TableMask TablesReferenced =
          TableMask.Field
          | TableMask.MethodDef;
        internal const uint TagToTokenTypeByteVector = TokenTypeIds.FieldDef >> 24 | TokenTypeIds.MethodDef >> 16;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EntityHandle ConvertToHandle(uint memberForwarded)
        {
            uint tokenType = (TagToTokenTypeByteVector >> ((int)(memberForwarded & TagMask) << 3)) << TokenTypeIds.RowIdBitCount;
            uint rowId = (memberForwarded >> NumberOfBits);

            if ((rowId & ~TokenTypeIds.RIDMask) != 0)
            {
                Throw.InvalidCodedIndex();
            }

            return new EntityHandle(tokenType | rowId);
        }

        internal static uint ConvertMethodDefToTag(MethodDefinitionHandle methodDef)
        {
            return (uint)methodDef.RowId << NumberOfBits | MethodDef;
        }
    }
}
