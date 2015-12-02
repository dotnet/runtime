// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata.Decoding;

namespace System.Reflection.Metadata
{
    public struct StandaloneSignature
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
        ///
        /// Decode using <see cref="DecodeMethodSignature"/> if <see cref="GetKind"/> returns <see cref="StandaloneSignatureKind.Method"/>
        /// Decode using <see cref="DecodeLocalSignature"/> if <see cref="GetKind"/> returns <see cref="StandaloneSignatureKind.LocalVariables"/>
        /// </summary>
        public BlobHandle Signature
        {
            get { return _reader.StandAloneSigTable.GetSignature(_rowId); }
        }

        public MethodSignature<TType> DecodeMethodSignature<TType>(ISignatureTypeProvider<TType> provider, SignatureDecoderOptions options = SignatureDecoderOptions.None)
        {
            var decoder = new SignatureDecoder<TType>(provider, _reader, options);
            var blobReader = _reader.GetBlobReader(Signature);
            return decoder.DecodeMethodSignature(ref blobReader);
        }

        public ImmutableArray<TType> DecodeLocalSignature<TType>(ISignatureTypeProvider<TType> provider, SignatureDecoderOptions options = SignatureDecoderOptions.None)
        {
            var decoder = new SignatureDecoder<TType>(provider, _reader, options);
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