// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct ModuleReference
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal ModuleReference(MetadataReader reader, ModuleReferenceHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private ModuleReferenceHandle Handle
        {
            get { return ModuleReferenceHandle.FromRowId(_rowId); }
        }

        public StringHandle Name
        {
            get { return _reader.ModuleRefTable.GetName(Handle); }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }
    }
}