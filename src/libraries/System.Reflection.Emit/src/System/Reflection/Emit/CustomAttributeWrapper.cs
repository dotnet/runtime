// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    internal readonly struct CustomAttributeWrapper
    {
        private readonly ConstructorInfo _constructorInfo;
        private readonly byte[] _binaryAttribute;

        public CustomAttributeWrapper(ConstructorInfo constructorInfo, ReadOnlySpan<byte> binaryAttribute)
        {
            _constructorInfo = constructorInfo;
            _binaryAttribute = binaryAttribute.ToArray(); // TODO: Update to BlobHandle when public API public APi for MetadataBuilder.GetOrAddBlob(ReadOnlySpan<byte>) added
        }

        public ConstructorInfo Ctor => _constructorInfo;
        public byte[] Data => _binaryAttribute;
    }

    internal struct CustomAttributeInfo
    {
        public ConstructorInfo _ctor;
        public object?[] _ctorArgs;
        public string[] _namedParamNames;
        public object?[] _namedParamValues;
        private const int Field = 0x53;
        private const int EnumType = 0x55;
        private const int NullValue = 0xff;
        private const int OneByteMask = 0x7f;
        private const int TwoByteMask = 0x3f;
        private const int FourByteMask = 0x1f;

        internal static CustomAttributeInfo DecodeCustomAttribute(ConstructorInfo ctor, ReadOnlySpan<byte> binaryAttribute)
        {
            int pos = 2;
            CustomAttributeInfo info = default;

            if (binaryAttribute.Length < 2)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidCustomAttributeLength, ctor.DeclaringType, binaryAttribute.Length), nameof(binaryAttribute));
            }
            if ((binaryAttribute[0] != 0x01) || (binaryAttribute[1] != 0x00))
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidProlog, ctor.DeclaringType), nameof(binaryAttribute));
            }

            ParameterInfo[] pi = ctor.GetParameters();
            info._ctor = ctor;
            info._ctorArgs = new object?[pi.Length];
            for (int i = 0; i < pi.Length; ++i)
            {
                info._ctorArgs[i] = DecodeCustomAttributeValue(pi[i].ParameterType, binaryAttribute, pos, out pos);
            }
            int numNamed = BinaryPrimitives.ReadUInt16LittleEndian(binaryAttribute.Slice(pos));
            pos += 2;

            info._namedParamNames = new string[numNamed];
            info._namedParamValues = new object[numNamed];
            for (int i = 0; i < numNamed; ++i)
            {
                int namedType = binaryAttribute[pos++];
                int dataType = binaryAttribute[pos++];

                if (dataType == EnumType)
                {
                    // skip bytes for Enum type name;
                    int len2 = DecodeLen(binaryAttribute, pos, out pos);
                    pos += len2;
                }

                int len = DecodeLen(binaryAttribute, pos, out pos);
                string name = StringFromBytes(binaryAttribute, pos, len);
                info._namedParamNames[i] = name;
                pos += len;

                if (namedType == Field)
                {
                    // For known pseudo custom attributes underlying Enum type is int
                    Type fieldType = dataType == EnumType ? typeof(int) : ElementTypeToType((PrimitiveSerializationTypeCode)dataType);
                    info._namedParamValues[i] = DecodeCustomAttributeValue(fieldType, binaryAttribute, pos, out pos); ;
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.Argument_UnknownNamedType, ctor.DeclaringType, namedType), nameof(binaryAttribute));
                }
            }

            return info;
        }

        private static string StringFromBytes(ReadOnlySpan<byte> data, int pos, int len)
        {
            return Text.Encoding.UTF8.GetString(data.Slice(pos, len));
        }

        private static int DecodeLen(ReadOnlySpan<byte> data, int pos, out int rpos)
        {
            int len;
            if ((data[pos] & 0x80) == 0)
            {
                len = (data[pos++] & OneByteMask);
            }
            else if ((data[pos] & 0x40) == 0)
            {
                len = ((data[pos] & TwoByteMask) << 8) + data[pos + 1];
                pos += 2;
            }
            else
            {
                len = ((data[pos] & FourByteMask) << 24) + (data[pos + 1] << 16) + (data[pos + 2] << 8) + data[pos + 3];
                pos += 4;
            }
            rpos = pos;
            return len;
        }

        private static object? DecodeCustomAttributeValue(Type t, ReadOnlySpan<byte> data, int pos, out int rpos)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.String:
                    if (data[pos] == NullValue)
                    {
                        rpos = pos + 1;
                        return null;
                    }
                    int len = DecodeLen(data, pos, out pos);
                    rpos = pos + len;
                    return StringFromBytes(data, pos, len);
                case TypeCode.Int32:
                    rpos = pos + 4;
                    return BinaryPrimitives.ReadInt32LittleEndian(data.Slice(pos));
                case TypeCode.Int16:
                    rpos = pos + 2;
                    return BinaryPrimitives.ReadInt16LittleEndian(data.Slice(pos));
                case TypeCode.Boolean:
                    rpos = pos + 1;
                    return (data[pos] == 0) ? false : true;
                case TypeCode.Object:
                    int subtype = data[pos];
                    pos += 1;

                    if (subtype >= 0x02 && subtype <= 0x0e)
                    {
                        return DecodeCustomAttributeValue(ElementTypeToType((PrimitiveSerializationTypeCode)subtype), data, pos, out rpos);
                    }
                    break;
            }

            throw new NotImplementedException(SR.Format(SR.NotImplemented_TypeForValue, t));
        }

        private static Type ElementTypeToType(PrimitiveSerializationTypeCode elementType) =>
            elementType switch
            {
                PrimitiveSerializationTypeCode.Boolean => typeof(bool),
                PrimitiveSerializationTypeCode.Char => typeof(char),
                PrimitiveSerializationTypeCode.SByte => typeof(sbyte),
                PrimitiveSerializationTypeCode.Byte => typeof(byte),
                PrimitiveSerializationTypeCode.Int16 => typeof(short),
                PrimitiveSerializationTypeCode.UInt16 => typeof(ushort),
                PrimitiveSerializationTypeCode.Int32 => typeof(int),
                PrimitiveSerializationTypeCode.UInt32 => typeof(uint),
                PrimitiveSerializationTypeCode.Int64 => typeof(long),
                PrimitiveSerializationTypeCode.UInt64 => typeof(ulong),
                PrimitiveSerializationTypeCode.Single => typeof(float),
                PrimitiveSerializationTypeCode.Double => typeof(double),
                PrimitiveSerializationTypeCode.String => typeof(string),
                _ => throw new ArgumentException(SR.Argument_InvalidTypeCodeForTypeArgument, "binaryAttribute"),
            };
    }
}
