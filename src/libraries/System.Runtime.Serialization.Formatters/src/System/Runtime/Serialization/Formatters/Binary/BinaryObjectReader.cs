// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal sealed class ObjectReader
    {
        // System.Serializer information
        internal Stream _stream;
        internal ISurrogateSelector _surrogates;
        internal StreamingContext _context;
        internal ObjectManager _objectManager;
        internal InternalFE _formatterEnums;
        internal SerializationBinder _binder;

        // Top object and headers
        internal long _topId;
        internal bool _isSimpleAssembly = false;
        internal object _handlerObject;
        internal object _topObject;
        internal Header[] _headers;
        internal HeaderHandler _handler;
        internal SerObjectInfoInit _serObjectInfoInit;
        internal IFormatterConverter _formatterConverter;

        // Stack of Object ParseRecords
        internal SerStack _stack;

        // ValueType Fixup Stack
        private SerStack _valueFixupStack;

        // Cross AppDomain
        internal object[] _crossAppDomainArray; //Set by the BinaryFormatter

        //MethodCall and MethodReturn are handled special for perf reasons
        private bool _fullDeserialization;

        private SerStack ValueFixupStack => _valueFixupStack ?? (_valueFixupStack = new SerStack("ValueType Fixup Stack"));

        // Older formatters generate ids for valuetypes using a different counter than ref types. Newer ones use
        // a single counter, only value types have a negative value. Need a way to handle older formats.
        private const int ThresholdForValueTypeIds = int.MaxValue;
        private bool _oldFormatDetected = false;
        private IntSizedArray _valTypeObjectIdTable;

        private readonly NameCache _typeCache = new NameCache();

        internal object TopObject
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

        internal ObjectReader(Stream stream, ISurrogateSelector selector, StreamingContext context, InternalFE formatterEnums, SerializationBinder binder)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream), SR.ArgumentNull_Stream);
            }

            _stream = stream;
            _surrogates = selector;
            _context = context;
            _binder = binder;
            _formatterEnums = formatterEnums;
        }

        internal object Deserialize(HeaderHandler handler, BinaryParser serParser, bool fCheck)
        {
            if (serParser == null)
            {
                throw new ArgumentNullException(nameof(serParser), SR.Format(SR.ArgumentNull_WithParamName, serParser));
            }

            _fullDeserialization = false;
            TopObject = null;
            _topId = 0;

            _isSimpleAssembly = (_formatterEnums._FEassemblyFormat == FormatterAssemblyStyle.Simple);

            _handler = handler;

            if (_fullDeserialization)
            {
                // Reinitialize
                _objectManager = new ObjectManager(_surrogates, _context, false, false);
                _serObjectInfoInit = new SerObjectInfoInit();
            }

            // Will call back to ParseObject, ParseHeader for each object found
            serParser.Run();

            if (_fullDeserialization)
            {
                _objectManager.DoFixups();
            }

            if (TopObject == null)
            {
                throw new SerializationException(SR.Serialization_TopObject);
            }

            //if TopObject has a surrogate then the actual object may be changed during special fixup
            //So refresh it using topID.
            if (HasSurrogate(TopObject.GetType()) && _topId != 0)//Not yet resolved
            {
                TopObject = _objectManager.GetObject(_topId);
            }

            if (TopObject is IObjectReference)
            {
                TopObject = ((IObjectReference)TopObject).GetRealObject(_context);
            }

            if (_fullDeserialization)
            {
                _objectManager.RaiseDeserializationEvent(); // This will raise both IDeserialization and [OnDeserialized] events
            }

            // Return the headers if there is a handler
            if (handler != null)
            {
                _handlerObject = handler(_headers);
            }

            return TopObject;
        }

        private bool HasSurrogate(Type t)
        {
            ISurrogateSelector ignored;
            return _surrogates != null && _surrogates.GetSurrogate(t, _context, out ignored) != null;
        }

        private void CheckSerializable(Type t)
        {
            if (!t.GetTypeInfo().IsSerializable && !HasSurrogate(t))
            {
                throw new SerializationException(string.Format(CultureInfo.InvariantCulture, SR.Serialization_NonSerType, t.FullName, t.GetTypeInfo().Assembly.FullName));
            }
        }

        private void InitFullDeserialization()
        {
            _fullDeserialization = true;
            _stack = new SerStack("ObjectReader Object Stack");
            _objectManager = new ObjectManager(_surrogates, _context, false, false);
            if (_formatterConverter == null)
            {
                _formatterConverter = new FormatterConverter();
            }
        }

        internal object CrossAppDomainArray(int index)
        {
            Debug.Assert(index < _crossAppDomainArray.Length, "[System.Runtime.Serialization.Formatters.BinaryObjectReader index out of range for CrossAppDomainArray]");
            return _crossAppDomainArray[index];
        }

        internal ReadObjectInfo CreateReadObjectInfo(Type objectType)
        {
            return ReadObjectInfo.Create(objectType, _surrogates, _context, _objectManager, _serObjectInfoInit, _formatterConverter, _isSimpleAssembly);
        }

        internal ReadObjectInfo CreateReadObjectInfo(Type objectType, string[] memberNames, Type[] memberTypes)
        {
            return ReadObjectInfo.Create(objectType, memberNames, memberTypes, _surrogates, _context, _objectManager, _serObjectInfoInit, _formatterConverter, _isSimpleAssembly);
        }

        internal void Parse(ParseRecord pr)
        {
            switch (pr._PRparseTypeEnum)
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
                    throw new SerializationException(SR.Format(SR.Serialization_XMLElement, pr._PRname));
            }
        }

        // Styled ParseError output
        private void ParseError(ParseRecord processing, ParseRecord onStack)
        {
            throw new SerializationException(SR.Format(SR.Serialization_ParseError, onStack._PRname + " " + onStack._PRparseTypeEnum + " " + processing._PRname + " " + processing._PRparseTypeEnum));
        }

        // Parse the SerializedStreamHeader element. This is the first element in the stream if present
        private void ParseSerializedStreamHeader(ParseRecord pr) => _stack.Push(pr);

        // Parse the SerializedStreamHeader end element. This is the last element in the stream if present
        private void ParseSerializedStreamHeaderEnd(ParseRecord pr) => _stack.Pop();

        // New object encountered in stream
        private void ParseObject(ParseRecord pr)
        {
            if (!_fullDeserialization)
            {
                InitFullDeserialization();
            }

            if (pr._PRobjectPositionEnum == InternalObjectPositionE.Top)
            {
                _topId = pr._PRobjectId;
            }

            if (pr._PRparseTypeEnum == InternalParseTypeE.Object)
            {
                _stack.Push(pr); // Nested objects member names are already on stack
            }

            if (pr._PRobjectTypeEnum == InternalObjectTypeE.Array)
            {
                ParseArray(pr);
                return;
            }

            // If the Type is null, this means we have a typeload issue
            // mark the object with TypeLoadExceptionHolder
            if (pr._PRdtType == null)
            {
                pr._PRnewObj = new TypeLoadExceptionHolder(pr._PRkeyDt);
                return;
            }

            if (ReferenceEquals(pr._PRdtType, Converter.s_typeofString))
            {
                // String as a top level object
                if (pr._PRvalue != null)
                {
                    pr._PRnewObj = pr._PRvalue;
                    if (pr._PRobjectPositionEnum == InternalObjectPositionE.Top)
                    {
                        TopObject = pr._PRnewObj;
                        return;
                    }
                    else
                    {
                        _stack.Pop();
                        RegisterObject(pr._PRnewObj, pr, (ParseRecord)_stack.Peek());
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
                CheckSerializable(pr._PRdtType);
                pr._PRnewObj = FormatterServices.GetUninitializedObject(pr._PRdtType);

                // Run the OnDeserializing methods
                _objectManager.RaiseOnDeserializingEvent(pr._PRnewObj);
            }

            if (pr._PRnewObj == null)
            {
                throw new SerializationException(SR.Format(SR.Serialization_TopObjectInstantiate, pr._PRdtType));
            }

            if (pr._PRobjectPositionEnum == InternalObjectPositionE.Top)
            {
                TopObject = pr._PRnewObj;
            }

            if (pr._PRobjectInfo == null)
            {
                pr._PRobjectInfo = ReadObjectInfo.Create(pr._PRdtType, _surrogates, _context, _objectManager, _serObjectInfoInit, _formatterConverter, _isSimpleAssembly);
            }
        }

        // End of object encountered in stream
        private void ParseObjectEnd(ParseRecord pr)
        {
            ParseRecord objectPr = (ParseRecord)_stack.Peek() ?? pr;

            if (objectPr._PRobjectPositionEnum == InternalObjectPositionE.Top)
            {
                if (ReferenceEquals(objectPr._PRdtType, Converter.s_typeofString))
                {
                    objectPr._PRnewObj = objectPr._PRvalue;
                    TopObject = objectPr._PRnewObj;
                    return;
                }
            }

            _stack.Pop();
            ParseRecord parentPr = (ParseRecord)_stack.Peek();

            if (objectPr._PRnewObj == null)
            {
                return;
            }

            if (objectPr._PRobjectTypeEnum == InternalObjectTypeE.Array)
            {
                if (objectPr._PRobjectPositionEnum == InternalObjectPositionE.Top)
                {
                    TopObject = objectPr._PRnewObj;
                }

                RegisterObject(objectPr._PRnewObj, objectPr, parentPr);
                return;
            }

            objectPr._PRobjectInfo.PopulateObjectMembers(objectPr._PRnewObj, objectPr._PRmemberData);

            // Registration is after object is populated
            if ((!objectPr._PRisRegistered) && (objectPr._PRobjectId > 0))
            {
                RegisterObject(objectPr._PRnewObj, objectPr, parentPr);
            }

            if (objectPr._PRisValueTypeFixup)
            {
                ValueFixup fixup = (ValueFixup)ValueFixupStack.Pop(); //Value fixup
                fixup.Fixup(objectPr, parentPr);  // Value fixup
            }

            if (objectPr._PRobjectPositionEnum == InternalObjectPositionE.Top)
            {
                TopObject = objectPr._PRnewObj;
            }

            objectPr._PRobjectInfo.ObjectEnd();
        }

        // Array object encountered in stream
        private void ParseArray(ParseRecord pr)
        {
            long genId = pr._PRobjectId;

            if (pr._PRarrayTypeEnum == InternalArrayTypeE.Base64)
            {
                // ByteArray
                pr._PRnewObj = pr._PRvalue.Length > 0 ?
                    Convert.FromBase64String(pr._PRvalue) :
                    Array.Empty<byte>();

                if (_stack.Peek() == pr)
                {
                    _stack.Pop();
                }
                if (pr._PRobjectPositionEnum == InternalObjectPositionE.Top)
                {
                    TopObject = pr._PRnewObj;
                }

                ParseRecord parentPr = (ParseRecord)_stack.Peek();

                // Base64 can be registered at this point because it is populated
                RegisterObject(pr._PRnewObj, pr, parentPr);
            }
            else if ((pr._PRnewObj != null) && Converter.IsWriteAsByteArray(pr._PRarrayElementTypeCode))
            {
                // Primtive typed Array has already been read
                if (pr._PRobjectPositionEnum == InternalObjectPositionE.Top)
                {
                    TopObject = pr._PRnewObj;
                }

                ParseRecord parentPr = (ParseRecord)_stack.Peek();

                // Primitive typed array can be registered at this point because it is populated
                RegisterObject(pr._PRnewObj, pr, parentPr);
            }
            else if ((pr._PRarrayTypeEnum == InternalArrayTypeE.Jagged) || (pr._PRarrayTypeEnum == InternalArrayTypeE.Single))
            {
                // Multidimensional jagged array or single array
                bool couldBeValueType = true;
                if ((pr._PRlowerBoundA == null) || (pr._PRlowerBoundA[0] == 0))
                {
                    if (ReferenceEquals(pr._PRarrayElementType, Converter.s_typeofString))
                    {
                        pr._PRobjectA = new string[pr._PRlengthA[0]];
                        pr._PRnewObj = pr._PRobjectA;
                        couldBeValueType = false;
                    }
                    else if (ReferenceEquals(pr._PRarrayElementType, Converter.s_typeofObject))
                    {
                        pr._PRobjectA = new object[pr._PRlengthA[0]];
                        pr._PRnewObj = pr._PRobjectA;
                        couldBeValueType = false;
                    }
                    else if (pr._PRarrayElementType != null)
                    {
                        pr._PRnewObj = Array.CreateInstance(pr._PRarrayElementType, pr._PRlengthA[0]);
                    }
                    pr._PRisLowerBound = false;
                }
                else
                {
                    if (pr._PRarrayElementType != null)
                    {
                        pr._PRnewObj = Array.CreateInstance(pr._PRarrayElementType, pr._PRlengthA, pr._PRlowerBoundA);
                    }
                    pr._PRisLowerBound = true;
                }

                if (pr._PRarrayTypeEnum == InternalArrayTypeE.Single)
                {
                    if (!pr._PRisLowerBound && (Converter.IsWriteAsByteArray(pr._PRarrayElementTypeCode)))
                    {
                        pr._PRprimitiveArray = new PrimitiveArray(pr._PRarrayElementTypeCode, (Array)pr._PRnewObj);
                    }
                    else if (couldBeValueType && pr._PRarrayElementType != null)
                    {
                        if (!pr._PRarrayElementType.GetTypeInfo().IsValueType && !pr._PRisLowerBound)
                        {
                            pr._PRobjectA = (object[])pr._PRnewObj;
                        }
                    }
                }

                // For binary, headers comes in as an array of header objects
                if (pr._PRobjectPositionEnum == InternalObjectPositionE.Headers)
                {
                    _headers = (Header[])pr._PRnewObj;
                }

                pr._PRindexMap = new int[1];
            }
            else if (pr._PRarrayTypeEnum == InternalArrayTypeE.Rectangular)
            {
                // Rectangle array

                pr._PRisLowerBound = false;
                if (pr._PRlowerBoundA != null)
                {
                    for (int i = 0; i < pr._PRrank; i++)
                    {
                        if (pr._PRlowerBoundA[i] != 0)
                        {
                            pr._PRisLowerBound = true;
                        }
                    }
                }

                if (pr._PRarrayElementType != null)
                {
                    pr._PRnewObj = !pr._PRisLowerBound ?
                        Array.CreateInstance(pr._PRarrayElementType, pr._PRlengthA) :
                        Array.CreateInstance(pr._PRarrayElementType, pr._PRlengthA, pr._PRlowerBoundA);
                }

                // Calculate number of items
                int sum = 1;
                for (int i = 0; i < pr._PRrank; i++)
                {
                    sum = sum * pr._PRlengthA[i];
                }
                pr._PRindexMap = new int[pr._PRrank];
                pr._PRrectangularMap = new int[pr._PRrank];
                pr._PRlinearlength = sum;
            }
            else
            {
                throw new SerializationException(SR.Format(SR.Serialization_ArrayType, pr._PRarrayTypeEnum));
            }
        }

        // Builds a map for each item in an incoming rectangle array. The map specifies where the item is placed in the output Array Object
        private void NextRectangleMap(ParseRecord pr)
        {
            // For each invocation, calculate the next rectangular array position
            // example
            // indexMap 0 [0,0,0]
            // indexMap 1 [0,0,1]
            // indexMap 2 [0,0,2]
            // indexMap 3 [0,0,3]
            // indexMap 4 [0,1,0]       
            for (int irank = pr._PRrank - 1; irank > -1; irank--)
            {
                // Find the current or lower dimension which can be incremented.
                if (pr._PRrectangularMap[irank] < pr._PRlengthA[irank] - 1)
                {
                    // The current dimension is at maximum. Increase the next lower dimension by 1
                    pr._PRrectangularMap[irank]++;
                    if (irank < pr._PRrank - 1)
                    {
                        // The current dimension and higher dimensions are zeroed.
                        for (int i = irank + 1; i < pr._PRrank; i++)
                        {
                            pr._PRrectangularMap[i] = 0;
                        }
                    }
                    Array.Copy(pr._PRrectangularMap, 0, pr._PRindexMap, 0, pr._PRrank);
                    break;
                }
            }
        }


        // Array object item encountered in stream
        private void ParseArrayMember(ParseRecord pr)
        {
            ParseRecord objectPr = (ParseRecord)_stack.Peek();

            // Set up for inserting value into correct array position
            if (objectPr._PRarrayTypeEnum == InternalArrayTypeE.Rectangular)
            {
                if (objectPr._PRmemberIndex > 0)
                {
                    NextRectangleMap(objectPr); // Rectangle array, calculate position in array
                }
                if (objectPr._PRisLowerBound)
                {
                    for (int i = 0; i < objectPr._PRrank; i++)
                    {
                        objectPr._PRindexMap[i] = objectPr._PRrectangularMap[i] + objectPr._PRlowerBoundA[i];
                    }
                }
            }
            else
            {
                objectPr._PRindexMap[0] = !objectPr._PRisLowerBound ?
                    objectPr._PRmemberIndex : // Zero based array
                    objectPr._PRlowerBoundA[0] + objectPr._PRmemberIndex; // Lower Bound based array
            }

            // Set Array element according to type of element

            if (pr._PRmemberValueEnum == InternalMemberValueE.Reference)
            {
                // Object Reference

                // See if object has already been instantiated
                object refObj = _objectManager.GetObject(pr._PRidRef);
                if (refObj == null)
                {
                    // Object not instantiated
                    // Array fixup manager
                    int[] fixupIndex = new int[objectPr._PRrank];
                    Array.Copy(objectPr._PRindexMap, 0, fixupIndex, 0, objectPr._PRrank);

                    _objectManager.RecordArrayElementFixup(objectPr._PRobjectId, fixupIndex, pr._PRidRef);
                }
                else
                {
                    if (objectPr._PRobjectA != null)
                    {
                        objectPr._PRobjectA[objectPr._PRindexMap[0]] = refObj;
                    }
                    else
                    {
                        ((Array)objectPr._PRnewObj).SetValue(refObj, objectPr._PRindexMap); // Object has been instantiated
                    }
                }
            }
            else if (pr._PRmemberValueEnum == InternalMemberValueE.Nested)
            {
                //Set up dtType for ParseObject
                if (pr._PRdtType == null)
                {
                    pr._PRdtType = objectPr._PRarrayElementType;
                }

                ParseObject(pr);
                _stack.Push(pr);

                if (objectPr._PRarrayElementType != null)
                {
                    if ((objectPr._PRarrayElementType.GetTypeInfo().IsValueType) && (pr._PRarrayElementTypeCode == InternalPrimitiveTypeE.Invalid))
                    {
                        pr._PRisValueTypeFixup = true; //Valuefixup
                        ValueFixupStack.Push(new ValueFixup((Array)objectPr._PRnewObj, objectPr._PRindexMap)); //valuefixup
                    }
                    else
                    {
                        if (objectPr._PRobjectA != null)
                        {
                            objectPr._PRobjectA[objectPr._PRindexMap[0]] = pr._PRnewObj;
                        }
                        else
                        {
                            ((Array)objectPr._PRnewObj).SetValue(pr._PRnewObj, objectPr._PRindexMap);
                        }
                    }
                }
            }
            else if (pr._PRmemberValueEnum == InternalMemberValueE.InlineValue)
            {
                if ((ReferenceEquals(objectPr._PRarrayElementType, Converter.s_typeofString)) || (ReferenceEquals(pr._PRdtType, Converter.s_typeofString)))
                {
                    // String in either a string array, or a string element of an object array
                    ParseString(pr, objectPr);
                    if (objectPr._PRobjectA != null)
                    {
                        objectPr._PRobjectA[objectPr._PRindexMap[0]] = pr._PRvalue;
                    }
                    else
                    {
                        ((Array)objectPr._PRnewObj).SetValue(pr._PRvalue, objectPr._PRindexMap);
                    }
                }
                else if (objectPr._PRisArrayVariant)
                {
                    // Array of type object
                    if (pr._PRkeyDt == null)
                    {
                        throw new SerializationException(SR.Serialization_ArrayTypeObject);
                    }

                    object var = null;

                    if (ReferenceEquals(pr._PRdtType, Converter.s_typeofString))
                    {
                        ParseString(pr, objectPr);
                        var = pr._PRvalue;
                    }
                    else if (ReferenceEquals(pr._PRdtTypeCode, InternalPrimitiveTypeE.Invalid))
                    {
                        CheckSerializable(pr._PRdtType);
                        // Not nested and invalid, so it is an empty object
                        var = FormatterServices.GetUninitializedObject(pr._PRdtType);
                    }
                    else
                    {
                        var = pr._PRvarValue != null ?
                            pr._PRvarValue :
                            Converter.FromString(pr._PRvalue, pr._PRdtTypeCode);
                    }
                    if (objectPr._PRobjectA != null)
                    {
                        objectPr._PRobjectA[objectPr._PRindexMap[0]] = var;
                    }
                    else
                    {
                        ((Array)objectPr._PRnewObj).SetValue(var, objectPr._PRindexMap); // Primitive type
                    }
                }
                else
                {
                    // Primitive type
                    if (objectPr._PRprimitiveArray != null)
                    {
                        // Fast path for Soap primitive arrays. Binary was handled in the BinaryParser
                        objectPr._PRprimitiveArray.SetValue(pr._PRvalue, objectPr._PRindexMap[0]);
                    }
                    else
                    {
                        object var = pr._PRvarValue != null ?
                            pr._PRvarValue :
                            Converter.FromString(pr._PRvalue, objectPr._PRarrayElementTypeCode);
                        if (objectPr._PRobjectA != null)
                        {
                            objectPr._PRobjectA[objectPr._PRindexMap[0]] = var;
                        }
                        else
                        {
                            ((Array)objectPr._PRnewObj).SetValue(var, objectPr._PRindexMap); // Primitive type   
                        }
                    }
                }
            }
            else if (pr._PRmemberValueEnum == InternalMemberValueE.Null)
            {
                objectPr._PRmemberIndex += pr._consecutiveNullArrayEntryCount - 1; //also incremented again below
            }
            else
            {
                ParseError(pr, objectPr);
            }

            objectPr._PRmemberIndex++;
        }

        private void ParseArrayMemberEnd(ParseRecord pr)
        {
            // If this is a nested array object, then pop the stack
            if (pr._PRmemberValueEnum == InternalMemberValueE.Nested)
            {
                ParseObjectEnd(pr);
            }
        }

        // Object member encountered in stream
        private void ParseMember(ParseRecord pr)
        {
            ParseRecord objectPr = (ParseRecord)_stack.Peek();
            string objName = objectPr?._PRname;

            switch (pr._PRmemberTypeEnum)
            {
                case InternalMemberTypeE.Item:
                    ParseArrayMember(pr);
                    return;
                case InternalMemberTypeE.Field:
                    break;
            }

            //if ((pr.PRdtType == null) && !objectPr.PRobjectInfo.isSi)
            if (pr._PRdtType == null && objectPr._PRobjectInfo._isTyped)
            {
                pr._PRdtType = objectPr._PRobjectInfo.GetType(pr._PRname);

                if (pr._PRdtType != null)
                {
                    pr._PRdtTypeCode = Converter.ToCode(pr._PRdtType);
                }
            }

            if (pr._PRmemberValueEnum == InternalMemberValueE.Null)
            {
                // Value is Null
                objectPr._PRobjectInfo.AddValue(pr._PRname, null, ref objectPr._PRsi, ref objectPr._PRmemberData);
            }
            else if (pr._PRmemberValueEnum == InternalMemberValueE.Nested)
            {
                ParseObject(pr);
                _stack.Push(pr);

                if ((pr._PRobjectInfo != null) && pr._PRobjectInfo._objectType != null && (pr._PRobjectInfo._objectType.GetTypeInfo().IsValueType))
                {
                    pr._PRisValueTypeFixup = true; //Valuefixup
                    ValueFixupStack.Push(new ValueFixup(objectPr._PRnewObj, pr._PRname, objectPr._PRobjectInfo));//valuefixup
                }
                else
                {
                    objectPr._PRobjectInfo.AddValue(pr._PRname, pr._PRnewObj, ref objectPr._PRsi, ref objectPr._PRmemberData);
                }
            }
            else if (pr._PRmemberValueEnum == InternalMemberValueE.Reference)
            {
                // See if object has already been instantiated
                object refObj = _objectManager.GetObject(pr._PRidRef);
                if (refObj == null)
                {
                    objectPr._PRobjectInfo.AddValue(pr._PRname, null, ref objectPr._PRsi, ref objectPr._PRmemberData);
                    objectPr._PRobjectInfo.RecordFixup(objectPr._PRobjectId, pr._PRname, pr._PRidRef); // Object not instantiated
                }
                else
                {
                    objectPr._PRobjectInfo.AddValue(pr._PRname, refObj, ref objectPr._PRsi, ref objectPr._PRmemberData);
                }
            }

            else if (pr._PRmemberValueEnum == InternalMemberValueE.InlineValue)
            {
                // Primitive type or String
                if (ReferenceEquals(pr._PRdtType, Converter.s_typeofString))
                {
                    ParseString(pr, objectPr);
                    objectPr._PRobjectInfo.AddValue(pr._PRname, pr._PRvalue, ref objectPr._PRsi, ref objectPr._PRmemberData);
                }
                else if (pr._PRdtTypeCode == InternalPrimitiveTypeE.Invalid)
                {
                    // The member field was an object put the value is Inline either  bin.Base64 or invalid
                    if (pr._PRarrayTypeEnum == InternalArrayTypeE.Base64)
                    {
                        objectPr._PRobjectInfo.AddValue(pr._PRname, Convert.FromBase64String(pr._PRvalue), ref objectPr._PRsi, ref objectPr._PRmemberData);
                    }
                    else if (ReferenceEquals(pr._PRdtType, Converter.s_typeofObject))
                    {
                        throw new SerializationException(SR.Format(SR.Serialization_TypeMissing, pr._PRname));
                    }
                    else
                    {
                        ParseString(pr, objectPr); // Register the object if it has an objectId
                        // Object Class with no memberInfo data
                        // only special case where AddValue is needed?
                        if (ReferenceEquals(pr._PRdtType, Converter.s_typeofSystemVoid))
                        {
                            objectPr._PRobjectInfo.AddValue(pr._PRname, pr._PRdtType, ref objectPr._PRsi, ref objectPr._PRmemberData);
                        }
                        else if (objectPr._PRobjectInfo._isSi)
                        {
                            // ISerializable are added as strings, the conversion to type is done by the
                            // ISerializable object
                            objectPr._PRobjectInfo.AddValue(pr._PRname, pr._PRvalue, ref objectPr._PRsi, ref objectPr._PRmemberData);
                        }
                    }
                }
                else
                {
                    object var = pr._PRvarValue != null ?
                        pr._PRvarValue :
                        Converter.FromString(pr._PRvalue, pr._PRdtTypeCode);
                    objectPr._PRobjectInfo.AddValue(pr._PRname, var, ref objectPr._PRsi, ref objectPr._PRmemberData);
                }
            }
            else
            {
                ParseError(pr, objectPr);
            }
        }

        // Object member end encountered in stream
        private void ParseMemberEnd(ParseRecord pr)
        {
            switch (pr._PRmemberTypeEnum)
            {
                case InternalMemberTypeE.Item:
                    ParseArrayMemberEnd(pr);
                    return;
                case InternalMemberTypeE.Field:
                    if (pr._PRmemberValueEnum == InternalMemberValueE.Nested)
                    {
                        ParseObjectEnd(pr);
                    }
                    break;
                default:
                    ParseError(pr, (ParseRecord)_stack.Peek());
                    break;
            }
        }

        // Processes a string object by getting an internal ID for it and registering it with the objectManager
        private void ParseString(ParseRecord pr, ParseRecord parentPr)
        {
            // Process String class
            if ((!pr._PRisRegistered) && (pr._PRobjectId > 0))
            {
                // String is treated as an object if it has an id
                //m_objectManager.RegisterObject(pr.PRvalue, pr.PRobjectId);
                RegisterObject(pr._PRvalue, pr, parentPr, true);
            }
        }

        private void RegisterObject(object obj, ParseRecord pr, ParseRecord objectPr)
        {
            RegisterObject(obj, pr, objectPr, false);
        }

        private void RegisterObject(object obj, ParseRecord pr, ParseRecord objectPr, bool bIsString)
        {
            if (!pr._PRisRegistered)
            {
                pr._PRisRegistered = true;

                SerializationInfo si = null;
                long parentId = 0;
                MemberInfo memberInfo = null;
                int[] indexMap = null;

                if (objectPr != null)
                {
                    indexMap = objectPr._PRindexMap;
                    parentId = objectPr._PRobjectId;

                    if (objectPr._PRobjectInfo != null)
                    {
                        if (!objectPr._PRobjectInfo._isSi)
                        {
                            // ParentId is only used if there is a memberInfo
                            memberInfo = objectPr._PRobjectInfo.GetMemberInfo(pr._PRname);
                        }
                    }
                }
                // SerializationInfo is always needed for ISerialization                        
                si = pr._PRsi;

                if (bIsString)
                {
                    _objectManager.RegisterString((string)obj, pr._PRobjectId, si, parentId, memberInfo);
                }
                else
                {
                    _objectManager.RegisterObject(obj, pr._PRobjectId, si, parentId, memberInfo, indexMap);
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
                if (_valTypeObjectIdTable == null)
                {
                    _valTypeObjectIdTable = new IntSizedArray();
                }

                long tempObjId = 0;
                if ((tempObjId = _valTypeObjectIdTable[(int)objectId]) == 0)
                {
                    tempObjId = ThresholdForValueTypeIds + objectId;
                    _valTypeObjectIdTable[(int)objectId] = (int)tempObjId;
                }
                return tempObjId;
            }

            return -1 * objectId;
        }

        internal Type Bind(string assemblyString, string typeString)
        {
            Type type = null;
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
            public Type Type;
            public string AssemblyName;
        }

        internal Type FastBindToType(string assemblyName, string typeName)
        {
            Type type = null;

            TypeNAssembly entry = (TypeNAssembly)_typeCache.GetCachedValue(typeName);

            if (entry == null || entry.AssemblyName != assemblyName)
            {
                Assembly assm = null;
                if (_isSimpleAssembly)
                {
                    try
                    {
                        Assembly.Load(new AssemblyName(assemblyName));
                    }
                    catch { }

                    if (assm == null)
                    {
                        return null;
                    }

                    GetSimplyNamedTypeFromAssembly(assm, typeName, ref type);
                }
                else
                {
                    try
                    {
                        assm = Assembly.Load(new AssemblyName(assemblyName));
                    }
                    catch { }

                    if (assm == null)
                    {
                        return null;
                    }

                    type = FormatterServices.GetTypeFromAssembly(assm, typeName);
                }

                if (type == null)
                {
                    return null;
                }

                // before adding it to cache, let us do the security check 
                CheckTypeForwardedTo(assm, type.GetTypeInfo().Assembly, type);

                entry = new TypeNAssembly();
                entry.Type = type;
                entry.AssemblyName = assemblyName;
                _typeCache.SetCachedValue(entry);
            }
            return entry.Type;
        }

        private static void GetSimplyNamedTypeFromAssembly(Assembly assm, string typeName, ref Type type)
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
        }

        private string _previousAssemblyString;
        private string _previousName;
        private Type _previousType;

        internal Type GetType(BinaryAssemblyInfo assemblyInfo, string name)
        {
            Type objectType = null;

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
                        CheckTypeForwardedTo(sourceAssembly, objectType.GetTypeInfo().Assembly, objectType);
                    }
                }

                _previousAssemblyString = assemblyInfo._assemblyString;
                _previousName = name;
                _previousType = objectType;
            }
            //Console.WriteLine("name "+name+" assembly "+assemblyInfo.assemblyString+" objectType "+objectType);
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

            public Type ResolveType(Assembly assembly, string simpleTypeName, bool ignoreCase) =>
                (assembly ?? _topLevelAssembly).GetType(simpleTypeName, false, ignoreCase);
        }
    }
}
