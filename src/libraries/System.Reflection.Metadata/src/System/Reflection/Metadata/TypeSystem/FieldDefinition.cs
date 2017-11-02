// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public readonly struct FieldDefinition
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint _treatmentAndRowId;

        internal FieldDefinition(MetadataReader reader, uint treatmentAndRowId)
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

        private FieldDefTreatment Treatment
        {
            get { return (FieldDefTreatment)(_treatmentAndRowId >> TokenTypeIds.RowIdBitCount); }
        }

        private FieldDefinitionHandle Handle
        {
            get { return FieldDefinitionHandle.FromRowId(RowId); }
        }

        public StringHandle Name
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.FieldTable.GetName(Handle);
                }

                return GetProjectedName();
            }
        }

        public FieldAttributes Attributes
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.FieldTable.GetFlags(Handle);
                }

                return GetProjectedFlags();
            }
        }

        public BlobHandle Signature
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.FieldTable.GetSignature(Handle);
                }

                return GetProjectedSignature();
            }
        }

        public TType DecodeSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
        {
            var decoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
            var blob = _reader.GetBlobReader(Signature);
            return decoder.DecodeFieldSignature(ref blob);
        }

        public TypeDefinitionHandle GetDeclaringType()
        {
            return _reader.GetDeclaringType(Handle);
        }

        public ConstantHandle GetDefaultValue()
        {
            return _reader.ConstantTable.FindConstant(Handle);
        }

        public int GetRelativeVirtualAddress()
        {
            int fieldRvaRowId = _reader.FieldRvaTable.FindFieldRvaRowId(Handle.RowId);
            if (fieldRvaRowId == 0)
            {
                return 0;
            }

            return _reader.FieldRvaTable.GetRva(fieldRvaRowId);
        }

        /// <summary>
        /// Returns field layout offset, or -1 if not available.
        /// </summary>
        public int GetOffset()
        {
            int layoutRowId = _reader.FieldLayoutTable.FindFieldLayoutRowId(Handle);
            if (layoutRowId == 0)
            {
                return -1;
            }

            uint offset = _reader.FieldLayoutTable.GetOffset(layoutRowId);
            if (offset > int.MaxValue)
            {
                // CLI spec says:
                // "Offset (a 4-byte constant)"
                // "Offset shall be zero or more"
                // 
                // Peverify fails with "Type load failed" error if offset is greater than Int32.MaxValue.
                return -1;
            }

            return (int)offset;
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

#region Projections

        private StringHandle GetProjectedName()
        {
            // no change:
            return _reader.FieldTable.GetName(Handle);
        }

        private FieldAttributes GetProjectedFlags()
        {
            var flags = _reader.FieldTable.GetFlags(Handle);

            if (Treatment == FieldDefTreatment.EnumValue)
            {
                return (flags & ~FieldAttributes.FieldAccessMask) | FieldAttributes.Public;
            }

            return flags;
        }

        private BlobHandle GetProjectedSignature()
        {
            return _reader.FieldTable.GetSignature(Handle);
        }
#endregion
    }
}
