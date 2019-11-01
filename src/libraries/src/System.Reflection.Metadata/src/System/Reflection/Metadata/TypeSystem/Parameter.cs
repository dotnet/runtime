// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public readonly struct Parameter
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal Parameter(MetadataReader reader, ParameterHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private ParameterHandle Handle
        {
            get { return ParameterHandle.FromRowId(_rowId); }
        }

        public ParameterAttributes Attributes
        {
            get
            {
                return _reader.ParamTable.GetFlags(Handle);
            }
        }

        public int SequenceNumber
        {
            get
            {
                return _reader.ParamTable.GetSequence(Handle);
            }
        }

        public StringHandle Name
        {
            get
            {
                return _reader.ParamTable.GetName(Handle);
            }
        }

        public ConstantHandle GetDefaultValue()
        {
            return _reader.ConstantTable.FindConstant(Handle);
        }

        public BlobHandle GetMarshallingDescriptor()
        {
            int marshalRowId = _reader.FieldMarshalTable.FindFieldMarshalRowId(Handle);
            if (marshalRowId == 0)
            {
                return default(BlobHandle);
            }

            return _reader.FieldMarshalTable.GetNativeType(marshalRowId);
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }
    }
}
