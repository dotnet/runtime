// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335
{
    internal static class CustomAttributeTypeTag
    {
        internal const int NumberOfBits = 3;
        internal const int LargeRowSize = 0x00000001 << (16 - NumberOfBits);
        internal const uint MethodDef = 0x00000002;
        internal const uint MemberRef = 0x00000003;
        internal const uint TagMask = 0x0000007;
        internal const ulong TagToTokenTypeByteVector = TokenTypeIds.MethodDef >> 8 | TokenTypeIds.MemberRef;
        internal const TableMask TablesReferenced =
          TableMask.MethodDef
          | TableMask.MemberRef;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EntityHandle ConvertToHandle(uint customAttributeType)
        {
            uint tokenType = unchecked((uint)(TagToTokenTypeByteVector >> ((int)(customAttributeType & TagMask) << 3)) << TokenTypeIds.RowIdBitCount);
            uint rowId = (customAttributeType >> NumberOfBits);

            if (tokenType == 0 || (rowId & ~TokenTypeIds.RIDMask) != 0)
            {
                Throw.InvalidCodedIndex();
            }

            return new EntityHandle(tokenType | rowId);
        }
    }
}
