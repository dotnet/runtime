// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.NativeFormat;
using System.Diagnostics;
using System.Text;

namespace Internal.Runtime
{
    internal sealed unsafe partial class EETypeOptionalFieldsBuilder
    {
        private NativePrimitiveEncoder _encoder;
        private OptionalField[] _rgFields = new OptionalField[(int)EETypeOptionalFieldTag.Count];

        private struct OptionalField
        {
            internal bool _fieldPresent;
            internal uint _value;
        }

        internal EETypeOptionalFieldsBuilder() { }

        internal uint GetFieldValue(EETypeOptionalFieldTag eTag, uint defaultValueIfNotFound)
        {
            return _rgFields[(int)eTag]._fieldPresent ? _rgFields[(int)eTag]._value : defaultValueIfNotFound;
        }

        internal void SetFieldValue(EETypeOptionalFieldTag eTag, uint value)
        {
            _rgFields[(int)eTag]._fieldPresent = true;
            _rgFields[(int)eTag]._value = value;
        }

        internal void ClearField(EETypeOptionalFieldTag eTag)
        {
            _rgFields[(int)eTag]._fieldPresent = false;
        }

        private int Encode()
        {
            EETypeOptionalFieldTag eLastTag = EETypeOptionalFieldTag.Count;

            for (EETypeOptionalFieldTag eTag = 0; eTag < EETypeOptionalFieldTag.Count; eTag++)
                eLastTag = _rgFields[(int)eTag]._fieldPresent ? eTag : eLastTag;

            if (eLastTag == EETypeOptionalFieldTag.Count)
                return 0;

            _encoder = default(NativePrimitiveEncoder);
            _encoder.Init();

            for (EETypeOptionalFieldTag eTag = 0; eTag < EETypeOptionalFieldTag.Count; eTag++)
            {
                if (!_rgFields[(int)eTag]._fieldPresent)
                    continue;

                _encoder.WriteByte((byte)((byte)eTag | (eTag == eLastTag ? 0x80 : 0)));
                _encoder.WriteUnsigned(_rgFields[(int)eTag]._value);
            }

            return _encoder.Size;
        }

        public byte[] GetBytes()
        {
            Debug.Assert(IsAtLeastOneFieldUsed());
            if (_encoder.Size == 0)
            {
                Encode();
            }

            return _encoder.GetBytes();
        }

        public bool IsAtLeastOneFieldUsed()
        {
            for (int i = 0; i < (int)EETypeOptionalFieldTag.Count; i++)
            {
                if (_rgFields[i]._fieldPresent)
                    return true;
            }

            return false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < (int)EETypeOptionalFieldTag.Count; i++)
            {
                if (_rgFields[i]._fieldPresent)
                {
                    sb.Append(_rgFields[i]._value.ToStringInvariant());
                }
                else
                {
                    sb.Append('x');
                }


                if (i != (int)EETypeOptionalFieldTag.Count - 1)
                {
                    sb.Append('_');
                }
            }

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is EETypeOptionalFieldsBuilder))
                return false;

            EETypeOptionalFieldsBuilder other = obj as EETypeOptionalFieldsBuilder;

            if (ReferenceEquals(this, other))
                return true;

            for (EETypeOptionalFieldTag eTag = 0; eTag < EETypeOptionalFieldTag.Count; eTag++)
            {
                int index = (int)eTag;
                if (_rgFields[index]._fieldPresent != other._rgFields[index]._fieldPresent ||
                    (_rgFields[index]._fieldPresent && _rgFields[index]._value != other._rgFields[index]._value))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 31;

            for (EETypeOptionalFieldTag eTag = 0; eTag < EETypeOptionalFieldTag.Count; eTag++)
            {
                hash = hash * 486187739 + (int)GetFieldValue(eTag, 0);
            }

            return hash;
        }
    }
}
