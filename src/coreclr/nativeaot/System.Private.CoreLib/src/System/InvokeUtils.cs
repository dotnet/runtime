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

            void* rawDstValue = null;

            Unsafe.SkipInit(out char charValue);
            Unsafe.SkipInit(out sbyte sbyteValue);
            Unsafe.SkipInit(out byte byteValue);
            Unsafe.SkipInit(out short shortValue);
            Unsafe.SkipInit(out ushort ushortValue);
            Unsafe.SkipInit(out int intValue);
            Unsafe.SkipInit(out uint uintValue);
            Unsafe.SkipInit(out long longValue);
            Unsafe.SkipInit(out ulong ulongValue);
            Unsafe.SkipInit(out float floatValue);
            Unsafe.SkipInit(out double doubleValue);

            Unsafe.SkipInit(out dstObject);

            // This is the table of all supported widening conversions:
            //
            // Boolean (W = BOOL)
            // Char (W = U2, CHAR, I4, U4, I8, U8, R4, R8)
            // SByte (W = I1, I2, I4, I8, R4, R8)
            // Byte (W = CHAR, U1, I2, U2, I4, U4, I8, U8, R4, R8)
            // Int16 (W = I2, I4, I8, R4, R8)
            // UInt16 (W = U2, CHAR, I4, U4, I8, U8, R4, R8)
            // Int32 (W = I4, I8, R4, R8)
            // UInt32 (W = U4, I8, U8, R4, R8)
            // Int64 (W = I8, R4, R8)
            // UInt64 (W = U8, R4, R8)
            // Single (W = R4, R8)
            // Double (W = R8)
            switch (dstElementType)
            {
                case EETypeElementType.Boolean:
                    switch (srcElementType)
                    {
                        case EETypeElementType.Boolean:
                            dstObject = srcObject;
                            break;
                        default:
                            goto Failure;
                    }
                    break;

                case EETypeElementType.Char:
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            charValue = RuntimeHelpers.FastUnbox<char>(srcObject);
                            break;
                        case EETypeElementType.Byte:
                            charValue = (char)RuntimeHelpers.FastUnbox<byte>(srcObject);
                            break;
                        case EETypeElementType.UInt16:
                            charValue = (char)RuntimeHelpers.FastUnbox<ushort>(srcObject);
                            break;
                        default:
                            goto Failure;
                    }
                    rawDstValue = &charValue;
                    break;

                case EETypeElementType.SByte:
                    switch (srcElementType)
                    {
                        case EETypeElementType.SByte:
                            sbyteValue = RuntimeHelpers.FastUnbox<sbyte>(srcObject);
                            break;
                        default:
                            goto Failure;
                    }
                    rawDstValue = &sbyteValue;
                    break;

                case EETypeElementType.Byte:
                    switch (srcElementType)
                    {
                        case EETypeElementType.Byte:
                            byteValue = RuntimeHelpers.FastUnbox<byte>(srcObject);
                            break;
                        default:
                            goto Failure;
                    }
                    rawDstValue = &byteValue;
                    break;

                case EETypeElementType.Int16:
                    switch (srcElementType)
                    {
                        case EETypeElementType.SByte:
                            shortValue = (short)RuntimeHelpers.FastUnbox<sbyte>(srcObject);
                            break;
                        case EETypeElementType.Byte:
                            shortValue = (short)RuntimeHelpers.FastUnbox<byte>(srcObject);
                            break;
                        case EETypeElementType.Int16:
                            shortValue = RuntimeHelpers.FastUnbox<short>(srcObject);
                            break;
                        default:
                            goto Failure;
                    }
                    rawDstValue = &shortValue;
                    break;

                case EETypeElementType.UInt16:
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            ushortValue = (ushort)RuntimeHelpers.FastUnbox<char>(srcObject);
                            break;
                        case EETypeElementType.Byte:
                            ushortValue = (ushort)RuntimeHelpers.FastUnbox<byte>(srcObject);
                            break;
                        case EETypeElementType.UInt16:
                            ushortValue = RuntimeHelpers.FastUnbox<ushort>(srcObject);
                            break;
                        default:
                            goto Failure;
                    }
                    rawDstValue = &ushortValue;
                    break;

                case EETypeElementType.Int32:
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            intValue = (int)RuntimeHelpers.FastUnbox<char>(srcObject);
                            break;
                        case EETypeElementType.SByte:
                            intValue = (int)RuntimeHelpers.FastUnbox<sbyte>(srcObject);
                            break;
                        case EETypeElementType.Byte:
                            intValue = (int)RuntimeHelpers.FastUnbox<byte>(srcObject);
                            break;
                        case EETypeElementType.Int16:
                            intValue = (int)RuntimeHelpers.FastUnbox<short>(srcObject);
                            break;
                        case EETypeElementType.UInt16:
                            intValue = (int)RuntimeHelpers.FastUnbox<ushort>(srcObject);
                            break;
                        case EETypeElementType.Int32:
                            intValue = RuntimeHelpers.FastUnbox<int>(srcObject);
                            break;
                        default:
                            goto Failure;
                    }
                    rawDstValue = &intValue;
                    break;

                case EETypeElementType.UInt32:
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            uintValue = (uint)RuntimeHelpers.FastUnbox<char>(srcObject);
                            break;
                        case EETypeElementType.Byte:
                            uintValue = (uint)RuntimeHelpers.FastUnbox<byte>(srcObject);
                            break;
                        case EETypeElementType.UInt16:
                            uintValue = (uint)RuntimeHelpers.FastUnbox<ushort>(srcObject);
                            break;
                        case EETypeElementType.UInt32:
                            uintValue = RuntimeHelpers.FastUnbox<uint>(srcObject);
                            break;
                        default:
                            goto Failure;
                    }
                    rawDstValue = &uintValue;
                    break;

                case EETypeElementType.Int64:
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            longValue = (long)RuntimeHelpers.FastUnbox<char>(srcObject);
                            break;
                        case EETypeElementType.SByte:
                            longValue = (long)RuntimeHelpers.FastUnbox<sbyte>(srcObject);
                            break;
                        case EETypeElementType.Byte:
                            longValue = (long)RuntimeHelpers.FastUnbox<byte>(srcObject);
                            break;
                        case EETypeElementType.Int16:
                            longValue = (long)RuntimeHelpers.FastUnbox<short>(srcObject);
                            break;
                        case EETypeElementType.UInt16:
                            longValue = (long)RuntimeHelpers.FastUnbox<ushort>(srcObject);
                            break;
                        case EETypeElementType.Int32:
                            longValue = (long)RuntimeHelpers.FastUnbox<int>(srcObject);
                            break;
                        case EETypeElementType.UInt32:
                            longValue = (long)RuntimeHelpers.FastUnbox<uint>(srcObject);
                            break;
                        case EETypeElementType.Int64:
                            longValue = RuntimeHelpers.FastUnbox<long>(srcObject);
                            break;
                        default:
                            goto Failure;
                    }
                    rawDstValue = &longValue;
                    break;

                case EETypeElementType.UInt64:
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            ulongValue = (ulong)RuntimeHelpers.FastUnbox<char>(srcObject);
                            break;
                        case EETypeElementType.Byte:
                            ulongValue = (ulong)RuntimeHelpers.FastUnbox<byte>(srcObject);
                            break;
                        case EETypeElementType.UInt16:
                            ulongValue = (ulong)RuntimeHelpers.FastUnbox<ushort>(srcObject);
                            break;
                        case EETypeElementType.UInt32:
                            ulongValue = (ulong)RuntimeHelpers.FastUnbox<uint>(srcObject);
                            break;
                        case EETypeElementType.UInt64:
                            ulongValue = RuntimeHelpers.FastUnbox<ulong>(srcObject);
                            break;
                        default:
                            goto Failure;
                    }
                    rawDstValue = &ulongValue;
                    break;

                case EETypeElementType.Single:
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            floatValue = (float)RuntimeHelpers.FastUnbox<char>(srcObject);
                            break;
                        case EETypeElementType.SByte:
                            floatValue = (float)RuntimeHelpers.FastUnbox<sbyte>(srcObject);
                            break;
                        case EETypeElementType.Byte:
                            floatValue = (float)RuntimeHelpers.FastUnbox<byte>(srcObject);
                            break;
                        case EETypeElementType.Int16:
                            floatValue = (float)RuntimeHelpers.FastUnbox<short>(srcObject);
                            break;
                        case EETypeElementType.UInt16:
                            floatValue = (float)RuntimeHelpers.FastUnbox<ushort>(srcObject);
                            break;
                        case EETypeElementType.Int32:
                            floatValue = (float)RuntimeHelpers.FastUnbox<int>(srcObject);
                            break;
                        case EETypeElementType.UInt32:
                            floatValue = (float)RuntimeHelpers.FastUnbox<uint>(srcObject);
                            break;
                        case EETypeElementType.Int64:
                            floatValue = (float)RuntimeHelpers.FastUnbox<long>(srcObject);
                            break;
                        case EETypeElementType.UInt64:
                            floatValue = (float)RuntimeHelpers.FastUnbox<ulong>(srcObject);
                            break;
                        case EETypeElementType.Single:
                            floatValue = RuntimeHelpers.FastUnbox<float>(srcObject);
                            break;
                        default:
                            goto Failure;
                    }
                    rawDstValue = &floatValue;
                    break;

                case EETypeElementType.Double:
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            doubleValue = (double)RuntimeHelpers.FastUnbox<char>(srcObject);
                            break;
                        case EETypeElementType.SByte:
                            doubleValue = (double)RuntimeHelpers.FastUnbox<sbyte>(srcObject);
                            break;
                        case EETypeElementType.Byte:
                            doubleValue = (double)RuntimeHelpers.FastUnbox<byte>(srcObject);
                            break;
                        case EETypeElementType.Int16:
                            doubleValue = (double)RuntimeHelpers.FastUnbox<short>(srcObject);
                            break;
                        case EETypeElementType.UInt16:
                            doubleValue = (double)RuntimeHelpers.FastUnbox<ushort>(srcObject);
                            break;
                        case EETypeElementType.Int32:
                            doubleValue = (double)RuntimeHelpers.FastUnbox<int>(srcObject);
                            break;
                        case EETypeElementType.UInt32:
                            doubleValue = (double)RuntimeHelpers.FastUnbox<uint>(srcObject);
                            break;
                        case EETypeElementType.Int64:
                            doubleValue = (double)RuntimeHelpers.FastUnbox<long>(srcObject);
                            break;
                        case EETypeElementType.UInt64:
                            doubleValue = (double)RuntimeHelpers.FastUnbox<ulong>(srcObject);
                            break;
                        case EETypeElementType.Single:
                            doubleValue = (double)RuntimeHelpers.FastUnbox<float>(srcObject);
                            break;
                        case EETypeElementType.Double:
                            doubleValue = RuntimeHelpers.FastUnbox<double>(srcObject);
                            break;
                        default:
                            goto Failure;
                    }
                    rawDstValue = &doubleValue;
                    break;

                default:
                    goto Failure;
            }

            // The only case where 'rawDstValue' is not set is for bool values, which set 'dstObject'
            // directly. In all other cases, 'rawDstValue' is guaranteed to not be null.
            if (rawDstValue != null)
            {
                dstObject = RuntimeHelpers.Box(ref *(byte*)&rawDstValue, new RuntimeTypeHandle(dstEEType));
            }

            Debug.Assert(dstObject.GetMethodTable() == dstEEType);
            return null;

            Failure:
            dstObject = null;
            return CreateChangeTypeArgumentException(srcEEType, dstEEType);
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
