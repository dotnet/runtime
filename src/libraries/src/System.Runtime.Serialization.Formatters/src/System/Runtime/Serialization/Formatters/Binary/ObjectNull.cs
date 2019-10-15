// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal sealed class ObjectNull : IStreamable
    {
        internal int _nullCount;

        internal ObjectNull() { }

        internal void SetNullCount(int nullCount)
        {
            _nullCount = nullCount;
        }

        public void Write(BinaryFormatterWriter output)
        {
            if (_nullCount == 1)
            {
                output.WriteByte((byte)BinaryHeaderEnum.ObjectNull);
            }
            else if (_nullCount < 256)
            {
                output.WriteByte((byte)BinaryHeaderEnum.ObjectNullMultiple256);
                output.WriteByte((byte)_nullCount);
            }
            else
            {
                output.WriteByte((byte)BinaryHeaderEnum.ObjectNullMultiple);
                output.WriteInt32(_nullCount);
            }
        }

        public void Read(BinaryParser input)
        {
            Read(input, BinaryHeaderEnum.ObjectNull);
        }

        public void Read(BinaryParser input, BinaryHeaderEnum binaryHeaderEnum)
        {
            //binaryHeaderEnum = input.ReadByte(); already read
            switch (binaryHeaderEnum)
            {
                case BinaryHeaderEnum.ObjectNull:
                    _nullCount = 1;
                    break;
                case BinaryHeaderEnum.ObjectNullMultiple256:
                    _nullCount = input.ReadByte();
                    break;
                case BinaryHeaderEnum.ObjectNullMultiple:
                    _nullCount = input.ReadInt32();
                    break;
            }
        }
    }
}
