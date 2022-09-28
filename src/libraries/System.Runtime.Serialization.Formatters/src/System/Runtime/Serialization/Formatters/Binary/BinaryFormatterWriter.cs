// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal sealed class BinaryFormatterWriter
    {
        private const int ChunkSize = 4096;

        private readonly Stream _outputStream;
        private readonly FormatterTypeStyle _formatterTypeStyle;
        private readonly ObjectWriter _objectWriter;
        private readonly BinaryWriter _dataWriter;

        private int _consecutiveNullArrayEntryCount;
        private Dictionary<string, ObjectMapInfo>? _objectMapTable;

        private BinaryObject? _binaryObject;
        private BinaryObjectWithMap? _binaryObjectWithMap;
        private BinaryObjectWithMapTyped? _binaryObjectWithMapTyped;
        private BinaryObjectString? _binaryObjectString;
        private BinaryArray? _binaryArray;
        private byte[]? _byteBuffer;
        private MemberPrimitiveUnTyped? _memberPrimitiveUnTyped;
        private MemberPrimitiveTyped? _memberPrimitiveTyped;
        private ObjectNull? _objectNull;
        private MemberReference? _memberReference;
        private BinaryAssembly? _binaryAssembly;

        internal BinaryFormatterWriter(Stream outputStream, ObjectWriter objectWriter, FormatterTypeStyle formatterTypeStyle)
        {
            _outputStream = outputStream;
            _formatterTypeStyle = formatterTypeStyle;
            _objectWriter = objectWriter;
            _dataWriter = new BinaryWriter(outputStream, Encoding.UTF8);
        }

        internal void WriteBegin() { }

        internal void WriteEnd()
        {
            _dataWriter.Flush();
        }

        internal void WriteBoolean(bool value) => _dataWriter.Write(value);

        internal void WriteByte(byte value) => _dataWriter.Write(value);

        private void WriteBytes(byte[] value) => _dataWriter.Write(value);

        private void WriteBytes(byte[] byteA, int offset, int size) => _dataWriter.Write(byteA, offset, size);

        internal void WriteChar(char value) => _dataWriter.Write(value);

        internal void WriteChars(char[] value) => _dataWriter.Write(value);

        internal void WriteDecimal(decimal value) => WriteString(value.ToString(CultureInfo.InvariantCulture));

        internal void WriteSingle(float value) => _dataWriter.Write(value);

        internal void WriteDouble(double value) => _dataWriter.Write(value);

        internal void WriteInt16(short value) => _dataWriter.Write(value);

        internal void WriteInt32(int value) => _dataWriter.Write(value);

        internal void WriteInt64(long value) => _dataWriter.Write(value);

        internal void WriteSByte(sbyte value) => WriteByte(unchecked((byte)value));

        internal void WriteString(string value) => _dataWriter.Write(value);

        internal void WriteTimeSpan(TimeSpan value) => WriteInt64(value.Ticks);

        internal void WriteDateTime(DateTime value)
        {
            // In .NET Framework, BinaryFormatter is able to access DateTime's ToBinaryRaw,
            // which just returns the value of its sole Int64 dateData field.  Here, we don't
            // have access to that member (which doesn't even exist anymore, since it was only for
            // BinaryFormatter, which is now in a separate assembly).  To address that,
            // we access the sole field directly via an unsafe cast.
            long dateData = Unsafe.As<DateTime, long>(ref value);
            WriteInt64(dateData);
        }

        internal void WriteUInt16(ushort value) => _dataWriter.Write(value);

        internal void WriteUInt32(uint value) => _dataWriter.Write(value);

        internal void WriteUInt64(ulong value) => _dataWriter.Write(value);

        internal void WriteObjectEnd(NameInfo memberNameInfo, NameInfo typeNameInfo) { }

        internal void WriteSerializationHeaderEnd()
        {
            var record = new MessageEnd();
            record.Write(this);
        }

        internal void WriteSerializationHeader(int topId, int headerId, int minorVersion, int majorVersion)
        {
            var record = new SerializationHeaderRecord(BinaryHeaderEnum.SerializedStreamHeader, topId, headerId, minorVersion, majorVersion);
            record.Write(this);
        }

        internal void WriteObject(NameInfo nameInfo, NameInfo? typeNameInfo, int numMembers, string[] memberNames, Type[] memberTypes, WriteObjectInfo[] memberObjectInfos)
        {
            InternalWriteItemNull();
            int assemId;
            int objectId = (int)nameInfo._objectId;

            Debug.Assert(typeNameInfo != null); // Explicitly called with null. Potential bug, but closed as Won't Fix: https://github.com/dotnet/runtime/issues/31402
            string? objectName = objectId < 0 ?
                typeNameInfo.NIname : // Nested Object
                nameInfo.NIname; // Non-Nested

            _objectMapTable ??= new Dictionary<string, ObjectMapInfo>();

            Debug.Assert(objectName != null);
            if (_objectMapTable.TryGetValue(objectName, out ObjectMapInfo? objectMapInfo) &&
                objectMapInfo.IsCompatible(numMembers, memberNames, memberTypes))
            {
                // Object
                _binaryObject ??= new BinaryObject();

                _binaryObject.Set(objectId, objectMapInfo._objectId);
                _binaryObject.Write(this);
            }
            else if (!typeNameInfo._transmitTypeOnObject)
            {
                // ObjectWithMap
                _binaryObjectWithMap ??= new BinaryObjectWithMap();

                // BCL types are not placed into table
                assemId = (int)typeNameInfo._assemId;
                _binaryObjectWithMap.Set(objectId, objectName, numMembers, memberNames, assemId);

                _binaryObjectWithMap.Write(this);
                if (objectMapInfo == null)
                {
                    _objectMapTable.Add(objectName, new ObjectMapInfo(objectId, numMembers, memberNames, memberTypes));
                }
            }
            else
            {
                // ObjectWithMapTyped
                var binaryTypeEnumA = new BinaryTypeEnum[numMembers];
                var typeInformationA = new object?[numMembers];
                var assemIdA = new int[numMembers];
                for (int i = 0; i < numMembers; i++)
                {
                    object? typeInformation;
                    binaryTypeEnumA[i] = BinaryTypeConverter.GetBinaryTypeInfo(memberTypes[i], memberObjectInfos[i], null, _objectWriter, out typeInformation, out assemId);
                    typeInformationA[i] = typeInformation;
                    assemIdA[i] = assemId;
                }

                _binaryObjectWithMapTyped ??= new BinaryObjectWithMapTyped();

                // BCL types are not placed in table
                assemId = (int)typeNameInfo._assemId;
                _binaryObjectWithMapTyped.Set(objectId, objectName, numMembers, memberNames, binaryTypeEnumA, typeInformationA, assemIdA, assemId);
                _binaryObjectWithMapTyped.Write(this);
                if (objectMapInfo == null)
                {
                    _objectMapTable.Add(objectName, new ObjectMapInfo(objectId, numMembers, memberNames, memberTypes));
                }
            }
        }

        internal void WriteObjectString(int objectId, string? value)
        {
            InternalWriteItemNull();

            _binaryObjectString ??= new BinaryObjectString();

            _binaryObjectString.Set(objectId, value);
            _binaryObjectString.Write(this);
        }

        internal void WriteSingleArray(NameInfo memberNameInfo, NameInfo arrayNameInfo, WriteObjectInfo? objectInfo, NameInfo arrayElemTypeNameInfo, int length, int lowerBound, Array array)
        {
            InternalWriteItemNull();
            BinaryArrayTypeEnum binaryArrayTypeEnum;
            var lengthA = new int[1];
            lengthA[0] = length;
            int[]? lowerBoundA = null;
            object? typeInformation;

            if (lowerBound == 0)
            {
                binaryArrayTypeEnum = BinaryArrayTypeEnum.Single;
            }
            else
            {
                binaryArrayTypeEnum = BinaryArrayTypeEnum.SingleOffset;
                lowerBoundA = new int[1];
                lowerBoundA[0] = lowerBound;
            }

            int assemId;
            BinaryTypeEnum binaryTypeEnum = BinaryTypeConverter.GetBinaryTypeInfo(
                arrayElemTypeNameInfo._type!, objectInfo, arrayElemTypeNameInfo.NIname, _objectWriter, out typeInformation, out assemId);

            _binaryArray ??= new BinaryArray();
            _binaryArray.Set((int)arrayNameInfo._objectId, 1, lengthA, lowerBoundA, binaryTypeEnum, typeInformation, binaryArrayTypeEnum, assemId);

            _binaryArray.Write(this);

            if (Converter.IsWriteAsByteArray(arrayElemTypeNameInfo._primitiveTypeEnum) && (lowerBound == 0))
            {
                //array is written out as an array of bytes
                if (arrayElemTypeNameInfo._primitiveTypeEnum == InternalPrimitiveTypeE.Byte)
                {
                    WriteBytes((byte[])array);
                }
                else if (arrayElemTypeNameInfo._primitiveTypeEnum == InternalPrimitiveTypeE.Char)
                {
                    WriteChars((char[])array);
                }
                else
                {
                    WriteArrayAsBytes(array, Converter.TypeLength(arrayElemTypeNameInfo._primitiveTypeEnum));
                }
            }
        }

        private void WriteArrayAsBytes(Array array, int typeLength)
        {
            InternalWriteItemNull();
            int arrayOffset = 0;
            _byteBuffer ??= new byte[ChunkSize];

            while (arrayOffset < array.Length)
            {
                int numArrayItems = Math.Min(ChunkSize / typeLength, array.Length - arrayOffset);
                int bufferUsed = numArrayItems * typeLength;
                Buffer.BlockCopy(array, arrayOffset * typeLength, _byteBuffer, 0, bufferUsed);
                if (!BitConverter.IsLittleEndian)
                {
                    // we know that we are writing a primitive type, so just do a simple swap
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
                WriteBytes(_byteBuffer, 0, bufferUsed);
                arrayOffset += numArrayItems;
            }
        }

        internal void WriteJaggedArray(NameInfo memberNameInfo, NameInfo arrayNameInfo, WriteObjectInfo? objectInfo, NameInfo arrayElemTypeNameInfo, int length, int lowerBound)
        {
            InternalWriteItemNull();
            BinaryArrayTypeEnum binaryArrayTypeEnum;
            var lengthA = new int[1];
            lengthA[0] = length;
            int[]? lowerBoundA = null;
            object? typeInformation;
            int assemId;

            if (lowerBound == 0)
            {
                binaryArrayTypeEnum = BinaryArrayTypeEnum.Jagged;
            }
            else
            {
                binaryArrayTypeEnum = BinaryArrayTypeEnum.JaggedOffset;
                lowerBoundA = new int[1];
                lowerBoundA[0] = lowerBound;
            }

            BinaryTypeEnum binaryTypeEnum = BinaryTypeConverter.GetBinaryTypeInfo(arrayElemTypeNameInfo._type!, objectInfo, arrayElemTypeNameInfo.NIname, _objectWriter, out typeInformation, out assemId);

            _binaryArray ??= new BinaryArray();
            _binaryArray.Set((int)arrayNameInfo._objectId, 1, lengthA, lowerBoundA, binaryTypeEnum, typeInformation, binaryArrayTypeEnum, assemId);

            _binaryArray.Write(this);
        }

        internal void WriteRectangleArray(NameInfo memberNameInfo, NameInfo arrayNameInfo, WriteObjectInfo? objectInfo, NameInfo arrayElemTypeNameInfo, int rank, int[] lengthA, int[] lowerBoundA)
        {
            InternalWriteItemNull();

            BinaryArrayTypeEnum binaryArrayTypeEnum = BinaryArrayTypeEnum.Rectangular;
            object? typeInformation;
            int assemId;
            BinaryTypeEnum binaryTypeEnum = BinaryTypeConverter.GetBinaryTypeInfo(arrayElemTypeNameInfo._type!, objectInfo, arrayElemTypeNameInfo.NIname, _objectWriter, out typeInformation, out assemId);

            _binaryArray ??= new BinaryArray();

            for (int i = 0; i < rank; i++)
            {
                if (lowerBoundA[i] != 0)
                {
                    binaryArrayTypeEnum = BinaryArrayTypeEnum.RectangularOffset;
                    break;
                }
            }

            _binaryArray.Set((int)arrayNameInfo._objectId, rank, lengthA, lowerBoundA, binaryTypeEnum, typeInformation, binaryArrayTypeEnum, assemId);
            _binaryArray.Write(this);
        }

        internal void WriteObjectByteArray(NameInfo memberNameInfo, NameInfo arrayNameInfo, WriteObjectInfo? objectInfo, NameInfo arrayElemTypeNameInfo, int length, int lowerBound, byte[] byteA)
        {
            InternalWriteItemNull();
            WriteSingleArray(memberNameInfo, arrayNameInfo, objectInfo, arrayElemTypeNameInfo, length, lowerBound, byteA);
        }

        internal void WriteMember(NameInfo memberNameInfo, NameInfo typeNameInfo, object value)
        {
            InternalWriteItemNull();
            InternalPrimitiveTypeE typeInformation = typeNameInfo._primitiveTypeEnum;

            // Writes Members with primitive values
            if (memberNameInfo._transmitTypeOnMember)
            {
                _memberPrimitiveTyped ??= new MemberPrimitiveTyped();
                _memberPrimitiveTyped.Set(typeInformation, value);
                _memberPrimitiveTyped.Write(this);
            }
            else
            {
                _memberPrimitiveUnTyped ??= new MemberPrimitiveUnTyped();
                _memberPrimitiveUnTyped.Set(typeInformation, value);
                _memberPrimitiveUnTyped.Write(this);
            }
        }

        internal void WriteNullMember(NameInfo memberNameInfo, NameInfo typeNameInfo)
        {
            InternalWriteItemNull();
            _objectNull ??= new ObjectNull();

            if (!memberNameInfo._isArrayItem)
            {
                _objectNull.SetNullCount(1);
                _objectNull.Write(this);
                _consecutiveNullArrayEntryCount = 0;
            }
        }

        internal void WriteMemberObjectRef(NameInfo memberNameInfo, int idRef)
        {
            InternalWriteItemNull();
            _memberReference ??= new MemberReference();
            _memberReference.Set(idRef);
            _memberReference.Write(this);
        }

        internal void WriteMemberNested(NameInfo memberNameInfo)
        {
            InternalWriteItemNull();
        }

        internal void WriteMemberString(NameInfo memberNameInfo, NameInfo typeNameInfo, string? value)
        {
            InternalWriteItemNull();
            WriteObjectString((int)typeNameInfo._objectId, value);
        }

        internal void WriteItem(NameInfo itemNameInfo, NameInfo typeNameInfo, object value)
        {
            InternalWriteItemNull();
            WriteMember(itemNameInfo, typeNameInfo, value);
        }

        internal void WriteNullItem(NameInfo itemNameInfo, NameInfo typeNameInfo)
        {
            _consecutiveNullArrayEntryCount++;
            InternalWriteItemNull();
        }

        internal void WriteDelayedNullItem()
        {
            _consecutiveNullArrayEntryCount++;
        }

        internal void WriteItemEnd() => InternalWriteItemNull();

        private void InternalWriteItemNull()
        {
            if (_consecutiveNullArrayEntryCount > 0)
            {
                _objectNull ??= new ObjectNull();
                _objectNull.SetNullCount(_consecutiveNullArrayEntryCount);
                _objectNull.Write(this);
                _consecutiveNullArrayEntryCount = 0;
            }
        }

        internal void WriteItemObjectRef(NameInfo nameInfo, int idRef)
        {
            InternalWriteItemNull();
            WriteMemberObjectRef(nameInfo, idRef);
        }

        internal void WriteAssembly(Type? type, string assemblyString, int assemId, bool isNew)
        {
            //If the file being tested wasn't built as an assembly, then we're going to get null back
            //for the assembly name.  This is very unfortunate.
            InternalWriteItemNull();
            assemblyString ??= string.Empty;

            if (isNew)
            {
                _binaryAssembly ??= new BinaryAssembly();
                _binaryAssembly.Set(assemId, assemblyString);
                _binaryAssembly.Write(this);
            }
        }

        // Method to write a value onto a stream given its primitive type code
        internal void WriteValue(InternalPrimitiveTypeE code, object? value)
        {
            switch (code)
            {
                case InternalPrimitiveTypeE.Boolean: WriteBoolean(Convert.ToBoolean(value, CultureInfo.InvariantCulture)); break;
                case InternalPrimitiveTypeE.Byte: WriteByte(Convert.ToByte(value, CultureInfo.InvariantCulture)); break;
                case InternalPrimitiveTypeE.Char: WriteChar(Convert.ToChar(value, CultureInfo.InvariantCulture)); break;
                case InternalPrimitiveTypeE.Double: WriteDouble(Convert.ToDouble(value, CultureInfo.InvariantCulture)); break;
                case InternalPrimitiveTypeE.Int16: WriteInt16(Convert.ToInt16(value, CultureInfo.InvariantCulture)); break;
                case InternalPrimitiveTypeE.Int32: WriteInt32(Convert.ToInt32(value, CultureInfo.InvariantCulture)); break;
                case InternalPrimitiveTypeE.Int64: WriteInt64(Convert.ToInt64(value, CultureInfo.InvariantCulture)); break;
                case InternalPrimitiveTypeE.SByte: WriteSByte(Convert.ToSByte(value, CultureInfo.InvariantCulture)); break;
                case InternalPrimitiveTypeE.Single: WriteSingle(Convert.ToSingle(value, CultureInfo.InvariantCulture)); break;
                case InternalPrimitiveTypeE.UInt16: WriteUInt16(Convert.ToUInt16(value, CultureInfo.InvariantCulture)); break;
                case InternalPrimitiveTypeE.UInt32: WriteUInt32(Convert.ToUInt32(value, CultureInfo.InvariantCulture)); break;
                case InternalPrimitiveTypeE.UInt64: WriteUInt64(Convert.ToUInt64(value, CultureInfo.InvariantCulture)); break;
                case InternalPrimitiveTypeE.Decimal: WriteDecimal(Convert.ToDecimal(value, CultureInfo.InvariantCulture)); break;
                case InternalPrimitiveTypeE.TimeSpan: WriteTimeSpan((TimeSpan)value!); break;
                case InternalPrimitiveTypeE.DateTime: WriteDateTime((DateTime)value!); break;
                default: throw new SerializationException(SR.Format(SR.Serialization_TypeCode, code.ToString()));
            }
        }

        private sealed class ObjectMapInfo
        {
            internal readonly int _objectId;
            private readonly int _numMembers;
            private readonly string[] _memberNames;
            private readonly Type[] _memberTypes;

            internal ObjectMapInfo(int objectId, int numMembers, string[] memberNames, Type[] memberTypes)
            {
                _objectId = objectId;
                _numMembers = numMembers;
                _memberNames = memberNames;
                _memberTypes = memberTypes;
            }

            internal bool IsCompatible(int numMembers, string[] memberNames, Type[]? memberTypes)
            {
                if (_numMembers != numMembers)
                {
                    return false;
                }

                for (int i = 0; i < numMembers; i++)
                {
                    if (!(_memberNames[i].Equals(memberNames[i])))
                    {
                        return false;
                    }

                    if ((memberTypes != null) && (_memberTypes[i] != memberTypes[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
