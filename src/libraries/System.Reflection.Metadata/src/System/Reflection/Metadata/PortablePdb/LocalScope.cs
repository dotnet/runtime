// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Scope of local variables and constants. Stored in debug metadata.
    /// </summary>
    /// <remarks>
    /// See https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#localscope-table-0x32.
    /// </remarks>
    public readonly struct LocalScope
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

        private LocalScopeHandle Handle => LocalScopeHandle.FromRowId(_rowId);

        public MethodDefinitionHandle Method => _reader.LocalScopeTable.GetMethod(_rowId);
        public ImportScopeHandle ImportScope => _reader.LocalScopeTable.GetImportScope(Handle);
        public int StartOffset => _reader.LocalScopeTable.GetStartOffset(_rowId);
        public int Length => _reader.LocalScopeTable.GetLength(_rowId);
        public int EndOffset => _reader.LocalScopeTable.GetEndOffset(_rowId);

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
