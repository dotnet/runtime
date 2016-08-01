// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization.Formatters.Binary
{
    // For each object or array being read off the stream, an ObjectProgress object is created. This object
    // keeps track of the progress of the parsing. When an object is being parsed, it keeps track of
    // the object member being parsed. When an array is being parsed it keeps track of the position within the
    // array.
    internal sealed class ObjectProgress
    {
        // Control
        internal bool _isInitial;
        internal int _count; //Progress count
        internal BinaryTypeEnum _expectedType = BinaryTypeEnum.ObjectUrt;
        internal object _expectedTypeInformation = null;

        internal string _name;
        internal InternalObjectTypeE _objectTypeEnum = InternalObjectTypeE.Empty;
        internal InternalMemberTypeE _memberTypeEnum;
        internal InternalMemberValueE _memberValueEnum;
        internal Type _dtType;

        // Array Information
        internal int _numItems;
        internal BinaryTypeEnum _binaryTypeEnum;
        internal object _typeInformation;

        // Member Information
        internal int _memberLength;
        internal BinaryTypeEnum[] _binaryTypeEnumA;
        internal object[] _typeInformationA;
        internal string[] _memberNames;
        internal Type[] _memberTypes;

        // ParseRecord
        internal ParseRecord _pr = new ParseRecord();

        internal ObjectProgress() { }

        internal void Init()
        {
            _isInitial = false;
            _count = 0;
            _expectedType = BinaryTypeEnum.ObjectUrt;
            _expectedTypeInformation = null;

            _name = null;
            _objectTypeEnum = InternalObjectTypeE.Empty;
            _memberTypeEnum = InternalMemberTypeE.Empty;
            _memberValueEnum = InternalMemberValueE.Empty;
            _dtType = null;

            // Array Information
            _numItems = 0;

            //binaryTypeEnum
            _typeInformation = null;

            // Member Information
            _memberLength = 0;
            _binaryTypeEnumA = null;
            _typeInformationA = null;
            _memberNames = null;
            _memberTypes = null;

            _pr.Init();
        }

        //Array item entry of nulls has a count of nulls represented by that item. The first null has been 
        // incremented by GetNext, the rest of the null counts are incremented here
        internal void ArrayCountIncrement(int value) => _count += value;

        // Specifies what is to parsed next from the wire.
        internal bool GetNext(out BinaryTypeEnum outBinaryTypeEnum, out object outTypeInformation)
        {
            //Initialize the out params up here.
            outBinaryTypeEnum = BinaryTypeEnum.Primitive;
            outTypeInformation = null;

            if (_objectTypeEnum == InternalObjectTypeE.Array)
            {
                // Array
                if (_count == _numItems)
                {
                    return false;
                }
                else
                {
                    outBinaryTypeEnum = _binaryTypeEnum;
                    outTypeInformation = _typeInformation;
                    if (_count == 0)
                        _isInitial = false;
                    _count++;
                    return true;
                }
            }
            else
            {
                // Member
                if ((_count == _memberLength) && (!_isInitial))
                {
                    return false;
                }
                else
                {
                    outBinaryTypeEnum = _binaryTypeEnumA[_count];
                    outTypeInformation = _typeInformationA[_count];
                    if (_count == 0)
                    {
                        _isInitial = false;
                    }
                    _name = _memberNames[_count];
                    _dtType = _memberTypes[_count];
                    _count++;
                    return true;
                }
            }
        }
    }
}
