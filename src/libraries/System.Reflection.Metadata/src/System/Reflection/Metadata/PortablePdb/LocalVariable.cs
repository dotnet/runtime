// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Local variable. Stored in debug metadata.
    /// </summary>
    /// <remarks>
    /// See https://github.com/dotnet/runtime/blob/main/src/libraries/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#localvariable-table-0x33.
    /// </remarks>
    public readonly struct LocalVariable
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal LocalVariable(MetadataReader reader, LocalVariableHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private LocalVariableHandle Handle => LocalVariableHandle.FromRowId(_rowId);

        public LocalVariableAttributes Attributes => _reader.LocalVariableTable.GetAttributes(Handle);
        public int Index => _reader.LocalVariableTable.GetIndex(Handle);
        public StringHandle Name => _reader.LocalVariableTable.GetName(Handle);
    }
}
