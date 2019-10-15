// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public readonly struct AssemblyFile
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal AssemblyFile(MetadataReader reader, AssemblyFileHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private AssemblyFileHandle Handle
        {
            get { return AssemblyFileHandle.FromRowId(_rowId); }
        }

        /// <summary>
        /// True if the file contains metadata.
        /// </summary>
        /// <remarks>
        /// Corresponds to Flags field of File table in ECMA-335 Standard.
        /// </remarks>
        public bool ContainsMetadata
        {
            get { return _reader.FileTable.GetFlags(Handle) == 0; }
        }

        /// <summary>
        /// File name with extension.
        /// </summary>
        /// <remarks>
        /// Corresponds to Name field of File table in ECMA-335 Standard.
        /// </remarks>
        public StringHandle Name
        {
            get { return _reader.FileTable.GetName(Handle); }
        }

        /// <summary>
        /// Hash value of the file content calculated using <see cref="AssemblyDefinition.HashAlgorithm"/>.
        /// </summary>
        /// <remarks>
        /// Corresponds to HashValue field of File table in ECMA-335 Standard.
        /// </remarks>
        public BlobHandle HashValue
        {
            get { return _reader.FileTable.GetHashValue(Handle); }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }
    }
}
