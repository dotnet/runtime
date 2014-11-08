// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public struct DeclarativeSecurityAttribute
    {
        private readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint rowId;

        internal DeclarativeSecurityAttribute(MetadataReader reader, uint rowId)
        {
            Debug.Assert(reader != null);
            Debug.Assert(rowId != 0);

            this.reader = reader;
            this.rowId = rowId;
        }

        private uint RowId
        {
            get { return rowId & TokenTypeIds.RIDMask; }
        }

        private DeclarativeSecurityAttributeHandle Handle
        {
            get { return DeclarativeSecurityAttributeHandle.FromRowId(RowId); }
        }

        private MethodDefTreatment Treatment
        {
            get { return (MethodDefTreatment)(rowId >> TokenTypeIds.RowIdBitCount); }
        }

        public DeclarativeSecurityAction Action
        {
            get
            {
                return reader.DeclSecurityTable.GetAction(rowId);
            }
        }

        public Handle Parent
        {
            get
            {
                return reader.DeclSecurityTable.GetParent(rowId);
            }
        }

        public BlobHandle PermissionSet
        {
            get
            {
                return reader.DeclSecurityTable.GetPermissionSet(rowId);
            }
        }
    }
}