// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    internal sealed class DllImportData
    {
        private readonly string _moduleName;
        private readonly string? _entryPoint;
        private readonly MethodImportAttributes _flags;

        internal DllImportData(string moduleName, string? entryPoint, MethodImportAttributes flags)
        {
            _moduleName = moduleName;
            _entryPoint = entryPoint;
            _flags = flags;
        }

        public string ModuleName => _moduleName;

        public string? EntryPoint => _entryPoint;

        public MethodImportAttributes Flags => _flags;

        internal static DllImportData Create(CustomAttributeInfo attr, out bool preserveSig)
        {
            string? moduleName = (string?)attr._ctorArgs[0];
            if (string.IsNullOrEmpty(moduleName))
            {
                throw new ArgumentException(SR.Argument_DllNameCannotBeEmpty);
            }

            MethodImportAttributes importAttributes = MethodImportAttributes.None;
            string? entryPoint = null;
            preserveSig = true;
            for (int i = 0; i < attr._namedParamNames.Length; ++i)
            {
                string name = attr._namedParamNames[i];
                object value = attr._namedParamValues[i]!;
                switch (name)
                {
                    case "PreserveSig":
                        preserveSig = (bool)value;
                        break;
                    case "CallingConvention":
                        importAttributes |= MatchNativeCallingConvention((CallingConvention)value);
                        break;
                    case "CharSet":
                        importAttributes |= MatchNativeCharSet((CharSet)value);
                        break;
                    case "EntryPoint":
                        entryPoint = (string?)value;
                        break;
                    case "ExactSpelling":
                        if ((bool)value)
                        {
                            importAttributes |= MethodImportAttributes.ExactSpelling;
                        }
                        break;
                    case "SetLastError":
                        if ((bool)value)
                        {
                            importAttributes |= MethodImportAttributes.SetLastError;
                        }
                        break;
                    case "BestFitMapping":
                        if ((bool)value)
                        {
                            importAttributes |= MethodImportAttributes.BestFitMappingEnable;
                        }
                        else
                        {
                            importAttributes |= MethodImportAttributes.BestFitMappingDisable;
                        }
                        break;
                    case "ThrowOnUnmappableChar":
                        if ((bool)value)
                        {
                            importAttributes |= MethodImportAttributes.ThrowOnUnmappableCharEnable;
                        }
                        else
                        {
                            importAttributes |= MethodImportAttributes.ThrowOnUnmappableCharDisable;
                        }
                        break;
                }
            }

            return new DllImportData(moduleName, entryPoint, importAttributes);
        }

        internal static DllImportData Create(string moduleName, string entryName, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                throw new ArgumentException(SR.Argument_DllNameCannotBeEmpty);
            }

            MethodImportAttributes importAttributes = MatchNativeCallingConvention(nativeCallConv);
            importAttributes |= MatchNativeCharSet(nativeCharSet);

            return new DllImportData(moduleName, entryName, importAttributes);
        }

        private static MethodImportAttributes MatchNativeCharSet(CharSet nativeCharSet) =>
            nativeCharSet switch
            {
                CharSet.Ansi => MethodImportAttributes.CharSetAnsi,
                CharSet.Auto => MethodImportAttributes.CharSetAuto,
                CharSet.Unicode => MethodImportAttributes.CharSetUnicode,
                _ => MethodImportAttributes.CharSetAuto
            };

        private static MethodImportAttributes MatchNativeCallingConvention(CallingConvention nativeCallConv) =>
            nativeCallConv switch
            {
                CallingConvention.Cdecl => MethodImportAttributes.CallingConventionCDecl,
                CallingConvention.FastCall => MethodImportAttributes.CallingConventionFastCall,
                CallingConvention.StdCall => MethodImportAttributes.CallingConventionStdCall,
                CallingConvention.ThisCall => MethodImportAttributes.CallingConventionThisCall,
                _ => MethodImportAttributes.CallingConventionWinApi // Roslyn defaults with this
            };
    }

    internal sealed class MarshallingData
    {
        private UnmanagedType _marshalType;
        private int _marshalArrayElementType;      // safe array: VarEnum; array: UnmanagedType
        private int _marshalArrayElementCount;     // number of elements in an array, length of a string, or Unspecified
        private int _marshalParameterIndex;        // index of parameter that specifies array size (short) or IID (int), or Unspecified
        private string? _marshalTypeName;          // custom marshaller: string or type name; safe array: element type name
        private string? _marshalCookie;

        internal const int Invalid = -1;
        private const UnmanagedType InvalidUnmanagedType = (UnmanagedType)Invalid;
        private const VarEnum InvalidVariantType = (VarEnum)Invalid;
        private const int MaxMarshalInteger = 0x1fffffff;

        // The logic imported from https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/PEWriter/MetadataWriter.cs#L3543
        internal BlobBuilder SerializeMarshallingData()
        {
            BlobBuilder writer = new BlobBuilder(); ;
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

                    if (_marshalTypeName != null)
                    {
                        writer.WriteSerializedString(_marshalTypeName);
                    }
                    else
                    {
                        writer.WriteByte(0);

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

                        if (_marshalTypeName != null)
                        {
                            writer.WriteSerializedString(_marshalTypeName);
                        }
                        else
                        {
                            writer.WriteByte(0);
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

            return writer;
        }

        internal void SetMarshalAsCustom(string? name, string? cookie)
        {
            _marshalType = UnmanagedType.CustomMarshaler;
            _marshalTypeName = name;
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

        internal void SetMarshalAsSafeArray(VarEnum? elementType, string? type)
        {
            Debug.Assert(elementType == null || elementType >= 0 && (int)elementType <= MaxMarshalInteger);

            _marshalType = UnmanagedType.SafeArray;
            _marshalArrayElementType = (int)(elementType ?? InvalidVariantType);
            _marshalTypeName = type;
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
        internal static MarshallingData CreateMarshallingData(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute, bool isField)
        {
            CustomAttributeInfo attributeInfo = CustomAttributeInfo.DecodeCustomAttribute(con, binaryAttribute);
            MarshallingData info = new();
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

        private static void DecodeMarshalAsFixedString(string[] paramNames, object?[] values, MarshallingData info)
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

        private static void DecodeMarshalAsSafeArray(string[] paramNames, object?[] values, MarshallingData info)
        {
            VarEnum? elementTypeVariant = null;
            string? elementType = null;
            int symbolIndex = -1;

            for (int i = 0; i < paramNames.Length; i++)
            {
                switch (paramNames[i])
                {
                    case "SafeArraySubType":
                        elementTypeVariant = (VarEnum)values[i]!;
                        break;
                    case "SafeArrayUserDefinedSubType":
                        elementType = (string?)values[i];
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

        private static void DecodeMarshalAsArray(string[] paramNames, object?[] values, bool isFixed, MarshallingData info)
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

        private static void DecodeMarshalAsComInterface(string[] paramNames, object?[] values, UnmanagedType unmanagedType, MarshallingData info)
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

        private static void DecodeMarshalAsCustom(string[] paramNames, object?[] values, MarshallingData info)
        {
            string? cookie = null;
            string? name = null;
            for (int i = 0; i < paramNames.Length; i++)
            {
                switch (paramNames[i])
                {
                    case "MarshalType":
                    case "MarshalTypeRef":
                        name = (string?)values[i];
                        break;
                    case "MarshalCookie":
                        cookie = (string?)values[i];
                        break;
                        // other parameters ignored with no error
                }
            }

            info.SetMarshalAsCustom(name, cookie);
        }
    }
}
