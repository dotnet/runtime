// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public struct MemberReference
    {
        private readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint treatmentAndRowId;

        internal MemberReference(MetadataReader reader, uint treatmentAndRowId)
        {
            Debug.Assert(reader != null);
            Debug.Assert(treatmentAndRowId != 0);

            this.reader = reader;
            this.treatmentAndRowId = treatmentAndRowId;
        }

        private uint RowId
        {
            get { return treatmentAndRowId & TokenTypeIds.RIDMask; }
        }

        private MemberRefTreatment Treatment
        {
            get { return (MemberRefTreatment)(treatmentAndRowId >> TokenTypeIds.RowIdBitCount); }
        }

        private MemberReferenceHandle Handle
        {
            get { return MemberReferenceHandle.FromRowId(RowId); }
        }

        /// <summary>
        /// MethodDef, ModuleRef,TypeDef, TypeRef, or TypeSpec handle.
        /// </summary>
        public Handle Parent
        {
            get
            {
                if (Treatment == 0)
                {
                    return reader.MemberRefTable.GetClass(Handle);
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
                    return reader.MemberRefTable.GetName(Handle);
                }

                return GetProjectedName();
            }
        }

        public BlobHandle Signature
        {
            get
            {
                if (Treatment == 0)
                {
                    return reader.MemberRefTable.GetSignature(Handle);
                }

                return GetProjectedSignature();
            }
        }

        /// <summary>
        /// Determines if the member reference is to a method or field.
        /// </summary>
        public MemberReferenceKind GetKind()
        {
            BlobReader blobReader = reader.GetBlobReader(this.Signature);
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
            return new CustomAttributeHandleCollection(reader, Handle);
        }

        #region Projections

        private Handle GetProjectedParent()
        {
            // no change
            return reader.MemberRefTable.GetClass(Handle);
        }

        private StringHandle GetProjectedName()
        {
            if (Treatment == MemberRefTreatment.Dispose)
            {
                return StringHandle.FromVirtualIndex(StringHandle.VirtualIndex.Dispose);
            }

            return reader.MemberRefTable.GetName(Handle);
        }

        private BlobHandle GetProjectedSignature()
        {
            // no change
            return reader.MemberRefTable.GetSignature(Handle);
        }
        #endregion
    }
}