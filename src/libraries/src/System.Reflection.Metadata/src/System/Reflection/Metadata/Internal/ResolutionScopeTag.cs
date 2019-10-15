// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335
{
    internal static class ResolutionScopeTag
    {
        internal const int NumberOfBits = 2;
        internal const int LargeRowSize = 0x00000001 << (16 - NumberOfBits);
        internal const uint Module = 0x00000000;
        internal const uint ModuleRef = 0x00000001;
        internal const uint AssemblyRef = 0x00000002;
        internal const uint TypeRef = 0x00000003;
        internal const uint TagMask = 0x00000003;
        internal const uint TagToTokenTypeByteVector = TokenTypeIds.Module >> 24 | TokenTypeIds.ModuleRef >> 16 | TokenTypeIds.AssemblyRef >> 8 | TokenTypeIds.TypeRef;
        internal const TableMask TablesReferenced =
          TableMask.Module
          | TableMask.ModuleRef
          | TableMask.AssemblyRef
          | TableMask.TypeRef;

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        internal static EntityHandle ConvertToHandle(uint resolutionScope)
        {
            uint tokenType = (TagToTokenTypeByteVector >> ((int)(resolutionScope & TagMask) << 3)) << TokenTypeIds.RowIdBitCount;
            uint rowId = (resolutionScope >> NumberOfBits);

            if ((rowId & ~TokenTypeIds.RIDMask) != 0)
            {
                Throw.InvalidCodedIndex();
            }

            return new EntityHandle(tokenType | rowId);
        }
    }
}
