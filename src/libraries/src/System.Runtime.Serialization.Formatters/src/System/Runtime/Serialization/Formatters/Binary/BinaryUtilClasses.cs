// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal sealed class ParseRecord
    {
        // Enums
        internal InternalParseTypeE _PRparseTypeEnum = InternalParseTypeE.Empty;
        internal InternalObjectTypeE _PRobjectTypeEnum = InternalObjectTypeE.Empty;
        internal InternalArrayTypeE _PRarrayTypeEnum = InternalArrayTypeE.Empty;
        internal InternalMemberTypeE _PRmemberTypeEnum = InternalMemberTypeE.Empty;
        internal InternalMemberValueE _PRmemberValueEnum = InternalMemberValueE.Empty;
        internal InternalObjectPositionE _PRobjectPositionEnum = InternalObjectPositionE.Empty;

        // Object
        internal string _PRname;

        // Value
        internal string _PRvalue;
        internal object _PRvarValue;

        // dt attribute
        internal string _PRkeyDt;
        internal Type _PRdtType;
        internal InternalPrimitiveTypeE _PRdtTypeCode;

        // Object ID
        internal long _PRobjectId;

        // Reference ID
        internal long _PRidRef;

        // Array

        // Array Element Type
        internal string _PRarrayElementTypeString;
        internal Type _PRarrayElementType;
        internal bool _PRisArrayVariant = false;
        internal InternalPrimitiveTypeE _PRarrayElementTypeCode;

        // Parsed array information
        internal int _PRrank;
        internal int[] _PRlengthA;
        internal int[] _PRlowerBoundA;

        // Array map for placing array elements in array
        internal int[] _PRindexMap;
        internal int _PRmemberIndex;
        internal int _PRlinearlength;
        internal int[] _PRrectangularMap;
        internal bool _PRisLowerBound;

        // MemberInfo accumulated during parsing of members

        internal ReadObjectInfo _PRobjectInfo;

        // ValueType Fixup needed
        internal bool _PRisValueTypeFixup = false;

        // Created object
        internal object _PRnewObj;
        internal object[] _PRobjectA; //optimization, will contain object[]
        internal PrimitiveArray _PRprimitiveArray; // for Primitive Soap arrays, optimization
        internal bool _PRisRegistered; // Used when registering nested classes
        internal object[] _PRmemberData; // member data is collected here before populating
        internal SerializationInfo _PRsi;
        
        internal int _consecutiveNullArrayEntryCount;

        internal ParseRecord() { }

        // Initialize ParseRecord. Called when reusing.
        internal void Init()
        {
            // Enums
            _PRparseTypeEnum = InternalParseTypeE.Empty;
            _PRobjectTypeEnum = InternalObjectTypeE.Empty;
            _PRarrayTypeEnum = InternalArrayTypeE.Empty;
            _PRmemberTypeEnum = InternalMemberTypeE.Empty;
            _PRmemberValueEnum = InternalMemberValueE.Empty;
            _PRobjectPositionEnum = InternalObjectPositionE.Empty;

            // Object
            _PRname = null;

            // Value
            _PRvalue = null;

            // dt attribute
            _PRkeyDt = null;
            _PRdtType = null;
            _PRdtTypeCode = InternalPrimitiveTypeE.Invalid;

            // Object ID
            _PRobjectId = 0;

            // Reference ID
            _PRidRef = 0;

            // Array

            // Array Element Type
            _PRarrayElementTypeString = null;
            _PRarrayElementType = null;
            _PRisArrayVariant = false;
            _PRarrayElementTypeCode = InternalPrimitiveTypeE.Invalid;

            // Parsed array information
            _PRrank = 0;
            _PRlengthA = null;
            _PRlowerBoundA = null;

            // Array map for placing array elements in array
            _PRindexMap = null;
            _PRmemberIndex = 0;
            _PRlinearlength = 0;
            _PRrectangularMap = null;
            _PRisLowerBound = false;

            // ValueType Fixup needed
            _PRisValueTypeFixup = false;

            _PRnewObj = null;
            _PRobjectA = null;
            _PRprimitiveArray = null;
            _PRobjectInfo = null;
            _PRisRegistered = false;
            _PRmemberData = null;
            _PRsi = null;

            _consecutiveNullArrayEntryCount = 0;
        }
    }

    // Implements a stack used for parsing
    internal sealed class SerStack
    {
        internal object[] _objects = new object[5];
        internal string _stackId;
        internal int _top = -1;

        internal SerStack(string stackId)
        {
            _stackId = stackId;
        }

        // Push the object onto the stack
        internal void Push(object obj)
        {
            if (_top == (_objects.Length - 1))
            {
                IncreaseCapacity();
            }
            _objects[++_top] = obj;
        }

        // Pop the object from the stack
        internal object Pop()
        {
            if (_top < 0)
            {
                return null;
            }

            object obj = _objects[_top];
            _objects[_top--] = null;
            return obj;
        }

        internal void IncreaseCapacity()
        {
            int size = _objects.Length * 2;
            object[] newItems = new object[size];
            Array.Copy(_objects, 0, newItems, 0, _objects.Length);
            _objects = newItems;
        }

        // Gets the object on the top of the stack
        internal object Peek() => _top < 0 ? null : _objects[_top];

        // Gets the second entry in the stack.
        internal object PeekPeek() => _top < 1 ? null : _objects[_top - 1];

        // The number of entries in the stack
        internal bool IsEmpty() => _top <= 0;
    }

    // Implements a Growable array
    [Serializable]
    internal sealed class SizedArray : ICloneable
    {
        internal object[] _objects = null;
        internal object[] _negObjects = null;

        internal SizedArray()
        {
            _objects = new object[16];
            _negObjects = new object[4];
        }

        internal SizedArray(int length)
        {
            _objects = new object[length];
            _negObjects = new object[length];
        }

        private SizedArray(SizedArray sizedArray)
        {
            _objects = new object[sizedArray._objects.Length];
            sizedArray._objects.CopyTo(_objects, 0);
            _negObjects = new object[sizedArray._negObjects.Length];
            sizedArray._negObjects.CopyTo(_negObjects, 0);
        }

        public object Clone() => new SizedArray(this);

        internal object this[int index]
        {
            get
            {
                if (index < 0)
                {
                    return -index > _negObjects.Length - 1 ? null : _negObjects[-index];
                }
                else
                {
                    return index > _objects.Length - 1 ? null  : _objects[index];
                }
            }
            set
            {
                if (index < 0)
                {
                    if (-index > _negObjects.Length - 1)
                    {
                        IncreaseCapacity(index);
                    }
                    _negObjects[-index] = value;
                }
                else
                {
                    if (index > _objects.Length - 1)
                    {
                        IncreaseCapacity(index);
                    }
                    _objects[index] = value;
                }
            }
        }

        internal void IncreaseCapacity(int index)
        {
            try
            {
                if (index < 0)
                {
                    int size = Math.Max(_negObjects.Length * 2, (-index) + 1);
                    object[] newItems = new object[size];
                    Array.Copy(_negObjects, 0, newItems, 0, _negObjects.Length);
                    _negObjects = newItems;
                }
                else
                {
                    int size = Math.Max(_objects.Length * 2, index + 1);
                    object[] newItems = new object[size];
                    Array.Copy(_objects, 0, newItems, 0, _objects.Length);
                    _objects = newItems;
                }
            }
            catch (Exception)
            {
                throw new SerializationException(SR.Serialization_CorruptedStream);
            }
        }
    }

    [Serializable]
    internal sealed class IntSizedArray : ICloneable
    {
        internal int[] objects = new int[16];
        internal int[] negObjects = new int[4];

        public IntSizedArray() { }

        private IntSizedArray(IntSizedArray sizedArray)
        {
            objects = new int[sizedArray.objects.Length];
            sizedArray.objects.CopyTo(objects, 0);
            negObjects = new int[sizedArray.negObjects.Length];
            sizedArray.negObjects.CopyTo(negObjects, 0);
        }

        public object Clone() => new IntSizedArray(this);

        internal int this[int index]
        {
            get
            {
                if (index < 0)
                {
                    return -index > negObjects.Length - 1 ? 0 : negObjects[-index];
                }
                else
                {
                    return index > objects.Length - 1 ? 0 : objects[index];
                }
            }
            set
            {
                if (index < 0)
                {
                    if (-index > negObjects.Length - 1)
                    {
                        IncreaseCapacity(index);
                    }
                    negObjects[-index] = value;
                }
                else
                {
                    if (index > objects.Length - 1)
                    {
                        IncreaseCapacity(index);
                    }
                    objects[index] = value;
                }
            }
        }

        internal void IncreaseCapacity(int index)
        {
            try
            {
                if (index < 0)
                {
                    int size = Math.Max(negObjects.Length * 2, (-index) + 1);
                    int[] newItems = new int[size];
                    Array.Copy(negObjects, 0, newItems, 0, negObjects.Length);
                    negObjects = newItems;
                }
                else
                {
                    int size = Math.Max(objects.Length * 2, index + 1);
                    int[] newItems = new int[size];
                    Array.Copy(objects, 0, newItems, 0, objects.Length);
                    objects = newItems;
                }
            }
            catch (Exception)
            {
                throw new SerializationException(SR.Serialization_CorruptedStream);
            }
        }
    }

    internal sealed class NameCache
    {
        private static readonly ConcurrentDictionary<string, object> s_ht = new ConcurrentDictionary<string, object>();
        private string _name = null;

        internal object GetCachedValue(string name)
        {
            _name = name;
            object value;
            return s_ht.TryGetValue(name, out value) ? value : null;
        }

        internal void SetCachedValue(object value) => s_ht[_name] = value;
    }


    // Used to fixup value types. Only currently used for valuetypes which are array items.
    internal sealed class ValueFixup
    {
        internal ValueFixupEnum _valueFixupEnum = ValueFixupEnum.Empty;
        internal Array _arrayObj;
        internal int[] _indexMap;
        internal object _header = null;
        internal object _memberObject;
        internal static volatile MemberInfo _valueInfo;
        internal ReadObjectInfo _objectInfo;
        internal string _memberName;

        internal ValueFixup(Array arrayObj, int[] indexMap)
        {
            _valueFixupEnum = ValueFixupEnum.Array;
            _arrayObj = arrayObj;
            _indexMap = indexMap;
        }

        internal ValueFixup(object memberObject, string memberName, ReadObjectInfo objectInfo)
        {
            _valueFixupEnum = ValueFixupEnum.Member;
            _memberObject = memberObject;
            _memberName = memberName;
            _objectInfo = objectInfo;
        }

        internal void Fixup(ParseRecord record, ParseRecord parent)
        {
            object obj = record._PRnewObj;
            switch (_valueFixupEnum)
            {
                case ValueFixupEnum.Array:
                    _arrayObj.SetValue(obj, _indexMap);
                    break;
                case ValueFixupEnum.Header:
                    Type type = typeof(Header);
                    if (_valueInfo == null)
                    {
                        MemberInfo[] valueInfos = type.GetMember("Value");
                        if (valueInfos.Length != 1)
                        {
                            throw new SerializationException(SR.Format(SR.Serialization_HeaderReflection, valueInfos.Length));
                        }
                        _valueInfo = valueInfos[0];
                    }
                    FormatterServices.SerializationSetValue(_valueInfo, _header, obj);
                    break;
                case ValueFixupEnum.Member:
                    if (_objectInfo._isSi)
                    {
                        _objectInfo._objectManager.RecordDelayedFixup(parent._PRobjectId, _memberName, record._PRobjectId);
                    }
                    else
                    {
                        MemberInfo memberInfo = _objectInfo.GetMemberInfo(_memberName);
                        if (memberInfo != null)
                        {
                            _objectInfo._objectManager.RecordFixup(parent._PRobjectId, memberInfo, record._PRobjectId);
                        }
                    }
                    break;
            }
        }
    }

    // Class used to transmit Enums from the XML and Binary Formatter class to the ObjectWriter and ObjectReader class
    internal sealed class InternalFE
    {
        internal FormatterTypeStyle _FEtypeFormat;
        internal FormatterAssemblyStyle _FEassemblyFormat;
        internal TypeFilterLevel _FEsecurityLevel;
        internal InternalSerializerTypeE _FEserializerTypeEnum;
    }

    internal sealed class NameInfo
    {
        internal string _NIFullName; // Name from SerObjectInfo.GetType
        internal long _NIobjectId;
        internal long _NIassemId;
        internal InternalPrimitiveTypeE _NIprimitiveTypeEnum = InternalPrimitiveTypeE.Invalid;
        internal Type _NItype;
        internal bool _NIisSealed;
        internal bool _NIisArray;
        internal bool _NIisArrayItem;
        internal bool _NItransmitTypeOnObject;
        internal bool _NItransmitTypeOnMember;
        internal bool _NIisParentTypeOnObject;
        internal InternalArrayTypeE _NIarrayEnum;
        private bool _NIsealedStatusChecked = false;

        internal NameInfo() { }

        internal void Init()
        {
            _NIFullName = null;
            _NIobjectId = 0;
            _NIassemId = 0;
            _NIprimitiveTypeEnum = InternalPrimitiveTypeE.Invalid;
            _NItype = null;
            _NIisSealed = false;
            _NItransmitTypeOnObject = false;
            _NItransmitTypeOnMember = false;
            _NIisParentTypeOnObject = false;
            _NIisArray = false;
            _NIisArrayItem = false;
            _NIarrayEnum = InternalArrayTypeE.Empty;
            _NIsealedStatusChecked = false;
        }

        public bool IsSealed
        {
            get
            {
                if (!_NIsealedStatusChecked)
                {
                    _NIisSealed = _NItype.GetTypeInfo().IsSealed;
                    _NIsealedStatusChecked = true;
                }
                return _NIisSealed;
            }
        }

        public string NIname
        {
            get { return _NIFullName ?? (_NIFullName = _NItype.FullName); }
            set { _NIFullName = value; }
        }
    }

    internal sealed class PrimitiveArray
    {
        private InternalPrimitiveTypeE _code;
        private bool[] _booleanA = null;
        private char[] _charA = null;
        private double[] _doubleA = null;
        private short[] _int16A = null;
        private int[] _int32A = null;
        private long[] _int64A = null;
        private sbyte[] _sbyteA = null;
        private float[] _singleA = null;
        private ushort[] _uint16A = null;
        private uint[] _uint32A = null;
        private ulong[] _uint64A = null;

        internal PrimitiveArray(InternalPrimitiveTypeE code, Array array)
        {
            _code = code;
            switch (code)
            {
                case InternalPrimitiveTypeE.Boolean: _booleanA = (bool[])array; break;
                case InternalPrimitiveTypeE.Char: _charA = (char[])array; break;
                case InternalPrimitiveTypeE.Double: _doubleA = (double[])array; break;
                case InternalPrimitiveTypeE.Int16: _int16A = (short[])array; break;
                case InternalPrimitiveTypeE.Int32: _int32A = (int[])array; break;
                case InternalPrimitiveTypeE.Int64: _int64A = (long[])array; break;
                case InternalPrimitiveTypeE.SByte: _sbyteA = (sbyte[])array; break;
                case InternalPrimitiveTypeE.Single: _singleA = (float[])array; break;
                case InternalPrimitiveTypeE.UInt16: _uint16A = (ushort[])array; break;
                case InternalPrimitiveTypeE.UInt32: _uint32A = (uint[])array; break;
                case InternalPrimitiveTypeE.UInt64: _uint64A = (ulong[])array; break;
            }
        }

        internal void SetValue(string value, int index)
        {
            switch (_code)
            {
                case InternalPrimitiveTypeE.Boolean:
                    _booleanA[index] = bool.Parse(value);
                    break;
                case InternalPrimitiveTypeE.Char:
                    if ((value[0] == '_') && (value.Equals("_0x00_")))
                    {
                        _charA[index] = char.MinValue;
                    }
                    else
                    {
                        _charA[index] = char.Parse(value);
                    }
                    break;
                case InternalPrimitiveTypeE.Double:
                    _doubleA[index] = double.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case InternalPrimitiveTypeE.Int16:
                    _int16A[index] = short.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case InternalPrimitiveTypeE.Int32:
                    _int32A[index] = int.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case InternalPrimitiveTypeE.Int64:
                    _int64A[index] = long.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case InternalPrimitiveTypeE.SByte:
                    _sbyteA[index] = sbyte.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case InternalPrimitiveTypeE.Single:
                    _singleA[index] = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case InternalPrimitiveTypeE.UInt16:
                    _uint16A[index] = ushort.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case InternalPrimitiveTypeE.UInt32:
                    _uint32A[index] = uint.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case InternalPrimitiveTypeE.UInt64:
                    _uint64A[index] = ulong.Parse(value, CultureInfo.InvariantCulture);
                    break;
            }
        }
    }
}
