// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;

using MethodTable = Internal.Runtime.MethodTable;
using EETypeElementType = Internal.Runtime.EETypeElementType;

namespace System
{
    internal static unsafe class InvokeUtils
    {
        //
        // Various reflection scenarios (Array.SetValue(), reflection Invoke, delegate DynamicInvoke and FieldInfo.Set()) perform
        // automatic conveniences such as automatically widening primitive types to fit the destination type.
        //
        // This method attempts to collect as much of that logic as possible in place. (This may not be completely possible
        // as the desktop CLR is not particularly consistent across all these scenarios either.)
        //
        // The transforms supported are:
        //
        //    Value-preserving widenings of primitive integrals and floats.
        //    Enums can be converted to the same or wider underlying primitive.
        //    Primitives can be converted to an enum with the same or wider underlying primitive.
        //
        //    null converted to default(T) (this is important when T is a valuetype.)
        //
        // There is also another transform of T -> Nullable<T>. This method acknowledges that rule but does not actually transform the T.
        // Rather, the transformation happens naturally when the caller unboxes the value to its final destination.
        //

        // This option tweaks the coercion rules to match classic inconsistencies.
        internal enum CheckArgumentSemantics
        {
            ArraySet,            // Throws InvalidCastException
            DynamicInvoke,       // Throws ArgumentException
            SetFieldDirect,      // Throws ArgumentException - other than that, like DynamicInvoke except that enums and integers cannot be intermingled, and null cannot substitute for default(valuetype).
        }

        internal static object? CheckArgument(object? srcObject, MethodTable* dstEEType, CheckArgumentSemantics semantics, BinderBundle? binderBundle)
        {
            // Methods with ByRefLike types in signatures should be filtered out earlier
            Debug.Assert(!dstEEType->IsByRefLike);

            if (srcObject == null)
            {
                // null -> default(T)
                if (dstEEType->IsPointer)
                {
                    return default(IntPtr);
                }
                else if (dstEEType->IsValueType && !dstEEType->IsNullable)
                {
                    if (semantics == CheckArgumentSemantics.SetFieldDirect)
                        throw CreateChangeTypeException(MethodTable.Of<object>(), dstEEType, semantics);
                    return Runtime.RuntimeImports.RhNewObject(dstEEType);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                MethodTable* srcEEType = srcObject.GetMethodTable();

                if (srcEEType == dstEEType ||
                    RuntimeImports.AreTypesAssignable(srcEEType, dstEEType) ||
                    (dstEEType->IsInterface && srcObject is Runtime.InteropServices.IDynamicInterfaceCastable castable
                        && castable.IsInterfaceImplemented(new RuntimeTypeHandle(dstEEType), throwIfNotImplemented: false)))
                {
                    return srcObject;
                }

                return CheckArgumentConversions(srcObject, dstEEType, semantics, binderBundle);
            }
        }

        internal static object? CheckArgumentConversions(object srcObject, MethodTable* dstEEType, CheckArgumentSemantics semantics, BinderBundle? binderBundle)
        {
            object? dstObject;
            Exception exception = ConvertOrWidenPrimitivesEnumsAndPointersIfPossible(srcObject, dstEEType, semantics, out dstObject);
            if (exception == null)
                return dstObject;

            if (binderBundle == null)
                throw exception;

            // Our normal coercion rules could not convert the passed in argument but we were supplied a custom binder. See if it can do it.
            Type exactDstType = Type.GetTypeFromHandle(new RuntimeTypeHandle(dstEEType))!;

            srcObject = binderBundle.ChangeType(srcObject, exactDstType);

            // For compat with desktop, the result of the binder call gets processed through the default rules again.
            return CheckArgument(srcObject, dstEEType, semantics, binderBundle: null);
        }

        // Special coersion rules for primitives, enums and pointer.
        private static Exception ConvertOrWidenPrimitivesEnumsAndPointersIfPossible(object srcObject, MethodTable* dstEEType, CheckArgumentSemantics semantics, out object? dstObject)
        {
            MethodTable* srcEEType = srcObject.GetMethodTable();

            if (semantics == CheckArgumentSemantics.SetFieldDirect && (srcEEType->IsEnum || dstEEType->IsEnum))
            {
                dstObject = null;
                return CreateChangeTypeException(srcEEType, dstEEType, semantics);
            }

            if (dstEEType->IsPointer || dstEEType->IsFunctionPointer)
            {
                Exception exception = ConvertPointerIfPossible(srcObject, dstEEType, semantics, out object dstPtr);
                if (exception != null)
                {
                    dstObject = null;
                    return exception;
                }
                dstObject = dstPtr;
                return null;
            }

            if (!(srcEEType->IsPrimitive && dstEEType->IsPrimitive))
            {
                dstObject = null;
                return CreateChangeTypeException(srcEEType, dstEEType, semantics);
            }

            EETypeElementType dstElementType = dstEEType->ElementType;
            if (!CanPrimitiveWiden(dstElementType, srcEEType->ElementType))
            {
                dstObject = null;
                return CreateChangeTypeArgumentException(srcEEType, dstEEType);
            }

            EETypeElementType srcElementType = srcEEType->ElementType;

            // This switch supports the conversions present in the table below, in 'CanPrimitiveWiden'
            switch (dstElementType)
            {
                case EETypeElementType.Boolean:

                    // Can only be bool here
                    dstObject = srcObject;
                    break;

                case EETypeElementType.Char:
                    char charValue;
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            charValue = (char)srcObject;
                            break;
                        case EETypeElementType.Byte:
                            charValue = (char)(byte)srcObject;
                            break;
                        case EETypeElementType.UInt16:
                            charValue = (char)(ushort)srcObject;
                            break;
                        default:
                            goto Failure;
                    }
                    dstObject = dstEEType->IsEnum ? Enum.ToObject(dstEEType, charValue) : charValue;
                    break;

                case EETypeElementType.SByte:

                    // Can only be sbyte here
                    sbyte sbyteValue = (sbyte)srcObject;
                    dstObject = dstEEType->IsEnum ? Enum.ToObject(dstEEType, sbyteValue) : sbyteValue;
                    break;

                case EETypeElementType.Byte:

                    // Can only be byte here
                    byte byteValue = (byte)srcObject;
                    dstObject = dstEEType->IsEnum ? Enum.ToObject(dstEEType, byteValue) : byteValue;
                    break;

                case EETypeElementType.Int16:
                    short shortValue;
                    switch (srcElementType)
                    {
                        case EETypeElementType.SByte:
                            shortValue = (short)(sbyte)srcObject;
                            break;
                        case EETypeElementType.Byte:
                            shortValue = (short)(byte)srcObject;
                            break;
                        case EETypeElementType.Int16:
                            shortValue = (short)srcObject;
                            break;
                        default:
                            goto Failure;
                    }
                    dstObject = dstEEType->IsEnum ? Enum.ToObject(dstEEType, shortValue) : shortValue;
                    break;

                case EETypeElementType.UInt16:
                    ushort ushortValue;
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            ushortValue = (ushort)(char)srcObject;
                            break;
                        case EETypeElementType.Byte:
                            ushortValue = (ushort)(byte)srcObject;
                            break;
                        case EETypeElementType.UInt16:
                            ushortValue = (ushort)srcObject;
                            break;
                        default:
                            goto Failure;
                    }
                    dstObject = dstEEType->IsEnum ? Enum.ToObject(dstEEType, ushortValue) : ushortValue;
                    break;

                case EETypeElementType.Int32:
                    int intValue;
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            intValue = (int)(char)srcObject;
                            break;
                        case EETypeElementType.SByte:
                            intValue = (int)(sbyte)srcObject;
                            break;
                        case EETypeElementType.Byte:
                            intValue = (int)(byte)srcObject;
                            break;
                        case EETypeElementType.Int16:
                            intValue = (int)(short)srcObject;
                            break;
                        case EETypeElementType.UInt16:
                            intValue = (int)(ushort)srcObject;
                            break;
                        case EETypeElementType.Int32:
                            intValue = (int)srcObject;
                            break;
                        default:
                            goto Failure;
                    }
                    dstObject = dstEEType->IsEnum ? Enum.ToObject(dstEEType, intValue) : intValue;
                    break;

                case EETypeElementType.UInt32:
                    uint uintValue;
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            uintValue = (uint)(char)srcObject;
                            break;
                        case EETypeElementType.Byte:
                            uintValue = (uint)(byte)srcObject;
                            break;
                        case EETypeElementType.UInt16:
                            uintValue = (uint)(ushort)srcObject;
                            break;
                        case EETypeElementType.UInt32:
                            uintValue = (uint)srcObject;
                            break;
                        default:
                            goto Failure;
                    }
                    dstObject = dstEEType->IsEnum ? Enum.ToObject(dstEEType, uintValue) : uintValue;
                    break;

                case EETypeElementType.Int64:
                    long longValue;
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            longValue = (long)(char)srcObject;
                            break;
                        case EETypeElementType.SByte:
                            longValue = (long)(sbyte)srcObject;
                            break;
                        case EETypeElementType.Byte:
                            longValue = (long)(byte)srcObject;
                            break;
                        case EETypeElementType.Int16:
                            longValue = (long)(short)srcObject;
                            break;
                        case EETypeElementType.UInt16:
                            longValue = (long)(ushort)srcObject;
                            break;
                        case EETypeElementType.Int32:
                            longValue = (long)(int)srcObject;
                            break;
                        case EETypeElementType.UInt32:
                            longValue = (long)(uint)srcObject;
                            break;
                        case EETypeElementType.Int64:
                            longValue = (long)srcObject;
                            break;
                        default:
                            goto Failure;
                    }
                    dstObject = dstEEType->IsEnum ? Enum.ToObject(dstEEType, longValue) : longValue;
                    break;

                case EETypeElementType.UInt64:
                    ulong ulongValue;
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            ulongValue = (ulong)(char)srcObject;
                            break;
                        case EETypeElementType.Byte:
                            ulongValue = (ulong)(byte)srcObject;
                            break;
                        case EETypeElementType.UInt16:
                            ulongValue = (ulong)(ushort)srcObject;
                            break;
                        case EETypeElementType.UInt32:
                            ulongValue = (ulong)(uint)srcObject;
                            break;
                        case EETypeElementType.UInt64:
                            ulongValue = (ulong)srcObject;
                            break;
                        default:
                            goto Failure;
                    }
                    dstObject = dstEEType->IsEnum ? Enum.ToObject(dstEEType, (long)ulongValue) : ulongValue;
                    break;

                case EETypeElementType.Single:
                    float floatValue;
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            floatValue = (float)(char)srcObject;
                            break;
                        case EETypeElementType.SByte:
                            floatValue = (float)(sbyte)srcObject;
                            break;
                        case EETypeElementType.Byte:
                            floatValue = (float)(byte)srcObject;
                            break;
                        case EETypeElementType.Int16:
                            floatValue = (float)(short)srcObject;
                            break;
                        case EETypeElementType.UInt16:
                            floatValue = (float)(ushort)srcObject;
                            break;
                        case EETypeElementType.Int32:
                            floatValue = (float)(int)srcObject;
                            break;
                        case EETypeElementType.UInt32:
                            floatValue = (float)(uint)srcObject;
                            break;
                        case EETypeElementType.Int64:
                            floatValue = (float)(long)srcObject;
                            break;
                        case EETypeElementType.UInt64:
                            floatValue = (float)(ulong)srcObject;
                            break;
                        case EETypeElementType.Single:
                            floatValue = (float)srcObject;
                            break;
                        default:
                            goto Failure;
                    }
                    dstObject = floatValue;
                    break;

                case EETypeElementType.Double:
                    double doubleValue;
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            doubleValue = (double)(char)srcObject;
                            break;
                        case EETypeElementType.SByte:
                            doubleValue = (double)(sbyte)srcObject;
                            break;
                        case EETypeElementType.Byte:
                            doubleValue = (double)(byte)srcObject;
                            break;
                        case EETypeElementType.Int16:
                            doubleValue = (double)(short)srcObject;
                            break;
                        case EETypeElementType.UInt16:
                            doubleValue = (double)(ushort)srcObject;
                            break;
                        case EETypeElementType.Int32:
                            doubleValue = (double)(int)srcObject;
                            break;
                        case EETypeElementType.UInt32:
                            doubleValue = (double)(uint)srcObject;
                            break;
                        case EETypeElementType.Int64:
                            doubleValue = (double)(long)srcObject;
                            break;
                        case EETypeElementType.UInt64:
                            doubleValue = (double)(ulong)srcObject;
                            break;
                        case EETypeElementType.Single:
                            doubleValue = (double)(float)srcObject;
                            break;
                        case EETypeElementType.Double:
                            doubleValue = (double)srcObject;
                            break;
                        default:
                            goto Failure;
                    }
                    dstObject = doubleValue;
                    break;

                default:
                    goto Failure;
            }

            Debug.Assert(dstObject.GetMethodTable() == dstEEType);
            return null;

            Failure:
            Debug.Fail("Unexpected CorElementType: " + dstElementType + ": Not a valid widening target.");
            dstObject = null;
            return null; // This code can never be reached
        }

        private static bool CanPrimitiveWiden(EETypeElementType destType, EETypeElementType srcType)
        {
            Debug.Assert(destType is < EETypeElementType.ValueType and >= EETypeElementType.Boolean);
            Debug.Assert(srcType is < EETypeElementType.ValueType and >= EETypeElementType.Boolean);

            ReadOnlySpan<ushort> primitiveAttributes = [
                0x0000, // Unknown
                0x0000, // Void
                0x0004, // Boolean (W = BOOL)
                0xCf88, // Char (W = U2, CHAR, I4, U4, I8, U8, R4, R8)
                0xC550, // SByte (W = I1, I2, I4, I8, R4, R8)
                0xCFE8, // Byte (W = CHAR, U1, I2, U2, I4, U4, I8, U8, R4, R8)
                0xC540, // Int16 (W = I2, I4, I8, R4, R8)
                0xCF88, // UInt16 (W = U2, CHAR, I4, U4, I8, U8, R4, R8)
                0xC500, // Int32 (W = I4, I8, R4, R8)
                0xCE0B, // UInt32 (W = U4, I8, U8, R4, R8)
                0xC400, // Int64 (W = I8, R4, R8)
                0xC800, // UInt64 (W = U8, R4, R8)
                0x0000, // IntPtr
                0x0000, // UIntPtr
                0xC000, // Single (W = R4, R8)
                0x8000, // Double (W = R8)
            ];

            ushort mask = (ushort)(1 << (byte)destType);
            return (primitiveAttributes[(int)srcType & 0xF] & mask) != 0;
        }

        private static Exception ConvertPointerIfPossible(object srcObject, MethodTable* dstEEType, CheckArgumentSemantics semantics, out object dstPtr)
        {
            if (srcObject is IntPtr or UIntPtr)
            {
                dstPtr = srcObject;
                return null;
            }

            if (srcObject is Pointer srcPointer)
            {
                if (dstEEType == typeof(void*).TypeHandle.ToMethodTable() || RuntimeImports.AreTypesAssignable(pSourceType: srcPointer.GetPointerType().TypeHandle.ToMethodTable(), pTargetType: dstEEType))
                {
                    dstPtr = srcPointer.GetPointerValue();
                    return null;
                }
            }

            dstPtr = null;
            return CreateChangeTypeException(srcObject.GetMethodTable(), dstEEType, semantics);
        }

        private static Exception CreateChangeTypeException(MethodTable* srcEEType, MethodTable* dstEEType, CheckArgumentSemantics semantics)
        {
            switch (semantics)
            {
                case CheckArgumentSemantics.DynamicInvoke:
                case CheckArgumentSemantics.SetFieldDirect:
                    return CreateChangeTypeArgumentException(srcEEType, dstEEType);
                case CheckArgumentSemantics.ArraySet:
                    return CreateChangeTypeInvalidCastException();
                default:
                    Debug.Fail("Unexpected CheckArgumentSemantics value: " + semantics);
                    throw new InvalidOperationException();
            }
        }

        internal static ArgumentException CreateChangeTypeArgumentException(MethodTable* srcEEType, MethodTable* dstEEType, bool destinationIsByRef = false)
            => CreateChangeTypeArgumentException(srcEEType, Type.GetTypeFromHandle(new RuntimeTypeHandle(dstEEType)), destinationIsByRef);

        internal static ArgumentException CreateChangeTypeArgumentException(MethodTable* srcEEType, Type dstType, bool destinationIsByRef = false)
        {
            object? destinationTypeName = dstType;
            if (destinationIsByRef)
                destinationTypeName += "&";
            return new ArgumentException(SR.Format(SR.Arg_ObjObjEx, Type.GetTypeFromHandle(new RuntimeTypeHandle(srcEEType)), destinationTypeName));
        }

        private static InvalidCastException CreateChangeTypeInvalidCastException()
        {
            return new InvalidCastException(SR.InvalidCast_StoreArrayElement);
        }
    }
}
