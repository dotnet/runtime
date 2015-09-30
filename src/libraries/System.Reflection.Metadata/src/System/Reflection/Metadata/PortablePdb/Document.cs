// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct Document
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

        private DocumentHandle Handle
        {
            get { return DocumentHandle.FromRowId(_rowId); }
        }

        /// <summary>
        /// Returns Document Name Blob.
        /// </summary>
        public DocumentNameBlobHandle Name
        {
            get
            {
                return _reader.DocumentTable.GetName(Handle);
            }
        }

        public GuidHandle Language
        {
            get
            {
                return _reader.DocumentTable.GetLanguage(Handle);
            }
        }

        public GuidHandle HashAlgorithm
        {
            get
            {
                return _reader.DocumentTable.GetHashAlgorithm(Handle);
            }
        }

        public BlobHandle Hash
        {
            get
            {
                return _reader.DocumentTable.GetHash(Handle);
            }
        }
    }
}