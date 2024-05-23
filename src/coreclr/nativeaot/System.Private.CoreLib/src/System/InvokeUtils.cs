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
            EETypeElementType srcElementType = srcEEType->ElementType;

            if (dstElementType == srcElementType)
            {
                // Rebox the value if the EETypeElementTypes match
                dstObject = RuntimeImports.RhBox(dstEEType, ref srcObject.GetRawData());
            }
            else
            {
                if (!CanPrimitiveWiden(dstElementType, srcElementType))
                {
                    dstObject = null;
                    return CreateChangeTypeArgumentException(srcEEType, dstEEType);
                }

                dstObject = RuntimeImports.RhNewObject(dstEEType);
                PrimitiveWiden(dstElementType, srcElementType, ref dstObject.GetRawData(), ref srcObject.GetRawData());
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Two callers, one of them is potentially perf sensitive
        internal static void PrimitiveWiden(EETypeElementType dstType, EETypeElementType srcType, ref byte dstValue, ref byte srcValue)
        {
            // Caller must check that the conversion is valid and the source/destination types are different
            Debug.Assert(CanPrimitiveWiden(dstType, srcType) && dstType != srcType);

            switch (srcType)
            {
                case EETypeElementType.Byte:
                    switch (dstType)
                    {
                        case EETypeElementType.Single:
                            Unsafe.As<byte, float>(ref dstValue) = srcValue;
                            break;
                        case EETypeElementType.Double:
                            Unsafe.As<byte, double>(ref dstValue) = srcValue;
                            break;
                        case EETypeElementType.Char:
                        case EETypeElementType.Int16:
                        case EETypeElementType.UInt16:
                            Unsafe.As<byte, short>(ref dstValue) = srcValue;
                            break;
                        case EETypeElementType.Int32:
                        case EETypeElementType.UInt32:
                            Unsafe.As<byte, int>(ref dstValue) = srcValue;
                            break;
                        case EETypeElementType.Int64:
                        case EETypeElementType.UInt64:
                            Unsafe.As<byte, long>(ref dstValue) = srcValue;
                            break;
                        default:
                            Debug.Fail("Expected to be unreachable");
                            break;
                    }
                    break;

                case EETypeElementType.SByte:
                    switch (dstType)
                    {
                        case EETypeElementType.Int16:
                            Unsafe.As<byte, short>(ref dstValue) = Unsafe.As<byte, sbyte>(ref srcValue);
                            break;
                        case EETypeElementType.Int32:
                            Unsafe.As<byte, int>(ref dstValue) = Unsafe.As<byte, sbyte>(ref srcValue);
                            break;
                        case EETypeElementType.Int64:
                            Unsafe.As<byte, long>(ref dstValue) = Unsafe.As<byte, sbyte>(ref srcValue);
                            break;
                        case EETypeElementType.Single:
                            Unsafe.As<byte, float>(ref dstValue) = Unsafe.As<byte, sbyte>(ref srcValue);
                            break;
                        case EETypeElementType.Double:
                            Unsafe.As<byte, double>(ref dstValue) = Unsafe.As<byte, sbyte>(ref srcValue);
                            break;
                        default:
                            Debug.Fail("Expected to be unreachable");
                            break;
                    }
                    break;

                case EETypeElementType.UInt16:
                case EETypeElementType.Char:
                    switch (dstType)
                    {
                        case EETypeElementType.Single:
                            Unsafe.As<byte, float>(ref dstValue) = Unsafe.As<byte, ushort>(ref srcValue);
                            break;
                        case EETypeElementType.Double:
                            Unsafe.As<byte, double>(ref dstValue) = Unsafe.As<byte, ushort>(ref srcValue);
                            break;
                        case EETypeElementType.UInt16:
                        case EETypeElementType.Char:
                            Unsafe.As<byte, ushort>(ref dstValue) = Unsafe.As<byte, ushort>(ref srcValue);
                            break;
                        case EETypeElementType.Int32:
                        case EETypeElementType.UInt32:
                            Unsafe.As<byte, uint>(ref dstValue) = Unsafe.As<byte, ushort>(ref srcValue);
                            break;
                        case EETypeElementType.Int64:
                        case EETypeElementType.UInt64:
                            Unsafe.As<byte, ulong>(ref dstValue) = Unsafe.As<byte, ushort>(ref srcValue);
                            break;
                        default:
                            Debug.Fail("Expected to be unreachable");
                            break;
                    }
                    break;

                case EETypeElementType.Int16:
                    switch (dstType)
                    {
                        case EETypeElementType.Int32:
                            Unsafe.As<byte, int>(ref dstValue) = Unsafe.As<byte, short>(ref srcValue);
                            break;
                        case EETypeElementType.Int64:
                            Unsafe.As<byte, long>(ref dstValue) = Unsafe.As<byte, short>(ref srcValue);
                            break;
                        case EETypeElementType.Single:
                            Unsafe.As<byte, float>(ref dstValue) = Unsafe.As<byte, short>(ref srcValue);
                            break;
                        case EETypeElementType.Double:
                            Unsafe.As<byte, double>(ref dstValue) = Unsafe.As<byte, short>(ref srcValue);
                            break;
                        default:
                            Debug.Fail("Expected to be unreachable");
                            break;
                    }
                    break;

                case EETypeElementType.Int32:
                    switch (dstType)
                    {
                        case EETypeElementType.Int64:
                            Unsafe.As<byte, long>(ref dstValue) = Unsafe.As<byte, int>(ref srcValue);
                            break;
                        case EETypeElementType.Single:
                            Unsafe.As<byte, float>(ref dstValue) = (float)Unsafe.As<byte, int>(ref srcValue);
                            break;
                        case EETypeElementType.Double:
                            Unsafe.As<byte, double>(ref dstValue) = Unsafe.As<byte, int>(ref srcValue);
                            break;
                        default:
                            Debug.Fail("Expected to be unreachable");
                            break;
                    }
                    break;

                case EETypeElementType.UInt32:
                    switch (dstType)
                    {
                        case EETypeElementType.Int64:
                        case EETypeElementType.UInt64:
                            Unsafe.As<byte, long>(ref dstValue) = Unsafe.As<byte, uint>(ref srcValue);
                            break;
                        case EETypeElementType.Single:
                            Unsafe.As<byte, float>(ref dstValue) = (float)Unsafe.As<byte, uint>(ref srcValue);
                            break;
                        case EETypeElementType.Double:
                            Unsafe.As<byte, double>(ref dstValue) = Unsafe.As<byte, uint>(ref srcValue);
                            break;
                        default:
                            Debug.Fail("Expected to be unreachable");
                            break;
                    }
                    break;

                case EETypeElementType.Int64:
                    switch (dstType)
                    {
                        case EETypeElementType.Single:
                            Unsafe.As<byte, float>(ref dstValue) = (float)Unsafe.As<byte, long>(ref srcValue);
                            break;
                        case EETypeElementType.Double:
                            Unsafe.As<byte, double>(ref dstValue) = (double)Unsafe.As<byte, long>(ref srcValue);
                            break;
                        default:
                            Debug.Fail("Expected to be unreachable");
                            break;
                    }
                    break;

                case EETypeElementType.UInt64:
                    switch (dstType)
                    {
                        case EETypeElementType.Single:
                            Unsafe.As<byte, float>(ref dstValue) = (float)Unsafe.As<byte, ulong>(ref srcValue);
                            break;
                        case EETypeElementType.Double:
                            Unsafe.As<byte, double>(ref dstValue) = (double)Unsafe.As<byte, ulong>(ref srcValue);
                            break;
                        default:
                            Debug.Fail("Expected to be unreachable");
                            break;
                    }
                    break;

                case EETypeElementType.Single:
                    switch (dstType)
                    {
                        case EETypeElementType.Double:
                            Unsafe.As<byte, double>(ref dstValue) = Unsafe.As<byte, float>(ref srcValue);
                            break;
                        default:
                            Debug.Fail("Expected to be unreachable");
                            break;
                    }
                    break;

                default:
                    Debug.Fail("Expected to be unreachable");
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Two callers, one of them is potentially perf sensitive
        internal static bool CanPrimitiveWiden(EETypeElementType dstType, EETypeElementType srcType)
        {
            Debug.Assert(dstType is < EETypeElementType.ValueType and >= EETypeElementType.Boolean);
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
                0xCE00, // UInt32 (W = U4, I8, U8, R4, R8)
                0xC400, // Int64 (W = I8, R4, R8)
                0xC800, // UInt64 (W = U8, R4, R8)
                0x0000, // IntPtr
                0x0000, // UIntPtr
                0xC000, // Single (W = R4, R8)
                0x8000, // Double (W = R8)
            ];

            ushort mask = (ushort)(1 << (byte)dstType);
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
