// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata.Decoding;

namespace System.Reflection.Metadata
{
    public struct MethodSpecification
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal MethodSpecification(MetadataReader reader, MethodSpecificationHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private MethodSpecificationHandle Handle
        {
            get { return MethodSpecificationHandle.FromRowId(_rowId); }
        }

        /// <summary>
        /// MethodDef or MemberRef handle specifying to which generic method this <see cref="MethodSpecification"/> refers,
        /// that is which generic method is it an instantiation of.
        /// </summary>
        public EntityHandle Method
        {
            get
            {
                return _reader.MethodSpecTable.GetMethod(Handle);
            }
        }

        /// <summary>
        /// Gets a handle to the signature blob.
        ///
        /// Decode using <see cref="DecodeSignature"/>.
        /// </summary>
        public BlobHandle Signature
        {
            get
            {
                return _reader.MethodSpecTable.GetInstantiation(Handle);
            }
        }

        public ImmutableArray<TType> DecodeSignature<TType>(ISignatureTypeProvider<TType> provider, SignatureDecoderOptions options = SignatureDecoderOptions.None)
        {
            var decoder = new SignatureDecoder<TType>(provider, _reader, options);
            var blobReader = _reader.GetBlobReader(Signature);
            return decoder.DecodeMethodSpecificationSignature(ref blobReader);
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }
    }
}