// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335
{
    internal static class HasSemanticsTag
    {
        internal const int NumberOfBits = 1;
        internal const int LargeRowSize = 0x00000001 << (16 - NumberOfBits);
        internal const uint Event = 0x00000000;
        internal const uint Property = 0x00000001;
        internal const uint TagMask = 0x00000001;
        internal const TableMask TablesReferenced =
          TableMask.Event
          | TableMask.Property;
        internal const uint TagToTokenTypeByteVector = TokenTypeIds.Event >> 24 | TokenTypeIds.Property >> 16;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EntityHandle ConvertToHandle(uint hasSemantic)
        {
            uint tokenType = (TagToTokenTypeByteVector >> ((int)(hasSemantic & TagMask) << 3)) << TokenTypeIds.RowIdBitCount;
            uint rowId = (hasSemantic >> NumberOfBits);

            if ((rowId & ~TokenTypeIds.RIDMask) != 0)
            {
                Handle.ThrowInvalidCodedIndex();
            }

            return new EntityHandle(tokenType | rowId);
        }

        internal static uint ConvertEventHandleToTag(EventDefinitionHandle eventDef)
        {
            return (uint)eventDef.RowId << NumberOfBits | Event;
        }

        internal static uint ConvertPropertyHandleToTag(PropertyDefinitionHandle propertyDef)
        {
            return (uint)propertyDef.RowId << NumberOfBits | Property;
        }
    }
}