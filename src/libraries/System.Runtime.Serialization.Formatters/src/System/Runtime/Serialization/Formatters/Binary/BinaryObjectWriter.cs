// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Collections.Generic;

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal sealed class ObjectWriter
    {
        private Queue<object> _objectQueue;
        private ObjectIDGenerator _idGenerator;
        private int _currentId;

        private ISurrogateSelector _surrogates;
        private StreamingContext _context;
        private BinaryFormatterWriter _serWriter;
        private SerializationObjectManager _objectManager;

        private long _topId;
        private string _topName = null;

        private InternalFE _formatterEnums;
        private SerializationBinder _binder;

        private SerObjectInfoInit _serObjectInfoInit;

        private IFormatterConverter _formatterConverter;

        internal object[] _crossAppDomainArray = null;
        internal List<object> _internalCrossAppDomainArray = null;

        private object _previousObj = null;
        private long _previousId = 0;

        private Type _previousType = null;
        private InternalPrimitiveTypeE _previousCode = InternalPrimitiveTypeE.Invalid;

        internal ObjectWriter(ISurrogateSelector selector, StreamingContext context, InternalFE formatterEnums, SerializationBinder binder)
        {
            _currentId = 1;
            _surrogates = selector;
            _context = context;
            _binder = binder;
            _formatterEnums = formatterEnums;
            _objectManager = new SerializationObjectManager(context);
        }

        internal void Serialize(object graph, BinaryFormatterWriter serWriter, bool fCheck)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }
            if (serWriter == null)
            {
                throw new ArgumentNullException(nameof(serWriter));
            }

            _serWriter = serWriter;

            serWriter.WriteBegin();
            long headerId = 0;
            object obj;
            long objectId;
            bool isNew;

            // allocations if methodCall or methodResponse and no graph
            _idGenerator = new ObjectIDGenerator();
            _objectQueue = new Queue<object>();
            _formatterConverter = new FormatterConverter();
            _serObjectInfoInit = new SerObjectInfoInit();

            _topId = InternalGetId(graph, false, null, out isNew);
            headerId = -1;
            WriteSerializedStreamHeader(_topId, headerId);

            _objectQueue.Enqueue(graph);
            while ((obj = GetNext(out objectId)) != null)
            {
                WriteObjectInfo objectInfo = null;

                // GetNext will return either an object or a WriteObjectInfo. 
                // A WriteObjectInfo is returned if this object was member of another object
                if (obj is WriteObjectInfo)
                {
                    objectInfo = (WriteObjectInfo)obj;
                }
                else
                {
                    objectInfo = WriteObjectInfo.Serialize(obj, _surrogates, _context, _serObjectInfoInit, _formatterConverter, this, _binder);
                    objectInfo._assemId = GetAssemblyId(objectInfo);
                }

                objectInfo._objectId = objectId;
                NameInfo typeNameInfo = TypeToNameInfo(objectInfo);
                Write(objectInfo, typeNameInfo, typeNameInfo);
                PutNameInfo(typeNameInfo);
                objectInfo.ObjectEnd();
            }

            serWriter.WriteSerializationHeaderEnd();
            serWriter.WriteEnd();

            // Invoke OnSerialized Event
            _objectManager.RaiseOnSerializedEvent();
        }

        internal SerializationObjectManager ObjectManager => _objectManager;

        // Writes a given object to the stream.
        private void Write(WriteObjectInfo objectInfo, NameInfo memberNameInfo, NameInfo typeNameInfo)
        {
            object obj = objectInfo._obj;
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(objectInfo) + "." + nameof(objectInfo._obj));
            }
            Type objType = objectInfo._objectType;
            long objectId = objectInfo._objectId;

            if (ReferenceEquals(objType, Converter.s_typeofString))
            {
                // Top level String
                memberNameInfo._objectId = objectId;
                _serWriter.WriteObjectString((int)objectId, obj.ToString());
            }
            else
            {
                if (objectInfo._isArray)
                {
                    WriteArray(objectInfo, memberNameInfo, null);
                }
                else
                {
                    string[] memberNames;
                    Type[] memberTypes;
                    object[] memberData;

                    objectInfo.GetMemberInfo(out memberNames, out memberTypes, out memberData);

                    // Only Binary needs to transmit types for ISerializable because the binary formatter transmits the types in URT format.
                    // Soap transmits all types as strings, so it is up to the ISerializable object to convert the string back to its URT type
                    if (objectInfo._isSi || CheckTypeFormat(_formatterEnums._typeFormat, FormatterTypeStyle.TypesAlways))
                    {
                        memberNameInfo._transmitTypeOnObject = true;
                        memberNameInfo._isParentTypeOnObject = true;
                        typeNameInfo._transmitTypeOnObject = true;
                        typeNameInfo._isParentTypeOnObject = true;
                    }

                    var memberObjectInfos = new WriteObjectInfo[memberNames.Length];

                    // Get assembly information
                    // Binary Serializer, assembly names need to be
                    // written before objects are referenced.
                    // GetAssemId here will write out the
                    // assemblyStrings at the right Binary
                    // Serialization object boundary.
                    for (int i = 0; i < memberTypes.Length; i++)
                    {
                        Type type =
                            memberTypes[i] != null ? memberTypes[i] :
                            memberData[i] != null ? GetType(memberData[i]) :
                            Converter.s_typeofObject;

                        InternalPrimitiveTypeE code = ToCode(type);
                        if ((code == InternalPrimitiveTypeE.Invalid) &&
                            (!ReferenceEquals(type, Converter.s_typeofString)))
                        {
                            if (memberData[i] != null)
                            {
                                memberObjectInfos[i] = WriteObjectInfo.Serialize(
                                    memberData[i],
                                    _surrogates,
                                    _context,
                                    _serObjectInfoInit,
                                    _formatterConverter,
                                    this,
                                    _binder);
                                memberObjectInfos[i]._assemId = GetAssemblyId(memberObjectInfos[i]);
                            }
                            else
                            {
                                memberObjectInfos[i] = WriteObjectInfo.Serialize(
                                    memberTypes[i],
                                    _surrogates,
                                    _context,
                                    _serObjectInfoInit,
                                    _formatterConverter,
                                    _binder);
                                memberObjectInfos[i]._assemId = GetAssemblyId(memberObjectInfos[i]);
                            }
                        }
                    }
                    Write(objectInfo, memberNameInfo, typeNameInfo, memberNames, memberTypes, memberData, memberObjectInfos);
                }
            }
        }

        // Writes a given object to the stream.
        private void Write(WriteObjectInfo objectInfo,
                           NameInfo memberNameInfo,
                           NameInfo typeNameInfo,
                           string[] memberNames,
                           Type[] memberTypes,
                           object[] memberData,
                           WriteObjectInfo[] memberObjectInfos)
        {
            int numItems = memberNames.Length;
            NameInfo topNameInfo = null;

            if (memberNameInfo != null)
            {
                memberNameInfo._objectId = objectInfo._objectId;
                _serWriter.WriteObject(memberNameInfo, typeNameInfo, numItems, memberNames, memberTypes, memberObjectInfos);
            }
            else if ((objectInfo._objectId == _topId) && (_topName != null))
            {
                topNameInfo = MemberToNameInfo(_topName);
                topNameInfo._objectId = objectInfo._objectId;
                _serWriter.WriteObject(topNameInfo, typeNameInfo, numItems, memberNames, memberTypes, memberObjectInfos);
            }
            else
            {
                if (!ReferenceEquals(objectInfo._objectType, Converter.s_typeofString))
                {
                    typeNameInfo._objectId = objectInfo._objectId;
                    _serWriter.WriteObject(typeNameInfo, null, numItems, memberNames, memberTypes, memberObjectInfos);
                }
            }

            if (memberNameInfo._isParentTypeOnObject)
            {
                memberNameInfo._transmitTypeOnObject = true;
                memberNameInfo._isParentTypeOnObject = false;
            }
            else
            {
                memberNameInfo._transmitTypeOnObject = false;
            }

            // Write members
            for (int i = 0; i < numItems; i++)
            {
                WriteMemberSetup(objectInfo, memberNameInfo, typeNameInfo, memberNames[i], memberTypes[i], memberData[i], memberObjectInfos[i]);
            }

            if (memberNameInfo != null)
            {
                memberNameInfo._objectId = objectInfo._objectId;
                _serWriter.WriteObjectEnd(memberNameInfo, typeNameInfo);
            }
            else if ((objectInfo._objectId == _topId) && (_topName != null))
            {
                _serWriter.WriteObjectEnd(topNameInfo, typeNameInfo);
                PutNameInfo(topNameInfo);
            }
            else if (!ReferenceEquals(objectInfo._objectType, Converter.s_typeofString))
            {
                _serWriter.WriteObjectEnd(typeNameInfo, typeNameInfo);
            }
        }

        private void WriteMemberSetup(WriteObjectInfo objectInfo,
                                      NameInfo memberNameInfo,
                                      NameInfo typeNameInfo,
                                      string memberName,
                                      Type memberType,
                                      object memberData,
                                      WriteObjectInfo memberObjectInfo)
        {
            NameInfo newMemberNameInfo = MemberToNameInfo(memberName); // newMemberNameInfo contains the member type

            if (memberObjectInfo != null)
            {
                newMemberNameInfo._assemId = memberObjectInfo._assemId;
            }
            newMemberNameInfo._type = memberType;

            // newTypeNameInfo contains the data type
            NameInfo newTypeNameInfo = null;
            if (memberObjectInfo == null)
            {
                newTypeNameInfo = TypeToNameInfo(memberType);
            }
            else
            {
                newTypeNameInfo = TypeToNameInfo(memberObjectInfo);
            }

            newMemberNameInfo._transmitTypeOnObject = memberNameInfo._transmitTypeOnObject;
            newMemberNameInfo._isParentTypeOnObject = memberNameInfo._isParentTypeOnObject;
            WriteMembers(newMemberNameInfo, newTypeNameInfo, memberData, objectInfo, typeNameInfo, memberObjectInfo);
            PutNameInfo(newMemberNameInfo);
            PutNameInfo(newTypeNameInfo);
        }

        // Writes the members of an object
        private void WriteMembers(NameInfo memberNameInfo,
                                  NameInfo memberTypeNameInfo,
                                  object memberData,
                                  WriteObjectInfo objectInfo,
                                  NameInfo typeNameInfo,
                                  WriteObjectInfo memberObjectInfo)
        {
            Type memberType = memberNameInfo._type;
            bool assignUniqueIdToValueType = false;

            // Types are transmitted for a member as follows:
            // The member is of type object
            // The member object of type is ISerializable and
            //  Binary - Types always transmitted.

            if (ReferenceEquals(memberType, Converter.s_typeofObject) || Nullable.GetUnderlyingType(memberType) != null)
            {
                memberTypeNameInfo._transmitTypeOnMember = true;
                memberNameInfo._transmitTypeOnMember = true;
            }

            if (CheckTypeFormat(_formatterEnums._typeFormat, FormatterTypeStyle.TypesAlways) || (objectInfo._isSi))
            {
                memberTypeNameInfo._transmitTypeOnObject = true;
                memberNameInfo._transmitTypeOnObject = true;
                memberNameInfo._isParentTypeOnObject = true;
            }

            if (CheckForNull(objectInfo, memberNameInfo, memberTypeNameInfo, memberData))
            {
                return;
            }

            object outObj = memberData;
            Type outType = null;

            // If member type does not equal data type, transmit type on object.
            if (memberTypeNameInfo._primitiveTypeEnum == InternalPrimitiveTypeE.Invalid)
            {
                outType = GetType(outObj);
                if (!ReferenceEquals(memberType, outType))
                {
                    memberTypeNameInfo._transmitTypeOnMember = true;
                    memberNameInfo._transmitTypeOnMember = true;
                }
            }

            if (ReferenceEquals(memberType, Converter.s_typeofObject))
            {
                assignUniqueIdToValueType = true;
                memberType = GetType(memberData);
                if (memberObjectInfo == null)
                {
                    TypeToNameInfo(memberType, memberTypeNameInfo);
                }
                else
                {
                    TypeToNameInfo(memberObjectInfo, memberTypeNameInfo);
                }
            }

            if (memberObjectInfo != null && memberObjectInfo._isArray)
            {
                // Array
                long arrayId = 0;
                if (outType == null)
                {
                    outType = GetType(outObj);
                }
                
                // outObj is an array. It can never be a value type..
                arrayId = Schedule(outObj, false, null, memberObjectInfo);
                if (arrayId > 0)
                {
                    // Array as object
                    memberNameInfo._objectId = arrayId;
                    WriteObjectRef(memberNameInfo, arrayId);
                }
                else
                {
                    // Nested Array
                    _serWriter.WriteMemberNested(memberNameInfo);

                    memberObjectInfo._objectId = arrayId;
                    memberNameInfo._objectId = arrayId;
                    WriteArray(memberObjectInfo, memberNameInfo, memberObjectInfo);
                    objectInfo.ObjectEnd();
                }
                return;
            }

            if (!WriteKnownValueClass(memberNameInfo, memberTypeNameInfo, memberData))
            {
                if (outType == null)
                {
                    outType = GetType(outObj);
                }

                long memberObjectId = Schedule(outObj, assignUniqueIdToValueType, outType, memberObjectInfo);
                if (memberObjectId < 0)
                {
                    // Nested object
                    memberObjectInfo._objectId = memberObjectId;
                    NameInfo newTypeNameInfo = TypeToNameInfo(memberObjectInfo);
                    newTypeNameInfo._objectId = memberObjectId;
                    Write(memberObjectInfo, memberNameInfo, newTypeNameInfo);
                    PutNameInfo(newTypeNameInfo);
                    memberObjectInfo.ObjectEnd();
                }
                else
                {
                    // Object reference
                    memberNameInfo._objectId = memberObjectId;
                    WriteObjectRef(memberNameInfo, memberObjectId);
                }
            }
        }

        // Writes out an array
        private void WriteArray(WriteObjectInfo objectInfo, NameInfo memberNameInfo, WriteObjectInfo memberObjectInfo)
        {
            bool isAllocatedMemberNameInfo = false;
            if (memberNameInfo == null)
            {
                memberNameInfo = TypeToNameInfo(objectInfo);
                isAllocatedMemberNameInfo = true;
            }

            memberNameInfo._isArray = true;

            long objectId = objectInfo._objectId;
            memberNameInfo._objectId = objectInfo._objectId;

            // Get array type
            Array array = (Array)objectInfo._obj;
            //Type arrayType = array.GetType();
            Type arrayType = objectInfo._objectType;

            // Get type of array element 
            Type arrayElemType = arrayType.GetElementType();
            WriteObjectInfo arrayElemObjectInfo = null;
            if (!arrayElemType.IsPrimitive)
            {
                arrayElemObjectInfo = WriteObjectInfo.Serialize(arrayElemType, _surrogates, _context, _serObjectInfoInit, _formatterConverter, _binder);
                arrayElemObjectInfo._assemId = GetAssemblyId(arrayElemObjectInfo);
            }

            NameInfo arrayElemTypeNameInfo = arrayElemObjectInfo == null ?
                TypeToNameInfo(arrayElemType) :
                TypeToNameInfo(arrayElemObjectInfo);
            arrayElemTypeNameInfo._isArray = arrayElemTypeNameInfo._type.IsArray;

            NameInfo arrayNameInfo = memberNameInfo;
            arrayNameInfo._objectId = objectId;
            arrayNameInfo._isArray = true;
            arrayElemTypeNameInfo._objectId = objectId;
            arrayElemTypeNameInfo._transmitTypeOnMember = memberNameInfo._transmitTypeOnMember;
            arrayElemTypeNameInfo._transmitTypeOnObject = memberNameInfo._transmitTypeOnObject;
            arrayElemTypeNameInfo._isParentTypeOnObject = memberNameInfo._isParentTypeOnObject;

            // Get rank and length information
            int rank = array.Rank;
            int[] lengthA = new int[rank];
            int[] lowerBoundA = new int[rank];
            int[] upperBoundA = new int[rank];
            for (int i = 0; i < rank; i++)
            {
                lengthA[i] = array.GetLength(i);
                lowerBoundA[i] = array.GetLowerBound(i);
                upperBoundA[i] = array.GetUpperBound(i);
            }

            InternalArrayTypeE arrayEnum;
            if (arrayElemTypeNameInfo._isArray)
            {
                arrayEnum = rank == 1 ? InternalArrayTypeE.Jagged : InternalArrayTypeE.Rectangular;
            }
            else if (rank == 1)
            {
                arrayEnum = InternalArrayTypeE.Single;
            }
            else
            {
                arrayEnum = InternalArrayTypeE.Rectangular;
            }
            arrayElemTypeNameInfo._arrayEnum = arrayEnum;

            // Byte array
            if ((ReferenceEquals(arrayElemType, Converter.s_typeofByte)) && (rank == 1) && (lowerBoundA[0] == 0))
            {
                _serWriter.WriteObjectByteArray(memberNameInfo, arrayNameInfo, arrayElemObjectInfo, arrayElemTypeNameInfo, lengthA[0], lowerBoundA[0], (byte[])array);
                return;
            }

            if (ReferenceEquals(arrayElemType, Converter.s_typeofObject) || Nullable.GetUnderlyingType(arrayElemType) != null)
            {
                memberNameInfo._transmitTypeOnMember = true;
                arrayElemTypeNameInfo._transmitTypeOnMember = true;
            }

            if (CheckTypeFormat(_formatterEnums._typeFormat, FormatterTypeStyle.TypesAlways))
            {
                memberNameInfo._transmitTypeOnObject = true;
                arrayElemTypeNameInfo._transmitTypeOnObject = true;
            }

            if (arrayEnum == InternalArrayTypeE.Single)
            {
                // Single Dimensional array

                // BinaryFormatter array of primitive types is written out in the WriteSingleArray statement
                // as a byte buffer
                _serWriter.WriteSingleArray(memberNameInfo, arrayNameInfo, arrayElemObjectInfo, arrayElemTypeNameInfo, lengthA[0], lowerBoundA[0], array);

                if (!(Converter.IsWriteAsByteArray(arrayElemTypeNameInfo._primitiveTypeEnum) && (lowerBoundA[0] == 0)))
                {
                    object[] objectA = null;
                    if (!arrayElemType.IsValueType)
                    {
                        // Non-primitive type array                 
                        objectA = (object[])array;
                    }

                    int upperBound = upperBoundA[0] + 1;
                    for (int i = lowerBoundA[0]; i < upperBound; i++)
                    {
                        if (objectA == null)
                        {
                            WriteArrayMember(objectInfo, arrayElemTypeNameInfo, array.GetValue(i));
                        }
                        else
                        {
                            WriteArrayMember(objectInfo, arrayElemTypeNameInfo, objectA[i]);
                        }
                    }
                    _serWriter.WriteItemEnd();
                }
            }
            else if (arrayEnum == InternalArrayTypeE.Jagged)
            {
                // Jagged Array

                arrayNameInfo._objectId = objectId;

                _serWriter.WriteJaggedArray(memberNameInfo, arrayNameInfo, arrayElemObjectInfo, arrayElemTypeNameInfo, lengthA[0], lowerBoundA[0]);

                var objectA = (Array)array;
                for (int i = lowerBoundA[0]; i < upperBoundA[0] + 1; i++)
                {
                    WriteArrayMember(objectInfo, arrayElemTypeNameInfo, objectA.GetValue(i));
                }
                _serWriter.WriteItemEnd();
            }
            else
            {
                // Rectangle Array
                // Get the length for all the ranks

                arrayNameInfo._objectId = objectId;
                _serWriter.WriteRectangleArray(memberNameInfo, arrayNameInfo, arrayElemObjectInfo, arrayElemTypeNameInfo, rank, lengthA, lowerBoundA);

                // Check for a length of zero
                bool bzero = false;
                for (int i = 0; i < rank; i++)
                {
                    if (lengthA[i] == 0)
                    {
                        bzero = true;
                        break;
                    }
                }

                if (!bzero)
                {
                    WriteRectangle(objectInfo, rank, lengthA, array, arrayElemTypeNameInfo, lowerBoundA);
                }
                _serWriter.WriteItemEnd();
            }

            _serWriter.WriteObjectEnd(memberNameInfo, arrayNameInfo);

            PutNameInfo(arrayElemTypeNameInfo);
            if (isAllocatedMemberNameInfo)
            {
                PutNameInfo(memberNameInfo);
            }
        }

        // Writes out an array element
        private void WriteArrayMember(WriteObjectInfo objectInfo, NameInfo arrayElemTypeNameInfo, object data)
        {
            arrayElemTypeNameInfo._isArrayItem = true;

            if (CheckForNull(objectInfo, arrayElemTypeNameInfo, arrayElemTypeNameInfo, data))
            {
                return;
            }

            NameInfo actualTypeInfo = null;
            Type dataType = null;
            bool isObjectOnMember = false;

            if (arrayElemTypeNameInfo._transmitTypeOnMember)
            {
                isObjectOnMember = true;
            }

            if (!isObjectOnMember && !arrayElemTypeNameInfo.IsSealed)
            {
                dataType = GetType(data);
                if (!ReferenceEquals(arrayElemTypeNameInfo._type, dataType))
                {
                    isObjectOnMember = true;
                }
            }

            if (isObjectOnMember)
            {
                // Object array, need type of member
                if (dataType == null)
                {
                    dataType = GetType(data);
                }
                actualTypeInfo = TypeToNameInfo(dataType);
                actualTypeInfo._transmitTypeOnMember = true;
                actualTypeInfo._objectId = arrayElemTypeNameInfo._objectId;
                actualTypeInfo._assemId = arrayElemTypeNameInfo._assemId;
                actualTypeInfo._isArrayItem = true;
            }
            else
            {
                actualTypeInfo = arrayElemTypeNameInfo;
                actualTypeInfo._isArrayItem = true;
            }

            if (!WriteKnownValueClass(arrayElemTypeNameInfo, actualTypeInfo, data))
            {
                object obj = data;
                bool assignUniqueIdForValueTypes = false;
                if (ReferenceEquals(arrayElemTypeNameInfo._type, Converter.s_typeofObject))
                {
                    assignUniqueIdForValueTypes = true;
                }

                long arrayId = Schedule(obj, assignUniqueIdForValueTypes, actualTypeInfo._type);
                arrayElemTypeNameInfo._objectId = arrayId;
                actualTypeInfo._objectId = arrayId;
                if (arrayId < 1)
                {
                    WriteObjectInfo newObjectInfo = WriteObjectInfo.Serialize(obj, _surrogates, _context, _serObjectInfoInit, _formatterConverter, this, _binder);
                    newObjectInfo._objectId = arrayId;
                    newObjectInfo._assemId = !ReferenceEquals(arrayElemTypeNameInfo._type, Converter.s_typeofObject) && Nullable.GetUnderlyingType(arrayElemTypeNameInfo._type) == null ?
                        actualTypeInfo._assemId :
                        GetAssemblyId(newObjectInfo);
                    NameInfo typeNameInfo = TypeToNameInfo(newObjectInfo);
                    typeNameInfo._objectId = arrayId;
                    newObjectInfo._objectId = arrayId;
                    Write(newObjectInfo, actualTypeInfo, typeNameInfo);
                    newObjectInfo.ObjectEnd();
                }
                else
                {
                    _serWriter.WriteItemObjectRef(arrayElemTypeNameInfo, (int)arrayId);
                }
            }
            if (arrayElemTypeNameInfo._transmitTypeOnMember)
            {
                PutNameInfo(actualTypeInfo);
            }
        }

        // Iterates over a Rectangle array, for each element of the array invokes WriteArrayMember
        private void WriteRectangle(WriteObjectInfo objectInfo, int rank, int[] maxA, Array array, NameInfo arrayElemNameTypeInfo, int[] lowerBoundA)
        {
            int[] currentA = new int[rank];
            int[] indexMap = null;
            bool isLowerBound = false;
            if (lowerBoundA != null)
            {
                for (int i = 0; i < rank; i++)
                {
                    if (lowerBoundA[i] != 0)
                    {
                        isLowerBound = true;
                    }
                }
            }
            if (isLowerBound)
            {
                indexMap = new int[rank];
            }

            bool isLoop = true;
            while (isLoop)
            {
                isLoop = false;
                if (isLowerBound)
                {
                    for (int i = 0; i < rank; i++)
                    {
                        indexMap[i] = currentA[i] + lowerBoundA[i];
                    }

                    WriteArrayMember(objectInfo, arrayElemNameTypeInfo, array.GetValue(indexMap));
                }
                else
                {
                    WriteArrayMember(objectInfo, arrayElemNameTypeInfo, array.GetValue(currentA));
                }

                for (int irank = rank - 1; irank > -1; irank--)
                {
                    // Find the current or lower dimension which can be incremented.
                    if (currentA[irank] < maxA[irank] - 1)
                    {
                        // The current dimension is at maximum. Increase the next lower dimension by 1
                        currentA[irank]++;
                        if (irank < rank - 1)
                        {
                            // The current dimension and higher dimensions are zeroed.
                            for (int i = irank + 1; i < rank; i++)
                            {
                                currentA[i] = 0;
                            }
                        }
                        isLoop = true;
                        break;
                    }
                }
            }
        }

        // This gives back the next object to be serialized.  Objects
        // are returned in a FIFO order based on how they were passed
        // to Schedule.  The id of the object is put into the objID parameter
        // and the Object itself is returned from the function.
        private object GetNext(out long objID)
        {
            bool isNew;

            //The Queue is empty here.  We'll throw if we try to dequeue the empty queue.
            if (_objectQueue.Count == 0)
            {
                objID = 0;
                return null;
            }

            object obj = _objectQueue.Dequeue();

            // A WriteObjectInfo is queued if this object was a member of another object
            object realObj = obj is WriteObjectInfo ? ((WriteObjectInfo)obj)._obj : obj;
            objID = _idGenerator.HasId(realObj, out isNew);
            if (isNew)
            {
                throw new SerializationException(SR.Format(SR.Serialization_ObjNoID, realObj));
            }

            return obj;
        }

        // If the type is a value type, we dont attempt to generate a unique id, unless its a boxed entity
        // (in which case, there might be 2 references to the same boxed obj. in a graph.)
        // "assignUniqueIdToValueType" is true, if the field type holding reference to "obj" is Object.
        private long InternalGetId(object obj, bool assignUniqueIdToValueType, Type type, out bool isNew)
        {
            if (obj == _previousObj)
            {
                // good for benchmarks
                isNew = false;
                return _previousId;
            }
            _idGenerator._currentCount = _currentId;
            if (type != null && type.IsValueType)
            {
                if (!assignUniqueIdToValueType)
                {
                    isNew = false;
                    return -1 * _currentId++;
                }
            }
            _currentId++;
            long retId = _idGenerator.GetId(obj, out isNew);

            _previousObj = obj;
            _previousId = retId;
            return retId;
        }


        // Schedules an object for later serialization if it hasn't already been scheduled.
        // We get an ID for obj and put it on the queue for later serialization
        // if this is a new object id.

        private long Schedule(object obj, bool assignUniqueIdToValueType, Type type) => 
            Schedule(obj, assignUniqueIdToValueType, type, null);

        private long Schedule(object obj, bool assignUniqueIdToValueType, Type type, WriteObjectInfo objectInfo)
        {
            long id = 0;
            if (obj != null)
            {
                bool isNew;
                id = InternalGetId(obj, assignUniqueIdToValueType, type, out isNew);
                if (isNew && id > 0)
                {
                    _objectQueue.Enqueue(objectInfo ?? obj);
                }
            }
            return id;
        }

        // Determines if a type is a primitive type, if it is it is written
        private bool WriteKnownValueClass(NameInfo memberNameInfo, NameInfo typeNameInfo, object data)
        {
            if (ReferenceEquals(typeNameInfo._type, Converter.s_typeofString))
            {
                WriteString(memberNameInfo, typeNameInfo, data);
            }
            else
            {
                if (typeNameInfo._primitiveTypeEnum == InternalPrimitiveTypeE.Invalid)
                {
                    return false;
                }
                else
                {
                    if (typeNameInfo._isArray) // null if an array
                    {
                        _serWriter.WriteItem(memberNameInfo, typeNameInfo, data);
                    }
                    else
                    {
                        _serWriter.WriteMember(memberNameInfo, typeNameInfo, data);
                    }
                }
            }

            return true;
        }


        // Writes an object reference to the stream.
        private void WriteObjectRef(NameInfo nameInfo, long objectId) =>
            _serWriter.WriteMemberObjectRef(nameInfo, (int)objectId);

        // Writes a string into the XML stream
        private void WriteString(NameInfo memberNameInfo, NameInfo typeNameInfo, object stringObject)
        {
            bool isFirstTime = true;

            long stringId = -1;

            if (!CheckTypeFormat(_formatterEnums._typeFormat, FormatterTypeStyle.XsdString))
            {
                stringId = InternalGetId(stringObject, false, null, out isFirstTime);
            }
            typeNameInfo._objectId = stringId;

            if ((isFirstTime) || (stringId < 0))
            {
                _serWriter.WriteMemberString(memberNameInfo, typeNameInfo, (string)stringObject);
            }
            else
            {
                WriteObjectRef(memberNameInfo, stringId);
            }
        }

        // Writes a null member into the stream
        private bool CheckForNull(WriteObjectInfo objectInfo, NameInfo memberNameInfo, NameInfo typeNameInfo, object data)
        {
            bool isNull = data == null;

            // Optimization, Null members are only written for Binary
            if ((isNull) && (((_formatterEnums._serializerTypeEnum == InternalSerializerTypeE.Binary)) ||
                             memberNameInfo._isArrayItem ||
                             memberNameInfo._transmitTypeOnObject ||
                             memberNameInfo._transmitTypeOnMember ||
                             objectInfo._isSi ||
                             (CheckTypeFormat(_formatterEnums._typeFormat, FormatterTypeStyle.TypesAlways))))
            {
                if (typeNameInfo._isArrayItem)
                {
                    if (typeNameInfo._arrayEnum == InternalArrayTypeE.Single)
                    {
                        _serWriter.WriteDelayedNullItem();
                    }
                    else
                    {
                        _serWriter.WriteNullItem(memberNameInfo, typeNameInfo);
                    }
                }
                else
                {
                    _serWriter.WriteNullMember(memberNameInfo, typeNameInfo);
                }
            }

            return isNull;
        }


        // Writes the SerializedStreamHeader
        private void WriteSerializedStreamHeader(long topId, long headerId) =>
            _serWriter.WriteSerializationHeader((int)topId, (int)headerId, 1, 0);

        // Transforms a type to the serialized string form. URT Primitive types are converted to XMLData Types
        private NameInfo TypeToNameInfo(Type type, WriteObjectInfo objectInfo, InternalPrimitiveTypeE code, NameInfo nameInfo)
        {
            if (nameInfo == null)
            {
                nameInfo = GetNameInfo();
            }
            else
            {
                nameInfo.Init();
            }

            if (code == InternalPrimitiveTypeE.Invalid)
            {
                if (objectInfo != null)
                {
                    nameInfo.NIname = objectInfo.GetTypeFullName();
                    nameInfo._assemId = objectInfo._assemId;
                }
            }
            nameInfo._primitiveTypeEnum = code;
            nameInfo._type = type;

            return nameInfo;
        }

        private NameInfo TypeToNameInfo(Type type) => 
            TypeToNameInfo(type, null, ToCode(type), null);

        private NameInfo TypeToNameInfo(WriteObjectInfo objectInfo) =>
            TypeToNameInfo(objectInfo._objectType, objectInfo, ToCode(objectInfo._objectType), null);

        private NameInfo TypeToNameInfo(WriteObjectInfo objectInfo, NameInfo nameInfo) =>
            TypeToNameInfo(objectInfo._objectType, objectInfo, ToCode(objectInfo._objectType), nameInfo);

        private void TypeToNameInfo(Type type, NameInfo nameInfo) =>
            TypeToNameInfo(type, null, ToCode(type), nameInfo);

        private NameInfo MemberToNameInfo(string name)
        {
            NameInfo memberNameInfo = GetNameInfo();
            memberNameInfo.NIname = name;
            return memberNameInfo;
        }

        internal InternalPrimitiveTypeE ToCode(Type type)
        {
            if (ReferenceEquals(_previousType, type))
            {
                return _previousCode;
            }
            else
            {
                InternalPrimitiveTypeE code = Converter.ToCode(type);
                if (code != InternalPrimitiveTypeE.Invalid)
                {
                    _previousType = type;
                    _previousCode = code;
                }
                return code;
            }
        }

        private Dictionary<string, long> _assemblyToIdTable = null;
        private long GetAssemblyId(WriteObjectInfo objectInfo)
        {
            //use objectInfo to get assembly string with new criteria
            if (_assemblyToIdTable == null)
            {
                _assemblyToIdTable = new Dictionary<string, long>();
            }

            long assemId = 0;
            string assemblyString = objectInfo.GetAssemblyString();

            string serializedAssemblyString = assemblyString;
            if (assemblyString.Length == 0)
            {
                assemId = 0;
            }
            else if (assemblyString.Equals(Converter.s_urtAssemblyString) || assemblyString.Equals(Converter.s_urtAlternativeAssemblyString))
            {
                // Urt type is an assemId of 0. No assemblyString needs
                // to be sent 
                assemId = 0;
            }
            else
            {
                // Assembly needs to be sent
                // Need to prefix assembly string to separate the string names from the
                // assemblyName string names. That is a string can have the same value
                // as an assemblyNameString, but it is serialized differently
                bool isNew = false;
                if (_assemblyToIdTable.TryGetValue(assemblyString, out assemId))
                {
                    isNew = false;
                }
                else
                {
                    assemId = InternalGetId("___AssemblyString___" + assemblyString, false, null, out isNew);
                    _assemblyToIdTable[assemblyString] = assemId;
                }

                _serWriter.WriteAssembly(objectInfo._objectType, serializedAssemblyString, (int)assemId, isNew);
            }
            return assemId;
        }

        private Type GetType(object obj) => obj.GetType();

        private SerStack _niPool = new SerStack("NameInfo Pool");

        private NameInfo GetNameInfo()
        {
            NameInfo nameInfo = null;

            if (!_niPool.IsEmpty())
            {
                nameInfo = (NameInfo)_niPool.Pop();
                nameInfo.Init();
            }
            else
            {
                nameInfo = new NameInfo();
            }

            return nameInfo;
        }

        private bool CheckTypeFormat(FormatterTypeStyle test, FormatterTypeStyle want) => (test & want) == want;

        private void PutNameInfo(NameInfo nameInfo) => _niPool.Push(nameInfo);
    }
}
