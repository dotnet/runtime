// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Source document in debug metadata. 
    /// </summary>
    /// <remarks>
    /// See also https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#document-table-0x30.
    /// </remarks>
    public readonly struct Document
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal Document(MetadataReader reader, DocumentHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private DocumentHandle Handle => DocumentHandle.FromRowId(_rowId);

        /// <summary>
        /// Returns Document Name Blob.
        /// </summary>
        public DocumentNameBlobHandle Name => _reader.DocumentTable.GetName(Handle);

        /// <summary>
        /// Source code language (C#, VB, F#, etc.)
        /// </summary>
        public GuidHandle Language => _reader.DocumentTable.GetLanguage(Handle);

        /// <summary>
        /// Hash algorithm used to calculate <see cref="Hash"/> (SHA1, SHA256, etc.)
        /// </summary>
        public GuidHandle HashAlgorithm => _reader.DocumentTable.GetHashAlgorithm(Handle);

        /// <summary>
        /// Document content hash.
        /// </summary>
        /// <remarks>
        /// <see cref="HashAlgorithm"/> determines the algorithm used to produce this hash.
        /// The source document is hashed in its binary form as stored in the file. 
        /// </remarks>
        public BlobHandle Hash => _reader.DocumentTable.GetHash(Handle);
    }
}
