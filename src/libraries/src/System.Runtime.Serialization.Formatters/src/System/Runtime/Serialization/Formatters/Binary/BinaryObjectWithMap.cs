// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal sealed class BinaryObjectWithMap : IStreamable
    {
        internal BinaryHeaderEnum _binaryHeaderEnum;
        internal int _objectId;
        internal string _name;
        internal int _numMembers;
        internal string[] _memberNames;
        internal int _assemId;

        internal BinaryObjectWithMap() { }

        internal BinaryObjectWithMap(BinaryHeaderEnum binaryHeaderEnum)
        {
            _binaryHeaderEnum = binaryHeaderEnum;
        }

        internal void Set(int objectId, string name, int numMembers, string[] memberNames, int assemId)
        {
            _objectId = objectId;
            _name = name;
            _numMembers = numMembers;
            _memberNames = memberNames;
            _assemId = assemId;

            _binaryHeaderEnum = assemId > 0 ?
                BinaryHeaderEnum.ObjectWithMapAssemId :
                BinaryHeaderEnum.ObjectWithMap;
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

            if (_assemId > 0)
            {
                output.WriteInt32(_assemId);
            }
        }

        public void Read(BinaryParser input)
        {
            _objectId = input.ReadInt32();
            _name = input.ReadString();
            _numMembers = input.ReadInt32();

            _memberNames = new string[_numMembers];
            for (int i = 0; i < _numMembers; i++)
            {
                _memberNames[i] = input.ReadString();
            }

            if (_binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMapAssemId)
            {
                _assemId = input.ReadInt32();
            }
        }
    }
}
