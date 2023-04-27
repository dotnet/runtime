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

    internal sealed class MarshallingInfo
    {
        private UnmanagedType _marshalType;
        private int _marshalArrayElementType;      // safe array: VarEnum; array: UnmanagedType
        private int _marshalArrayElementCount;     // number of elements in an array, length of a string, or Unspecified
        private int _marshalParameterIndex;        // index of parameter that specifies array size (short) or IID (int), or Unspecified
        private object? _marshalTypeNameOrSymbol;  // custom marshaller: string or Type; safe array: element type
        private string? _marshalCookie;

        internal const int Invalid = -1;
        private const UnmanagedType InvalidUnmanagedType = (UnmanagedType)Invalid;
        private const VarEnum InvalidVariantType = (VarEnum)Invalid;
        private const int MaxMarshalInteger = 0x1fffffff;

        internal BlobHandle PopulateMarshallingBlob(MetadataBuilder builder)
        {
            var blobBuilder = new BlobBuilder();
            SerializeMarshallingDescriptor(blobBuilder);
            return builder.GetOrAddBlob(blobBuilder);

        }

        // The logic imported from https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/PEWriter/MetadataWriter.cs#L3543
        internal void SerializeMarshallingDescriptor(BlobBuilder writer)
        {
            writer.WriteCompressedInteger((int)_marshalType);
            switch (_marshalType)
            {
                case UnmanagedType.ByValArray: // NATIVE_TYPE_FIXEDARRAY
                    Debug.Assert(_marshalArrayElementCount >= 0);
                    writer.WriteCompressedInteger(_marshalArrayElementCount);
                    if (_marshalArrayElementType >= 0)
                    {
                        writer.WriteCompressedInteger(_marshalArrayElementType);
                    }
                    break;
                case UnmanagedType.CustomMarshaler:
                    writer.WriteUInt16(0); // padding

                    switch (_marshalTypeNameOrSymbol)
                    {
                        case Type type:
                            writer.WriteSerializedString(type.FullName); // or AssemblyQualifiedName?
                            break;
                        case null:
                            writer.WriteByte(0);
                            break;
                        default:
                            writer.WriteSerializedString((string)_marshalTypeNameOrSymbol);
                            break;
                    }

                    if (_marshalCookie != null)
                    {
                        writer.WriteSerializedString(_marshalCookie);
                    }
                    else
                    {
                        writer.WriteByte(0);
                    }
                    break;
                case UnmanagedType.LPArray: // NATIVE_TYPE_ARRAY
                    Debug.Assert(_marshalArrayElementType >= 0);
                    writer.WriteCompressedInteger(_marshalArrayElementType);
                    if (_marshalParameterIndex >= 0)
                    {
                        writer.WriteCompressedInteger(_marshalParameterIndex);
                        if (_marshalArrayElementCount >= 0)
                        {
                            writer.WriteCompressedInteger(_marshalArrayElementCount);
                            writer.WriteByte(1); // The parameter number is valid
                        }
                    }
                    else if (_marshalArrayElementCount >= 0)
                    {
                        writer.WriteByte(0); // Dummy parameter value emitted so that NumberOfElements can be in a known position
                        writer.WriteCompressedInteger(_marshalArrayElementCount);
                        writer.WriteByte(0); // The parameter number is not valid
                    }
                    break;
                case UnmanagedType.SafeArray:
                    VarEnum safeArrayElementSubtype = (VarEnum)_marshalArrayElementType;
                    if (safeArrayElementSubtype >= 0)
                    {
                        writer.WriteCompressedInteger((int)safeArrayElementSubtype);

                        if (_marshalTypeNameOrSymbol is Type elementType)
                        {
                            writer.WriteSerializedString(elementType.FullName);
                        }
                    }
                    break;
                case UnmanagedType.ByValTStr: // NATIVE_TYPE_FIXEDSYSSTRING
                    writer.WriteCompressedInteger(_marshalArrayElementCount);
                    break;

                case UnmanagedType.Interface:
                case UnmanagedType.IDispatch:
                case UnmanagedType.IUnknown:
                    if (_marshalParameterIndex >= 0)
                    {
                        writer.WriteCompressedInteger(_marshalParameterIndex);
                    }
                    break;
            }
        }

        internal void SetMarshalAsCustom(object typeSymbolOrName, string? cookie)
        {
            _marshalType = UnmanagedType.CustomMarshaler;
            _marshalTypeNameOrSymbol = typeSymbolOrName;
            _marshalCookie = cookie;
        }

        internal void SetMarshalAsComInterface(UnmanagedType unmanagedType, int? parameterIndex)
        {
            Debug.Assert(parameterIndex == null || parameterIndex >= 0 && parameterIndex <= MaxMarshalInteger);

            _marshalType = unmanagedType;
            _marshalParameterIndex = parameterIndex ?? Invalid;
        }

        internal void SetMarshalAsArray(UnmanagedType? elementType, int? elementCount, short? parameterIndex)
        {
            Debug.Assert(elementCount == null || elementCount >= 0 && elementCount <= MaxMarshalInteger);
            Debug.Assert(parameterIndex == null || parameterIndex >= 0);

            _marshalType = UnmanagedType.LPArray;
            _marshalArrayElementType = (int)(elementType ?? (UnmanagedType)0x50);
            _marshalArrayElementCount = elementCount ?? Invalid;
            _marshalParameterIndex = parameterIndex ?? Invalid;
        }

        internal void SetMarshalAsFixedArray(UnmanagedType? elementType, int? elementCount)
        {
            Debug.Assert(elementCount == null || elementCount >= 0 && elementCount <= MaxMarshalInteger);
            Debug.Assert(elementType == null || elementType >= 0 && (int)elementType <= MaxMarshalInteger);

            _marshalType = UnmanagedType.ByValArray;
            _marshalArrayElementType = (int)(elementType ?? InvalidUnmanagedType);
            _marshalArrayElementCount = elementCount ?? Invalid;
        }

        internal void SetMarshalAsSafeArray(VarEnum? elementType, Type? type)
        {
            Debug.Assert(elementType == null || elementType >= 0 && (int)elementType <= MaxMarshalInteger);

            _marshalType = UnmanagedType.SafeArray;
            _marshalArrayElementType = (int)(elementType ?? InvalidVariantType);
            _marshalTypeNameOrSymbol = type;
        }

        internal void SetMarshalAsFixedString(int elementCount)
        {
            Debug.Assert(elementCount >= 0 && elementCount <= MaxMarshalInteger);

            _marshalType = UnmanagedType.ByValTStr;
            _marshalArrayElementCount = elementCount;
        }

        internal void SetMarshalAsSimpleType(UnmanagedType type)
        {
            Debug.Assert(type >= 0 && (int)type <= MaxMarshalInteger);
            _marshalType = type;
        }

        // The logic imported from https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Symbols/Attributes/MarshalAsAttributeDecoder.cs
        internal static MarshallingInfo ParseMarshallingInfo(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute, bool isField)
        {
            CustomAttributeInfo attributeInfo = CustomAttributeInfo.DecodeCustomAttribute(con, binaryAttribute);
            MarshallingInfo info = new();
            UnmanagedType unmanagedType;

            if (attributeInfo._ctorArgs[0] is short shortValue)
            {
                unmanagedType = (UnmanagedType)shortValue;
            }
            else
            {
                unmanagedType = (UnmanagedType)attributeInfo._ctorArgs[0]!;
            }

            switch (unmanagedType)
            {
                case UnmanagedType.CustomMarshaler:
                    DecodeMarshalAsCustom(attributeInfo._namedParamNames, attributeInfo._namedParamValues, info);
                    break;
                case UnmanagedType.Interface:
                case UnmanagedType.IDispatch:
                case UnmanagedType.IUnknown:
                    DecodeMarshalAsComInterface(attributeInfo._namedParamNames, attributeInfo._namedParamValues, unmanagedType, info);
                    break;
                case UnmanagedType.LPArray:
                    DecodeMarshalAsArray(attributeInfo._namedParamNames, attributeInfo._namedParamValues, isFixed: false, info);
                    break;
                case UnmanagedType.ByValArray:
                    if (!isField)
                    {
                        throw new NotSupportedException(SR.Format(SR.NotSupported_UnmanagedTypeOnlyForFields, nameof(UnmanagedType.ByValArray)));
                    }
                    DecodeMarshalAsArray(attributeInfo._namedParamNames, attributeInfo._namedParamValues, isFixed: true, info);
                    break;
                case UnmanagedType.SafeArray:
                    DecodeMarshalAsSafeArray(attributeInfo._namedParamNames, attributeInfo._namedParamValues, info);
                    break;
                case UnmanagedType.ByValTStr:
                    if (!isField)
                    {
                        throw new NotSupportedException(SR.Format(SR.NotSupported_UnmanagedTypeOnlyForFields, nameof(UnmanagedType.ByValArray)));
                    }
                    DecodeMarshalAsFixedString(attributeInfo._namedParamNames, attributeInfo._namedParamValues, info);
                    break;
#pragma warning disable CS0618 // Type or member is obsolete
                case UnmanagedType.VBByRefStr:
#pragma warning restore CS0618
                    // named parameters ignored with no error
                    info.SetMarshalAsSimpleType(unmanagedType);
                    break;
                default:
                    if ((int)unmanagedType < 0 || (int)unmanagedType > MaxMarshalInteger)
                    {
                        throw new ArgumentException(SR.Argument_InvalidArgumentForAttribute, nameof(con));
                    }
                    else
                    {
                        // named parameters ignored with no error
                        info.SetMarshalAsSimpleType(unmanagedType);
                    }
                    break;
            }

            return info;
        }

        private static void DecodeMarshalAsFixedString(string[] paramNames, object?[] values, MarshallingInfo info)
        {
            int elementCount = -1;

            for (int i = 0; i < paramNames.Length; i++)
            {
                switch (paramNames[i])
                {
                    case "SizeConst":
                        elementCount = (int)values[i]!;
                        break;
                    case "ArraySubType":
                    case "SizeParamIndex":
                        throw new ArgumentException(SR.Format(SR.Argument_InvalidParameterForUnmanagedType, paramNames[i], "ByValTStr"), "binaryAttribute");
                        // other parameters ignored with no error
                }
            }

            if (elementCount < 0)
            {
                // SizeConst must be specified:
                throw new ArgumentException(SR.Argument_SizeConstMustBeSpecified, "binaryAttribute");
            }

            info.SetMarshalAsFixedString(elementCount);
        }

        private static void DecodeMarshalAsSafeArray(string[] paramNames, object?[] values, MarshallingInfo info)
        {
            VarEnum? elementTypeVariant = null;
            Type? elementType = null;
            int symbolIndex = -1;

            for (int i = 0; i < paramNames.Length; i++)
            {
                switch (paramNames[i])
                {
                    case "SafeArraySubType":
                        elementTypeVariant = (VarEnum)values[i]!;
                        break;
                    case "SafeArrayUserDefinedSubType":
                        elementType = (Type?)values[i];
                        symbolIndex = i;
                        break;
                    case "ArraySubType":
                    case "SizeConst":
                    case "SizeParamIndex":
                        throw new ArgumentException(SR.Format(SR.Argument_InvalidParameterForUnmanagedType, paramNames[i], "SafeArray"), "binaryAttribute");
                        // other parameters ignored with no error
                }
            }

            switch (elementTypeVariant)
            {
                case VarEnum.VT_DISPATCH:
                case VarEnum.VT_UNKNOWN:
                case VarEnum.VT_RECORD:
                    // only these variants accept specification of user defined subtype
                    break;

                default:
                    if (elementTypeVariant != null && symbolIndex >= 0)
                    {
                        throw new ArgumentException(SR.Format(SR.Argument_InvalidParameterForUnmanagedType, elementType, "SafeArray"), "binaryAttribute");
                    }
                    else
                    {
                        // type ignored:
                        elementType = null;
                    }
                    break;
            }

            info.SetMarshalAsSafeArray(elementTypeVariant, elementType);
        }

        private static void DecodeMarshalAsArray(string[] paramNames, object?[] values, bool isFixed, MarshallingInfo info)
        {
            UnmanagedType? elementType = null;
            int? elementCount = isFixed ? 1 : null;
            short? parameterIndex = null;

            for (int i = 0; i < paramNames.Length; i++)
            {
                switch (paramNames[i])
                {
                    case "ArraySubType":
                        elementType = (UnmanagedType)values[i]!;
                        break;
                    case "SizeConst":
                        elementCount = (int?)values[i];
                        break;
                    case "SizeParamIndex":
                        if (isFixed)
                        {
                            goto case "SafeArraySubType";
                        }
                        parameterIndex = (short?)values[i];
                        break;
                    case "SafeArraySubType":
                        throw new ArgumentException(SR.Format(SR.Argument_InvalidParameterForUnmanagedType,
                            paramNames[i], isFixed ? "ByValArray" : "LPArray"), "binaryAttribute");
                        // other parameters ignored with no error
                }
            }

            if (isFixed)
            {
                info.SetMarshalAsFixedArray(elementType, elementCount);
            }
            else
            {
                info.SetMarshalAsArray(elementType, elementCount, parameterIndex);
            }
        }

        private static void DecodeMarshalAsComInterface(string[] paramNames, object?[] values, UnmanagedType unmanagedType, MarshallingInfo info)
        {
            int? parameterIndex = null;
            for (int i = 0; i < paramNames.Length; i++)
            {
                if (paramNames[i] == "IidParameterIndex")
                {
                    parameterIndex = (int?)values[i];
                    break;
                }
            }

            info.SetMarshalAsComInterface(unmanagedType, parameterIndex);
        }

        private static void DecodeMarshalAsCustom(string[] paramNames, object?[] values, MarshallingInfo info)
        {
            string? cookie = null;
            Type? type = null;
            string? name = null;
            for (int i = 0; i < paramNames.Length; i++)
            {
                switch (paramNames[i])
                {
                    case "MarshalType":
                        name = (string?)values[i];
                        break;
                    case "MarshalTypeRef":
                        type = (Type?)values[i];
                        break;
                    case "MarshalCookie":
                        cookie = (string?)values[i];
                        break;
                        // other parameters ignored with no error
                }
            }

            info.SetMarshalAsCustom((object?)name ?? type!, cookie);
        }
    }
}
