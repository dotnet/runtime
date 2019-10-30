// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335
{
    internal static class TypeOrMethodDefTag
    {
        internal const int NumberOfBits = 1;
        internal const int LargeRowSize = 0x00000001 << (16 - NumberOfBits);
        internal const uint TypeDef = 0x00000000;
        internal const uint MethodDef = 0x00000001;
        internal const uint TagMask = 0x0000001;
        internal const uint TagToTokenTypeByteVector = TokenTypeIds.TypeDef >> 24 | TokenTypeIds.MethodDef >> 16;
        internal const TableMask TablesReferenced =
          TableMask.TypeDef
          | TableMask.MethodDef;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EntityHandle ConvertToHandle(uint typeOrMethodDef)
        {
            uint tokenType = (TagToTokenTypeByteVector >> ((int)(typeOrMethodDef & TagMask) << 3)) << TokenTypeIds.RowIdBitCount;
            uint rowId = (typeOrMethodDef >> NumberOfBits);

            if ((rowId & ~TokenTypeIds.RIDMask) != 0)
            {
                Throw.InvalidCodedIndex();
            }

            return new EntityHandle(tokenType | rowId);
        }

        internal static uint ConvertTypeDefRowIdToTag(TypeDefinitionHandle typeDef)
        {
            return (uint)typeDef.RowId << NumberOfBits | TypeDef;
        }

        internal static uint ConvertMethodDefToTag(MethodDefinitionHandle methodDef)
        {
            return (uint)methodDef.RowId << NumberOfBits | MethodDef;
        }
    }
}
