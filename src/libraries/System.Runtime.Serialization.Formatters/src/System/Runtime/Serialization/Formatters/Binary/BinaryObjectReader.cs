// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal sealed class ObjectReader
    {
        private const string ObjectReaderUnreferencedCodeMessage = "ObjectReader requires unreferenced code";

        // System.Serializer information
        internal Stream _stream;
        internal ISurrogateSelector? _surrogates;
        internal StreamingContext _context;
        internal ObjectManager? _objectManager;
        internal InternalFE _formatterEnums;
        internal SerializationBinder? _binder;

        // Top object and headers
        internal long _topId;
        internal bool _isSimpleAssembly;
        internal object? _topObject;
        internal SerObjectInfoInit? _serObjectInfoInit;
        internal IFormatterConverter? _formatterConverter;

        // Stack of Object ParseRecords
        internal SerStack? _stack;

        // ValueType Fixup Stack
        private SerStack? _valueFixupStack;

        // Cross AppDomain
        internal object[]? _crossAppDomainArray; //Set by the BinaryFormatter

        //MethodCall and MethodReturn are handled special for perf reasons
        private bool _fullDeserialization;

        private SerStack ValueFixupStack => _valueFixupStack ??= new SerStack("ValueType Fixup Stack");

        // Older formatters generate ids for valuetypes using a different counter than ref types. Newer ones use
        // a single counter, only value types have a negative value. Need a way to handle older formats.
        private const int ThresholdForValueTypeIds = int.MaxValue;
        private bool _oldFormatDetected;
        private IntSizedArray? _valTypeObjectIdTable;

        private readonly NameCache _typeCache = new NameCache();

        internal object? TopObject
        {
            get { return _topObject; }
            set
            {
                _topObject = value;
                if (_objectManager != null)
                {
                    _objectManager.TopObject = value;
                }
            }
        }

        internal ObjectReader(Stream stream, ISurrogateSelector? selector, StreamingContext context, InternalFE formatterEnums, SerializationBinder? binder)
        {
            ArgumentNullException.ThrowIfNull(stream);

            _stream = stream;
            _surrogates = selector;
            _context = context;
            _binder = binder;
            _formatterEnums = formatterEnums;
        }

        [RequiresUnreferencedCode("Types might be removed")]
        internal object Deserialize(BinaryParser serParser)
        {
            ArgumentNullException.ThrowIfNull(serParser);

            _fullDeserialization = false;
            TopObject = null;
            _topId = 0;

            _isSimpleAssembly = (_formatterEnums._assemblyFormat == FormatterAssemblyStyle.Simple);

            using (DeserializationToken token = SerializationInfo.StartDeserialization())
            {
                if (_fullDeserialization)
                {
                    // Reinitialize
                    _objectManager = new ObjectManager(_surrogates, _context);
                    _serObjectInfoInit = new SerObjectInfoInit();
                }

                // Will call back to ParseObject, ParseHeader for each object found
                serParser.Run();

                if (_fullDeserialization)
                {
                    _objectManager!.DoFixups();
                }

                if (TopObject == null)
                {
                    throw new SerializationException(SR.Serialization_TopObject);
                }

                //if TopObject has a surrogate then the actual object may be changed during special fixup
                //So refresh it using topID.
                if (HasSurrogate(TopObject.GetType()) && _topId != 0)//Not yet resolved
                {
                    Debug.Assert(_objectManager != null);
                    TopObject = _objectManager.GetObject(_topId);
                }

                if (TopObject is IObjectReference)
                {
                    TopObject = ((IObjectReference)TopObject).GetRealObject(_context);
                }

                if (_fullDeserialization)
                {
                    _objectManager!.RaiseDeserializationEvent(); // This will raise both IDeserialization and [OnDeserialized] events
                }

                return TopObject!;
            }
        }
        private bool HasSurrogate(Type t)
        {
            return _surrogates != null && _surrogates.GetSurrogate(t, _context, out _) != null;
        }

        private void CheckSerializable(Type t)
        {
            if (!t.IsSerializable && !HasSurrogate(t))
            {
                throw new SerializationException(SR.Format(CultureInfo.InvariantCulture, SR.Serialization_NonSerType, t.FullName, t.Assembly.FullName));
            }
        }

        private void InitFullDeserialization()
        {
            _fullDeserialization = true;
            _stack = new SerStack("ObjectReader Object Stack");
            _objectManager = new ObjectManager(_surrogates, _context);
            _formatterConverter ??= new FormatterConverter();
        }

        internal object CrossAppDomainArray(int index)
        {
            Debug.Assert(_crossAppDomainArray != null);
            Debug.Assert(index < _crossAppDomainArray.Length, "[System.Runtime.Serialization.Formatters.BinaryObjectReader index out of range for CrossAppDomainArray]");
            return _crossAppDomainArray[index];
        }

        internal ReadObjectInfo CreateReadObjectInfo(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType)
        {
            return ReadObjectInfo.Create(objectType, _surrogates, _context, _objectManager, _serObjectInfoInit, _formatterConverter, _isSimpleAssembly);
        }

        internal ReadObjectInfo CreateReadObjectInfo(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? objectType,
            string[] memberNames,
            Type[]? memberTypes)
        {
            return ReadObjectInfo.Create(objectType, memberNames, memberTypes, _surrogates, _context, _objectManager, _serObjectInfoInit, _formatterConverter, _isSimpleAssembly);
        }

        [RequiresUnreferencedCode(ObjectReaderUnreferencedCodeMessage)]
        internal void Parse(ParseRecord pr)
        {
            switch (pr._parseTypeEnum)
            {
                case InternalParseTypeE.SerializedStreamHeader:
                    ParseSerializedStreamHeader(pr);
                    break;
                case InternalParseTypeE.SerializedStreamHeaderEnd:
                    ParseSerializedStreamHeaderEnd(pr);
                    break;
                case InternalParseTypeE.Object:
                    ParseObject(pr);
                    break;
                case InternalParseTypeE.ObjectEnd:
                    ParseObjectEnd(pr);
                    break;
                case InternalParseTypeE.Member:
                    ParseMember(pr);
                    break;
                case InternalParseTypeE.MemberEnd:
                    ParseMemberEnd(pr);
                    break;
                case InternalParseTypeE.Body:
                case InternalParseTypeE.BodyEnd:
                case InternalParseTypeE.Envelope:
                case InternalParseTypeE.EnvelopeEnd:
                    break;
                case InternalParseTypeE.Empty:
                default:
                    throw new SerializationException(SR.Format(SR.Serialization_XMLElement, pr._name));
            }
        }

        // Styled ParseError output
        private void ParseError(ParseRecord processing, ParseRecord onStack)
        {
            throw new SerializationException(SR.Format(SR.Serialization_ParseError, onStack._name + " " + onStack._parseTypeEnum + " " + processing._name + " " + processing._parseTypeEnum));
        }

        // Parse the SerializedStreamHeader element. This is the first element in the stream if present
        private void ParseSerializedStreamHeader(ParseRecord pr) => _stack!.Push(pr);

        // Parse the SerializedStreamHeader end element. This is the last element in the stream if present
        private void ParseSerializedStreamHeaderEnd(ParseRecord pr) => _stack!.Pop();

        // New object encountered in stream
        [RequiresUnreferencedCode(ObjectReaderUnreferencedCodeMessage)]
        private void ParseObject(ParseRecord pr)
        {
            if (!_fullDeserialization)
            {
                InitFullDeserialization();
            }

            if (pr._objectPositionEnum == InternalObjectPositionE.Top)
            {
                _topId = pr._objectId;
            }

            if (pr._parseTypeEnum == InternalParseTypeE.Object)
            {
                _stack!.Push(pr); // Nested objects member names are already on stack
            }

            if (pr._objectTypeEnum == InternalObjectTypeE.Array)
            {
                ParseArray(pr);
                return;
            }

            // If the Type is null, this means we have a typeload issue
            // mark the object with TypeLoadExceptionHolder
            if (pr._dtType == null)
            {
                pr._newObj = new TypeLoadExceptionHolder(pr._keyDt);
                return;
            }

            if (ReferenceEquals(pr._dtType, Converter.s_typeofString))
            {
                // String as a top level object
                if (pr._value != null)
                {
                    pr._newObj = pr._value;
                    if (pr._objectPositionEnum == InternalObjectPositionE.Top)
                    {
                        TopObject = pr._newObj;
                        return;
                    }
                    else
                    {
                        _stack!.Pop();
                        RegisterObject(pr._newObj, pr, (ParseRecord?)_stack.Peek());
                        return;
                    }
                }
                else
                {
                    // xml Doesn't have the value until later
                    return;
                }
            }
            else
            {
                CheckSerializable(pr._dtType);
                pr._newObj = FormatterServices.GetUninitializedObject(pr._dtType);

                Debug.Assert(_objectManager != null);
                // Run the OnDeserializing methods
                _objectManager.RaiseOnDeserializingEvent(pr._newObj);
            }

            if (pr._newObj == null)
            {
                throw new SerializationException(SR.Format(SR.Serialization_TopObjectInstantiate, pr._dtType));
            }

            if (pr._objectPositionEnum == InternalObjectPositionE.Top)
            {
                TopObject = pr._newObj;
            }

            pr._objectInfo ??= ReadObjectInfo.Create(pr._dtType, _surrogates, _context, _objectManager, _serObjectInfoInit, _formatterConverter, _isSimpleAssembly);
        }

        // End of object encountered in stream
        [RequiresUnreferencedCode(ObjectReaderUnreferencedCodeMessage)]
        private void ParseObjectEnd(ParseRecord pr)
        {
            Debug.Assert(_stack != null);
            ParseRecord objectPr = (ParseRecord?)_stack.Peek() ?? pr;

            if (objectPr._objectPositionEnum == InternalObjectPositionE.Top)
            {
                if (ReferenceEquals(objectPr._dtType, Converter.s_typeofString))
                {
                    objectPr._newObj = objectPr._value;
                    TopObject = objectPr._newObj;
                    return;
                }
            }

            _stack.Pop();
            ParseRecord? parentPr = (ParseRecord?)_stack.Peek();

            if (objectPr._newObj == null)
            {
                return;
            }

            if (objectPr._objectTypeEnum == InternalObjectTypeE.Array)
            {
                if (objectPr._objectPositionEnum == InternalObjectPositionE.Top)
                {
                    TopObject = objectPr._newObj;
                }

                RegisterObject(objectPr._newObj, objectPr, parentPr);
                return;
            }

            Debug.Assert(objectPr._objectInfo != null);
            objectPr._objectInfo.PopulateObjectMembers(objectPr._newObj, objectPr._memberData);

            // Registration is after object is populated
            if ((!objectPr._isRegistered) && (objectPr._objectId > 0))
            {
                RegisterObject(objectPr._newObj, objectPr, parentPr);
            }

            if (objectPr._isValueTypeFixup)
            {
                ValueFixup? fixup = (ValueFixup?)ValueFixupStack.Pop(); //Value fixup
                Debug.Assert(fixup != null && parentPr != null);
                fixup.Fixup(objectPr, parentPr);  // Value fixup
            }

            if (objectPr._objectPositionEnum == InternalObjectPositionE.Top)
            {
                TopObject = objectPr._newObj;
            }

            objectPr._objectInfo.ObjectEnd();
        }

        // Array object encountered in stream
        [RequiresUnreferencedCode(ObjectReaderUnreferencedCodeMessage)]
        private void ParseArray(ParseRecord pr)
        {
            Debug.Assert(_stack != null);
            if (pr._arrayTypeEnum == InternalArrayTypeE.Base64)
            {
                Debug.Assert(pr._value != null);
                // ByteArray
                pr._newObj = pr._value.Length > 0 ?
                    Convert.FromBase64String(pr._value) :
                    Array.Empty<byte>();

                if (_stack.Peek() == pr)
                {
                    _stack.Pop();
                }
                if (pr._objectPositionEnum == InternalObjectPositionE.Top)
                {
                    TopObject = pr._newObj;
                }

                ParseRecord? parentPr = (ParseRecord?)_stack.Peek();

                // Base64 can be registered at this point because it is populated
                RegisterObject(pr._newObj, pr, parentPr);
            }
            else if ((pr._newObj != null) && Converter.IsWriteAsByteArray(pr._arrayElementTypeCode))
            {
                // Primtive typed Array has already been read
                if (pr._objectPositionEnum == InternalObjectPositionE.Top)
                {
                    TopObject = pr._newObj;
                }

                ParseRecord? parentPr = (ParseRecord?)_stack.Peek();

                // Primitive typed array can be registered at this point because it is populated
                RegisterObject(pr._newObj, pr, parentPr);
            }
            else if ((pr._arrayTypeEnum == InternalArrayTypeE.Jagged) || (pr._arrayTypeEnum == InternalArrayTypeE.Single))
            {
                Debug.Assert(pr._lengthA != null);
                // Multidimensional jagged array or single array
                bool couldBeValueType = true;
                if ((pr._lowerBoundA == null) || (pr._lowerBoundA[0] == 0))
                {
                    if (ReferenceEquals(pr._arrayElementType, Converter.s_typeofString))
                    {
                        pr._objectA = new string[pr._lengthA[0]];
                        pr._newObj = pr._objectA;
                        couldBeValueType = false;
                    }
                    else if (ReferenceEquals(pr._arrayElementType, Converter.s_typeofObject))
                    {
                        pr._objectA = new object[pr._lengthA[0]];
                        pr._newObj = pr._objectA;
                        couldBeValueType = false;
                    }
                    else if (pr._arrayElementType != null)
                    {
                        pr._newObj = Array.CreateInstance(pr._arrayElementType, pr._lengthA[0]);
                    }
                    pr._isLowerBound = false;
                }
                else
                {
                    if (pr._arrayElementType != null)
                    {
                        pr._newObj = Array.CreateInstance(pr._arrayElementType, pr._lengthA, pr._lowerBoundA);
                    }
                    pr._isLowerBound = true;
                }

                if (pr._arrayTypeEnum == InternalArrayTypeE.Single)
                {
                    if (!pr._isLowerBound && (Converter.IsWriteAsByteArray(pr._arrayElementTypeCode)))
                    {
                        Debug.Assert(pr._newObj != null);
                        pr._primitiveArray = new PrimitiveArray(pr._arrayElementTypeCode, (Array)pr._newObj);
                    }
                    else if (couldBeValueType && pr._arrayElementType != null)
                    {
                        if (!pr._arrayElementType.IsValueType && !pr._isLowerBound)
                        {
                            pr._objectA = (object[]?)pr._newObj;
                        }
                    }
                }

                pr._indexMap = new int[1];
            }
            else if (pr._arrayTypeEnum == InternalArrayTypeE.Rectangular)
            {
                // Rectangle array

                pr._isLowerBound = false;
                if (pr._lowerBoundA != null)
                {
                    for (int i = 0; i < pr._rank; i++)
                    {
                        if (pr._lowerBoundA[i] != 0)
                        {
                            pr._isLowerBound = true;
                        }
                    }
                }

                Debug.Assert(pr._lengthA != null);
                if (pr._arrayElementType != null)
                {
                    Debug.Assert(pr._lowerBoundA != null);
                    pr._newObj = !pr._isLowerBound ?
                        Array.CreateInstance(pr._arrayElementType, pr._lengthA) :
                        Array.CreateInstance(pr._arrayElementType, pr._lengthA, pr._lowerBoundA);
                }

                // Calculate number of items
                int sum = 1;
                for (int i = 0; i < pr._rank; i++)
                {
                    sum *= pr._lengthA[i];
                }
                pr._indexMap = new int[pr._rank];
                pr._rectangularMap = new int[pr._rank];
                pr._linearlength = sum;
            }
            else
            {
                throw new SerializationException(SR.Format(SR.Serialization_ArrayType, pr._arrayTypeEnum));
            }
        }

        // Builds a map for each item in an incoming rectangle array. The map specifies where the item is placed in the output Array Object
        private void NextRectangleMap(ParseRecord pr)
        {
            Debug.Assert(pr._rectangularMap != null && pr._lengthA != null && pr._indexMap != null);
            // For each invocation, calculate the next rectangular array position
            // example
            // indexMap 0 [0,0,0]
            // indexMap 1 [0,0,1]
            // indexMap 2 [0,0,2]
            // indexMap 3 [0,0,3]
            // indexMap 4 [0,1,0]
            for (int irank = pr._rank - 1; irank > -1; irank--)
            {
                // Find the current or lower dimension which can be incremented.
                if (pr._rectangularMap[irank] < pr._lengthA[irank] - 1)
                {
                    // The current dimension is at maximum. Increase the next lower dimension by 1
                    pr._rectangularMap[irank]++;
                    if (irank < pr._rank - 1)
                    {
                        // The current dimension and higher dimensions are zeroed.
                        for (int i = irank + 1; i < pr._rank; i++)
                        {
                            pr._rectangularMap[i] = 0;
                        }
                    }
                    Array.Copy(pr._rectangularMap, pr._indexMap, pr._rank);
                    break;
                }
            }
        }


        // Array object item encountered in stream
        [RequiresUnreferencedCode(ObjectReaderUnreferencedCodeMessage)]
        private void ParseArrayMember(ParseRecord pr)
        {
            Debug.Assert(_stack != null);
            ParseRecord? objectPr = (ParseRecord?)_stack.Peek();

            Debug.Assert(objectPr != null && objectPr._indexMap != null && objectPr._lowerBoundA != null);
            // Set up for inserting value into correct array position
            if (objectPr._arrayTypeEnum == InternalArrayTypeE.Rectangular)
            {
                if (objectPr._memberIndex > 0)
                {
                    NextRectangleMap(objectPr); // Rectangle array, calculate position in array
                }
                if (objectPr._isLowerBound)
                {
                    Debug.Assert(objectPr._rectangularMap != null);
                    for (int i = 0; i < objectPr._rank; i++)
                    {
                        objectPr._indexMap[i] = objectPr._rectangularMap[i] + objectPr._lowerBoundA[i];
                    }
                }
            }
            else
            {
                objectPr._indexMap[0] = !objectPr._isLowerBound ?
                    objectPr._memberIndex : // Zero based array
                    objectPr._lowerBoundA[0] + objectPr._memberIndex; // Lower Bound based array
            }

            // Set Array element according to type of element

            if (pr._memberValueEnum == InternalMemberValueE.Reference)
            {
                // Object Reference

                Debug.Assert(_objectManager != null);
                // See if object has already been instantiated
                object? refObj = _objectManager.GetObject(pr._idRef);
                if (refObj == null)
                {
                    // Object not instantiated
                    // Array fixup manager
                    int[] fixupIndex = new int[objectPr._rank];
                    Array.Copy(objectPr._indexMap, fixupIndex, objectPr._rank);

                    _objectManager.RecordArrayElementFixup(objectPr._objectId, fixupIndex, pr._idRef);
                }
                else
                {
                    if (objectPr._objectA != null)
                    {
                        objectPr._objectA[objectPr._indexMap[0]] = refObj;
                    }
                    else
                    {
                        Debug.Assert(objectPr._newObj != null);
                        ((Array)objectPr._newObj).SetValue(refObj, objectPr._indexMap); // Object has been instantiated
                    }
                }
            }
            else if (pr._memberValueEnum == InternalMemberValueE.Nested)
            {
                //Set up dtType for ParseObject
                if (pr._dtType == null)
                {
                    pr._dtType = objectPr._arrayElementType;
                }

                ParseObject(pr);
                _stack.Push(pr);

                if (objectPr._arrayElementType != null)
                {
                    Debug.Assert(objectPr._newObj != null);
                    if ((objectPr._arrayElementType.IsValueType) && (pr._arrayElementTypeCode == InternalPrimitiveTypeE.Invalid))
                    {
                        pr._isValueTypeFixup = true; //Valuefixup
                        ValueFixupStack.Push(new ValueFixup((Array)objectPr._newObj, objectPr._indexMap)); //valuefixup
                    }
                    else
                    {
                        if (objectPr._objectA != null)
                        {
                            objectPr._objectA[objectPr._indexMap[0]] = pr._newObj;
                        }
                        else
                        {
                            ((Array)objectPr._newObj).SetValue(pr._newObj, objectPr._indexMap);
                        }
                    }
                }
            }
            else if (pr._memberValueEnum == InternalMemberValueE.InlineValue)
            {
                if ((ReferenceEquals(objectPr._arrayElementType, Converter.s_typeofString)) || (ReferenceEquals(pr._dtType, Converter.s_typeofString)))
                {
                    // String in either a string array, or a string element of an object array
                    ParseString(pr, objectPr);
                    if (objectPr._objectA != null)
                    {
                        objectPr._objectA[objectPr._indexMap[0]] = pr._value;
                    }
                    else
                    {
                        Debug.Assert(objectPr._newObj != null);
                        ((Array)objectPr._newObj).SetValue(pr._value, objectPr._indexMap);
                    }
                }
                else if (objectPr._isArrayVariant)
                {
                    // Array of type object
                    if (pr._keyDt == null)
                    {
                        throw new SerializationException(SR.Serialization_ArrayTypeObject);
                    }

                    object? var;

                    if (ReferenceEquals(pr._dtType, Converter.s_typeofString))
                    {
                        ParseString(pr, objectPr);
                        var = pr._value;
                    }
                    else
                    {
                        var = pr._varValue ?? Converter.FromString(pr._value, pr._dtTypeCode);
                    }
                    if (objectPr._objectA != null)
                    {
                        objectPr._objectA[objectPr._indexMap[0]] = var;
                    }
                    else
                    {
                        Debug.Assert(objectPr._newObj != null);
                        ((Array)objectPr._newObj).SetValue(var, objectPr._indexMap); // Primitive type
                    }
                }
                else
                {
                    // Primitive type
                    if (objectPr._primitiveArray != null)
                    {
                        Debug.Assert(pr._value != null);
                        // Fast path for Soap primitive arrays. Binary was handled in the BinaryParser
                        objectPr._primitiveArray.SetValue(pr._value, objectPr._indexMap[0]);
                    }
                    else
                    {
                        object? var = pr._varValue ?? Converter.FromString(pr._value, objectPr._arrayElementTypeCode);
                        if (objectPr._objectA != null)
                        {
                            objectPr._objectA[objectPr._indexMap[0]] = var;
                        }
                        else
                        {
                            Debug.Assert(objectPr._newObj != null);
                            ((Array)objectPr._newObj).SetValue(var, objectPr._indexMap); // Primitive type
                        }
                    }
                }
            }
            else if (pr._memberValueEnum == InternalMemberValueE.Null)
            {
                objectPr._memberIndex += pr._consecutiveNullArrayEntryCount - 1; //also incremented again below
            }
            else
            {
                ParseError(pr, objectPr);
            }

            objectPr._memberIndex++;
        }

        [RequiresUnreferencedCode(ObjectReaderUnreferencedCodeMessage)]
        private void ParseArrayMemberEnd(ParseRecord pr)
        {
            // If this is a nested array object, then pop the stack
            if (pr._memberValueEnum == InternalMemberValueE.Nested)
            {
                ParseObjectEnd(pr);
            }
        }

        // Object member encountered in stream
        [RequiresUnreferencedCode(ObjectReaderUnreferencedCodeMessage)]
        private void ParseMember(ParseRecord pr)
        {
            Debug.Assert(_stack != null);
            ParseRecord? objectPr = (ParseRecord?)_stack.Peek();

            switch (pr._memberTypeEnum)
            {
                case InternalMemberTypeE.Item:
                    ParseArrayMember(pr);
                    return;
                case InternalMemberTypeE.Field:
                    break;
            }

            Debug.Assert(objectPr!= null && objectPr._objectInfo != null && pr._name != null);
            //if ((pr.PRdtType == null) && !objectPr.PRobjectInfo.isSi)
            if (pr._dtType == null && objectPr._objectInfo._isTyped)
            {
                pr._dtType = objectPr._objectInfo.GetType(pr._name);

                if (pr._dtType != null)
                {
                    pr._dtTypeCode = Converter.ToCode(pr._dtType);
                }
            }

            if (pr._memberValueEnum == InternalMemberValueE.Null)
            {
                // Value is Null
                objectPr._objectInfo.AddValue(pr._name, null, ref objectPr._si, ref objectPr._memberData);
            }
            else if (pr._memberValueEnum == InternalMemberValueE.Nested)
            {
                ParseObject(pr);
                _stack.Push(pr);

                if ((pr._objectInfo != null) && pr._objectInfo._objectType != null && (pr._objectInfo._objectType.IsValueType))
                {
                    pr._isValueTypeFixup = true; //Valuefixup
                    ValueFixupStack.Push(new ValueFixup(objectPr._newObj, pr._name, objectPr._objectInfo)); //valuefixup
                }
                else
                {
                    objectPr._objectInfo.AddValue(pr._name, pr._newObj, ref objectPr._si, ref objectPr._memberData);
                }
            }
            else if (pr._memberValueEnum == InternalMemberValueE.Reference)
            {
                Debug.Assert(_objectManager != null);
                // See if object has already been instantiated
                object? refObj = _objectManager.GetObject(pr._idRef);
                if (refObj == null)
                {
                    objectPr._objectInfo.AddValue(pr._name, null, ref objectPr._si, ref objectPr._memberData);
                    objectPr._objectInfo.RecordFixup(objectPr._objectId, pr._name, pr._idRef); // Object not instantiated
                }
                else
                {
                    objectPr._objectInfo.AddValue(pr._name, refObj, ref objectPr._si, ref objectPr._memberData);
                }
            }

            else if (pr._memberValueEnum == InternalMemberValueE.InlineValue)
            {
                // Primitive type or String
                if (ReferenceEquals(pr._dtType, Converter.s_typeofString))
                {
                    ParseString(pr, objectPr);
                    objectPr._objectInfo.AddValue(pr._name, pr._value, ref objectPr._si, ref objectPr._memberData);
                }
                else if (pr._dtTypeCode == InternalPrimitiveTypeE.Invalid)
                {
                    // The member field was an object put the value is Inline either  bin.Base64 or invalid
                    if (pr._arrayTypeEnum == InternalArrayTypeE.Base64)
                    {
                        Debug.Assert(pr._value != null);
                        objectPr._objectInfo.AddValue(pr._name, Convert.FromBase64String(pr._value), ref objectPr._si, ref objectPr._memberData);
                    }
                    else if (ReferenceEquals(pr._dtType, Converter.s_typeofObject))
                    {
                        throw new SerializationException(SR.Format(SR.Serialization_TypeMissing, pr._name));
                    }
                    else
                    {
                        ParseString(pr, objectPr); // Register the object if it has an objectId
                        // Object Class with no memberInfo data
                        // only special case where AddValue is needed?
                        if (ReferenceEquals(pr._dtType, Converter.s_typeofSystemVoid))
                        {
                            objectPr._objectInfo.AddValue(pr._name, pr._dtType, ref objectPr._si, ref objectPr._memberData);
                        }
                        else if (objectPr._objectInfo._isSi)
                        {
                            // ISerializable are added as strings, the conversion to type is done by the
                            // ISerializable object
                            objectPr._objectInfo.AddValue(pr._name, pr._value, ref objectPr._si, ref objectPr._memberData);
                        }
                    }
                }
                else
                {
                    object? var = pr._varValue ?? Converter.FromString(pr._value, pr._dtTypeCode);
                    objectPr._objectInfo.AddValue(pr._name, var, ref objectPr._si, ref objectPr._memberData);
                }
            }
            else
            {
                ParseError(pr, objectPr);
            }
        }

        // Object member end encountered in stream
        [RequiresUnreferencedCode(ObjectReaderUnreferencedCodeMessage)]
        private void ParseMemberEnd(ParseRecord pr)
        {
            switch (pr._memberTypeEnum)
            {
                case InternalMemberTypeE.Item:
                    ParseArrayMemberEnd(pr);
                    return;
                case InternalMemberTypeE.Field:
                    if (pr._memberValueEnum == InternalMemberValueE.Nested)
                    {
                        ParseObjectEnd(pr);
                    }
                    break;
                default:
                    Debug.Assert(_stack != null);
                    ParseError(pr, (ParseRecord)_stack.Peek()!);
                    break;
            }
        }

        // Processes a string object by getting an internal ID for it and registering it with the objectManager
        [RequiresUnreferencedCode(ObjectReaderUnreferencedCodeMessage)]
        private void ParseString(ParseRecord pr, ParseRecord parentPr)
        {
            // Process String class
            if ((!pr._isRegistered) && (pr._objectId > 0))
            {
                // String is treated as an object if it has an id
                //m_objectManager.RegisterObject(pr.PRvalue, pr.PRobjectId);
                RegisterObject(pr._value, pr, parentPr, true);
            }
        }

        [RequiresUnreferencedCode(ObjectReaderUnreferencedCodeMessage)]
        private void RegisterObject(object obj, ParseRecord pr, ParseRecord? objectPr)
        {
            RegisterObject(obj, pr, objectPr, false);
        }

        [RequiresUnreferencedCode(ObjectReaderUnreferencedCodeMessage)]
        private void RegisterObject(object? obj, ParseRecord pr, ParseRecord? objectPr, bool bIsString)
        {
            if (!pr._isRegistered)
            {
                pr._isRegistered = true;

                SerializationInfo? si;
                long parentId = 0;
                MemberInfo? memberInfo = null;
                int[]? indexMap = null;

                if (objectPr != null)
                {
                    indexMap = objectPr._indexMap;
                    parentId = objectPr._objectId;

                    if (objectPr._objectInfo != null)
                    {
                        if (!objectPr._objectInfo._isSi)
                        {
                            // ParentId is only used if there is a memberInfo
                            memberInfo = objectPr._objectInfo.GetMemberInfo(pr._name);
                        }
                    }
                }
                // SerializationInfo is always needed for ISerialization
                si = pr._si;

                Debug.Assert(_objectManager != null);
                if (bIsString)
                {
                    _objectManager.RegisterString((string?)obj, pr._objectId, si, parentId, memberInfo);
                }
                else
                {
                    Debug.Assert(obj != null);
                    _objectManager.RegisterObject(obj, pr._objectId, si, parentId, memberInfo, indexMap);
                }
            }
        }

        // Assigns an internal ID associated with the binary id number
        internal long GetId(long objectId)
        {
            if (!_fullDeserialization)
            {
                InitFullDeserialization();
            }

            if (objectId > 0)
            {
                return objectId;
            }

            if (_oldFormatDetected || objectId == -1)
            {
                // Alarm bells. This is an old format. Deal with it.
                _oldFormatDetected = true;
                _valTypeObjectIdTable ??= new IntSizedArray();

                long tempObjId;
                if ((tempObjId = _valTypeObjectIdTable[(int)objectId]) == 0)
                {
                    tempObjId = ThresholdForValueTypeIds + objectId;
                    _valTypeObjectIdTable[(int)objectId] = (int)tempObjId;
                }
                return tempObjId;
            }

            return -1 * objectId;
        }

        [RequiresUnreferencedCode("Types might be removed")]
        internal Type? Bind(string assemblyString, string typeString)
        {
            Type? type = null;
            if (_binder != null)
            {
                type = _binder.BindToType(assemblyString, typeString);
            }
            if (type == null)
            {
                type = FastBindToType(assemblyString, typeString);
            }
            return type;
        }

        internal sealed class TypeNAssembly
        {
            public Type? Type;
            public string? AssemblyName;
        }

        [RequiresUnreferencedCode("Types might be removed")]
        internal Type? FastBindToType(string? assemblyName, string typeName)
        {
            Type? type = null;

            TypeNAssembly? entry = (TypeNAssembly?)_typeCache.GetCachedValue(typeName);

            if (entry == null || entry.AssemblyName != assemblyName)
            {
                // Check early to avoid throwing unnecessary exceptions
                if (assemblyName == null)
                {
                    return null;
                }

                Assembly? assm = null;
                AssemblyName? assmName;

                try
                {
                    assmName = new AssemblyName(assemblyName);
                }
                catch
                {
                    return null;
                }

                if (_isSimpleAssembly)
                {
                    assm = ResolveSimpleAssemblyName(assmName);
                }
                else
                {
                    try
                    {
                        assm = Assembly.Load(assmName);
                    }
                    catch { }
                }

                if (assm == null)
                {
                    return null;
                }

                if (_isSimpleAssembly)
                {
                    GetSimplyNamedTypeFromAssembly(assm, typeName, ref type);
                }
                else
                {
                    type = FormatterServices.GetTypeFromAssembly(assm, typeName);
                }

                if (type == null)
                {
                    return null;
                }

                // before adding it to cache, let us do the security check
                CheckTypeForwardedTo(assm, type.Assembly, type);

                entry = new TypeNAssembly();
                entry.Type = type;
                entry.AssemblyName = assemblyName;
                _typeCache.SetCachedValue(entry);
            }
            return entry.Type;
        }

        private static Assembly? ResolveSimpleAssemblyName(AssemblyName assemblyName)
        {
            try
            {
                return Assembly.Load(assemblyName);
            }
            catch { }

            if (assemblyName != null)
            {
                try
                {
                    return Assembly.Load(assemblyName.Name!);
                }
                catch { }
            }

            return null;
        }

        [RequiresUnreferencedCode("Types might be removed")]
        private static void GetSimplyNamedTypeFromAssembly(Assembly assm, string typeName, ref Type? type)
        {
            // Catching any exceptions that could be thrown from a failure on assembly load
            // This is necessary, for example, if there are generic parameters that are qualified with a version of the assembly that predates the one available
            try
            {
                type = FormatterServices.GetTypeFromAssembly(assm, typeName);
            }
            catch (TypeLoadException) { }
            catch (FileNotFoundException) { }
            catch (FileLoadException) { }
            catch (BadImageFormatException) { }

            if (type == null)
            {
                type = Type.GetType(typeName, ResolveSimpleAssemblyName, new TopLevelAssemblyTypeResolver(assm).ResolveType, throwOnError: false);
            }
        }

        private string? _previousAssemblyString;
        private string? _previousName;
        private Type? _previousType;

        [RequiresUnreferencedCode("Types might be removed")]
        internal Type? GetType(BinaryAssemblyInfo assemblyInfo, string name)
        {
            Type? objectType;

            if (((_previousName != null) && (_previousName.Length == name.Length) && (_previousName.Equals(name))) &&
                ((_previousAssemblyString != null) && (_previousAssemblyString.Length == assemblyInfo._assemblyString.Length) && (_previousAssemblyString.Equals(assemblyInfo._assemblyString))))
            {
                objectType = _previousType;
            }
            else
            {
                objectType = Bind(assemblyInfo._assemblyString, name);
                if (objectType == null)
                {
                    Assembly sourceAssembly = assemblyInfo.GetAssembly();

                    if (_isSimpleAssembly)
                    {
                        GetSimplyNamedTypeFromAssembly(sourceAssembly, name, ref objectType);
                    }
                    else
                    {
                        objectType = FormatterServices.GetTypeFromAssembly(sourceAssembly, name);
                    }

                    // here let us do the security check
                    if (objectType != null)
                    {
                        CheckTypeForwardedTo(sourceAssembly, objectType.Assembly, objectType);
                    }
                }

                _previousAssemblyString = assemblyInfo._assemblyString;
                _previousName = name;
                _previousType = objectType;
            }
            return objectType;
        }

        private static void CheckTypeForwardedTo(Assembly sourceAssembly, Assembly destAssembly, Type resolvedType)
        {
            // nop on core
        }

        internal sealed class TopLevelAssemblyTypeResolver
        {
            private readonly Assembly _topLevelAssembly;

            public TopLevelAssemblyTypeResolver(Assembly topLevelAssembly)
            {
                _topLevelAssembly = topLevelAssembly;
            }

            [RequiresUnreferencedCode("Types might be removed")]
            public Type? ResolveType(Assembly? assembly, string simpleTypeName, bool ignoreCase)
            {
                if (assembly == null)
                {
                    assembly = _topLevelAssembly;
                }

                return assembly.GetType(simpleTypeName, throwOnError: false, ignoreCase: ignoreCase);
            }
        }
    }
}
