// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

        private static object? CheckArgumentConversions(object srcObject, EETypePtr dstEEType, CheckArgumentSemantics semantics, BinderBundle? binderBundle)
        {
            object? dstObject;
            Exception exception = ConvertOrWidenPrimitivesEnumsAndPointersIfPossible(srcObject, dstEEType, CheckArgumentSemantics.DynamicInvoke, out dstObject);
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
                Exception exception = ConvertPointerIfPossible(srcObject, dstEEType, semantics, out IntPtr dstIntPtr);
                if (exception != null)
                {
                    dstObject = null;
                    return exception;
                }
                dstObject = dstIntPtr;
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

        private static Exception ConvertPointerIfPossible(object srcObject, EETypePtr dstEEType, CheckArgumentSemantics semantics, out IntPtr dstIntPtr)
        {
            if (srcObject is IntPtr srcIntPtr)
            {
                dstIntPtr = srcIntPtr;
                return null;
            }

            if (srcObject is Pointer srcPointer)
            {
                if (dstEEType == typeof(void*).TypeHandle.ToEETypePtr() || RuntimeImports.AreTypesAssignable(pSourceType: srcPointer.GetPointerType().TypeHandle.ToEETypePtr(), pTargetType: dstEEType))
                {
                    dstIntPtr = srcPointer.GetPointerValue();
                    return null;
                }
            }

            dstIntPtr = IntPtr.Zero;
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

        private static ArgumentException CreateChangeTypeArgumentException(EETypePtr srcEEType, EETypePtr dstEEType, bool destinationIsByRef = false)
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

        [DebuggerGuidedStepThroughAttribute]
        internal static unsafe object? CallDynamicInvokeMethod(
            object? thisPtr,
            IntPtr methodToCall,
            DynamicInvokeInfo dynamicInvokeInfo,
            object?[]? parameters,
            BinderBundle? binderBundle,
            bool wrapInTargetInvocationException)
        {
            int argCount = parameters?.Length ?? 0;
            if (argCount != dynamicInvokeInfo.ArgumentCount)
            {
                if (dynamicInvokeInfo.ArgumentCount < 0)
                {
                    if (dynamicInvokeInfo.ArgumentCount == DynamicInvokeInfo.ArgumentCount_NotSupported_ByRefLike)
                        throw new NotSupportedException(SR.NotSupported_ByRefLike);
                    throw new NotSupportedException();
                }

                throw new TargetParameterCountException(SR.Arg_ParmCnt);
            }

            object? returnObject = null;

            scoped ref byte thisArg = ref Unsafe.NullRef<byte>();
            if (!dynamicInvokeInfo.IsStatic)
            {
                // The caller is expected to validate this
                Debug.Assert(thisPtr != null);

                // See TODO comment in DynamicInvokeMethodThunk.NormalizeSignature
                // if (dynamicInvokeInfo.IsValueTypeInstanceMethod)
                // {
                //     // thisArg is a raw data byref for valuetype instance methods
                //     thisArg = ref thisPtr.GetRawData();
                // }
                // else
                {
                    thisArg = ref Unsafe.As<object?, byte>(ref thisPtr);
                }
            }

            scoped ref byte ret = ref Unsafe.As<object?, byte>(ref returnObject);
            if ((dynamicInvokeInfo.ReturnTransform & DynamicInvokeTransform.AllocateReturnBox) != 0)
            {
                returnObject = RuntimeImports.RhNewObject(
                    (dynamicInvokeInfo.ReturnTransform & DynamicInvokeTransform.Pointer) != 0 ?
                        EETypePtr.EETypePtrOf<IntPtr>() : dynamicInvokeInfo.ReturnType);
                ret = ref returnObject.GetRawData();
            }

            if (argCount == 0)
            {
                try
                {
                    ret = ref RawCalliHelper.Call(dynamicInvokeInfo.InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, null);
                    DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                }
                catch (Exception e) when (wrapInTargetInvocationException)
                {
                    throw new TargetInvocationException(e);
                }
            }
            else if (argCount > MaxStackAllocArgCount)
            {
                ret = ref InvokeWithManyArguments(dynamicInvokeInfo, methodToCall, ref thisArg, ref ret,
                    parameters, binderBundle, wrapInTargetInvocationException);
            }
            else
            {
                StackAllocedArguments argStorage = default;
                StackAllocatedByRefs byrefStorage = default;

                CheckArguments(dynamicInvokeInfo, ref argStorage._arg0!, (ByReference*)&byrefStorage, parameters, binderBundle);

                try
                {
                    ret = ref RawCalliHelper.Call(dynamicInvokeInfo.InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, &byrefStorage);
                    DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                }
                catch (Exception e) when (wrapInTargetInvocationException)
                {
                    throw new TargetInvocationException(e);
                }
                finally
                {
                    if (dynamicInvokeInfo.NeedsCopyBack)
                        CopyBack(dynamicInvokeInfo, ref argStorage._arg0!, parameters);
                }
            }

            return ((dynamicInvokeInfo.ReturnTransform & (DynamicInvokeTransform.Nullable | DynamicInvokeTransform.Pointer | DynamicInvokeTransform.ByRef)) != 0) ?
                ReturnTranform(dynamicInvokeInfo, ref ret, wrapInTargetInvocationException) : returnObject;
        }

        private static unsafe ref byte InvokeWithManyArguments(
            DynamicInvokeInfo dynamicInvokeInfo, IntPtr methodToCall, ref byte thisArg, ref byte ret,
            object?[] parameters, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            int argCount = dynamicInvokeInfo.ArgumentCount;

            // We don't check a max stack size since we are invoking a method which
            // naturally requires a stack size that is dependent on the arg count\size.
            IntPtr* pStorage = stackalloc IntPtr[2 * argCount];
            NativeMemory.Clear(pStorage, (nuint)(2 * argCount) * (nuint)sizeof(IntPtr));

            ByReference* pByRefStorage = (ByReference*)(pStorage + argCount);

            RuntimeImports.GCFrameRegistration regArgStorage = new(pStorage, (uint)argCount, areByRefs: false);
            RuntimeImports.GCFrameRegistration regByRefStorage = new(pByRefStorage, (uint)argCount, areByRefs: true);

            try
            {
                RuntimeImports.RhRegisterForGCReporting(&regArgStorage);
                RuntimeImports.RhRegisterForGCReporting(&regByRefStorage);

                CheckArguments(dynamicInvokeInfo, ref Unsafe.As<IntPtr, object>(ref *pStorage), pByRefStorage, parameters, binderBundle);

                try
                {
                    ret = ref RawCalliHelper.Call(dynamicInvokeInfo.InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, pByRefStorage);
                    DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                }
                catch (Exception e) when (wrapInTargetInvocationException)
                {
                    throw new TargetInvocationException(e);
                }
                finally
                {
                    if (dynamicInvokeInfo.NeedsCopyBack)
                        CopyBack(dynamicInvokeInfo, ref Unsafe.As<IntPtr, object>(ref *pStorage), parameters);
                }
            }
            finally
            {
                RuntimeImports.RhUnregisterForGCReporting(&regByRefStorage);
                RuntimeImports.RhUnregisterForGCReporting(&regArgStorage);
            }

            return ref ret;
        }

        private static object? GetCoercedDefaultValue(DynamicInvokeInfo dynamicInvokeInfo, int index, in ArgumentInfo argumentInfo)
        {
            object? defaultValue = dynamicInvokeInfo.Method.GetParametersNoCopy()[index].DefaultValue;
            if (defaultValue == DBNull.Value)
                throw new ArgumentException(SR.Arg_VarMissNull, "parameters");

            if (defaultValue != null && (argumentInfo.Transform & DynamicInvokeTransform.Nullable) != 0)
            {
                // In case if the parameter is nullable Enum type the ParameterInfo.DefaultValue returns a raw value which
                // needs to be parsed to the Enum type, for more info: https://github.com/dotnet/runtime/issues/12924
                EETypePtr nullableType = argumentInfo.Type.NullableType;
                if (nullableType.IsEnum)
                {
                    defaultValue = Enum.ToObject(Type.GetTypeFromEETypePtr(nullableType), defaultValue);
                }
            }

            return defaultValue;
        }

        private static unsafe void CheckArguments(
            DynamicInvokeInfo dynamicInvokeInfo,
            ref object copyOfParameters,
            ByReference* byrefParameters,
            object?[] parameters,
            BinderBundle binderBundle)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                object? arg = parameters[i];

                ref readonly ArgumentInfo argumentInfo = ref dynamicInvokeInfo.Arguments[i];

            Again:
                if (arg is null)
                {
                    // null is substituded by zero-initialized value for non-reference type
                    if ((argumentInfo.Transform & DynamicInvokeTransform.Reference) == 0)
                        arg = RuntimeImports.RhNewObject(
                            (argumentInfo.Transform & DynamicInvokeTransform.Pointer) != 0 ?
                                EETypePtr.EETypePtrOf<IntPtr>() : argumentInfo.Type);
                }
                else
                {
                    // Check for Missing by comparing the type. It will save us from allocating the Missing instance
                    // unless it is needed.
                    if (arg.GetType() == typeof(Missing))
                    {
                        // Missing is substited by metadata default value
                        arg = GetCoercedDefaultValue(dynamicInvokeInfo, i, in argumentInfo);

                        // The metadata default value is written back into the parameters array
                        parameters[i] = arg;
                        if (arg is null)
                            goto Again; // Redo the argument handling to deal with null
                    }

                    EETypePtr srcEEType = arg.GetEETypePtr();
                    EETypePtr dstEEType = argumentInfo.Type;

                    if (!(srcEEType.RawValue == dstEEType.RawValue ||
                        RuntimeImports.AreTypesAssignable(srcEEType, dstEEType) ||
                        (dstEEType.IsInterface && arg is Runtime.InteropServices.IDynamicInterfaceCastable castable
                            && castable.IsInterfaceImplemented(new RuntimeTypeHandle(dstEEType), throwIfNotImplemented: false))))
                    {
                        // ByRefs have to be exact match
                        if ((argumentInfo.Transform & DynamicInvokeTransform.ByRef) != 0)
                            throw CreateChangeTypeArgumentException(srcEEType, argumentInfo.Type, destinationIsByRef: true);

                        arg = CheckArgumentConversions(arg, argumentInfo.Type, CheckArgumentSemantics.DynamicInvoke, binderBundle);
                    }

                    if ((argumentInfo.Transform & DynamicInvokeTransform.Reference) == 0)
                    {
                        if ((argumentInfo.Transform & (DynamicInvokeTransform.ByRef | DynamicInvokeTransform.Nullable)) != 0)
                        {
                            // Rebox the value to avoid mutating the original box. This also takes care of
                            // T -> Nullable<T> transformation as side-effect.
                            object box = Runtime.RuntimeImports.RhNewObject(argumentInfo.Type);
                            RuntimeImports.RhUnbox(arg, ref box.GetRawData(), argumentInfo.Type);
                            arg = box;
                        }
                    }
                }

                // We need to perform type safety validation against the incoming arguments, but we also need
                // to be resilient against the possibility that some other thread (or even the binder itself!)
                // may mutate the array after we've validated the arguments but before we've properly invoked
                // the method. The solution is to copy the arguments to a different, not-user-visible buffer
                // as we validate them. This separate array is also used to hold default values when 'null'
                // is specified for value types, and also used to hold the results from conversions such as
                // from Int16 to Int32.

                Unsafe.Add(ref copyOfParameters, i) = arg!;

                byrefParameters[i] = new ByReference(ref (argumentInfo.Transform & DynamicInvokeTransform.Reference) != 0 ?
                    ref Unsafe.As<object, byte>(ref Unsafe.Add(ref copyOfParameters, i)) : ref arg.GetRawData());
            }
        }

        private static unsafe void CopyBack(DynamicInvokeInfo dynamicInvokeInfo, ref object copyOfParameters, object?[] parameters)
        {
            ArgumentInfo[] arguments = dynamicInvokeInfo.Arguments;

            for (int i = 0; i < arguments.Length; i++)
            {
                ArgumentInfo argumentInfo = arguments[i];
                DynamicInvokeTransform transform = argumentInfo.Transform;

                if ((transform & DynamicInvokeTransform.ByRef) == 0)
                    continue;

                object obj = Unsafe.Add(ref copyOfParameters, i);

                if ((transform & (DynamicInvokeTransform.Pointer | DynamicInvokeTransform.Nullable)) != 0)
                {
                    if ((transform & DynamicInvokeTransform.Pointer) != 0)
                    {
                        Type type = Type.GetTypeFromEETypePtr(argumentInfo.Type);
                        Debug.Assert(type.IsPointer);
                        obj = Pointer.Box((void*)Unsafe.As<byte, IntPtr>(ref obj.GetRawData()), type);
                    }
                    else
                    {
                        obj = RuntimeImports.RhBox(argumentInfo.Type, ref obj.GetRawData());
                    }
                }

                parameters[i] = obj;
            }
        }

        private static unsafe object ReturnTranform(DynamicInvokeInfo dynamicInvokeInfo, ref byte byref, bool wrapInTargetInvocationException)
        {
            if (Unsafe.IsNullRef(ref byref))
            {
                Debug.Assert((dynamicInvokeInfo.ReturnTransform & DynamicInvokeTransform.ByRef) != 0);
                Exception exception = new NullReferenceException(SR.NullReference_InvokeNullRefReturned);
                if (wrapInTargetInvocationException)
                    exception = new TargetInvocationException(exception);
                throw exception;
            }

            object obj;
            if ((dynamicInvokeInfo.ReturnTransform & DynamicInvokeTransform.Pointer) != 0)
            {
                Type type = Type.GetTypeFromEETypePtr(dynamicInvokeInfo.ReturnType);
                Debug.Assert(type.IsPointer);
                obj = Pointer.Box((void*)Unsafe.As<byte, IntPtr>(ref byref), type);
            }
            else if ((dynamicInvokeInfo.ReturnTransform & DynamicInvokeTransform.Reference) != 0)
            {
                Debug.Assert((dynamicInvokeInfo.ReturnTransform & DynamicInvokeTransform.ByRef) != 0);
                obj = Unsafe.As<byte, object>(ref byref);
            }
            else
            {
                Debug.Assert((dynamicInvokeInfo.ReturnTransform & (DynamicInvokeTransform.ByRef | DynamicInvokeTransform.Nullable)) != 0);
                obj = RuntimeImports.RhBox(dynamicInvokeInfo.ReturnType, ref byref);
            }
            return obj;
        }

        private const int MaxStackAllocArgCount = 4;

        // Helper struct to avoid intermediate object[] allocation in calls to the native reflection stack.
        // When argument count <= MaxStackAllocArgCount, define a local of type default(StackAllocatedByRefs)
        // and pass it to CheckArguments().
        // For argument count > MaxStackAllocArgCount, do a stackalloc of void* pointers along with
        // GCReportingRegistration to safely track references.
        [StructLayout(LayoutKind.Sequential)]
        private ref struct StackAllocedArguments
        {
            internal object? _arg0;
#pragma warning disable CA1823, CS0169, IDE0051 // accessed via 'CheckArguments' ref arithmetic
            private object? _arg1;
            private object? _arg2;
            private object? _arg3;
#pragma warning restore CA1823, CS0169, IDE0051
        }

        // Helper struct to avoid intermediate IntPtr[] allocation and RegisterForGCReporting in calls to the native reflection stack.
        [StructLayout(LayoutKind.Sequential)]
        private ref struct StackAllocatedByRefs
        {
            internal ref byte _arg0;
#pragma warning disable CA1823, CS0169, IDE0051 // accessed via 'CheckArguments' ref arithmetic
            private ref byte _arg1;
            private ref byte _arg2;
            private ref byte _arg3;
#pragma warning restore CA1823, CS0169, IDE0051
        }
    }
}
