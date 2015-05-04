// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct StandaloneSignature
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal StandaloneSignature(MetadataReader reader, StandaloneSignatureHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private StandaloneSignatureHandle Handle
        {
            get { return StandaloneSignatureHandle.FromRowId(_rowId); }
        }

        public BlobHandle Signature
        {
            get { return _reader.StandAloneSigTable.GetSignature(_rowId); }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }
    }
}