// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public readonly struct DeclarativeSecurityAttribute
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal DeclarativeSecurityAttribute(MetadataReader reader, int rowId)
        {
            Debug.Assert(reader != null);
            Debug.Assert(rowId != 0);

            _reader = reader;
            _rowId = rowId;
        }

        public DeclarativeSecurityAction Action
        {
            get
            {
                return _reader.DeclSecurityTable.GetAction(_rowId);
            }
        }

        public EntityHandle Parent
        {
            get
            {
                return _reader.DeclSecurityTable.GetParent(_rowId);
            }
        }

        public BlobHandle PermissionSet
        {
            get
            {
                return _reader.DeclSecurityTable.GetPermissionSet(_rowId);
            }
        }
    }
}
