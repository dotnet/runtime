// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct ImportScope
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal ImportScope(MetadataReader reader, ImportScopeHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private ImportScopeHandle Handle
        {
            get { return ImportScopeHandle.FromRowId(_rowId); }
        }

        public ImportScopeHandle Parent
        {
            get
            {
                return _reader.ImportScopeTable.GetParent(Handle);
            }
        }

        public BlobHandle Imports
        {
            get
            {
                return _reader.ImportScopeTable.GetImports(Handle);
            }
        }
    }
}