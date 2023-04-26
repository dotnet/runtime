// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Reflection.Emit
{
    internal sealed class FieldBuilderImpl : FieldBuilder
    {
        private readonly TypeBuilderImpl _typeBuilder;
        private readonly string _fieldName;
        private readonly Type _fieldType;
        private FieldAttributes _attributes;

        internal MarshallingInfo? _marshallingInfo;
        internal int _offset;
        internal List<CustomAttributeWrapper>? _customAttributes;

        internal FieldBuilderImpl(TypeBuilderImpl typeBuilder, string fieldName, Type type, FieldAttributes attributes)
        {
            _fieldName = fieldName;
            _typeBuilder = typeBuilder;
            _fieldType = type;
            _attributes = attributes & ~FieldAttributes.ReservedMask;
            _offset = -1;
        }

        protected override void SetConstantCore(object? defaultValue) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            // Handle pseudo custom attributes
            switch (con.ReflectedType!.FullName)
            {
                case "System.Runtime.InteropServices.FieldOffsetAttribute":
                    Debug.Assert(binaryAttribute.Length >= 6);
                    _offset = BinaryPrimitives.ReadInt32LittleEndian(binaryAttribute.Slice(2));
                return;
                case "System.NonSerializedAttribute":
#pragma warning disable SYSLIB0050 // 'FieldAttributes.NotSerialized' is obsolete: 'Formatter-based serialization is obsolete and should not be used'.
                    _attributes |= FieldAttributes.NotSerialized;
#pragma warning restore SYSLIB0050
                return;
                case "System.Runtime.CompilerServices.SpecialNameAttribute":
                    _attributes |= FieldAttributes.SpecialName;
                return;
                case "System.Runtime.InteropServices.MarshalAsAttribute":
                    _attributes |= FieldAttributes.HasFieldMarshal;
                    _marshallingInfo = MarshallingInfo.ParseMarshallingInfo(con, binaryAttribute);
                return;
            }

            _customAttributes ??= new List<CustomAttributeWrapper>();
            _customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
        }

        protected override void SetOffsetCore(int iOffset)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(iOffset);

            _offset = iOffset;
        }

        #region MemberInfo Overrides

        public override int MetadataToken => throw new NotImplementedException();

        public override Module Module => _typeBuilder.Module;

        public override string Name => _fieldName;

        public override Type? DeclaringType => _typeBuilder;

        public override Type? ReflectedType => _typeBuilder;

        #endregion

        #region FieldInfo Overrides
        public override Type FieldType => _fieldType;

        public override object? GetValue(object? obj) => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override void SetValue(object? obj, object? val, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
            => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override RuntimeFieldHandle FieldHandle => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override FieldAttributes Attributes => _attributes;

        #endregion

        #region ICustomAttributeProvider Implementation
        public override object[] GetCustomAttributes(bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        #endregion

        internal sealed class MarshallingInfo
        {
            internal UnmanagedType _marshalType;
            private int _marshalArrayElementType;      // safe array: VarEnum; array: UnmanagedType
            private int _marshalArrayElementCount;     // number of elements in an array, length of a string, or Unspecified
            private int _marshalParameterIndex;        // index of parameter that specifies array size (short) or IID (int), or Unspecified
            private object? _marshalTypeNameOrSymbol;  // custom marshaller: string or Type; safe array: element type
            private string? _marshalCookie;

            internal const int Invalid = -1;
            private const UnmanagedType InvalidUnmanagedType = (UnmanagedType)Invalid;
            private const VarEnum InvalidVariantType = (VarEnum)Invalid;
            internal const int MaxMarshalInteger = 0x1fffffff;

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
            internal static MarshallingInfo ParseMarshallingInfo(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
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
                        DecodeMarshalAsArray(attributeInfo._namedParamNames, attributeInfo._namedParamValues, isFixed: true, info);
                        break;
                    case UnmanagedType.SafeArray:
                        DecodeMarshalAsSafeArray(attributeInfo._namedParamNames, attributeInfo._namedParamValues, info);
                        break;
                    case UnmanagedType.ByValTStr:
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
                            throw new ArgumentException(SR.Argument_InvalidTypeArgument, nameof(binaryAttribute));
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
                            throw new ArgumentException(SR.Argument_InvalidTypeArgument, "binaryAttribute");
                        // other parameters ignored with no error
                    }
                }

                if (elementCount < 0)
                {
                    // SizeConst must be specified:
                    throw new ArgumentException(SR.Argument_InvalidTypeArgument, "binaryAttribute");
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
                            throw new ArgumentException(SR.Argument_InvalidTypeArgument, "binaryAttribute");
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
                            throw new ArgumentException(SR.Argument_InvalidTypeArgument, "binaryAttribute");
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
                            throw new ArgumentException(SR.Argument_InvalidTypeArgument, "binaryAttribute");
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
}
