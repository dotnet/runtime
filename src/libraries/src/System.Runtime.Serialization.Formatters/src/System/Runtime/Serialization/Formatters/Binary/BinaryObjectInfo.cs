// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Collections.Generic;
using System.Reflection;

namespace System.Runtime.Serialization.Formatters.Binary
{
    // This class contains information about an object. It is used so that
    // the rest of the Formatter routines can use a common interface for
    // a normal object, an ISerializable object, and a surrogate object
    internal sealed class WriteObjectInfo
    {
        internal int _objectInfoId;
        internal object _obj;
        internal Type _objectType;

        internal bool _isSi = false;
        internal bool _isNamed = false;
        internal bool _isArray = false;

        internal SerializationInfo _si = null;
        internal SerObjectInfoCache _cache = null;

        internal object[] _memberData = null;
        internal ISerializationSurrogate _serializationSurrogate = null;
        internal StreamingContext _context;
        internal SerObjectInfoInit _serObjectInfoInit = null;

        // Writing and Parsing information
        internal long _objectId;
        internal long _assemId;

        // Binder information
        private string _binderTypeName;
        private string _binderAssemblyString;

        internal WriteObjectInfo() { }

        internal void ObjectEnd()
        {
            PutObjectInfo(_serObjectInfoInit, this);
        }

        private void InternalInit()
        {
            _obj = null;
            _objectType = null;
            _isSi = false;
            _isNamed = false;
            _isArray = false;
            _si = null;
            _cache = null;
            _memberData = null;

            // Writing and Parsing information
            _objectId = 0;
            _assemId = 0;

            // Binder information
            _binderTypeName = null;
            _binderAssemblyString = null;
        }

        internal static WriteObjectInfo Serialize(object obj, ISurrogateSelector surrogateSelector, StreamingContext context, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, ObjectWriter objectWriter, SerializationBinder binder)
        {
            WriteObjectInfo woi = GetObjectInfo(serObjectInfoInit);
            woi.InitSerialize(obj, surrogateSelector, context, serObjectInfoInit, converter, objectWriter, binder);
            return woi;
        }

        // Write constructor
        internal void InitSerialize(object obj, ISurrogateSelector surrogateSelector, StreamingContext context, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, ObjectWriter objectWriter, SerializationBinder binder)
        {
            _context = context;
            _obj = obj;
            _serObjectInfoInit = serObjectInfoInit;
            _objectType = obj.GetType();

            if (_objectType.IsArray)
            {
                _isArray = true;
                InitNoMembers();
                return;
            }

            InvokeSerializationBinder(binder);
            objectWriter.ObjectManager.RegisterObject(obj);

            ISurrogateSelector surrogateSelectorTemp;
            if (surrogateSelector != null && (_serializationSurrogate = surrogateSelector.GetSurrogate(_objectType, context, out surrogateSelectorTemp)) != null)
            {
                _si = new SerializationInfo(_objectType, converter);
                if (!_objectType.IsPrimitive)
                {
                    _serializationSurrogate.GetObjectData(obj, _si, context);
                }
                InitSiWrite();
            }
            else if (obj is ISerializable)
            {
                if (!_objectType.IsSerializable)
                {
                    throw new SerializationException(SR.Format(SR.Serialization_NonSerType, _objectType.FullName, _objectType.Assembly.FullName));
                }
                _si = new SerializationInfo(_objectType, converter);
                ((ISerializable)obj).GetObjectData(_si, context);
                InitSiWrite();
                CheckTypeForwardedFrom(_cache, _objectType, _binderAssemblyString);
            }
            else
            {
                InitMemberInfo();
                CheckTypeForwardedFrom(_cache, _objectType, _binderAssemblyString);
            }
        }

        internal static WriteObjectInfo Serialize(Type objectType, ISurrogateSelector surrogateSelector, StreamingContext context, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, SerializationBinder binder)
        {
            WriteObjectInfo woi = GetObjectInfo(serObjectInfoInit);
            woi.InitSerialize(objectType, surrogateSelector, context, serObjectInfoInit, converter, binder);
            return woi;
        }

        // Write Constructor used for array types or null members
        internal void InitSerialize(Type objectType, ISurrogateSelector surrogateSelector, StreamingContext context, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, SerializationBinder binder)
        {
            _objectType = objectType;
            _context = context;
            _serObjectInfoInit = serObjectInfoInit;

            if (objectType.IsArray)
            {
                InitNoMembers();
                return;
            }

            InvokeSerializationBinder(binder);

            ISurrogateSelector surrogateSelectorTemp = null;
            if (surrogateSelector != null)
            {
                _serializationSurrogate = surrogateSelector.GetSurrogate(objectType, context, out surrogateSelectorTemp);
            }

            if (_serializationSurrogate != null)
            {
                // surrogate does not have this problem since user has pass in through the BF's ctor
                _si = new SerializationInfo(objectType, converter);
                _cache = new SerObjectInfoCache(objectType);
                _isSi = true;
            }
            else if (!ReferenceEquals(objectType, Converter.s_typeofObject) && Converter.s_typeofISerializable.IsAssignableFrom(objectType))
            {
                _si = new SerializationInfo(objectType, converter);
                _cache = new SerObjectInfoCache(objectType);
                CheckTypeForwardedFrom(_cache, objectType, _binderAssemblyString);
                _isSi = true;
            }

            if (!_isSi)
            {
                InitMemberInfo();
                CheckTypeForwardedFrom(_cache, objectType, _binderAssemblyString);
            }
        }

        private void InitSiWrite()
        {
            SerializationInfoEnumerator siEnum = null;
            _isSi = true;
            siEnum = _si.GetEnumerator();
            int infoLength = 0;

            infoLength = _si.MemberCount;

            int count = infoLength;

            // For ISerializable cache cannot be saved because each object instance can have different values
            // BinaryWriter only puts the map on the wire if the ISerializable map cannot be reused.
            TypeInformation typeInformation = null;
            string fullTypeName = _si.FullTypeName;
            string assemblyString = _si.AssemblyName;
            bool hasTypeForwardedFrom = false;

            if (!_si.IsFullTypeNameSetExplicit)
            {
                typeInformation = BinaryFormatter.GetTypeInformation(_si.ObjectType);
                fullTypeName = typeInformation.FullTypeName;
                hasTypeForwardedFrom = typeInformation.HasTypeForwardedFrom;
            }

            if (!_si.IsAssemblyNameSetExplicit)
            {
                if (typeInformation == null)
                {
                    typeInformation = BinaryFormatter.GetTypeInformation(_si.ObjectType);
                }
                assemblyString = typeInformation.AssemblyString;
                hasTypeForwardedFrom = typeInformation.HasTypeForwardedFrom;
            }

            _cache = new SerObjectInfoCache(fullTypeName, assemblyString, hasTypeForwardedFrom);

            _cache._memberNames = new string[count];
            _cache._memberTypes = new Type[count];
            _memberData = new object[count];

            siEnum = _si.GetEnumerator();
            for (int i = 0; siEnum.MoveNext(); i++)
            {
                _cache._memberNames[i] = siEnum.Name;
                _cache._memberTypes[i] = siEnum.ObjectType;
                _memberData[i] = siEnum.Value;
            }

            _isNamed = true;
        }

        private static void CheckTypeForwardedFrom(SerObjectInfoCache cache, Type objectType, string binderAssemblyString)
        {
            // nop
        }

        private void InitNoMembers()
        {
            if (!_serObjectInfoInit._seenBeforeTable.TryGetValue(_objectType, out _cache))
            {
                _cache = new SerObjectInfoCache(_objectType);
                _serObjectInfoInit._seenBeforeTable.Add(_objectType, _cache);
            }
        }

        private void InitMemberInfo()
        {
            if (!_serObjectInfoInit._seenBeforeTable.TryGetValue(_objectType, out _cache))
            {
                _cache = new SerObjectInfoCache(_objectType);

                _cache._memberInfos = FormatterServices.GetSerializableMembers(_objectType, _context);
                int count = _cache._memberInfos.Length;
                _cache._memberNames = new string[count];
                _cache._memberTypes = new Type[count];

                // Calculate new arrays
                for (int i = 0; i < count; i++)
                {
                    _cache._memberNames[i] = _cache._memberInfos[i].Name;
                    _cache._memberTypes[i] = ((FieldInfo)_cache._memberInfos[i]).FieldType;
                }
                _serObjectInfoInit._seenBeforeTable.Add(_objectType, _cache);
            }

            if (_obj != null)
            {
                _memberData = FormatterServices.GetObjectData(_obj, _cache._memberInfos);
            }

            _isNamed = true;
        }

        internal string GetTypeFullName() => _binderTypeName ?? _cache._fullTypeName;

        internal string GetAssemblyString() => _binderAssemblyString ?? _cache._assemblyString;

        private void InvokeSerializationBinder(SerializationBinder binder) =>
            binder?.BindToName(_objectType, out _binderAssemblyString, out _binderTypeName);

        internal void GetMemberInfo(out string[] outMemberNames, out Type[] outMemberTypes, out object[] outMemberData)
        {
            outMemberNames = _cache._memberNames;
            outMemberTypes = _cache._memberTypes;
            outMemberData = _memberData;

            if (_isSi && !_isNamed)
            {
                throw new SerializationException(SR.Serialization_ISerializableMemberInfo);
            }
        }

        private static WriteObjectInfo GetObjectInfo(SerObjectInfoInit serObjectInfoInit)
        {
            WriteObjectInfo objectInfo;

            if (!serObjectInfoInit._oiPool.IsEmpty())
            {
                objectInfo = (WriteObjectInfo)serObjectInfoInit._oiPool.Pop();
                objectInfo.InternalInit();
            }
            else
            {
                objectInfo = new WriteObjectInfo();
                objectInfo._objectInfoId = serObjectInfoInit._objectInfoIdCount++;
            }

            return objectInfo;
        }

        private static void PutObjectInfo(SerObjectInfoInit serObjectInfoInit, WriteObjectInfo objectInfo) =>
            serObjectInfoInit._oiPool.Push(objectInfo);
    }

    internal sealed class ReadObjectInfo
    {
        internal int _objectInfoId;
        internal static int _readObjectInfoCounter;

        internal Type _objectType;

        internal ObjectManager _objectManager;

        internal int _count;

        internal bool _isSi = false;
        internal bool _isTyped = false;
        internal bool _isSimpleAssembly = false;

        internal SerObjectInfoCache _cache;

        internal string[] _wireMemberNames;
        internal Type[] _wireMemberTypes;

        private int _lastPosition = 0;

        internal ISerializationSurrogate _serializationSurrogate = null;
        internal StreamingContext _context;

        // Si Read
        internal List<Type> _memberTypesList;
        internal SerObjectInfoInit _serObjectInfoInit = null;
        internal IFormatterConverter _formatterConverter;

        internal ReadObjectInfo() { }

        internal void ObjectEnd() { }

        internal void PrepareForReuse()
        {
            _lastPosition = 0;
        }

        internal static ReadObjectInfo Create(Type objectType, ISurrogateSelector surrogateSelector, StreamingContext context, ObjectManager objectManager, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, bool bSimpleAssembly)
        {
            ReadObjectInfo roi = GetObjectInfo(serObjectInfoInit);
            roi.Init(objectType, surrogateSelector, context, objectManager, serObjectInfoInit, converter, bSimpleAssembly);
            return roi;
        }

        internal void Init(Type objectType, ISurrogateSelector surrogateSelector, StreamingContext context, ObjectManager objectManager, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, bool bSimpleAssembly)
        {
            _objectType = objectType;
            _objectManager = objectManager;
            _context = context;
            _serObjectInfoInit = serObjectInfoInit;
            _formatterConverter = converter;
            _isSimpleAssembly = bSimpleAssembly;

            InitReadConstructor(objectType, surrogateSelector, context);
        }

        internal static ReadObjectInfo Create(Type objectType, string[] memberNames, Type[] memberTypes, ISurrogateSelector surrogateSelector, StreamingContext context, ObjectManager objectManager, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, bool bSimpleAssembly)
        {
            ReadObjectInfo roi = GetObjectInfo(serObjectInfoInit);
            roi.Init(objectType, memberNames, memberTypes, surrogateSelector, context, objectManager, serObjectInfoInit, converter, bSimpleAssembly);
            return roi;
        }

        internal void Init(Type objectType, string[] memberNames, Type[] memberTypes, ISurrogateSelector surrogateSelector, StreamingContext context, ObjectManager objectManager, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, bool bSimpleAssembly)
        {
            _objectType = objectType;
            _objectManager = objectManager;
            _wireMemberNames = memberNames;
            _wireMemberTypes = memberTypes;
            _context = context;
            _serObjectInfoInit = serObjectInfoInit;
            _formatterConverter = converter;
            _isSimpleAssembly = bSimpleAssembly;
            if (memberTypes != null)
            {
                _isTyped = true;
            }
            if (objectType != null)
            {
                InitReadConstructor(objectType, surrogateSelector, context);
            }
        }

        private void InitReadConstructor(Type objectType, ISurrogateSelector surrogateSelector, StreamingContext context)
        {
            if (objectType.IsArray)
            {
                InitNoMembers();
                return;
            }

            ISurrogateSelector surrogateSelectorTemp = null;
            if (surrogateSelector != null)
            {
                _serializationSurrogate = surrogateSelector.GetSurrogate(objectType, context, out surrogateSelectorTemp);
            }

            if (_serializationSurrogate != null)
            {
                _isSi = true;
            }
            else if (!ReferenceEquals(objectType, Converter.s_typeofObject) && Converter.s_typeofISerializable.IsAssignableFrom(objectType))
            {
                _isSi = true;
            }

            if (_isSi)
            {
                InitSiRead();
            }
            else
            {
                InitMemberInfo();
            }
        }

        private void InitSiRead()
        {
            if (_memberTypesList != null)
            {
                _memberTypesList = new List<Type>(20);
            }
        }

        private void InitNoMembers()
        {
            _cache = new SerObjectInfoCache(_objectType);
        }

        private void InitMemberInfo()
        {
            _cache = new SerObjectInfoCache(_objectType);
            _cache._memberInfos = FormatterServices.GetSerializableMembers(_objectType, _context);
            _count = _cache._memberInfos.Length;
            _cache._memberNames = new string[_count];
            _cache._memberTypes = new Type[_count];

            // Calculate new arrays
            for (int i = 0; i < _count; i++)
            {
                _cache._memberNames[i] = _cache._memberInfos[i].Name;
                _cache._memberTypes[i] = GetMemberType(_cache._memberInfos[i]);
            }

            _isTyped = true;
        }

        // Get the memberInfo for a memberName
        internal MemberInfo GetMemberInfo(string name)
        {
            if (_cache == null)
            {
                return null;
            }

            if (_isSi)
            {
                throw new SerializationException(SR.Format(SR.Serialization_MemberInfo, _objectType + " " + name));
            }

            if (_cache._memberInfos == null)
            {
                throw new SerializationException(SR.Format(SR.Serialization_NoMemberInfo, _objectType + " " + name));
            }

            int position = Position(name);
            return position != -1 ? _cache._memberInfos[position] : null;
        }

        // Get the ObjectType for a memberName
        internal Type GetType(string name)
        {
            int position = Position(name);
            if (position == -1)
            {
                return null;
            }

            Type type = _isTyped ? _cache._memberTypes[position] : _memberTypesList[position];
            if (type == null)
            {
                throw new SerializationException(SR.Format(SR.Serialization_ISerializableTypes, _objectType + " " + name));
            }

            return type;
        }

        // Adds the value for a memberName
        internal void AddValue(string name, object value, ref SerializationInfo si, ref object[] memberData)
        {
            if (_isSi)
            {
                si.AddValue(name, value);
            }
            else
            {
                // If a member in the stream is not found, ignore it
                int position = Position(name);
                if (position != -1)
                {
                    memberData[position] = value;
                }
            }
        }

        internal void InitDataStore(ref SerializationInfo si, ref object[] memberData)
        {
            if (_isSi)
            {
                if (si == null)
                {
                    si = new SerializationInfo(_objectType, _formatterConverter);
                }
            }
            else
            {
                if (memberData == null && _cache != null)
                {
                    memberData = new object[_cache._memberNames.Length];
                }
            }
        }

        // Records an objectId in a member when the actual object for that member is not yet known
        internal void RecordFixup(long objectId, string name, long idRef)
        {
            if (_isSi)
            {
                if (_objectManager == null)
                {
                    throw new SerializationException(SR.Serialization_CorruptedStream);
                }

                _objectManager.RecordDelayedFixup(objectId, name, idRef);
            }
            else
            {
                int position = Position(name);
                if (position != -1)
                {
                    if (_objectManager == null)
                    {
                        throw new SerializationException(SR.Serialization_CorruptedStream);
                    }

                    _objectManager.RecordFixup(objectId, _cache._memberInfos[position], idRef);
                }
            }
        }

        // Fills in the values for an object
        internal void PopulateObjectMembers(object obj, object[] memberData)
        {
            if (!_isSi && memberData != null)
            {
                FormatterServices.PopulateObjectMembers(obj, _cache._memberInfos, memberData);
            }
        }

        // Specifies the position in the memberNames array of this name
        private int Position(string name)
        {
            if (_cache == null)
            {
                return -1;
            }

            if (_cache._memberNames.Length > 0 && _cache._memberNames[_lastPosition].Equals(name))
            {
                return _lastPosition;
            }
            else if ((++_lastPosition < _cache._memberNames.Length) && (_cache._memberNames[_lastPosition].Equals(name)))
            {
                return _lastPosition;
            }
            else
            {
                // Search for name
                for (int i = 0; i < _cache._memberNames.Length; i++)
                {
                    if (_cache._memberNames[i].Equals(name))
                    {
                        _lastPosition = i;
                        return _lastPosition;
                    }
                }

                _lastPosition = 0;
                return -1;
            }
        }

        // Return the member Types in order of memberNames
        internal Type[] GetMemberTypes(string[] inMemberNames, Type objectType)
        {
            if (_isSi)
            {
                throw new SerializationException(SR.Format(SR.Serialization_ISerializableTypes, objectType));
            }

            if (_cache == null)
            {
                return null;
            }

            if (_cache._memberTypes == null)
            {
                _cache._memberTypes = new Type[_count];
                for (int i = 0; i < _count; i++)
                {
                    _cache._memberTypes[i] = GetMemberType(_cache._memberInfos[i]);
                }
            }

            bool memberMissing = false;
            if (inMemberNames.Length < _cache._memberInfos.Length)
            {
                memberMissing = true;
            }

            Type[] outMemberTypes = new Type[_cache._memberInfos.Length];
            bool isFound = false;
            for (int i = 0; i < _cache._memberInfos.Length; i++)
            {
                if (!memberMissing && inMemberNames[i].Equals(_cache._memberInfos[i].Name))
                {
                    outMemberTypes[i] = _cache._memberTypes[i];
                }
                else
                {
                    // MemberNames on wire in different order then memberInfos returned by reflection
                    isFound = false;
                    for (int j = 0; j < inMemberNames.Length; j++)
                    {
                        if (_cache._memberInfos[i].Name.Equals(inMemberNames[j]))
                        {
                            outMemberTypes[i] = _cache._memberTypes[i];
                            isFound = true;
                            break;
                        }
                    }
                    if (!isFound)
                    {
                        // A field on the type isn't found. See if the field has OptionalFieldAttribute.  We only throw
                        // when the assembly format is set appropriately.
                        if (!_isSimpleAssembly &&
                            _cache._memberInfos[i].GetCustomAttribute(typeof(OptionalFieldAttribute), inherit: false) == null)
                        {
                            throw new SerializationException(SR.Format(SR.Serialization_MissingMember, _cache._memberNames[i], objectType, typeof(OptionalFieldAttribute).FullName));
                        }
                    }
                }
            }

            return outMemberTypes;
        }

        // Retrieves the member type from the MemberInfo
        internal Type GetMemberType(MemberInfo objMember)
        {
            if (objMember is FieldInfo)
            {
                return ((FieldInfo)objMember).FieldType;
            }

            throw new SerializationException(SR.Format(SR.Serialization_SerMemberInfo, objMember.GetType()));
        }


        private static ReadObjectInfo GetObjectInfo(SerObjectInfoInit serObjectInfoInit)
        {
            ReadObjectInfo roi = new ReadObjectInfo();
            roi._objectInfoId = Interlocked.Increment(ref _readObjectInfoCounter);
            return roi;
        }
    }

    internal sealed class SerObjectInfoInit
    {
        internal readonly Dictionary<Type, SerObjectInfoCache> _seenBeforeTable = new Dictionary<Type, SerObjectInfoCache>();
        internal int _objectInfoIdCount = 1;
        internal SerStack _oiPool = new SerStack("SerObjectInfo Pool");
    }

    internal sealed class SerObjectInfoCache
    {
        internal readonly string _fullTypeName;
        internal readonly string _assemblyString;
        internal readonly bool _hasTypeForwardedFrom;

        internal MemberInfo[] _memberInfos;
        internal string[] _memberNames;
        internal Type[] _memberTypes;

        internal SerObjectInfoCache(string typeName, string assemblyName, bool hasTypeForwardedFrom)
        {
            _fullTypeName = typeName;
            _assemblyString = assemblyName;
            _hasTypeForwardedFrom = hasTypeForwardedFrom;
        }

        internal SerObjectInfoCache(Type type)
        {
            TypeInformation typeInformation = BinaryFormatter.GetTypeInformation(type);
            _fullTypeName = typeInformation.FullTypeName;
            _assemblyString = typeInformation.AssemblyString;
            _hasTypeForwardedFrom = typeInformation.HasTypeForwardedFrom;
        }
    }

    internal sealed class TypeInformation
    {
        internal TypeInformation(string fullTypeName, string assemblyString, bool hasTypeForwardedFrom)
        {
            FullTypeName = fullTypeName;
            AssemblyString = assemblyString;
            HasTypeForwardedFrom = hasTypeForwardedFrom;
        }

        internal string FullTypeName { get; }
        internal string AssemblyString { get; }
        internal bool HasTypeForwardedFrom { get; }
    }
}
