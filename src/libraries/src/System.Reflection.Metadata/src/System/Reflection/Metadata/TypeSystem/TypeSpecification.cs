// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public readonly struct TypeSpecification
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal TypeSpecification(MetadataReader reader, TypeSpecificationHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private TypeSpecificationHandle Handle
        {
            get { return TypeSpecificationHandle.FromRowId(_rowId); }
        }

        public BlobHandle Signature
        {
            get { return _reader.TypeSpecTable.GetSignature(Handle); }
        }

        public TType DecodeSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
        {
            var decoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
            var blobReader = _reader.GetBlobReader(Signature);
            return decoder.DecodeType(ref blobReader);
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }
    }
}
