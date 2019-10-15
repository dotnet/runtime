// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Lexical scope within which a group of imports are available. Stored in debug metadata.
    /// </summary>
    /// <remarks>
    /// See https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#importscope-table-0x35
    /// </remarks>
    public readonly struct ImportScope
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

        private ImportScopeHandle Handle => ImportScopeHandle.FromRowId(_rowId);

        public ImportScopeHandle Parent => _reader.ImportScopeTable.GetParent(Handle);
        public BlobHandle ImportsBlob => _reader.ImportScopeTable.GetImports(Handle);

        public ImportDefinitionCollection GetImports()
        {
            return new ImportDefinitionCollection(_reader.BlobHeap.GetMemoryBlock(ImportsBlob));
        }
    }
}
