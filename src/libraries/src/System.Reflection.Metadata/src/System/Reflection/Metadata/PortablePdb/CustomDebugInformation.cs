// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct CustomDebugInformation
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal CustomDebugInformation(MetadataReader reader, CustomDebugInformationHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private CustomDebugInformationHandle Handle
        {
            get { return CustomDebugInformationHandle.FromRowId(_rowId); }
        }

        public EntityHandle Parent
        {
            get
            {
                return _reader.CustomDebugInformationTable.GetParent(Handle);
            }
        }

        public GuidHandle Kind
        {
            get
            {
                return _reader.CustomDebugInformationTable.GetKind(Handle);
            }
        }

        public BlobHandle Value
        {
            get
            {
                return _reader.CustomDebugInformationTable.GetValue(Handle);
            }
        }
    }
}