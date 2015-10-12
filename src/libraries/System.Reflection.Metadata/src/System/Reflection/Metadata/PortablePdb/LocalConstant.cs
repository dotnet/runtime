// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct LocalConstant
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

        private LocalConstantHandle Handle
        {
            get { return LocalConstantHandle.FromRowId(_rowId); }
        }

        public StringHandle Name
        {
            get
            {
                return _reader.LocalConstantTable.GetName(Handle);
            }
        }

        /// <summary>
        /// The constant signature.
        /// </summary>
        public BlobHandle Signature
        {
            get
            {
                return _reader.LocalConstantTable.GetSignature(Handle);
            }
        }
    }
}
