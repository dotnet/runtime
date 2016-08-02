// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal sealed class MemberPrimitiveTyped : IStreamable
    {
        internal InternalPrimitiveTypeE _primitiveTypeEnum;
        internal object _value;

        internal MemberPrimitiveTyped() { }

        internal void Set(InternalPrimitiveTypeE primitiveTypeEnum, object value)
        {
            _primitiveTypeEnum = primitiveTypeEnum;
            _value = value;
        }

        public void Write(BinaryFormatterWriter output)
        {
            output.WriteByte((byte)BinaryHeaderEnum.MemberPrimitiveTyped);
            output.WriteByte((byte)_primitiveTypeEnum);
            output.WriteValue(_primitiveTypeEnum, _value);
        }

        public void Read(BinaryParser input)
        {
            _primitiveTypeEnum = (InternalPrimitiveTypeE)input.ReadByte(); //PDJ
            _value = input.ReadValue(_primitiveTypeEnum);
        }
    }
}
