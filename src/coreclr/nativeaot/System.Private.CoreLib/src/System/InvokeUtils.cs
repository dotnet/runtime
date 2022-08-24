// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace System
{
    internal static class InvokeUtils
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

        internal static object? CheckArgument(object? srcObject, EETypePtr dstEEType, CheckArgumentSemantics semantics, BinderBundle? binderBundle)
        {
            // Methods with ByRefLike types in signatures should be filtered out earlier
            Debug.Assert(!dstEEType.IsByRefLike);

            if (srcObject == null)
            {
                // null -> default(T)
                if (dstEEType.IsPointer)
                {
                    return default(IntPtr);
                }
                else if (dstEEType.IsValueType && !dstEEType.IsNullable)
                {
                    if (semantics == CheckArgumentSemantics.SetFieldDirect)
                        throw CreateChangeTypeException(typeof(object).TypeHandle.ToEETypePtr(), dstEEType, semantics);
                    return Runtime.RuntimeImports.RhNewObject(dstEEType);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                EETypePtr srcEEType = srcObject.GetEETypePtr();

                if (srcEEType.RawValue == dstEEType.RawValue ||
                    RuntimeImports.AreTypesAssignable(srcEEType, dstEEType) ||
                    (dstEEType.IsInterface && srcObject is Runtime.InteropServices.IDynamicInterfaceCastable castable
                        && castable.IsInterfaceImplemented(new RuntimeTypeHandle(dstEEType), throwIfNotImplemented: false)))
                {
                    return srcObject;
                }

                return CheckArgumentConversions(srcObject, dstEEType, semantics, binderBundle);
            }
        }

        internal static object? CheckArgumentConversions(object srcObject, EETypePtr dstEEType, CheckArgumentSemantics semantics, BinderBundle? binderBundle)
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
        private static Exception ConvertOrWidenPrimitivesEnumsAndPointersIfPossible(object srcObject, EETypePtr dstEEType, CheckArgumentSemantics semantics, out object? dstObject)
        {
            EETypePtr srcEEType = srcObject.GetEETypePtr();

            if (semantics == CheckArgumentSemantics.SetFieldDirect && (srcEEType.IsEnum || dstEEType.IsEnum))
            {
                dstObject = null;
                return CreateChangeTypeException(srcEEType, dstEEType, semantics);
            }

            if (dstEEType.IsPointer)
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

            if (!(srcEEType.IsPrimitive && dstEEType.IsPrimitive))
            {
                dstObject = null;
                return CreateChangeTypeException(srcEEType, dstEEType, semantics);
            }

            CorElementType dstCorElementType = dstEEType.CorElementType;
            if (!srcEEType.CorElementTypeInfo.CanWidenTo(dstCorElementType))
            {
                dstObject = null;
                return CreateChangeTypeArgumentException(srcEEType, dstEEType);
            }

            switch (dstCorElementType)
            {
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    bool boolValue = Convert.ToBoolean(srcObject);
                    dstObject = dstEEType.IsEnum ? Enum.ToObject(dstEEType, boolValue ? 1 : 0) : boolValue;
                    break;

                case CorElementType.ELEMENT_TYPE_CHAR:
                    char charValue = Convert.ToChar(srcObject);
                    dstObject = dstEEType.IsEnum ? Enum.ToObject(dstEEType, charValue) : charValue;
                    break;

                case CorElementType.ELEMENT_TYPE_I1:
                    sbyte sbyteValue = Convert.ToSByte(srcObject);
                    dstObject = dstEEType.IsEnum ? Enum.ToObject(dstEEType, sbyteValue) : sbyteValue;
                    break;

                case CorElementType.ELEMENT_TYPE_I2:
                    short shortValue = Convert.ToInt16(srcObject);
                    dstObject = dstEEType.IsEnum ? Enum.ToObject(dstEEType, shortValue) : shortValue;
                    break;

                case CorElementType.ELEMENT_TYPE_I4:
                    int intValue = Convert.ToInt32(srcObject);
                    dstObject = dstEEType.IsEnum ? Enum.ToObject(dstEEType, intValue) : intValue;
                    break;

                case CorElementType.ELEMENT_TYPE_I8:
                    long longValue = Convert.ToInt64(srcObject);
                    dstObject = dstEEType.IsEnum ? Enum.ToObject(dstEEType, longValue) : longValue;
                    break;

                case CorElementType.ELEMENT_TYPE_U1:
                    byte byteValue = Convert.ToByte(srcObject);
                    dstObject = dstEEType.IsEnum ? Enum.ToObject(dstEEType, byteValue) : byteValue;
                    break;

                case CorElementType.ELEMENT_TYPE_U2:
                    ushort ushortValue = Convert.ToUInt16(srcObject);
                    dstObject = dstEEType.IsEnum ? Enum.ToObject(dstEEType, ushortValue) : ushortValue;
                    break;

                case CorElementType.ELEMENT_TYPE_U4:
                    uint uintValue = Convert.ToUInt32(srcObject);
                    dstObject = dstEEType.IsEnum ? Enum.ToObject(dstEEType, uintValue) : uintValue;
                    break;

                case CorElementType.ELEMENT_TYPE_U8:
                    ulong ulongValue = Convert.ToUInt64(srcObject);
                    dstObject = dstEEType.IsEnum ? Enum.ToObject(dstEEType, (long)ulongValue) : ulongValue;
                    break;

                case CorElementType.ELEMENT_TYPE_R4:
                    if (srcEEType.CorElementType == CorElementType.ELEMENT_TYPE_CHAR)
                    {
                        dstObject = (float)(char)srcObject;
                    }
                    else
                    {
                        dstObject = Convert.ToSingle(srcObject);
                    }
                    break;

                case CorElementType.ELEMENT_TYPE_R8:
                    if (srcEEType.CorElementType == CorElementType.ELEMENT_TYPE_CHAR)
                    {
                        dstObject = (double)(char)srcObject;
                    }
                    else
                    {
                        dstObject = Convert.ToDouble(srcObject);
                    }
                    break;

                default:
                    Debug.Fail("Unexpected CorElementType: " + dstCorElementType + ": Not a valid widening target.");
                    dstObject = null;
                    return CreateChangeTypeException(srcEEType, dstEEType, semantics);
            }

            Debug.Assert(dstObject.GetEETypePtr() == dstEEType);
            return null;
        }

        private static Exception ConvertPointerIfPossible(object srcObject, EETypePtr dstEEType, CheckArgumentSemantics semantics, out object dstPtr)
        {
            if (srcObject is IntPtr or UIntPtr)
            {
                dstPtr = srcObject;
                return null;
            }

            if (srcObject is Pointer srcPointer)
            {
                if (dstEEType == typeof(void*).TypeHandle.ToEETypePtr() || RuntimeImports.AreTypesAssignable(pSourceType: srcPointer.GetPointerType().TypeHandle.ToEETypePtr(), pTargetType: dstEEType))
                {
                    dstPtr = srcPointer.GetPointerValue();
                    return null;
                }
            }

            dstPtr = null;
            return CreateChangeTypeException(srcObject.GetEETypePtr(), dstEEType, semantics);
        }

        private static Exception CreateChangeTypeException(EETypePtr srcEEType, EETypePtr dstEEType, CheckArgumentSemantics semantics)
        {
            switch (semantics)
            {
                case CheckArgumentSemantics.DynamicInvoke:
                case CheckArgumentSemantics.SetFieldDirect:
                    return CreateChangeTypeArgumentException(srcEEType, dstEEType);
                case CheckArgumentSemantics.ArraySet:
                    return CreateChangeTypeInvalidCastException(srcEEType, dstEEType);
                default:
                    Debug.Fail("Unexpected CheckArgumentSemantics value: " + semantics);
                    throw new InvalidOperationException();
            }
        }

        internal static ArgumentException CreateChangeTypeArgumentException(EETypePtr srcEEType, EETypePtr dstEEType, bool destinationIsByRef = false)
        {
            object? destinationTypeName = Type.GetTypeFromHandle(new RuntimeTypeHandle(dstEEType));
            if (destinationIsByRef)
                destinationTypeName += "&";
            return new ArgumentException(SR.Format(SR.Arg_ObjObjEx, Type.GetTypeFromHandle(new RuntimeTypeHandle(srcEEType)), destinationTypeName));
        }

        private static InvalidCastException CreateChangeTypeInvalidCastException(EETypePtr srcEEType, EETypePtr dstEEType)
        {
            return new InvalidCastException(SR.InvalidCast_StoreArrayElement);
        }
    }
}
