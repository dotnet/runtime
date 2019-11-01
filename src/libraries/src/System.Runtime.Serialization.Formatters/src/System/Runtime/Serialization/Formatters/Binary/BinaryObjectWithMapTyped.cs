// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal sealed class BinaryObjectWithMapTyped : IStreamable
    {
        internal BinaryHeaderEnum _binaryHeaderEnum;
        internal int _objectId;
        internal string _name;
        internal int _numMembers;
        internal string[] _memberNames;
        internal BinaryTypeEnum[] _binaryTypeEnumA;
        internal object[] _typeInformationA;
        internal int[] _memberAssemIds;
        internal int _assemId;

        internal BinaryObjectWithMapTyped() { }

        internal BinaryObjectWithMapTyped(BinaryHeaderEnum binaryHeaderEnum)
        {
            _binaryHeaderEnum = binaryHeaderEnum;
        }

        internal void Set(int objectId, string name, int numMembers, string[] memberNames, BinaryTypeEnum[] binaryTypeEnumA, object[] typeInformationA, int[] memberAssemIds, int assemId)
        {
            _objectId = objectId;
            _assemId = assemId;
            _name = name;
            _numMembers = numMembers;
            _memberNames = memberNames;
            _binaryTypeEnumA = binaryTypeEnumA;
            _typeInformationA = typeInformationA;
            _memberAssemIds = memberAssemIds;
            _assemId = assemId;

            _binaryHeaderEnum = assemId > 0 ?
                 BinaryHeaderEnum.ObjectWithMapTypedAssemId :
                 BinaryHeaderEnum.ObjectWithMapTyped;
        }

        public void Write(BinaryFormatterWriter output)
        {
            output.WriteByte((byte)_binaryHeaderEnum);
            output.WriteInt32(_objectId);
            output.WriteString(_name);

            output.WriteInt32(_numMembers);
            for (int i = 0; i < _numMembers; i++)
            {
                output.WriteString(_memberNames[i]);
            }
            for (int i = 0; i < _numMembers; i++)
            {
                output.WriteByte((byte)_binaryTypeEnumA[i]);
            }
            for (int i = 0; i < _numMembers; i++)
            {
                BinaryTypeConverter.WriteTypeInfo(_binaryTypeEnumA[i], _typeInformationA[i], _memberAssemIds[i], output);
            }

            if (_assemId > 0)
            {
                output.WriteInt32(_assemId);
            }
        }

        public void Read(BinaryParser input)
        {
            // binaryHeaderEnum has already been read
            _objectId = input.ReadInt32();
            _name = input.ReadString();
            _numMembers = input.ReadInt32();
            _memberNames = new string[_numMembers];
            _binaryTypeEnumA = new BinaryTypeEnum[_numMembers];
            _typeInformationA = new object[_numMembers];
            _memberAssemIds = new int[_numMembers];
            for (int i = 0; i < _numMembers; i++)
            {
                _memberNames[i] = input.ReadString();
            }
            for (int i = 0; i < _numMembers; i++)
            {
                _binaryTypeEnumA[i] = (BinaryTypeEnum)input.ReadByte();
            }
            for (int i = 0; i < _numMembers; i++)
            {
                if (_binaryTypeEnumA[i] != BinaryTypeEnum.ObjectUrt && _binaryTypeEnumA[i] != BinaryTypeEnum.ObjectUser)
                {
                    _typeInformationA[i] = BinaryTypeConverter.ReadTypeInfo(_binaryTypeEnumA[i], input, out _memberAssemIds[i]);
                }
                else
                {
                    BinaryTypeConverter.ReadTypeInfo(_binaryTypeEnumA[i], input, out _memberAssemIds[i]);
                }
            }

            if (_binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMapTypedAssemId)
            {
                _assemId = input.ReadInt32();
            }
        }
    }
}
