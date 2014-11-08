// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct StandaloneSignature
    {
        private readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint rowId;

        internal StandaloneSignature(MetadataReader reader, StandaloneSignatureHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            this.reader = reader;
            this.rowId = handle.RowId;
        }

        private StandaloneSignatureHandle Handle
        {
            get { return StandaloneSignatureHandle.FromRowId(rowId); }
        }

        public BlobHandle Signature
        {
            get { return reader.StandAloneSigTable.GetSignature(rowId); }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(reader, Handle);
        }
    }
}