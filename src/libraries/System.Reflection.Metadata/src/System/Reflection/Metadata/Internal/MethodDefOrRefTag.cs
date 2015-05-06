// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335
{
    internal static class MethodDefOrRefTag
    {
        internal const int NumberOfBits = 1;
        internal const int LargeRowSize = 0x00000001 << (16 - NumberOfBits);
        internal const uint MethodDef = 0x00000000;
        internal const uint MemberRef = 0x00000001;
        internal const uint TagMask = 0x00000001;
        internal const TableMask TablesReferenced =
          TableMask.MethodDef
          | TableMask.MemberRef;
        internal const uint TagToTokenTypeByteVector = TokenTypeIds.MethodDef >> 24 | TokenTypeIds.MemberRef >> 16;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EntityHandle ConvertToHandle(uint methodDefOrRef)
        {
            uint tokenType = (TagToTokenTypeByteVector >> ((int)(methodDefOrRef & TagMask) << 3)) << TokenTypeIds.RowIdBitCount;
            uint rowId = (methodDefOrRef >> NumberOfBits);

            if ((rowId & ~TokenTypeIds.RIDMask) != 0)
            {
                Handle.ThrowInvalidCodedIndex();
            }

            return new EntityHandle(tokenType | rowId);
        }
    }
}