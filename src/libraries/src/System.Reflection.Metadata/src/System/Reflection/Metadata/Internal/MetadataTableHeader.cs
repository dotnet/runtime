// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335
{
    internal struct MetadataTableHeader
    {
        internal uint Reserved;
        internal byte MajorVersion;
        internal byte MinorVersion;
        internal HeapSizeFlag HeapSizeFlags;
        internal byte RowId;
        internal TableMask ValidTables;
        internal TableMask SortedTables;

        // Helper methods
        internal int GetNumberOfTablesPresent()
        {
            return BitArithmetic.CountBits((ulong)this.ValidTables);
        }
    }
}