// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime.InteropServices;

using Internal.NativeFormat;

namespace Internal.Runtime.TypeLoader
{
    internal unsafe class OptionalFieldsRuntimeBuilder
    {
        private struct OptionalField
        {
            internal bool m_fPresent;
            internal uint m_uiValue;
        }

        internal OptionalFieldsRuntimeBuilder(byte* pInitializeFromOptionalFields = null)
        {
            if (pInitializeFromOptionalFields == null)
                return;

            bool isLastField = false;
            while (!isLastField)
            {
                byte fieldHeader = NativePrimitiveDecoder.ReadUInt8(ref pInitializeFromOptionalFields);
                isLastField = (fieldHeader & 0x80) != 0;
                EETypeOptionalFieldTag eCurrentTag = (EETypeOptionalFieldTag)(fieldHeader & 0x7f);
                uint uiCurrentValue = NativePrimitiveDecoder.DecodeUnsigned(ref pInitializeFromOptionalFields);

                _rgFields[(int)eCurrentTag].m_fPresent = true;
                _rgFields[(int)eCurrentTag].m_uiValue = uiCurrentValue;
            }
        }

        internal uint GetFieldValue(EETypeOptionalFieldTag eTag, uint defaultValueIfNotFound)
        {
            return _rgFields[(int)eTag].m_fPresent ? _rgFields[(int)eTag].m_uiValue : defaultValueIfNotFound;
        }

        internal void SetFieldValue(EETypeOptionalFieldTag eTag, uint value)
        {
            _rgFields[(int)eTag].m_fPresent = true;
            _rgFields[(int)eTag].m_uiValue = value;
        }

        internal void ClearField(EETypeOptionalFieldTag eTag)
        {
            _rgFields[(int)eTag].m_fPresent = false;
        }

        internal int Encode()
        {
            EETypeOptionalFieldTag eLastTag = EETypeOptionalFieldTag.Count;

            for (EETypeOptionalFieldTag eTag = 0; eTag < EETypeOptionalFieldTag.Count; eTag++)
                eLastTag = _rgFields[(int)eTag].m_fPresent ? eTag : eLastTag;

            if (eLastTag == EETypeOptionalFieldTag.Count)
                return 0;

            _encoder = new NativePrimitiveEncoder();
            _encoder.Init();

            for (EETypeOptionalFieldTag eTag = 0; eTag < EETypeOptionalFieldTag.Count; eTag++)
            {
                if (!_rgFields[(int)eTag].m_fPresent)
                    continue;

                _encoder.WriteByte((byte)((byte)eTag | (eTag == eLastTag ? 0x80 : 0)));
                _encoder.WriteUnsigned(_rgFields[(int)eTag].m_uiValue);
            }

            return _encoder.Size;
        }

        internal void WriteToEEType(MethodTable* pEEType, int sizeOfOptionalFieldsDataInEEType)
        {
            byte* pOptionalFieldsPtr = pEEType->OptionalFieldsPtr;
            _encoder.Save(pOptionalFieldsPtr, sizeOfOptionalFieldsDataInEEType);
        }

        private NativePrimitiveEncoder _encoder;
        private OptionalField[] _rgFields = new OptionalField[(int)EETypeOptionalFieldTag.Count];
    }
}
