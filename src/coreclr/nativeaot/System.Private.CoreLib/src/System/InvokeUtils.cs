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

            bool boolValue;
            char charValue;
            sbyte sbyteValue;
            byte byteValue;
            short shortValue;
            ushort ushortValue;
            int intValue;
            uint uintValue;
            long longValue;
            ulong ulongValue;
            float floatValue;
            double doubleValue;

            void* rawDstValue;
            ref byte rawSrcValue = ref RuntimeHelpers.GetRawData(srcObject);

            switch (dstElementType)
            {
                case EETypeElementType.Boolean:
                    switch (srcElementType)
                    {
                        case EETypeElementType.Boolean:
                            boolValue = Unsafe.As<byte, bool>(ref rawSrcValue);
                            break;
                        default:
                            goto Failure;
                    }
                    rawDstValue = &boolValue;
                    break;

                case EETypeElementType.Char:
                    switch (srcElementType)
                    {
                        case EETypeElementType.Char:
                            charValue = Unsafe.As<byte, char>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Byte:
                            charValue = (char)rawSrcValue;
                            break;
                        case EETypeElementType.UInt16:
                            charValue = (char)Unsafe.As<byte, ushort>(ref rawSrcValue);
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
                            sbyteValue = Unsafe.As<byte, sbyte>(ref rawSrcValue);
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
                            byteValue = rawSrcValue;
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
                            shortValue = (short)Unsafe.As<byte, sbyte>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Byte:
                            shortValue = (short)rawSrcValue;
                            break;
                        case EETypeElementType.Int16:
                            shortValue = Unsafe.As<byte, short>(ref rawSrcValue);
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
                            ushortValue = (ushort)Unsafe.As<byte, char>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Byte:
                            ushortValue = (ushort)rawSrcValue;
                            break;
                        case EETypeElementType.UInt16:
                            ushortValue = Unsafe.As<byte, ushort>(ref rawSrcValue);
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
                            intValue = (int)Unsafe.As<byte, char>(ref rawSrcValue);
                            break;
                        case EETypeElementType.SByte:
                            intValue = (int)Unsafe.As<byte, sbyte>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Byte:
                            intValue = (int)rawSrcValue;
                            break;
                        case EETypeElementType.Int16:
                            intValue = (int)Unsafe.As<byte, short>(ref rawSrcValue);
                            break;
                        case EETypeElementType.UInt16:
                            intValue = (int)Unsafe.As<byte, ushort>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Int32:
                            intValue = Unsafe.As<byte, int>(ref rawSrcValue);
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
                            uintValue = (uint)Unsafe.As<byte, char>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Byte:
                            uintValue = (uint)rawSrcValue;
                            break;
                        case EETypeElementType.UInt16:
                            uintValue = (uint)Unsafe.As<byte, ushort>(ref rawSrcValue);
                            break;
                        case EETypeElementType.UInt32:
                            uintValue = Unsafe.As<byte, uint>(ref rawSrcValue);
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
                            longValue = (long)Unsafe.As<byte, char>(ref rawSrcValue);
                            break;
                        case EETypeElementType.SByte:
                            longValue = (long)Unsafe.As<byte, sbyte>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Byte:
                            longValue = (long)rawSrcValue;
                            break;
                        case EETypeElementType.Int16:
                            longValue = (long)Unsafe.As<byte, short>(ref rawSrcValue);
                            break;
                        case EETypeElementType.UInt16:
                            longValue = (long)Unsafe.As<byte, ushort>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Int32:
                            longValue = (long)Unsafe.As<byte, int>(ref rawSrcValue);
                            break;
                        case EETypeElementType.UInt32:
                            longValue = (long)Unsafe.As<byte, uint>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Int64:
                            longValue = Unsafe.As<byte, long>(ref rawSrcValue);
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
                            ulongValue = (ulong)Unsafe.As<byte, char>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Byte:
                            ulongValue = (ulong)rawSrcValue;
                            break;
                        case EETypeElementType.UInt16:
                            ulongValue = (ulong)Unsafe.As<byte, ushort>(ref rawSrcValue);
                            break;
                        case EETypeElementType.UInt32:
                            ulongValue = (ulong)Unsafe.As<byte, uint>(ref rawSrcValue);
                            break;
                        case EETypeElementType.UInt64:
                            ulongValue = Unsafe.As<byte, ulong>(ref rawSrcValue);
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
                            floatValue = (float)Unsafe.As<byte, char>(ref rawSrcValue);
                            break;
                        case EETypeElementType.SByte:
                            floatValue = (float)Unsafe.As<byte, sbyte>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Byte:
                            floatValue = (float)rawSrcValue;
                            break;
                        case EETypeElementType.Int16:
                            floatValue = (float)Unsafe.As<byte, short>(ref rawSrcValue);
                            break;
                        case EETypeElementType.UInt16:
                            floatValue = (float)Unsafe.As<byte, ushort>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Int32:
                            floatValue = (float)Unsafe.As<byte, int>(ref rawSrcValue);
                            break;
                        case EETypeElementType.UInt32:
                            floatValue = (float)Unsafe.As<byte, uint>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Int64:
                            floatValue = (float)Unsafe.As<byte, long>(ref rawSrcValue);
                            break;
                        case EETypeElementType.UInt64:
                            floatValue = (float)Unsafe.As<byte, ulong>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Single:
                            floatValue = Unsafe.As<byte, float>(ref rawSrcValue);
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
                            doubleValue = (double)Unsafe.As<byte, char>(ref rawSrcValue);
                            break;
                        case EETypeElementType.SByte:
                            doubleValue = (double)Unsafe.As<byte, sbyte>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Byte:
                            doubleValue = (double)rawSrcValue;
                            break;
                        case EETypeElementType.Int16:
                            doubleValue = (double)Unsafe.As<byte, short>(ref rawSrcValue);
                            break;
                        case EETypeElementType.UInt16:
                            doubleValue = (double)Unsafe.As<byte, ushort>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Int32:
                            doubleValue = (double)Unsafe.As<byte, int>(ref rawSrcValue);
                            break;
                        case EETypeElementType.UInt32:
                            doubleValue = (double)Unsafe.As<byte, uint>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Int64:
                            doubleValue = (double)Unsafe.As<byte, long>(ref rawSrcValue);
                            break;
                        case EETypeElementType.UInt64:
                            doubleValue = (double)Unsafe.As<byte, ulong>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Single:
                            doubleValue = (double)Unsafe.As<byte, float>(ref rawSrcValue);
                            break;
                        case EETypeElementType.Double:
                            doubleValue = Unsafe.As<byte, double>(ref rawSrcValue);
                            break;
                        default:
                            goto Failure;
                    }
                    rawDstValue = &doubleValue;
                    break;

                default:
                    goto Failure;
            }

            dstObject = RuntimeImports.RhBox(dstEEType, ref *(byte*)rawDstValue);
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
