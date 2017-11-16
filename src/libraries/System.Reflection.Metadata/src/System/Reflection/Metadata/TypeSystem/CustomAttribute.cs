// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public readonly struct CustomAttribute
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint _treatmentAndRowId;

        internal CustomAttribute(MetadataReader reader, uint treatmentAndRowId)
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

        private CustomAttributeHandle Handle
        {
            get { return CustomAttributeHandle.FromRowId(RowId); }
        }

        private MethodDefTreatment Treatment
        {
            get { return (MethodDefTreatment)(_treatmentAndRowId >> TokenTypeIds.RowIdBitCount); }
        }

        /// <summary>
        /// The constructor (<see cref="MethodDefinitionHandle"/> or <see cref="MemberReferenceHandle"/>) of the custom attribute type.
        /// </summary>
        /// <remarks>
        /// Corresponds to Type field of CustomAttribute table in ECMA-335 Standard.
        /// </remarks>
        public EntityHandle Constructor
        {
            get
            {
                return _reader.CustomAttributeTable.GetConstructor(Handle);
            }
        }

        /// <summary>
        /// The handle of the metadata entity the attribute is applied to.
        /// </summary>
        /// <remarks>
        /// Corresponds to Parent field of CustomAttribute table in ECMA-335 Standard.
        /// </remarks>
        public EntityHandle Parent
        {
            get
            {
                return _reader.CustomAttributeTable.GetParent(Handle);
            }
        }

        /// <summary>
        /// The value of the attribute.
        /// </summary>
        /// <remarks>
        /// Corresponds to Value field of CustomAttribute table in ECMA-335 Standard.
        /// </remarks>
        public BlobHandle Value
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.CustomAttributeTable.GetValue(Handle);
                }

                return GetProjectedValue();
            }
        }

        /// <summary>
        /// Decodes the arguments encoded in the value blob.
        /// </summary>
        public CustomAttributeValue<TType> DecodeValue<TType>(ICustomAttributeTypeProvider<TType> provider)
        {
            var decoder = new CustomAttributeDecoder<TType>(provider, _reader);
            return decoder.DecodeValue(Constructor, Value);
        }

        #region Projections

        private BlobHandle GetProjectedValue()
        {
            // The usual pattern for accessing custom attributes differs from pattern for accessing e.g. TypeDef row fields.
            // The value blob is only accessed when the consumer is about to decode it (which is a nontrivial process), 
            // while the Constructor and Parent fields are often accessed when searching for a particular attribute.
            // 
            // The current WinMD projections only affect the blob and not the Constructor and Parent values.
            // It is thus more efficient to calculate the treatment here (and make GetValue more expensive) and
            // avoid calculating the treatment when the CustomAttributeHandle is looked up and CustomAttribute struct 
            // is initialized.

            CustomAttributeValueTreatment treatment = _reader.CalculateCustomAttributeValueTreatment(Handle);
            if (treatment == 0)
            {
                return _reader.CustomAttributeTable.GetValue(Handle);
            }

            return GetProjectedValue(treatment);
        }

        private BlobHandle GetProjectedValue(CustomAttributeValueTreatment treatment)
        {
            BlobHandle.VirtualIndex virtualIndex;
            bool isVersionOrDeprecated;
            switch (treatment)
            {
                case CustomAttributeValueTreatment.AttributeUsageVersionAttribute:
                case CustomAttributeValueTreatment.AttributeUsageDeprecatedAttribute:
                    virtualIndex = BlobHandle.VirtualIndex.AttributeUsage_AllowMultiple;
                    isVersionOrDeprecated = true;
                    break;

                case CustomAttributeValueTreatment.AttributeUsageAllowMultiple:
                    virtualIndex = BlobHandle.VirtualIndex.AttributeUsage_AllowMultiple;
                    isVersionOrDeprecated = false;
                    break;

                case CustomAttributeValueTreatment.AttributeUsageAllowSingle:
                    virtualIndex = BlobHandle.VirtualIndex.AttributeUsage_AllowSingle;
                    isVersionOrDeprecated = false;
                    break;

                default:
                    Debug.Assert(false);
                    return default(BlobHandle);
            }

            // Raw blob format:
            //    01 00        - Fixed prolog for CA's
            //    xx xx xx xx  - The Windows.Foundation.Metadata.AttributeTarget value
            //    00 00        - Indicates 0 name/value pairs following.
            var rawBlob = _reader.CustomAttributeTable.GetValue(Handle);
            var rawBlobReader = _reader.GetBlobReader(rawBlob);
            if (rawBlobReader.Length != 8)
            {
                return rawBlob;
            }

            if (rawBlobReader.ReadInt16() != 1)
            {
                return rawBlob;
            }

            AttributeTargets projectedValue = ProjectAttributeTargetValue(rawBlobReader.ReadUInt32());
            if (isVersionOrDeprecated)
            {
                projectedValue |= AttributeTargets.Constructor | AttributeTargets.Property;
            }

            return BlobHandle.FromVirtualIndex(virtualIndex, (ushort)projectedValue);
        }

        private static AttributeTargets ProjectAttributeTargetValue(uint rawValue)
        {
            // Windows.Foundation.Metadata.AttributeTargets.All
            if (rawValue == 0xffffffff)
            {
                return AttributeTargets.All;
            }

            AttributeTargets result = 0;

            if ((rawValue & 0x00000001) != 0) result |= AttributeTargets.Delegate;
            if ((rawValue & 0x00000002) != 0) result |= AttributeTargets.Enum;
            if ((rawValue & 0x00000004) != 0) result |= AttributeTargets.Event;
            if ((rawValue & 0x00000008) != 0) result |= AttributeTargets.Field;
            if ((rawValue & 0x00000010) != 0) result |= AttributeTargets.Interface;
            // InterfaceGroup (no equivalent in CLR)
            if ((rawValue & 0x00000040) != 0) result |= AttributeTargets.Method;
            if ((rawValue & 0x00000080) != 0) result |= AttributeTargets.Parameter;
            if ((rawValue & 0x00000100) != 0) result |= AttributeTargets.Property;
            if ((rawValue & 0x00000200) != 0) result |= AttributeTargets.Class;
            if ((rawValue & 0x00000400) != 0) result |= AttributeTargets.Struct;
            // InterfaceImpl (no equivalent in CLR)

            return result;
        }
        #endregion
    }
}
