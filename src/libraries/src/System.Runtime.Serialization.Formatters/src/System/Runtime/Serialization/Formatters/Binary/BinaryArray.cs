// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal sealed class BinaryArray : IStreamable
    {
        internal int _objectId;
        internal int _rank;
        internal int[] _lengthA;
        internal int[] _lowerBoundA;
        internal BinaryTypeEnum _binaryTypeEnum;
        internal object _typeInformation;
        internal int _assemId = 0;
        private BinaryHeaderEnum _binaryHeaderEnum;
        internal BinaryArrayTypeEnum _binaryArrayTypeEnum;

        internal BinaryArray() { }

        internal BinaryArray(BinaryHeaderEnum binaryHeaderEnum)
        {
            _binaryHeaderEnum = binaryHeaderEnum;
        }

        internal void Set(int objectId, int rank, int[] lengthA, int[] lowerBoundA, BinaryTypeEnum binaryTypeEnum, object typeInformation, BinaryArrayTypeEnum binaryArrayTypeEnum, int assemId)
        {
            _objectId = objectId;
            _binaryArrayTypeEnum = binaryArrayTypeEnum;
            _rank = rank;
            _lengthA = lengthA;
            _lowerBoundA = lowerBoundA;
            _binaryTypeEnum = binaryTypeEnum;
            _typeInformation = typeInformation;
            _assemId = assemId;

            _binaryHeaderEnum = BinaryHeaderEnum.Array;
            if (binaryArrayTypeEnum == BinaryArrayTypeEnum.Single)
            {
                if (binaryTypeEnum == BinaryTypeEnum.Primitive)
                {
                    _binaryHeaderEnum = BinaryHeaderEnum.ArraySinglePrimitive;
                }
                else if (binaryTypeEnum == BinaryTypeEnum.String)
                {
                    _binaryHeaderEnum = BinaryHeaderEnum.ArraySingleString;
                }
                else if (binaryTypeEnum == BinaryTypeEnum.Object)
                {
                    _binaryHeaderEnum = BinaryHeaderEnum.ArraySingleObject;
                }
            }
        }

        public void Write(BinaryFormatterWriter output)
        {
            switch (_binaryHeaderEnum)
            {
                case BinaryHeaderEnum.ArraySinglePrimitive:
                    output.WriteByte((byte)_binaryHeaderEnum);
                    output.WriteInt32(_objectId);
                    output.WriteInt32(_lengthA[0]);
                    output.WriteByte((byte)((InternalPrimitiveTypeE)_typeInformation));
                    break;
                case BinaryHeaderEnum.ArraySingleString:
                    output.WriteByte((byte)_binaryHeaderEnum);
                    output.WriteInt32(_objectId);
                    output.WriteInt32(_lengthA[0]);
                    break;
                case BinaryHeaderEnum.ArraySingleObject:
                    output.WriteByte((byte)_binaryHeaderEnum);
                    output.WriteInt32(_objectId);
                    output.WriteInt32(_lengthA[0]);
                    break;
                default:
                    output.WriteByte((byte)_binaryHeaderEnum);
                    output.WriteInt32(_objectId);
                    output.WriteByte((byte)_binaryArrayTypeEnum);
                    output.WriteInt32(_rank);
                    for (int i = 0; i < _rank; i++)
                    {
                        output.WriteInt32(_lengthA[i]);
                    }
                    if ((_binaryArrayTypeEnum == BinaryArrayTypeEnum.SingleOffset) ||
                        (_binaryArrayTypeEnum == BinaryArrayTypeEnum.JaggedOffset) ||
                        (_binaryArrayTypeEnum == BinaryArrayTypeEnum.RectangularOffset))
                    {
                        for (int i = 0; i < _rank; i++)
                        {
                            output.WriteInt32(_lowerBoundA[i]);
                        }
                    }
                    output.WriteByte((byte)_binaryTypeEnum);
                    BinaryTypeConverter.WriteTypeInfo(_binaryTypeEnum, _typeInformation, _assemId, output);
                    break;
            }
        }

        public void Read(BinaryParser input)
        {
            switch (_binaryHeaderEnum)
            {
                case BinaryHeaderEnum.ArraySinglePrimitive:
                    _objectId = input.ReadInt32();
                    _lengthA = new int[1];
                    _lengthA[0] = input.ReadInt32();
                    _binaryArrayTypeEnum = BinaryArrayTypeEnum.Single;
                    _rank = 1;
                    _lowerBoundA = new int[_rank];
                    _binaryTypeEnum = BinaryTypeEnum.Primitive;
                    _typeInformation = (InternalPrimitiveTypeE)input.ReadByte();
                    break;
                case BinaryHeaderEnum.ArraySingleString:
                    _objectId = input.ReadInt32();
                    _lengthA = new int[1];
                    _lengthA[0] = input.ReadInt32();
                    _binaryArrayTypeEnum = BinaryArrayTypeEnum.Single;
                    _rank = 1;
                    _lowerBoundA = new int[_rank];
                    _binaryTypeEnum = BinaryTypeEnum.String;
                    _typeInformation = null;
                    break;
                case BinaryHeaderEnum.ArraySingleObject:
                    _objectId = input.ReadInt32();
                    _lengthA = new int[1];
                    _lengthA[0] = input.ReadInt32();
                    _binaryArrayTypeEnum = BinaryArrayTypeEnum.Single;
                    _rank = 1;
                    _lowerBoundA = new int[_rank];
                    _binaryTypeEnum = BinaryTypeEnum.Object;
                    _typeInformation = null;
                    break;
                default:
                    _objectId = input.ReadInt32();
                    _binaryArrayTypeEnum = (BinaryArrayTypeEnum)input.ReadByte();
                    _rank = input.ReadInt32();
                    _lengthA = new int[_rank];
                    _lowerBoundA = new int[_rank];
                    for (int i = 0; i < _rank; i++)
                    {
                        _lengthA[i] = input.ReadInt32();
                    }
                    if ((_binaryArrayTypeEnum == BinaryArrayTypeEnum.SingleOffset) ||
                        (_binaryArrayTypeEnum == BinaryArrayTypeEnum.JaggedOffset) ||
                        (_binaryArrayTypeEnum == BinaryArrayTypeEnum.RectangularOffset))
                    {
                        for (int i = 0; i < _rank; i++)
                        {
                            _lowerBoundA[i] = input.ReadInt32();
                        }
                    }
                    _binaryTypeEnum = (BinaryTypeEnum)input.ReadByte();
                    _typeInformation = BinaryTypeConverter.ReadTypeInfo(_binaryTypeEnum, input, out _assemId);
                    break;
            }
        }
    }
}
