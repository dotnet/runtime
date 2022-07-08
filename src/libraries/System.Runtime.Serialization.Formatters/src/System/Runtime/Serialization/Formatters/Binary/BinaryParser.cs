// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal sealed class BinaryParser
    {
        private const string BinaryParserUnreferencedCodeMessage = "ObjectReader requires unreferenced code";

        private const int ChunkSize = 4096;
        private static readonly Encoding s_encoding = new UTF8Encoding(false, true);

        internal ObjectReader _objectReader;
        internal Stream _input;
        internal long _topId;
        internal long _headerId;
        internal SizedArray? _objectMapIdTable;
        internal SizedArray? _assemIdToAssemblyTable;    // Used to hold assembly information
        internal SerStack _stack = new SerStack("ObjectProgressStack");

        internal BinaryTypeEnum _expectedType = BinaryTypeEnum.ObjectUrt;
        internal object? _expectedTypeInformation;
        internal ParseRecord? _prs;

        private BinaryAssemblyInfo? _systemAssemblyInfo;
        private readonly BinaryReader _dataReader;
        private SerStack? _opPool;

        private BinaryObject? _binaryObject;
        private BinaryObjectWithMap? _bowm;
        private BinaryObjectWithMapTyped? _bowmt;

        internal BinaryObjectString? _objectString;
        internal BinaryCrossAppDomainString? _crossAppDomainString;
        internal MemberPrimitiveTyped? _memberPrimitiveTyped;
        private byte[]? _byteBuffer;
        internal MemberPrimitiveUnTyped? memberPrimitiveUnTyped;
        internal MemberReference? _memberReference;
        internal ObjectNull? _objectNull;
        internal static volatile MessageEnd? _messageEnd;

        internal BinaryParser(Stream stream, ObjectReader objectReader)
        {
            _input = stream;
            _objectReader = objectReader;
            _dataReader = new BinaryReader(_input, s_encoding);
        }

        internal BinaryAssemblyInfo SystemAssemblyInfo =>
            _systemAssemblyInfo ??= new BinaryAssemblyInfo(Converter.s_urtAssemblyString, Converter.s_urtAssembly);

        internal SizedArray ObjectMapIdTable =>
            _objectMapIdTable ??= new SizedArray();

        internal SizedArray AssemIdToAssemblyTable =>
            _assemIdToAssemblyTable ??= new SizedArray(2);

        internal ParseRecord PRs =>
            _prs ??= new ParseRecord();

        // Parse the input
        // Reads each record from the input stream. If the record is a primitive type (A number)
        //  then it doesn't have a BinaryHeaderEnum byte. For this case the expected type
        //  has been previously set to Primitive
        [RequiresUnreferencedCode(BinaryParserUnreferencedCodeMessage)]
        internal void Run()
        {
            try
            {
                bool isLoop = true;
                ReadBegin();
                ReadSerializationHeaderRecord();
                while (isLoop)
                {
                    BinaryHeaderEnum binaryHeaderEnum = BinaryHeaderEnum.Object;
                    switch (_expectedType)
                    {
                        case BinaryTypeEnum.ObjectUrt:
                        case BinaryTypeEnum.ObjectUser:
                        case BinaryTypeEnum.String:
                        case BinaryTypeEnum.Object:
                        case BinaryTypeEnum.ObjectArray:
                        case BinaryTypeEnum.StringArray:
                        case BinaryTypeEnum.PrimitiveArray:
                            byte inByte = _dataReader.ReadByte();
                            binaryHeaderEnum = (BinaryHeaderEnum)inByte;
                            switch (binaryHeaderEnum)
                            {
                                case BinaryHeaderEnum.Assembly:
                                case BinaryHeaderEnum.CrossAppDomainAssembly:
                                    ReadAssembly(binaryHeaderEnum);
                                    break;
                                case BinaryHeaderEnum.Object:
                                    ReadObject();
                                    break;
                                case BinaryHeaderEnum.CrossAppDomainMap:
                                    ReadCrossAppDomainMap();
                                    break;
                                case BinaryHeaderEnum.ObjectWithMap:
                                case BinaryHeaderEnum.ObjectWithMapAssemId:
                                    ReadObjectWithMap(binaryHeaderEnum);
                                    break;
                                case BinaryHeaderEnum.ObjectWithMapTyped:
                                case BinaryHeaderEnum.ObjectWithMapTypedAssemId:
                                    ReadObjectWithMapTyped(binaryHeaderEnum);
                                    break;
                                case BinaryHeaderEnum.ObjectString:
                                case BinaryHeaderEnum.CrossAppDomainString:
                                    ReadObjectString(binaryHeaderEnum);
                                    break;
                                case BinaryHeaderEnum.Array:
                                case BinaryHeaderEnum.ArraySinglePrimitive:
                                case BinaryHeaderEnum.ArraySingleObject:
                                case BinaryHeaderEnum.ArraySingleString:
                                    ReadArray(binaryHeaderEnum);
                                    break;
                                case BinaryHeaderEnum.MemberPrimitiveTyped:
                                    ReadMemberPrimitiveTyped();
                                    break;
                                case BinaryHeaderEnum.MemberReference:
                                    ReadMemberReference();
                                    break;
                                case BinaryHeaderEnum.ObjectNull:
                                case BinaryHeaderEnum.ObjectNullMultiple256:
                                case BinaryHeaderEnum.ObjectNullMultiple:
                                    ReadObjectNull(binaryHeaderEnum);
                                    break;
                                case BinaryHeaderEnum.MessageEnd:
                                    isLoop = false;
                                    ReadMessageEnd();
                                    ReadEnd();
                                    break;
                                default:
                                    throw new SerializationException(SR.Format(SR.Serialization_BinaryHeader, inByte));
                            }
                            break;
                        case BinaryTypeEnum.Primitive:
                            ReadMemberPrimitiveUnTyped();
                            break;
                        default:
                            throw new SerializationException(SR.Serialization_TypeExpected);
                    }

                    // If an assembly is encountered, don't advance
                    // object Progress,
                    if (binaryHeaderEnum != BinaryHeaderEnum.Assembly)
                    {
                        // End of parse loop.
                        bool isData = false;

                        // Set up loop for next iteration.
                        // If this is an object, and the end of object has been reached, then parse object end.
                        while (!isData)
                        {
                            ObjectProgress? op = (ObjectProgress?)_stack.Peek();
                            if (op == null)
                            {
                                // No more object on stack, then the next record is a top level object
                                _expectedType = BinaryTypeEnum.ObjectUrt;
                                _expectedTypeInformation = null;
                                isData = true;
                            }
                            else
                            {
                                // Find out what record is expected next
                                isData = op.GetNext(out op._expectedType, out op._expectedTypeInformation);
                                _expectedType = op._expectedType;
                                _expectedTypeInformation = op._expectedTypeInformation;

                                if (!isData)
                                {
                                    // No record is expected next, this is the end of an object or array
                                    PRs.Init();
                                    if (op._memberValueEnum == InternalMemberValueE.Nested)
                                    {
                                        // Nested object
                                        PRs._parseTypeEnum = InternalParseTypeE.MemberEnd;
                                        PRs._memberTypeEnum = op._memberTypeEnum;
                                        PRs._memberValueEnum = op._memberValueEnum;
                                        _objectReader.Parse(PRs);
                                    }
                                    else
                                    {
                                        // Top level object
                                        PRs._parseTypeEnum = InternalParseTypeE.ObjectEnd;
                                        PRs._memberTypeEnum = op._memberTypeEnum;
                                        PRs._memberValueEnum = op._memberValueEnum;
                                        _objectReader.Parse(PRs);
                                    }
                                    _stack.Pop();
                                    PutOp(op);
                                }
                            }
                        }
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // EOF should never be thrown since there is a MessageEnd record to stop parsing
                throw new SerializationException(SR.Serialization_StreamEnd);
            }
        }

        internal void ReadBegin() { }

        internal void ReadEnd() { }

        // Primitive Reads from Stream

        internal bool ReadBoolean() => _dataReader.ReadBoolean();

        internal byte ReadByte() => _dataReader.ReadByte();

        internal byte[] ReadBytes(int length) => _dataReader.ReadBytes(length);

        internal void ReadBytes(byte[] byteA, int offset, int size)
        {
            while (size > 0)
            {
                int n = _dataReader.Read(byteA, offset, size);
                if (n == 0)
                {
                    throw new EndOfStreamException(SR.IO_EOF_ReadBeyondEOF);
                }
                offset += n;
                size -= n;
            }
        }

        internal char ReadChar() => _dataReader.ReadChar();

        internal char[] ReadChars(int length) => _dataReader.ReadChars(length);

        internal decimal ReadDecimal() => decimal.Parse(_dataReader.ReadString(), CultureInfo.InvariantCulture);

        internal float ReadSingle() => _dataReader.ReadSingle();

        internal double ReadDouble() => _dataReader.ReadDouble();

        internal short ReadInt16() => _dataReader.ReadInt16();

        internal int ReadInt32() => _dataReader.ReadInt32();

        internal long ReadInt64() => _dataReader.ReadInt64();

        internal sbyte ReadSByte() => unchecked((sbyte)ReadByte());

        internal string ReadString() => _dataReader.ReadString();

        internal TimeSpan ReadTimeSpan() => new TimeSpan(ReadInt64());

        internal DateTime ReadDateTime() => FromBinaryRaw(ReadInt64());

        private static unsafe DateTime FromBinaryRaw(long dateData)
        {
            // Use DateTime's public constructor to validate the input, but we
            // can't return that result as it strips off the kind. To address
            // that, store the value directly into a DateTime via an unsafe cast.
            // See BinaryFormatterWriter.WriteDateTime for details.
            const long TicksMask = 0x3FFFFFFFFFFFFFFF;
            new DateTime(dateData & TicksMask);
            return *(DateTime*)&dateData;
        }

        internal ushort ReadUInt16() => _dataReader.ReadUInt16();

        internal uint ReadUInt32() => _dataReader.ReadUInt32();

        internal ulong ReadUInt64() => _dataReader.ReadUInt64();

        // Binary Stream Record Reads
        internal void ReadSerializationHeaderRecord()
        {
            var record = new SerializationHeaderRecord();
            record.Read(this);
            _topId = (record._topId > 0 ? _objectReader.GetId(record._topId) : record._topId);
            _headerId = (record._headerId > 0 ? _objectReader.GetId(record._headerId) : record._headerId);
        }

        internal void ReadAssembly(BinaryHeaderEnum binaryHeaderEnum)
        {
            var record = new BinaryAssembly();
            if (binaryHeaderEnum == BinaryHeaderEnum.CrossAppDomainAssembly)
            {
                var crossAppDomainAssembly = new BinaryCrossAppDomainAssembly();
                crossAppDomainAssembly.Read(this);
                record._assemId = crossAppDomainAssembly._assemId;
                record._assemblyString = _objectReader.CrossAppDomainArray(crossAppDomainAssembly._assemblyIndex) as string;
                if (record._assemblyString == null)
                {
                    throw new SerializationException(SR.Format(SR.Serialization_CrossAppDomainError, "String", crossAppDomainAssembly._assemblyIndex));
                }
            }
            else
            {
                record.Read(this);
            }

            AssemIdToAssemblyTable[record._assemId] = new BinaryAssemblyInfo(record._assemblyString!);
        }

        [RequiresUnreferencedCode(BinaryParserUnreferencedCodeMessage)]
        private void ReadObject()
        {
            _binaryObject ??= new BinaryObject();
            _binaryObject.Read(this);

            ObjectMap? objectMap = (ObjectMap?)ObjectMapIdTable[_binaryObject._mapId];
            if (objectMap == null)
            {
                throw new SerializationException(SR.Format(SR.Serialization_Map, _binaryObject._mapId));
            }

            ObjectProgress op = GetOp();
            ParseRecord pr = op._pr;
            _stack.Push(op);

            op._objectTypeEnum = InternalObjectTypeE.Object;
            op._binaryTypeEnumA = objectMap._binaryTypeEnumA;
            op._memberNames = objectMap._memberNames;
            op._memberTypes = objectMap._memberTypes;
            op._typeInformationA = objectMap._typeInformationA;
            op._memberLength = op._binaryTypeEnumA.Length;
            ObjectProgress? objectOp = (ObjectProgress?)_stack.PeekPeek();
            if ((objectOp == null) || (objectOp._isInitial))
            {
                // Non-Nested Object
                op._name = objectMap._objectName;
                pr._parseTypeEnum = InternalParseTypeE.Object;
                op._memberValueEnum = InternalMemberValueE.Empty;
            }
            else
            {
                // Nested Object
                pr._parseTypeEnum = InternalParseTypeE.Member;
                pr._memberValueEnum = InternalMemberValueE.Nested;
                op._memberValueEnum = InternalMemberValueE.Nested;

                switch (objectOp._objectTypeEnum)
                {
                    case InternalObjectTypeE.Object:
                        pr._name = objectOp._name;
                        pr._memberTypeEnum = InternalMemberTypeE.Field;
                        op._memberTypeEnum = InternalMemberTypeE.Field;
                        break;
                    case InternalObjectTypeE.Array:
                        pr._memberTypeEnum = InternalMemberTypeE.Item;
                        op._memberTypeEnum = InternalMemberTypeE.Item;
                        break;
                    default:
                        throw new SerializationException(SR.Format(SR.Serialization_Map, objectOp._objectTypeEnum.ToString()));
                }
            }

            pr._objectId = _objectReader.GetId(_binaryObject._objectId);
            pr._objectInfo = objectMap.CreateObjectInfo(ref pr._si, ref pr._memberData);

            if (pr._objectId == _topId)
            {
                pr._objectPositionEnum = InternalObjectPositionE.Top;
            }

            pr._objectTypeEnum = InternalObjectTypeE.Object;
            pr._keyDt = objectMap._objectName;
            pr._dtType = objectMap._objectType;
            pr._dtTypeCode = InternalPrimitiveTypeE.Invalid;
            _objectReader.Parse(pr);
        }

        [RequiresUnreferencedCode(BinaryParserUnreferencedCodeMessage)]
        internal void ReadCrossAppDomainMap()
        {
            BinaryCrossAppDomainMap record = new BinaryCrossAppDomainMap();
            record.Read(this);
            object mapObject = _objectReader.CrossAppDomainArray(record._crossAppDomainArrayIndex);
            if (mapObject is BinaryObjectWithMap binaryObjectWithMap)
            {
                ReadObjectWithMap(binaryObjectWithMap);
            }
            else
            {
                if (mapObject is BinaryObjectWithMapTyped binaryObjectWithMapTyped)
                {
                    ReadObjectWithMapTyped(binaryObjectWithMapTyped);
                }
                else
                {
                    throw new SerializationException(SR.Format(SR.Serialization_CrossAppDomainError, "BinaryObjectMap", mapObject));
                }
            }
        }

        [RequiresUnreferencedCode(BinaryParserUnreferencedCodeMessage)]
        internal void ReadObjectWithMap(BinaryHeaderEnum binaryHeaderEnum)
        {
            if (_bowm == null)
            {
                _bowm = new BinaryObjectWithMap(binaryHeaderEnum);
            }
            else
            {
                _bowm._binaryHeaderEnum = binaryHeaderEnum;
            }
            _bowm.Read(this);
            ReadObjectWithMap(_bowm);
        }

        [RequiresUnreferencedCode("Types might be removed")]
        private void ReadObjectWithMap(BinaryObjectWithMap record)
        {
            BinaryAssemblyInfo? assemblyInfo = null;
            ObjectProgress op = GetOp();
            ParseRecord pr = op._pr;
            _stack.Push(op);

            if (record._binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMapAssemId)
            {
                if (record._assemId < 1)
                {
                    throw new SerializationException(SR.Format(SR.Serialization_Assembly, record._name));
                }

                assemblyInfo = ((BinaryAssemblyInfo?)AssemIdToAssemblyTable[record._assemId]);

                if (assemblyInfo == null)
                {
                    throw new SerializationException(SR.Format(SR.Serialization_Assembly, record._assemId + " " + record._name));
                }
            }
            else if (record._binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMap)
            {
                assemblyInfo = SystemAssemblyInfo; //Urt assembly
            }

            Debug.Assert(record._name != null && record._memberNames != null);
            Type? objectType = _objectReader.GetType(assemblyInfo!, record._name);

            Debug.Assert(objectType != null);
            ObjectMap objectMap = ObjectMap.Create(record._name, objectType, record._memberNames, _objectReader, record._objectId, assemblyInfo!);
            ObjectMapIdTable[record._objectId] = objectMap;

            op._objectTypeEnum = InternalObjectTypeE.Object;
            op._binaryTypeEnumA = objectMap._binaryTypeEnumA;
            op._typeInformationA = objectMap._typeInformationA;
            op._memberLength = op._binaryTypeEnumA.Length;
            op._memberNames = objectMap._memberNames;
            op._memberTypes = objectMap._memberTypes;

            ObjectProgress? objectOp = (ObjectProgress?)_stack.PeekPeek();

            if ((objectOp == null) || (objectOp._isInitial))
            {
                // Non-Nested Object
                op._name = record._name;
                pr._parseTypeEnum = InternalParseTypeE.Object;
                op._memberValueEnum = InternalMemberValueE.Empty;
            }
            else
            {
                // Nested Object
                pr._parseTypeEnum = InternalParseTypeE.Member;
                pr._memberValueEnum = InternalMemberValueE.Nested;
                op._memberValueEnum = InternalMemberValueE.Nested;

                switch (objectOp._objectTypeEnum)
                {
                    case InternalObjectTypeE.Object:
                        pr._name = objectOp._name;
                        pr._memberTypeEnum = InternalMemberTypeE.Field;
                        op._memberTypeEnum = InternalMemberTypeE.Field;
                        break;
                    case InternalObjectTypeE.Array:
                        pr._memberTypeEnum = InternalMemberTypeE.Item;
                        op._memberTypeEnum = InternalMemberTypeE.Field;
                        break;
                    default:
                        throw new SerializationException(SR.Format(SR.Serialization_ObjectTypeEnum, objectOp._objectTypeEnum.ToString()));
                }
            }
            pr._objectTypeEnum = InternalObjectTypeE.Object;
            pr._objectId = _objectReader.GetId(record._objectId);
            pr._objectInfo = objectMap.CreateObjectInfo(ref pr._si, ref pr._memberData);

            if (pr._objectId == _topId)
            {
                pr._objectPositionEnum = InternalObjectPositionE.Top;
            }

            pr._keyDt = record._name;
            pr._dtType = objectMap._objectType;
            pr._dtTypeCode = InternalPrimitiveTypeE.Invalid;
            _objectReader.Parse(pr);
        }

        [RequiresUnreferencedCode("Types might be removed")]
        internal void ReadObjectWithMapTyped(BinaryHeaderEnum binaryHeaderEnum)
        {
            if (_bowmt == null)
            {
                _bowmt = new BinaryObjectWithMapTyped(binaryHeaderEnum);
            }
            else
            {
                _bowmt._binaryHeaderEnum = binaryHeaderEnum;
            }
            _bowmt.Read(this);
            ReadObjectWithMapTyped(_bowmt);
        }

        [RequiresUnreferencedCode("Types might be removed")]
        private void ReadObjectWithMapTyped(BinaryObjectWithMapTyped record)
        {
            BinaryAssemblyInfo? assemblyInfo = null;
            ObjectProgress op = GetOp();
            ParseRecord pr = op._pr;
            _stack.Push(op);

            if (record._binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMapTypedAssemId)
            {
                if (record._assemId < 1)
                {
                    throw new SerializationException(SR.Format(SR.Serialization_AssemblyId, record._name));
                }

                assemblyInfo = (BinaryAssemblyInfo?)AssemIdToAssemblyTable[record._assemId];
                if (assemblyInfo == null)
                {
                    throw new SerializationException(SR.Format(SR.Serialization_AssemblyId, record._assemId + " " + record._name));
                }
            }
            else if (record._binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMapTyped)
            {
                assemblyInfo = SystemAssemblyInfo; // Urt assembly
            }

            Debug.Assert(record._name != null && record._memberNames != null && record._binaryTypeEnumA != null && record._typeInformationA != null && record._memberAssemIds != null);
            ObjectMap objectMap = ObjectMap.Create(record._name, record._memberNames, record._binaryTypeEnumA, record._typeInformationA, record._memberAssemIds, _objectReader, record._objectId, assemblyInfo!, AssemIdToAssemblyTable);
            ObjectMapIdTable[record._objectId] = objectMap;
            op._objectTypeEnum = InternalObjectTypeE.Object;
            op._binaryTypeEnumA = objectMap._binaryTypeEnumA;
            op._typeInformationA = objectMap._typeInformationA;
            op._memberLength = op._binaryTypeEnumA.Length;
            op._memberNames = objectMap._memberNames;
            op._memberTypes = objectMap._memberTypes;

            ObjectProgress? objectOp = (ObjectProgress?)_stack.PeekPeek();

            if ((objectOp == null) || (objectOp._isInitial))
            {
                // Non-Nested Object
                op._name = record._name;
                pr._parseTypeEnum = InternalParseTypeE.Object;
                op._memberValueEnum = InternalMemberValueE.Empty;
            }
            else
            {
                // Nested Object
                pr._parseTypeEnum = InternalParseTypeE.Member;
                pr._memberValueEnum = InternalMemberValueE.Nested;
                op._memberValueEnum = InternalMemberValueE.Nested;

                switch (objectOp._objectTypeEnum)
                {
                    case InternalObjectTypeE.Object:
                        pr._name = objectOp._name;
                        pr._memberTypeEnum = InternalMemberTypeE.Field;
                        op._memberTypeEnum = InternalMemberTypeE.Field;
                        break;
                    case InternalObjectTypeE.Array:
                        pr._memberTypeEnum = InternalMemberTypeE.Item;
                        op._memberTypeEnum = InternalMemberTypeE.Item;
                        break;
                    default:
                        throw new SerializationException(SR.Format(SR.Serialization_ObjectTypeEnum, objectOp._objectTypeEnum.ToString()));
                }
            }

            pr._objectTypeEnum = InternalObjectTypeE.Object;
            pr._objectInfo = objectMap.CreateObjectInfo(ref pr._si, ref pr._memberData);
            pr._objectId = _objectReader.GetId(record._objectId);
            if (pr._objectId == _topId)
            {
                pr._objectPositionEnum = InternalObjectPositionE.Top;
            }
            pr._keyDt = record._name;
            pr._dtType = objectMap._objectType;
            pr._dtTypeCode = InternalPrimitiveTypeE.Invalid;
            _objectReader.Parse(pr);
        }

        [RequiresUnreferencedCode(BinaryParserUnreferencedCodeMessage)]
        private void ReadObjectString(BinaryHeaderEnum binaryHeaderEnum)
        {
            _objectString ??= new BinaryObjectString();

            if (binaryHeaderEnum == BinaryHeaderEnum.ObjectString)
            {
                _objectString.Read(this);
            }
            else
            {
                _crossAppDomainString ??= new BinaryCrossAppDomainString();
                _crossAppDomainString.Read(this);
                _objectString._value = _objectReader.CrossAppDomainArray(_crossAppDomainString._value) as string;
                if (_objectString._value == null)
                {
                    throw new SerializationException(SR.Format(SR.Serialization_CrossAppDomainError, "String", _crossAppDomainString._value));
                }

                _objectString._objectId = _crossAppDomainString._objectId;
            }

            PRs.Init();
            PRs._parseTypeEnum = InternalParseTypeE.Object;
            PRs._objectId = _objectReader.GetId(_objectString._objectId);

            if (PRs._objectId == _topId)
            {
                PRs._objectPositionEnum = InternalObjectPositionE.Top;
            }

            PRs._objectTypeEnum = InternalObjectTypeE.Object;

            ObjectProgress? objectOp = (ObjectProgress?)_stack.Peek();

            PRs._value = _objectString._value;
            PRs._keyDt = "System.String";
            PRs._dtType = Converter.s_typeofString;
            PRs._dtTypeCode = InternalPrimitiveTypeE.Invalid;
            PRs._varValue = _objectString._value; //Need to set it because ObjectReader is picking up value from variant, not pr.PRvalue

            if (objectOp == null)
            {
                // Top level String
                PRs._parseTypeEnum = InternalParseTypeE.Object;
                PRs._name = "System.String";
            }
            else
            {
                // Nested in an Object

                PRs._parseTypeEnum = InternalParseTypeE.Member;
                PRs._memberValueEnum = InternalMemberValueE.InlineValue;

                switch (objectOp._objectTypeEnum)
                {
                    case InternalObjectTypeE.Object:
                        PRs._name = objectOp._name;
                        PRs._memberTypeEnum = InternalMemberTypeE.Field;
                        break;
                    case InternalObjectTypeE.Array:
                        PRs._memberTypeEnum = InternalMemberTypeE.Item;
                        break;
                    default:
                        throw new SerializationException(SR.Format(SR.Serialization_ObjectTypeEnum, objectOp._objectTypeEnum.ToString()));
                }
            }

            _objectReader.Parse(PRs);
        }

        [RequiresUnreferencedCode(BinaryParserUnreferencedCodeMessage)]
        private void ReadMemberPrimitiveTyped()
        {
            _memberPrimitiveTyped ??= new MemberPrimitiveTyped();
            _memberPrimitiveTyped.Read(this);

            PRs._objectTypeEnum = InternalObjectTypeE.Object; //Get rid of
            ObjectProgress? objectOp = (ObjectProgress?)_stack.Peek();

            PRs.Init();
            PRs._varValue = _memberPrimitiveTyped._value;
            PRs._keyDt = Converter.ToComType(_memberPrimitiveTyped._primitiveTypeEnum);
            PRs._dtType = Converter.ToType(_memberPrimitiveTyped._primitiveTypeEnum);
            PRs._dtTypeCode = _memberPrimitiveTyped._primitiveTypeEnum;

            if (objectOp == null)
            {
                // Top level boxed primitive
                PRs._parseTypeEnum = InternalParseTypeE.Object;
                PRs._name = "System.Variant";
            }
            else
            {
                // Nested in an Object

                PRs._parseTypeEnum = InternalParseTypeE.Member;
                PRs._memberValueEnum = InternalMemberValueE.InlineValue;

                switch (objectOp._objectTypeEnum)
                {
                    case InternalObjectTypeE.Object:
                        PRs._name = objectOp._name;
                        PRs._memberTypeEnum = InternalMemberTypeE.Field;
                        break;
                    case InternalObjectTypeE.Array:
                        PRs._memberTypeEnum = InternalMemberTypeE.Item;
                        break;
                    default:
                        throw new SerializationException(SR.Format(SR.Serialization_ObjectTypeEnum, objectOp._objectTypeEnum.ToString()));
                }
            }

            _objectReader.Parse(PRs);
        }

        [RequiresUnreferencedCode(BinaryParserUnreferencedCodeMessage)]
        private void ReadArray(BinaryHeaderEnum binaryHeaderEnum)
        {
            BinaryAssemblyInfo? assemblyInfo;
            BinaryArray record = new BinaryArray(binaryHeaderEnum);
            record.Read(this);

            if (record._binaryTypeEnum == BinaryTypeEnum.ObjectUser)
            {
                if (record._assemId < 1)
                {
                    throw new SerializationException(SR.Format(SR.Serialization_AssemblyId, record._typeInformation));
                }
                assemblyInfo = (BinaryAssemblyInfo?)AssemIdToAssemblyTable[record._assemId];
            }
            else
            {
                assemblyInfo = SystemAssemblyInfo; //Urt assembly
            }

            ObjectProgress op = GetOp();
            ParseRecord pr = op._pr;

            op._objectTypeEnum = InternalObjectTypeE.Array;
            op._binaryTypeEnum = record._binaryTypeEnum;
            op._typeInformation = record._typeInformation;

            ObjectProgress? objectOp = (ObjectProgress?)_stack.PeekPeek();
            if ((objectOp == null) || (record._objectId > 0))
            {
                // Non-Nested Object
                op._name = "System.Array";
                pr._parseTypeEnum = InternalParseTypeE.Object;
                op._memberValueEnum = InternalMemberValueE.Empty;
            }
            else
            {
                // Nested Object
                pr._parseTypeEnum = InternalParseTypeE.Member;
                pr._memberValueEnum = InternalMemberValueE.Nested;
                op._memberValueEnum = InternalMemberValueE.Nested;

                switch (objectOp._objectTypeEnum)
                {
                    case InternalObjectTypeE.Object:
                        pr._name = objectOp._name;
                        pr._memberTypeEnum = InternalMemberTypeE.Field;
                        op._memberTypeEnum = InternalMemberTypeE.Field;
                        pr._keyDt = objectOp._name;
                        pr._dtType = objectOp._dtType;
                        break;
                    case InternalObjectTypeE.Array:
                        pr._memberTypeEnum = InternalMemberTypeE.Item;
                        op._memberTypeEnum = InternalMemberTypeE.Item;
                        break;
                    default:
                        throw new SerializationException(SR.Format(SR.Serialization_ObjectTypeEnum, objectOp._objectTypeEnum.ToString()));
                }
            }

            pr._objectId = _objectReader.GetId(record._objectId);
            if (pr._objectId == _topId)
            {
                pr._objectPositionEnum = InternalObjectPositionE.Top;
            }
            else if ((_headerId > 0) && (pr._objectId == _headerId))
            {
                pr._objectPositionEnum = InternalObjectPositionE.Headers; // Headers are an array of header objects
            }
            else
            {
                pr._objectPositionEnum = InternalObjectPositionE.Child;
            }

            pr._objectTypeEnum = InternalObjectTypeE.Array;

            BinaryTypeConverter.TypeFromInfo(record._binaryTypeEnum, record._typeInformation, _objectReader, assemblyInfo,
                                         out pr._arrayElementTypeCode, out pr._arrayElementTypeString,
                                         out pr._arrayElementType, out pr._isArrayVariant);

            pr._dtTypeCode = InternalPrimitiveTypeE.Invalid;

            pr._rank = record._rank;
            pr._lengthA = record._lengthA;
            pr._lowerBoundA = record._lowerBoundA;
            bool isPrimitiveArray = false;

            Debug.Assert(record._lengthA != null);
            switch (record._binaryArrayTypeEnum)
            {
                case BinaryArrayTypeEnum.Single:
                case BinaryArrayTypeEnum.SingleOffset:
                    op._numItems = record._lengthA[0];
                    pr._arrayTypeEnum = InternalArrayTypeE.Single;
                    Debug.Assert(record._lowerBoundA != null);
                    if (Converter.IsWriteAsByteArray(pr._arrayElementTypeCode) &&
                        (record._lowerBoundA[0] == 0))
                    {
                        isPrimitiveArray = true;
                        ReadArrayAsBytes(pr);
                    }
                    break;
                case BinaryArrayTypeEnum.Jagged:
                case BinaryArrayTypeEnum.JaggedOffset:
                    op._numItems = record._lengthA[0];
                    pr._arrayTypeEnum = InternalArrayTypeE.Jagged;
                    break;
                case BinaryArrayTypeEnum.Rectangular:
                case BinaryArrayTypeEnum.RectangularOffset:
                    int arrayLength = 1;
                    for (int i = 0; i < record._rank; i++)
                        arrayLength *= record._lengthA[i];
                    op._numItems = arrayLength;
                    pr._arrayTypeEnum = InternalArrayTypeE.Rectangular;
                    break;
                default:
                    throw new SerializationException(SR.Format(SR.Serialization_ArrayType, record._binaryArrayTypeEnum.ToString()));
            }

            if (!isPrimitiveArray)
            {
                _stack.Push(op);
            }
            else
            {
                PutOp(op);
            }

            _objectReader.Parse(pr);

            if (isPrimitiveArray)
            {
                pr._parseTypeEnum = InternalParseTypeE.ObjectEnd;
                _objectReader.Parse(pr);
            }
        }

        private void ReadArrayAsBytes(ParseRecord pr)
        {
            Debug.Assert(pr._lengthA != null);
            if (pr._arrayElementTypeCode == InternalPrimitiveTypeE.Byte)
            {
                pr._newObj = ReadBytes(pr._lengthA[0]);
            }
            else if (pr._arrayElementTypeCode == InternalPrimitiveTypeE.Char)
            {
                pr._newObj = ReadChars(pr._lengthA[0]);
            }
            else
            {
                int typeLength = Converter.TypeLength(pr._arrayElementTypeCode);

                pr._newObj = Converter.CreatePrimitiveArray(pr._arrayElementTypeCode, pr._lengthA[0]);
                Debug.Assert((pr._newObj != null), "[BinaryParser expected a Primitive Array]");

                Array array = (Array)pr._newObj;
                int arrayOffset = 0;
                _byteBuffer ??= new byte[ChunkSize];

                while (arrayOffset < array.Length)
                {
                    int numArrayItems = Math.Min(ChunkSize / typeLength, array.Length - arrayOffset);
                    int bufferUsed = numArrayItems * typeLength;
                    ReadBytes(_byteBuffer, 0, bufferUsed);
                    if (!BitConverter.IsLittleEndian)
                    {
                        // we know that we are reading a primitive type, so just do a simple swap
                        for (int i = 0; i < bufferUsed; i += typeLength)
                        {
                            for (int j = 0; j < typeLength / 2; j++)
                            {
                                byte tmp = _byteBuffer[i + j];
                                _byteBuffer[i + j] = _byteBuffer[i + typeLength - 1 - j];
                                _byteBuffer[i + typeLength - 1 - j] = tmp;
                            }
                        }
                    }
                    Buffer.BlockCopy(_byteBuffer, 0, array, arrayOffset * typeLength, bufferUsed);
                    arrayOffset += numArrayItems;
                }
            }
        }

        [RequiresUnreferencedCode(BinaryParserUnreferencedCodeMessage)]
        private void ReadMemberPrimitiveUnTyped()
        {
            ObjectProgress? objectOp = (ObjectProgress?)_stack.Peek();
            memberPrimitiveUnTyped ??= new MemberPrimitiveUnTyped();
            memberPrimitiveUnTyped.Set((InternalPrimitiveTypeE)_expectedTypeInformation!);
            memberPrimitiveUnTyped.Read(this);

            PRs.Init();
            PRs._varValue = memberPrimitiveUnTyped._value;

            PRs._dtTypeCode = (InternalPrimitiveTypeE)_expectedTypeInformation!;
            PRs._dtType = Converter.ToType(PRs._dtTypeCode);
            PRs._parseTypeEnum = InternalParseTypeE.Member;
            PRs._memberValueEnum = InternalMemberValueE.InlineValue;

            Debug.Assert(objectOp != null);
            if (objectOp._objectTypeEnum == InternalObjectTypeE.Object)
            {
                PRs._memberTypeEnum = InternalMemberTypeE.Field;
                PRs._name = objectOp._name;
            }
            else
            {
                PRs._memberTypeEnum = InternalMemberTypeE.Item;
            }

            _objectReader.Parse(PRs);
        }

        [RequiresUnreferencedCode(BinaryParserUnreferencedCodeMessage)]
        private void ReadMemberReference()
        {
            _memberReference ??= new MemberReference();
            _memberReference.Read(this);

            ObjectProgress? objectOp = (ObjectProgress?)_stack.Peek();

            PRs.Init();
            PRs._idRef = _objectReader.GetId(_memberReference._idRef);
            PRs._parseTypeEnum = InternalParseTypeE.Member;
            PRs._memberValueEnum = InternalMemberValueE.Reference;

            Debug.Assert(objectOp != null);
            if (objectOp._objectTypeEnum == InternalObjectTypeE.Object)
            {
                PRs._memberTypeEnum = InternalMemberTypeE.Field;
                PRs._name = objectOp._name;
                PRs._dtType = objectOp._dtType;
            }
            else
            {
                PRs._memberTypeEnum = InternalMemberTypeE.Item;
            }

            _objectReader.Parse(PRs);
        }

        [RequiresUnreferencedCode(BinaryParserUnreferencedCodeMessage)]
        private void ReadObjectNull(BinaryHeaderEnum binaryHeaderEnum)
        {
            _objectNull ??= new ObjectNull();
            _objectNull.Read(this, binaryHeaderEnum);

            ObjectProgress? objectOp = (ObjectProgress?)_stack.Peek();

            PRs.Init();
            PRs._parseTypeEnum = InternalParseTypeE.Member;
            PRs._memberValueEnum = InternalMemberValueE.Null;

            Debug.Assert(objectOp != null);
            if (objectOp._objectTypeEnum == InternalObjectTypeE.Object)
            {
                PRs._memberTypeEnum = InternalMemberTypeE.Field;
                PRs._name = objectOp._name;
                PRs._dtType = objectOp._dtType;
            }
            else
            {
                PRs._memberTypeEnum = InternalMemberTypeE.Item;
                PRs._consecutiveNullArrayEntryCount = _objectNull._nullCount;
                //only one null position has been incremented by GetNext
                //The position needs to be reset for the rest of the nulls
                objectOp.ArrayCountIncrement(_objectNull._nullCount - 1);
            }
            _objectReader.Parse(PRs);
        }

        private void ReadMessageEnd()
        {
            _messageEnd ??= new MessageEnd();
            _messageEnd.Read(this);

            if (!_stack.IsEmpty())
            {
                throw new SerializationException(SR.Serialization_StreamEnd);
            }
        }

        // ReadValue from stream using InternalPrimitiveTypeE code
        internal object ReadValue(InternalPrimitiveTypeE code) =>
            code switch
            {
                InternalPrimitiveTypeE.Boolean => ReadBoolean(),
                InternalPrimitiveTypeE.Byte => ReadByte(),
                InternalPrimitiveTypeE.Char => ReadChar(),
                InternalPrimitiveTypeE.Double => ReadDouble(),
                InternalPrimitiveTypeE.Int16 => ReadInt16(),
                InternalPrimitiveTypeE.Int32 => ReadInt32(),
                InternalPrimitiveTypeE.Int64 => ReadInt64(),
                InternalPrimitiveTypeE.SByte => ReadSByte(),
                InternalPrimitiveTypeE.Single => ReadSingle(),
                InternalPrimitiveTypeE.UInt16 => ReadUInt16(),
                InternalPrimitiveTypeE.UInt32 => ReadUInt32(),
                InternalPrimitiveTypeE.UInt64 => ReadUInt64(),
                InternalPrimitiveTypeE.Decimal => ReadDecimal(),
                InternalPrimitiveTypeE.TimeSpan => ReadTimeSpan(),
                InternalPrimitiveTypeE.DateTime => ReadDateTime(),
                _ => throw new SerializationException(SR.Format(SR.Serialization_TypeCode, code.ToString())),
            };

        private ObjectProgress GetOp()
        {
            ObjectProgress op;

            if (_opPool != null && !_opPool.IsEmpty())
            {
                op = (ObjectProgress)_opPool.Pop()!;
                op.Init();
            }
            else
            {
                op = new ObjectProgress();
            }

            return op;
        }

        private void PutOp(ObjectProgress op)
        {
            _opPool ??= new SerStack("opPool");
            _opPool.Push(op);
        }
    }
}
