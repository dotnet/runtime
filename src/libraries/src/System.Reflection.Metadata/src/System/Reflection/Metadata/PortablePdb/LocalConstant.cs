// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Local constant. Stored in debug metadata.
    /// </summary>
    /// <remarks>
    /// See https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#localconstant-table-0x34.
    /// </remarks>
    public readonly struct LocalConstant
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal LocalConstant(MetadataReader reader, LocalConstantHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private LocalConstantHandle Handle => LocalConstantHandle.FromRowId(_rowId);

        public StringHandle Name => _reader.LocalConstantTable.GetName(Handle);

        /// <summary>
        /// The constant signature.
        /// </summary>
        public BlobHandle Signature => _reader.LocalConstantTable.GetSignature(Handle);
    }
}
