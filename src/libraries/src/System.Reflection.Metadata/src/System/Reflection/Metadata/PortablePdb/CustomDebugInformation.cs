// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public readonly struct CustomDebugInformation
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

        private CustomDebugInformationHandle Handle => CustomDebugInformationHandle.FromRowId(_rowId);

        public EntityHandle Parent => _reader.CustomDebugInformationTable.GetParent(Handle);
        public GuidHandle Kind => _reader.CustomDebugInformationTable.GetKind(Handle);
        public BlobHandle Value => _reader.CustomDebugInformationTable.GetValue(Handle);
    }
}
