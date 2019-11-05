// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            NativeTypeKind nativeType = NativeTypeKind.Invalid;
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

                case MarshallerKind.UnicodeString:
                case MarshallerKind.UnicodeStringBuilder:
                    return context.GetWellKnownType(WellKnownType.Char).MakePointerType();

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

                case MarshallerKind.Unknown:
                default:
                    throw new NotSupportedException();
            }
        }

        internal static MarshallerKind GetMarshallerKind(
             TypeDesc type,
             MarshalAsDescriptor marshalAs,
             bool isReturn,
             bool isAnsi,
             MarshallerType marshallerType,
             out MarshallerKind elementMarshallerKind)
        {
            if (type.IsByRef)
            {
                type = type.GetParameterType();
            }
            TypeSystemContext context = type.Context;
            NativeTypeKind nativeType = NativeTypeKind.Invalid;
            bool isField = marshallerType == MarshallerType.Field;

            if (marshalAs != null)
                nativeType = (NativeTypeKind)marshalAs.Type;


            elementMarshallerKind = MarshallerKind.Invalid;

            //
            // Determine MarshalerKind
            //
            // This mostly resembles desktop CLR and .NET Native code as we need to match their behavior
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
                            case NativeTypeKind.Invalid:
                            case NativeTypeKind.Boolean:
                                return MarshallerKind.Bool;

                            case NativeTypeKind.U1:
                            case NativeTypeKind.I1:
                                return MarshallerKind.CBool;

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

                            case NativeTypeKind.Invalid:
                                if (isAnsi)
                                    return MarshallerKind.AnsiChar;
                                else
                                    return MarshallerKind.UnicodeChar;
                            default:
                                return MarshallerKind.Invalid;
                        }

                    case TypeFlags.SByte:
                    case TypeFlags.Byte:
                        if (nativeType == NativeTypeKind.I1 || nativeType == NativeTypeKind.U1 || nativeType == NativeTypeKind.Invalid)
                            return MarshallerKind.BlittableValue;
                        else
                            return MarshallerKind.Invalid;

                    case TypeFlags.Int16:
                    case TypeFlags.UInt16:
                        if (nativeType == NativeTypeKind.I2 || nativeType == NativeTypeKind.U2 || nativeType == NativeTypeKind.Invalid)
                            return MarshallerKind.BlittableValue;
                        else
                            return MarshallerKind.Invalid;

                    case TypeFlags.Int32:
                    case TypeFlags.UInt32:
                        if (nativeType == NativeTypeKind.I4 || nativeType == NativeTypeKind.U4 || nativeType == NativeTypeKind.Invalid)
                            return MarshallerKind.BlittableValue;
                        else
                            return MarshallerKind.Invalid;

                    case TypeFlags.Int64:
                    case TypeFlags.UInt64:
                        if (nativeType == NativeTypeKind.I8 || nativeType == NativeTypeKind.U8 || nativeType == NativeTypeKind.Invalid)
                            return MarshallerKind.BlittableValue;
                        else
                            return MarshallerKind.Invalid;

                    case TypeFlags.IntPtr:
                    case TypeFlags.UIntPtr:
                        if (nativeType == NativeTypeKind.Invalid)
                            return MarshallerKind.BlittableValue;
                        else
                            return MarshallerKind.Invalid;

                    case TypeFlags.Single:
                        if (nativeType == NativeTypeKind.R4 || nativeType == NativeTypeKind.Invalid)
                            return MarshallerKind.BlittableValue;
                        else
                            return MarshallerKind.Invalid;

                    case TypeFlags.Double:
                        if (nativeType == NativeTypeKind.R8 || nativeType == NativeTypeKind.Invalid)
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
                    if (nativeType == NativeTypeKind.Invalid ||
                        nativeType == NativeTypeKind.Struct)
                        return MarshallerKind.OleDateTime;
                    else
                        return MarshallerKind.Invalid;
                }
                else if (InteropTypes.IsHandleRef(context, type))
                {
                    if (nativeType == NativeTypeKind.Invalid)
                        return MarshallerKind.HandleRef;
                    else
                        return MarshallerKind.Invalid;
                }

                switch (nativeType)
                {
                    case NativeTypeKind.Invalid:
                    case NativeTypeKind.Struct:
                        if (InteropTypes.IsSystemDecimal(context, type))
                            return MarshallerKind.Decimal;
                        break;

                    case NativeTypeKind.LPStruct:
                        if (InteropTypes.IsSystemGuid(context, type) ||
                            InteropTypes.IsSystemDecimal(context, type))
                        {
                            if (isField || isReturn)
                                return MarshallerKind.Invalid;
                            else
                                return MarshallerKind.BlittableStructPtr;
                        }
                        break;

                    default:
                        return MarshallerKind.Invalid;
                }

                if (type is MetadataType)
                {
                    MetadataType metadataType = (MetadataType)type;
                    // the struct type need to be either sequential or explicit. If it is
                    // auto layout we will throw exception.
                    if (!metadataType.HasLayout())
                    {
                        throw new InvalidProgramException("The specified structure " + metadataType.Name + " has invalid StructLayout information. It must be either Sequential or Explicit.");
                    }
                }

                if (MarshalUtils.IsBlittableType(type))
                {
                    return MarshallerKind.BlittableStruct;
                }
                else
                {
                    return MarshallerKind.Struct;
                }
            }
            else if (type.IsSzArray)
            {
                if (nativeType == NativeTypeKind.Invalid)
                    nativeType = NativeTypeKind.Array;

                switch (nativeType)
                {
                    case NativeTypeKind.Array:
                        {
                            if (isField || isReturn)
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
            else if (type.IsPointer || type.IsFunctionPointer)
            {
                if (nativeType == NativeTypeKind.Invalid)
                    return MarshallerKind.BlittableValue;
                else
                    return MarshallerKind.Invalid;
            }
            else if (type.IsDelegate)
            {
                if (nativeType == NativeTypeKind.Invalid || nativeType == NativeTypeKind.Func)
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

                    case NativeTypeKind.Invalid:
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
                else
                    return MarshallerKind.Invalid;
            }
            else if (InteropTypes.IsStringBuilder(context, type))
            {
                switch (nativeType)
                {
                    case NativeTypeKind.Invalid:
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
                if (nativeType == NativeTypeKind.Invalid)
                    return MarshallerKind.SafeHandle;
                else
                    return MarshallerKind.Invalid;
            }
            else if (InteropTypes.IsCriticalHandle(context, type))
            {
                if (nativeType == NativeTypeKind.Invalid)
                    return MarshallerKind.CriticalHandle;
                else
                    return MarshallerKind.Invalid;
            }
            else if (type is MetadataType mdType && mdType.HasLayout())
            {
                if (!isField && nativeType == NativeTypeKind.Invalid || nativeType == NativeTypeKind.LPStruct)
                    return MarshallerKind.LayoutClassPtr;
                else if (isField && (nativeType == NativeTypeKind.Invalid || nativeType == NativeTypeKind.Struct))
                    return MarshallerKind.LayoutClass;
                else
                    return MarshallerKind.Invalid;
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
            NativeTypeKind nativeType = NativeTypeKind.Invalid;
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
                            case NativeTypeKind.Invalid:
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
                        case NativeTypeKind.Invalid:
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
                        case NativeTypeKind.Invalid:
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
                    if (nativeType == NativeTypeKind.Invalid ||
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
                    if (nativeType == NativeTypeKind.Invalid)
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
                            case NativeTypeKind.Invalid:
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
                if (nativeType == NativeTypeKind.Invalid)
                    return MarshallerKind.BlittableValue;
                else
                    return MarshallerKind.Invalid;
            }
            else if (elementType.IsString)
            {
                switch (nativeType)
                {
                    case NativeTypeKind.Invalid:
                        if (isAnsi)
                            return MarshallerKind.AnsiString;
                        else
                            return MarshallerKind.UnicodeString;
                    case NativeTypeKind.LPStr:
                        return MarshallerKind.AnsiString;
                    case NativeTypeKind.LPWStr:
                        return MarshallerKind.UnicodeString;
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
    }
}
