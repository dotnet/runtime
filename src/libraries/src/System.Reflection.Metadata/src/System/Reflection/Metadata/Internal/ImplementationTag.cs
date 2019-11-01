// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335
{
    internal static class ImplementationTag
    {
        internal const int NumberOfBits = 2;
        internal const int LargeRowSize = 0x00000001 << (16 - NumberOfBits);
        internal const uint File = 0x00000000;
        internal const uint AssemblyRef = 0x00000001;
        internal const uint ExportedType = 0x00000002;
        internal const uint TagMask = 0x00000003;
        internal const uint TagToTokenTypeByteVector = TokenTypeIds.File >> 24 | TokenTypeIds.AssemblyRef >> 16 | TokenTypeIds.ExportedType >> 8;
        internal const TableMask TablesReferenced =
          TableMask.File
          | TableMask.AssemblyRef
          | TableMask.ExportedType;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EntityHandle ConvertToHandle(uint implementation)
        {
            uint tokenType = (TagToTokenTypeByteVector >> ((int)(implementation & TagMask) << 3)) << TokenTypeIds.RowIdBitCount;
            uint rowId = (implementation >> NumberOfBits);

            if (tokenType == 0 || (rowId & ~TokenTypeIds.RIDMask) != 0)
            {
                Throw.InvalidCodedIndex();
            }

            return new EntityHandle(tokenType | rowId);
        }
    }
}
