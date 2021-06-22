// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.IL;
using Debug = System.Diagnostics.Debug;
using Internal.IL.Stubs;

namespace Internal.TypeSystem.Interop
{
    public static partial class MarshalHelpers
    {
        internal static TypeDesc GetNativeTypeFromMarshallerKind(TypeDesc type,
                MarshallerKind kind,
                MarshallerKind elementMarshallerKind,
#if !READYTORUN
                InteropStateManager interopStateManager,
#endif
                MarshalAsDescriptor marshalAs,
                bool isArrayElement = false)
        {
            TypeSystemContext context = type.Context;
            NativeTypeKind nativeType = NativeTypeKind.Default;
            if (marshalAs != null)
            {
                nativeType = isArrayElement ? marshalAs.ArraySubType : marshalAs.Type;
            }

            switch (kind)
            {
                case MarshallerKind.BlittableValue:
                    {
                        switch (nativeType)
                        {
                            case NativeTypeKind.I1:
                                return context.GetWellKnownType(WellKnownType.SByte);
                            case NativeTypeKind.U1:
                                return context.GetWellKnownType(WellKnownType.Byte);
                            case NativeTypeKind.I2:
                                return context.GetWellKnownType(WellKnownType.Int16);
                            case NativeTypeKind.U2:
                                return context.GetWellKnownType(WellKnownType.UInt16);
                            case NativeTypeKind.I4:
                                return context.GetWellKnownType(WellKnownType.Int32);
                            case NativeTypeKind.U4:
                                return context.GetWellKnownType(WellKnownType.UInt32);
                            case NativeTypeKind.I8:
                                return context.GetWellKnownType(WellKnownType.Int64);
                            case NativeTypeKind.U8:
                                return context.GetWellKnownType(WellKnownType.UInt64);
                            case NativeTypeKind.R4:
                                return context.GetWellKnownType(WellKnownType.Single);
                            case NativeTypeKind.R8:
                                return context.GetWellKnownType(WellKnownType.Double);
                            default:
                                return type.UnderlyingType;
                        }
                    }

                case MarshallerKind.Bool:
                    return context.GetWellKnownType(WellKnownType.Int32);

                case MarshallerKind.CBool:
                        return context.GetWellKnownType(WellKnownType.Byte);

                case MarshallerKind.VariantBool:
                    return context.GetWellKnownType(WellKnownType.Int16);

                case MarshallerKind.Enum:
                case MarshallerKind.BlittableStruct:
                case MarshallerKind.Decimal:
                case MarshallerKind.VoidReturn:
                    return type;

#if !READYTORUN
                case MarshallerKind.Struct:
                case MarshallerKind.LayoutClass:
                    return interopStateManager.GetStructMarshallingNativeType((MetadataType)type);
#endif

                case MarshallerKind.BlittableStructPtr:
                    return type.MakePointerType();

                case MarshallerKind.HandleRef:
                    return context.GetWellKnownType(WellKnownType.IntPtr);

                case MarshallerKind.UnicodeChar:
                    if (nativeType == NativeTypeKind.U2)
                        return context.GetWellKnownType(WellKnownType.UInt16);
                    else
                        return context.GetWellKnownType(WellKnownType.Int16);

                case MarshallerKind.OleDateTime:
                    return context.GetWellKnownType(WellKnownType.Double);

                case MarshallerKind.SafeHandle:
                case MarshallerKind.CriticalHandle:
                    return context.GetWellKnownType(WellKnownType.IntPtr);

                case MarshallerKind.BSTRString:
                case MarshallerKind.UnicodeString:
                case MarshallerKind.UnicodeStringBuilder:
                    return context.GetWellKnownType(WellKnownType.Char).MakePointerType();

                case MarshallerKind.AnsiBSTRString:
                case MarshallerKind.AnsiString:
                case MarshallerKind.AnsiStringBuilder:
                case MarshallerKind.UTF8String:
                    return context.GetWellKnownType(WellKnownType.Byte).MakePointerType();

                case MarshallerKind.BlittableArray:
                case MarshallerKind.Array:
                case MarshallerKind.AnsiCharArray:
                    {
                        ArrayType arrayType = type as ArrayType;
                        Debug.Assert(arrayType != null, "Expecting array");

                        //
                        // We need to construct the unsafe array from the right unsafe array element type
                        //
                        TypeDesc elementNativeType = GetNativeTypeFromMarshallerKind(
                            arrayType.ElementType,
                            elementMarshallerKind,
                            MarshallerKind.Unknown,
#if !READYTORUN
                            interopStateManager,
#endif
                            marshalAs,
                            isArrayElement: true);

                        return elementNativeType.MakePointerType();
                    }

                case MarshallerKind.AnsiChar:
                    return context.GetWellKnownType(WellKnownType.Byte);

                case MarshallerKind.FunctionPointer:
                    return context.GetWellKnownType(WellKnownType.IntPtr);

#if !READYTORUN
                case MarshallerKind.ByValUnicodeString:
                case MarshallerKind.ByValAnsiString:
                    {
                        var inlineArrayCandidate = GetInlineArrayCandidate(context.GetWellKnownType(WellKnownType.Char), elementMarshallerKind, interopStateManager, marshalAs);
                        return interopStateManager.GetInlineArrayType(inlineArrayCandidate);
                    }

                case MarshallerKind.ByValAnsiCharArray:
                case MarshallerKind.ByValArray:
                    {
                        ArrayType arrayType = type as ArrayType;
                        Debug.Assert(arrayType != null, "Expecting array");

                        var inlineArrayCandidate = GetInlineArrayCandidate(arrayType.ElementType, elementMarshallerKind, interopStateManager, marshalAs);

                        return interopStateManager.GetInlineArrayType(inlineArrayCandidate);
                    }
#endif

                case MarshallerKind.LayoutClassPtr:
                case MarshallerKind.AsAnyA:
                case MarshallerKind.AsAnyW:
                    return context.GetWellKnownType(WellKnownType.IntPtr);

                case MarshallerKind.ComInterface:
                    return context.GetWellKnownType(WellKnownType.IntPtr);

#if !READYTORUN
                case MarshallerKind.Variant:
                    return InteropTypes.GetVariant(context);
#endif

                case MarshallerKind.OleCurrency:
                    return context.GetWellKnownType(WellKnownType.Int64);

                case MarshallerKind.Unknown:
                default:
                    throw new NotSupportedException();
            }
        }

        private static bool HasCopyConstructorCustomModifier(int? parameterIndex,
            EmbeddedSignatureData[] customModifierData)
        {
            if (!parameterIndex.HasValue || customModifierData == null)
                return false;

            string customModifierIndex = MethodSignature.GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(parameterIndex.Value);
            foreach (var customModifier in customModifierData)
            {
                if (customModifier.kind != EmbeddedSignatureDataKind.RequiredCustomModifier)
                    continue;

                if (customModifier.index != customModifierIndex)
                    continue;

                var customModifierType = customModifier.type as DefType;
                if (customModifierType == null)
                    continue;

                if ((customModifierType.Namespace == "System.Runtime.CompilerServices" && customModifierType.Name == "IsCopyConstructed") ||
                    (customModifierType.Namespace == "Microsoft.VisualC" && customModifierType.Name == "NeedsCopyConstructorModifier"))
                {
                    return true;
                }
            }

            return false;
        }

        internal static MarshallerKind GetMarshallerKind(
            TypeDesc type,
            int? parameterIndex,
            EmbeddedSignatureData[] customModifierData,
            MarshalAsDescriptor marshalAs,
            bool isReturn,
            bool isAnsi,
            MarshallerType marshallerType,
            out MarshallerKind elementMarshallerKind)
        {
            elementMarshallerKind = MarshallerKind.Invalid;

            bool isByRef = false;
            if (type.IsByRef)
            {
                isByRef = true;

                type = type.GetParameterType();

                if (!type.IsPrimitive && type.IsValueType && marshallerType != MarshallerType.Field
                    && HasCopyConstructorCustomModifier(parameterIndex, customModifierData))
                {
                    return MarshallerKind.BlittableValueClassWithCopyCtor;
                }

                // Compat note: CLR allows ref returning blittable structs for IJW
                if (isReturn)
                    return MarshallerKind.Invalid;
            }
            TypeSystemContext context = type.Context;
            NativeTypeKind nativeType = NativeTypeKind.Default;
            bool isField = marshallerType == MarshallerType.Field;

            if (marshalAs != null)
                nativeType = marshalAs.Type;

            //
            // Determine MarshalerKind
            //
            if (type.IsPrimitive)
            {
                switch (type.Category)
                {
                    case TypeFlags.Void:
                        return MarshallerKind.VoidReturn;

                    case TypeFlags.Boolean:
                        switch (nativeType)
                        {
                            case NativeTypeKind.Default:
                            case NativeTypeKind.Boolean:
                                return MarshallerKind.Bool;

                            case NativeTypeKind.U1:
                            case NativeTypeKind.I1:
                                return MarshallerKind.CBool;

                            case NativeTypeKind.VariantBool:
                                return MarshallerKind.VariantBool;

                            default:
                                return MarshallerKind.Invalid;
                        }

                    case TypeFlags.Char:
                        switch (nativeType)
                        {
                            case NativeTypeKind.I1:
                            case NativeTypeKind.U1:
                                return MarshallerKind.AnsiChar;

                            case NativeTypeKind.I2:
                            case NativeTypeKind.U2:
                                return MarshallerKind.UnicodeChar;

                            case NativeTypeKind.Default:
                                if (isAnsi)
                                    return MarshallerKind.AnsiChar;
                                else
                                    return MarshallerKind.UnicodeChar;
                            default:
                                return MarshallerKind.Invalid;
                        }

                    case TypeFlags.SByte:
                    case TypeFlags.Byte:
                        if (nativeType == NativeTypeKind.I1 || nativeType == NativeTypeKind.U1 || nativeType == NativeTypeKind.Default)
                            return MarshallerKind.BlittableValue;
                        else
                            return MarshallerKind.Invalid;

                    case TypeFlags.Int16:
                    case TypeFlags.UInt16:
                        if (nativeType == NativeTypeKind.I2 || nativeType == NativeTypeKind.U2 || nativeType == NativeTypeKind.Default)
                            return MarshallerKind.BlittableValue;
                        else
                            return MarshallerKind.Invalid;

                    case TypeFlags.Int32:
                    case TypeFlags.UInt32:
                        if (nativeType == NativeTypeKind.I4 || nativeType == NativeTypeKind.U4 || nativeType == NativeTypeKind.Default)
                            return MarshallerKind.BlittableValue;
                        else
                            return MarshallerKind.Invalid;

                    case TypeFlags.Int64:
                    case TypeFlags.UInt64:
                        if (nativeType == NativeTypeKind.I8 || nativeType == NativeTypeKind.U8 || nativeType == NativeTypeKind.Default)
                            return MarshallerKind.BlittableValue;
                        else
                            return MarshallerKind.Invalid;

                    case TypeFlags.IntPtr:
                    case TypeFlags.UIntPtr:
                        if (nativeType == NativeTypeKind.SysInt || nativeType == NativeTypeKind.SysUInt || nativeType == NativeTypeKind.Default)
                            return MarshallerKind.BlittableValue;
                        else
                            return MarshallerKind.Invalid;

                    case TypeFlags.Single:
                        if (nativeType == NativeTypeKind.R4 || nativeType == NativeTypeKind.Default)
                            return MarshallerKind.BlittableValue;
                        else
                            return MarshallerKind.Invalid;

                    case TypeFlags.Double:
                        if (nativeType == NativeTypeKind.R8 || nativeType == NativeTypeKind.Default)
                            return MarshallerKind.BlittableValue;
                        else
                            return MarshallerKind.Invalid;

                    default:
                        return MarshallerKind.Invalid;
                }
            }
            else if (type.IsValueType)
            {
                if (type.IsEnum)
                    return MarshallerKind.Enum;

                if (InteropTypes.IsSystemDateTime(context, type))
                {
                    if (nativeType == NativeTypeKind.Default ||
                        nativeType == NativeTypeKind.Struct)
                        return MarshallerKind.OleDateTime;
                    else
                        return MarshallerKind.Invalid;
                }
                else if (InteropTypes.IsHandleRef(context, type))
                {
                    if (nativeType == NativeTypeKind.Default)
                        return MarshallerKind.HandleRef;
                    else
                        return MarshallerKind.Invalid;
                }
                else if (InteropTypes.IsSystemDecimal(context, type))
                {
                    if (nativeType == NativeTypeKind.Struct || nativeType == NativeTypeKind.Default)
                        return MarshallerKind.Decimal;
                    else if (nativeType == NativeTypeKind.LPStruct && !isField)
                        return MarshallerKind.BlittableStructPtr;
                    else if (nativeType == NativeTypeKind.Currency)
                        return MarshallerKind.OleCurrency;
                    else
                        return MarshallerKind.Invalid;
                }
                else if (InteropTypes.IsSystemGuid(context, type))
                {
                    if (nativeType == NativeTypeKind.Struct || nativeType == NativeTypeKind.Default)
                        return MarshallerKind.BlittableStruct;
                    else if (nativeType == NativeTypeKind.LPStruct && !isField)
                        return MarshallerKind.BlittableStructPtr;
                    else
                        return MarshallerKind.Invalid;
                }
                else if (InteropTypes.IsSystemArgIterator(context, type))
                {
                    // Don't want to fall through to the blittable/haslayout case
                    return MarshallerKind.Invalid;
                }

                bool isBlittable = MarshalUtils.IsBlittableType(type);

                // Blittable generics are allowed to be marshalled with the following exceptions:
                // * ByReference<T>: This represents an interior pointer and is not actually blittable
                // * Nullable<T>: We don't want to be locked into the default behavior as we may want special handling later
                // * Vector64<T>: Represents the __m64 ABI primitive which requires currently unimplemented handling
                // * Vector128<T>: Represents the __m128 ABI primitive which requires currently unimplemented handling
                // * Vector256<T>: Represents the __m256 ABI primitive which requires currently unimplemented handling
                // * Vector<T>: Has a variable size (either __m128 or __m256) and isn't readily usable for interop scenarios
                // We can't block these types for field scenarios for back-compat reasons.

                if (type.HasInstantiation && !isField && (!isBlittable
                    || InteropTypes.IsSystemByReference(context, type)
                    || InteropTypes.IsSystemSpan(context, type)
                    || InteropTypes.IsSystemReadOnlySpan(context, type)
                    || InteropTypes.IsSystemNullable(context, type)
                    || InteropTypes.IsSystemRuntimeIntrinsicsVector64T(context, type)
                    || InteropTypes.IsSystemRuntimeIntrinsicsVector128T(context, type)
                    || InteropTypes.IsSystemRuntimeIntrinsicsVector256T(context, type)
                    || InteropTypes.IsSystemNumericsVectorT(context, type)))
                {
                    // Generic types cannot be marshaled.
                    return MarshallerKind.Invalid;
                }

                if (isBlittable)
                {
                    if (nativeType != NativeTypeKind.Default && nativeType != NativeTypeKind.Struct)
                        return MarshallerKind.Invalid;

                    return MarshallerKind.BlittableStruct;
                }
                else if (((MetadataType)type).HasLayout())
                {
                    if (nativeType != NativeTypeKind.Default && nativeType != NativeTypeKind.Struct)
                        return MarshallerKind.Invalid;

                    return MarshallerKind.Struct;
                }
                else
                {
                    return MarshallerKind.Invalid;
                }
            }
            else if (type.IsSzArray)
            {
#if READYTORUN
                // We don't want the additional test/maintenance cost of this in R2R.
                if (isByRef)
                    return MarshallerKind.Invalid;
#else
                _ = isByRef;
#endif

                if (nativeType == NativeTypeKind.Default)
                    nativeType = NativeTypeKind.Array;

                switch (nativeType)
                {
                    case NativeTypeKind.Array:
                        {
                            if (isField)
                                return MarshallerKind.Invalid;

                            var arrayType = (ArrayType)type;

                            elementMarshallerKind = GetArrayElementMarshallerKind(
                                arrayType,
                                marshalAs,
                                isAnsi);

                            // If element is invalid type, the array itself is invalid
                            if (elementMarshallerKind == MarshallerKind.Invalid)
                                return MarshallerKind.Invalid;

                            if (elementMarshallerKind == MarshallerKind.AnsiChar)
                                return MarshallerKind.AnsiCharArray;
                            else if (elementMarshallerKind == MarshallerKind.UnicodeChar    // Arrays of unicode char should be marshalled as blittable arrays
                                || elementMarshallerKind == MarshallerKind.Enum
                                || elementMarshallerKind == MarshallerKind.BlittableValue)
                                return MarshallerKind.BlittableArray;
                            else
                                return MarshallerKind.Array;
                        }

                    case NativeTypeKind.ByValArray:         // fix sized array
                        {
                            var arrayType = (ArrayType)type;
                            elementMarshallerKind = GetArrayElementMarshallerKind(
                                arrayType,
                                marshalAs,
                                isAnsi);

                            // If element is invalid type, the array itself is invalid
                            if (elementMarshallerKind == MarshallerKind.Invalid)
                                return MarshallerKind.Invalid;

                            if (elementMarshallerKind == MarshallerKind.AnsiChar)
                                return MarshallerKind.ByValAnsiCharArray;
                            else
                                return MarshallerKind.ByValArray;
                        }

                    default:
                        return MarshallerKind.Invalid;
                }
            }
            else if (type.IsPointer)
            {
                if (nativeType == NativeTypeKind.Default)
                {
                    var pointedAtType = type.GetParameterType();
                    if (!pointedAtType.IsPrimitive && !type.IsEnum && marshallerType != MarshallerType.Field
                        && HasCopyConstructorCustomModifier(parameterIndex, customModifierData))
                    {
                        return MarshallerKind.BlittableValueClassWithCopyCtor;
                    }
                    return MarshallerKind.BlittableValue;
                }
                else
                    return MarshallerKind.Invalid;
            }
            else if (type.IsFunctionPointer)
            {
                if (nativeType == NativeTypeKind.Func || nativeType == NativeTypeKind.Default)
                    return MarshallerKind.BlittableValue;
                else
                    return MarshallerKind.Invalid;
            }
            else if (type.IsDelegate)
            {
                if (type.HasInstantiation)
                {
                    // Generic types cannot be marshaled.
                    return MarshallerKind.Invalid;
                }

                if (nativeType == NativeTypeKind.Default || nativeType == NativeTypeKind.Func)
                    return MarshallerKind.FunctionPointer;
                else
                    return MarshallerKind.Invalid;
            }
            else if (type.IsString)
            {
                switch (nativeType)
                {
                    case NativeTypeKind.LPWStr:
                        return MarshallerKind.UnicodeString;

                    case NativeTypeKind.LPStr:
                        return MarshallerKind.AnsiString;

                    case NativeTypeKind.LPUTF8Str:
                        return MarshallerKind.UTF8String;

                    case NativeTypeKind.LPTStr:
                        return MarshallerKind.UnicodeString;

                    case NativeTypeKind.ByValTStr:
                        if (isAnsi)
                        {
                            elementMarshallerKind = MarshallerKind.AnsiChar;
                            return MarshallerKind.ByValAnsiString;
                        }
                        else
                        {
                            elementMarshallerKind = MarshallerKind.UnicodeChar;
                            return MarshallerKind.ByValUnicodeString;
                        }

                    case NativeTypeKind.TBStr:
                    case NativeTypeKind.BStr:
                        return MarshallerKind.BSTRString;

                    case NativeTypeKind.AnsiBStr:
                        return MarshallerKind.AnsiBSTRString;

                    case NativeTypeKind.Default:
                        if (isAnsi)
                            return MarshallerKind.AnsiString;
                        else
                            return MarshallerKind.UnicodeString;

                    default:
                        return MarshallerKind.Invalid;
                }
            }
            else if (type.IsObject)
            {
                if (nativeType == NativeTypeKind.AsAny)
                    return isAnsi ? MarshallerKind.AsAnyA : MarshallerKind.AsAnyW;
                else if ((isField && nativeType == NativeTypeKind.Default)
                    || nativeType == NativeTypeKind.Intf
                    || nativeType == NativeTypeKind.IUnknown)
                    return MarshallerKind.ComInterface;
                else
                    return MarshallerKind.Variant;
            }
            else if (InteropTypes.IsStringBuilder(context, type))
            {
                switch (nativeType)
                {
                    case NativeTypeKind.Default:
                        if (isAnsi)
                        {
                            return MarshallerKind.AnsiStringBuilder;
                        }
                        else
                        {
                            return MarshallerKind.UnicodeStringBuilder;
                        }

                    case NativeTypeKind.LPStr:
                        return MarshallerKind.AnsiStringBuilder;

                    case NativeTypeKind.LPWStr:
                        return MarshallerKind.UnicodeStringBuilder;
                    default:
                        return MarshallerKind.Invalid;
                }
            }
            else if (InteropTypes.IsSafeHandle(context, type))
            {
                if (nativeType == NativeTypeKind.Default)
                    return MarshallerKind.SafeHandle;
                else
                    return MarshallerKind.Invalid;
            }
            else if (InteropTypes.IsCriticalHandle(context, type))
            {
                if (nativeType == NativeTypeKind.Default)
                    return MarshallerKind.CriticalHandle;
                else
                    return MarshallerKind.Invalid;
            }
            else if (type is MetadataType mdType && mdType.HasLayout())
            {
                if (type.HasInstantiation)
                {
                    // Generic types cannot be marshaled.
                    return MarshallerKind.Invalid;
                }

                if (!isField && nativeType == NativeTypeKind.Default || nativeType == NativeTypeKind.LPStruct)
                    return MarshallerKind.LayoutClassPtr;
                else if (isField && (nativeType == NativeTypeKind.Default || nativeType == NativeTypeKind.Struct))
                    return MarshallerKind.LayoutClass;
                else
                    return MarshallerKind.Invalid;
            }
            else if (type.IsInterface)
            {
                return MarshallerKind.ComInterface;
            }
            else
                return MarshallerKind.Invalid;
        }

        private static MarshallerKind GetArrayElementMarshallerKind(
                   ArrayType arrayType,
                   MarshalAsDescriptor marshalAs,
                   bool isAnsi)
        {
            TypeDesc elementType = arrayType.ElementType;
            NativeTypeKind nativeType = NativeTypeKind.Default;
            TypeSystemContext context = arrayType.Context;

            if (marshalAs != null)
                nativeType = (NativeTypeKind)marshalAs.ArraySubType;

            if (elementType.IsPrimitive)
            {
                switch (elementType.Category)
                {
                    case TypeFlags.Char:
                        switch (nativeType)
                        {
                            case NativeTypeKind.I1:
                            case NativeTypeKind.U1:
                                return MarshallerKind.AnsiChar;
                            case NativeTypeKind.I2:
                            case NativeTypeKind.U2:
                                return MarshallerKind.UnicodeChar;
                            default:
                                if (isAnsi)
                                    return MarshallerKind.AnsiChar;
                                else
                                    return MarshallerKind.UnicodeChar;
                        }

                    case TypeFlags.Boolean:
                        switch (nativeType)
                        {
                            case NativeTypeKind.Boolean:
                                return MarshallerKind.Bool;
                            case NativeTypeKind.I1:
                            case NativeTypeKind.U1:
                                return MarshallerKind.CBool;
                            case NativeTypeKind.Default:
                            default:
                                return MarshallerKind.Bool;
                        }
                    case TypeFlags.IntPtr:
                    case TypeFlags.UIntPtr:
                        return MarshallerKind.BlittableValue;

                    case TypeFlags.Void:
                        return MarshallerKind.Invalid;

                    case TypeFlags.SByte:
                    case TypeFlags.Int16:
                    case TypeFlags.Int32:
                    case TypeFlags.Int64:
                    case TypeFlags.Byte:
                    case TypeFlags.UInt16:
                    case TypeFlags.UInt32:
                    case TypeFlags.UInt64:
                    case TypeFlags.Single:
                    case TypeFlags.Double:
                        return MarshallerKind.BlittableValue;
                    default:
                        return MarshallerKind.Invalid;
                }
            }
            else if (elementType.IsValueType)
            {
                if (elementType.IsEnum)
                    return MarshallerKind.Enum;

                if (InteropTypes.IsSystemDecimal(context, elementType))
                {
                    switch (nativeType)
                    {
                        case NativeTypeKind.Default:
                        case NativeTypeKind.Struct:
                            return MarshallerKind.Decimal;

                        case NativeTypeKind.LPStruct:
                            return MarshallerKind.BlittableStructPtr;

                        default:
                            return MarshallerKind.Invalid;
                    }
                }
                else if (InteropTypes.IsSystemGuid(context, elementType))
                {
                    switch (nativeType)
                    {
                        case NativeTypeKind.Default:
                        case NativeTypeKind.Struct:
                            return MarshallerKind.BlittableValue;

                        case NativeTypeKind.LPStruct:
                            return MarshallerKind.BlittableStructPtr;

                        default:
                            return MarshallerKind.Invalid;
                    }
                }
                else if (InteropTypes.IsSystemDateTime(context, elementType))
                {
                    if (nativeType == NativeTypeKind.Default ||
                        nativeType == NativeTypeKind.Struct)
                    {
                        return MarshallerKind.OleDateTime;
                    }
                    else
                    {
                        return MarshallerKind.Invalid;
                    }
                }
                else if (InteropTypes.IsHandleRef(context, elementType))
                {
                    if (nativeType == NativeTypeKind.Default)
                        return MarshallerKind.HandleRef;
                    else
                        return MarshallerKind.Invalid;
                }
                else
                {
                    if (MarshalUtils.IsBlittableType(elementType))
                    {
                        switch (nativeType)
                        {
                            case NativeTypeKind.Default:
                            case NativeTypeKind.Struct:
                                return MarshallerKind.BlittableStruct;

                            default:
                                return MarshallerKind.Invalid;
                        }
                    }
                    else
                    {
                        // TODO: Differentiate between struct and Union, we only need to support struct not union here
                        return MarshallerKind.Struct;
                    }
                }
            }
            else if (elementType.IsPointer || elementType.IsFunctionPointer)
            {
                if (nativeType == NativeTypeKind.Default)
                    return MarshallerKind.BlittableValue;
                else
                    return MarshallerKind.Invalid;
            }
            else if (elementType.IsString)
            {
                switch (nativeType)
                {
                    case NativeTypeKind.Default:
                        if (isAnsi)
                            return MarshallerKind.AnsiString;
                        else
                            return MarshallerKind.UnicodeString;
                    case NativeTypeKind.LPStr:
                        return MarshallerKind.AnsiString;
                    case NativeTypeKind.LPWStr:
                        return MarshallerKind.UnicodeString;
                    case NativeTypeKind.LPUTF8Str:
                        return MarshallerKind.UTF8String;
                    case NativeTypeKind.BStr:
                    case NativeTypeKind.TBStr:
                        return MarshallerKind.BSTRString;
                    case NativeTypeKind.AnsiBStr:
                        return MarshallerKind.AnsiBSTRString;
                    default:
                        return MarshallerKind.Invalid;
                }
            }
            // else if (elementType.IsObject)
            // {
            //    if (nativeType == NativeTypeKind.Invalid)
            //        return MarshallerKind.Variant;
            //    else
            //        return MarshallerKind.Invalid;
            // }
            else
            {
                return MarshallerKind.Invalid;
            }
        }

        internal static bool ShouldCheckForPendingException(TargetDetails target, PInvokeMetadata metadata)
        {
            if (!target.IsOSX)
                return false;

            const string ObjectiveCLibrary = "/usr/lib/libobjc.dylib";
            const string ObjectiveCMsgSend = "objc_msgSend";

            // This is for the objc_msgSend suite of functions.
            //   objc_msgSend
            //   objc_msgSend_fpret
            //   objc_msgSend_stret
            //   objc_msgSendSuper
            //   objc_msgSendSuper_stret
            return metadata.Module.Equals(ObjectiveCLibrary)
                && metadata.Name.StartsWith(ObjectiveCMsgSend);
        }
    }
}
