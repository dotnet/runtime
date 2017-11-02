// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public readonly struct StandaloneSignature
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

        /// <summary>
        /// Gets a handle to the signature blob.
        /// </summary>
        public BlobHandle Signature
        {
            get { return _reader.StandAloneSigTable.GetSignature(_rowId); }
        }

        public MethodSignature<TType> DecodeMethodSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
        {
            var decoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
            var blobReader = _reader.GetBlobReader(Signature);
            return decoder.DecodeMethodSignature(ref blobReader);
        }

        public ImmutableArray<TType> DecodeLocalSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
        {
            var decoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
            var blobReader = _reader.GetBlobReader(Signature);
            return decoder.DecodeLocalSignature(ref blobReader);
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }

        /// <summary>
        /// Determines the kind of signature, which can be <see cref="SignatureKind.Method"/> or <see cref="SignatureKind.LocalVariables"/>
        /// </summary>
        /// <exception cref="BadImageFormatException">The signature is invalid.</exception>
        public StandaloneSignatureKind GetKind()
        {
            BlobReader blobReader = _reader.GetBlobReader(this.Signature);
            SignatureHeader header = blobReader.ReadSignatureHeader();

            switch (header.Kind)
            {
                case SignatureKind.Method:
                    return StandaloneSignatureKind.Method;

                case SignatureKind.LocalVariables:
                    return StandaloneSignatureKind.LocalVariables;

                default:
                    throw new BadImageFormatException();
            }
        }
    }
}
