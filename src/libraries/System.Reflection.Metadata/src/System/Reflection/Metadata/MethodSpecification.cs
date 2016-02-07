// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// </summary>
        public BlobHandle Signature
        {
            get
            {
                return _reader.MethodSpecTable.GetInstantiation(Handle);
            }
        }

#if FUTURE
        public ImmutableArray<TType> DecodeSignature<TType>(ISignatureTypeProvider<TType> provider, SignatureDecoderOptions options = SignatureDecoderOptions.None)
        {
            var decoder = new SignatureDecoder<TType>(provider, _reader, options);
            var blobReader = _reader.GetBlobReader(Signature);
            return decoder.DecodeMethodSpecificationSignature(ref blobReader);
        }
#endif

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }
    }
}
