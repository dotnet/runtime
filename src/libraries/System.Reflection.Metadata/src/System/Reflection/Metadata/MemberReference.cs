// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Metadata.Decoding;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public struct MemberReference
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint _treatmentAndRowId;

        internal MemberReference(MetadataReader reader, uint treatmentAndRowId)
        {
            Debug.Assert(reader != null);
            Debug.Assert(treatmentAndRowId != 0);

            _reader = reader;
            _treatmentAndRowId = treatmentAndRowId;
        }

        private int RowId
        {
            get { return (int)(_treatmentAndRowId & TokenTypeIds.RIDMask); }
        }

        private MemberRefTreatment Treatment
        {
            get { return (MemberRefTreatment)(_treatmentAndRowId >> TokenTypeIds.RowIdBitCount); }
        }

        private MemberReferenceHandle Handle
        {
            get { return MemberReferenceHandle.FromRowId(RowId); }
        }

        /// <summary>
        /// MethodDef, ModuleRef,TypeDef, TypeRef, or TypeSpec handle.
        /// </summary>
        public EntityHandle Parent
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.MemberRefTable.GetClass(Handle);
                }

                return GetProjectedParent();
            }
        }

        public StringHandle Name
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.MemberRefTable.GetName(Handle);
                }

                return GetProjectedName();
            }
        }

        /// <summary>
        /// Gets a handle to the signature blob.
        /// </summary>
        public BlobHandle Signature
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.MemberRefTable.GetSignature(Handle);
                }

                return GetProjectedSignature();
            }
        }

#if FUTURE
        public
#else
        internal
#endif
        TType DecodeFieldSignature<TType>(ISignatureTypeProvider<TType> provider, SignatureDecoderOptions options = SignatureDecoderOptions.None)
        {
            var decoder = new SignatureDecoder<TType>(provider, _reader, options);
            var blobReader = _reader.GetBlobReader(Signature);
            return decoder.DecodeFieldSignature(ref blobReader);
        }

#if FUTURE
        public 
#else
        internal
#endif
        MethodSignature<TType> DecodeMethodSignature<TType>(ISignatureTypeProvider<TType> provider, SignatureDecoderOptions options = SignatureDecoderOptions.None)
        {
            var decoder = new SignatureDecoder<TType>(provider, _reader, options);
            var blobReader = _reader.GetBlobReader(Signature);
            return decoder.DecodeMethodSignature(ref blobReader);
        }

        /// <summary>
        /// Determines if the member reference is to a method or field.
        /// </summary>
        /// <exception cref="BadImageFormatException">The member reference signature is invalid.</exception>
        public MemberReferenceKind GetKind()
        {
            BlobReader blobReader = _reader.GetBlobReader(this.Signature);
            SignatureHeader header = blobReader.ReadSignatureHeader();

            switch (header.Kind)
            {
                case SignatureKind.Method:
                    return MemberReferenceKind.Method;

                case SignatureKind.Field:
                    return MemberReferenceKind.Field;

                default:
                    throw new BadImageFormatException();
            }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }

#region Projections

        private EntityHandle GetProjectedParent()
        {
            // no change
            return _reader.MemberRefTable.GetClass(Handle);
        }

        private StringHandle GetProjectedName()
        {
            if (Treatment == MemberRefTreatment.Dispose)
            {
                return StringHandle.FromVirtualIndex(StringHandle.VirtualIndex.Dispose);
            }

            return _reader.MemberRefTable.GetName(Handle);
        }

        private BlobHandle GetProjectedSignature()
        {
            // no change
            return _reader.MemberRefTable.GetSignature(Handle);
        }
#endregion
    }
}
