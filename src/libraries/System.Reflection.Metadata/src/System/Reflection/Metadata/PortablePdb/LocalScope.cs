// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct LocalScope
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal LocalScope(MetadataReader reader, LocalScopeHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private LocalScopeHandle Handle
        {
            get { return LocalScopeHandle.FromRowId(_rowId); }
        }

        public MethodDefinitionHandle Method
        {
            get
            {
                return _reader.LocalScopeTable.GetMethod(_rowId);
            }
        }

        public ImportScopeHandle ImportScope
        {
            get
            {
                return _reader.LocalScopeTable.GetImportScope(Handle);
            }
        }

        public int StartOffset
        {
            get
            {
                return _reader.LocalScopeTable.GetStartOffset(_rowId);
            }
        }

        public int Length
        {
            get
            {
                return _reader.LocalScopeTable.GetLength(_rowId);
            }
        }

        public int EndOffset
        {
            get
            {
                return _reader.LocalScopeTable.GetEndOffset(_rowId);
            }
        }

        public LocalVariableHandleCollection GetLocalVariables()
        {
            return new LocalVariableHandleCollection(_reader, Handle);
        }

        public LocalConstantHandleCollection GetLocalConstants()
        {
            return new LocalConstantHandleCollection(_reader, Handle);
        }

        public LocalScopeHandleCollection.ChildrenEnumerator GetChildren()
        {
            return new LocalScopeHandleCollection.ChildrenEnumerator(_reader, _rowId);
        }
    }
}