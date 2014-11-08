// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public struct FieldDefinition
    {
        private readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint treatmentAndRowId;

        internal FieldDefinition(MetadataReader reader, uint treatmentAndRowId)
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

        private FieldDefTreatment Treatment
        {
            get { return (FieldDefTreatment)(treatmentAndRowId >> TokenTypeIds.RowIdBitCount); }
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
                    return reader.FieldTable.GetName(Handle);
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
                    return reader.FieldTable.GetFlags(Handle);
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
                    return reader.FieldTable.GetSignature(Handle);
                }

                return GetProjectedSignature();
            }
        }

        public TypeDefinitionHandle GetDeclaringType()
        {
            return reader.GetDeclaringType(Handle);
        }

        public ConstantHandle GetDefaultValue()
        {
            return reader.ConstantTable.FindConstant(Handle);
        }

        public int GetRelativeVirtualAddress()
        {
            uint fieldRvaRowId = reader.FieldRvaTable.FindFieldRVARowId(Handle.RowId);
            if (fieldRvaRowId == 0)
            {
                return 0;
            }

            return reader.FieldRvaTable.GetRVA(fieldRvaRowId);
        }

        /// <summary>
        /// Returns field layout offset, or -1 if not available.
        /// </summary>
        public int GetOffset()
        {
            uint layoutRowId = reader.FieldLayoutTable.FindFieldLayoutRowId(Handle);
            if (layoutRowId == 0)
            {
                return -1;
            }

            uint offset = reader.FieldLayoutTable.GetOffset(layoutRowId);
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
            uint marshalRowId = reader.FieldMarshalTable.FindFieldMarshalRowId(Handle);
            if (marshalRowId == 0)
            {
                return default(BlobHandle);
            }

            return reader.FieldMarshalTable.GetNativeType(marshalRowId);
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(reader, Handle);
        }

        #region Projections

        private StringHandle GetProjectedName()
        {
            // no change:
            return reader.FieldTable.GetName(Handle);
        }

        private FieldAttributes GetProjectedFlags()
        {
            var flags = reader.FieldTable.GetFlags(Handle);

            if (Treatment == FieldDefTreatment.EnumValue)
            {
                return (flags & ~FieldAttributes.FieldAccessMask) | FieldAttributes.Public;
            }

            return flags;
        }

        private BlobHandle GetProjectedSignature()
        {
            return reader.FieldTable.GetSignature(Handle);
        }
        #endregion
    }
}