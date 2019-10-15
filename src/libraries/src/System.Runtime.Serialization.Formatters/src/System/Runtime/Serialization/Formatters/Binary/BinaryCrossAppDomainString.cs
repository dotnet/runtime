// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal sealed class BinaryCrossAppDomainString : IStreamable
    {
        internal int _objectId;
        internal int _value;

        internal BinaryCrossAppDomainString()
        {
        }

        public void Write(BinaryFormatterWriter output)
        {
            output.WriteByte((byte)BinaryHeaderEnum.CrossAppDomainString);
            output.WriteInt32(_objectId);
            output.WriteInt32(_value);
        }

        public void Read(BinaryParser input)
        {
            _objectId = input.ReadInt32();
            _value = input.ReadInt32();
        }
    }
}
