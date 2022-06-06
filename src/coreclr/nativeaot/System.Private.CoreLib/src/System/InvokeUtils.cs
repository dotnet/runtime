// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;

using Internal.Reflection.Core.NonPortable;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

using EETypeElementType = Internal.Runtime.EETypeElementType;
using Interlocked = System.Threading.Interlocked;

namespace System
{
    [System.Runtime.CompilerServices.ReflectionBlocked]
    public static class InvokeUtils
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
        // This method is targeted by the Delegate ILTransformer.
        //
        //
        public static object? CheckArgument(object? srcObject, RuntimeTypeHandle dstType, BinderBundle? binderBundle)
        {
            EETypePtr dstEEType = dstType.ToEETypePtr();
            return CheckArgument(srcObject, dstEEType, CheckArgumentSemantics.DynamicInvoke, binderBundle, ref Unsafe.NullRef<ArgSetupState>());
        }

        // This option tweaks the coercion rules to match classic inconsistencies.
        internal enum CheckArgumentSemantics
        {
            ArraySet,            // Throws InvalidCastException
            DynamicInvoke,       // Throws ArgumentException
            SetFieldDirect,      // Throws ArgumentException - other than that, like DynamicInvoke except that enums and integers cannot be intermingled, and null cannot substitute for default(valuetype).
        }

        internal static object? CheckArgument(object? srcObject, EETypePtr dstEEType, CheckArgumentSemantics semantics, BinderBundle? binderBundle)
        {
            return CheckArgument(srcObject, dstEEType, semantics, binderBundle, ref Unsafe.NullRef<ArgSetupState>());
        }

        internal static object? CheckArgument(object? srcObject, EETypePtr dstEEType, CheckArgumentSemantics semantics, BinderBundle? binderBundle, ref ArgSetupState argSetupState)
        {
            // Methods with ByRefLike types in signatures should be filtered out by the compiler
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

                if (RuntimeImports.AreTypesAssignable(srcEEType, dstEEType))
                    return srcObject;

                if (dstEEType.IsInterface)
                {
                    if (srcObject is Runtime.InteropServices.IDynamicInterfaceCastable castable
                        && castable.IsInterfaceImplemented(new RuntimeTypeHandle(dstEEType), throwIfNotImplemented: false))
                        return srcObject;
                }

                object? dstObject;
                Exception exception = ConvertOrWidenPrimitivesEnumsAndPointersIfPossible(srcObject, srcEEType, dstEEType, semantics, out dstObject);
                if (exception == null)
                    return dstObject;

                if (binderBundle == null)
                    throw exception;

                // Our normal coercion rules could not convert the passed in argument but we were supplied a custom binder. See if it can do it.
                Type exactDstType;
                if (Unsafe.IsNullRef(ref argSetupState))
                {
                    // We were called by someone other than DynamicInvokeParamHelperCore(). Those callers pass the correct dstEEType.
                    exactDstType = Type.GetTypeFromHandle(new RuntimeTypeHandle(dstEEType))!;
                }
                else
                {
                    // We were called by DynamicInvokeParamHelperCore(). He passes a dstEEType that enums folded to int and possibly other adjustments. A custom binder
                    // is app code however and needs the exact type.
                    exactDstType = GetExactTypeForCustomBinder(argSetupState);
                }

                srcObject = binderBundle.ChangeType(srcObject, exactDstType);

                // For compat with desktop, the result of the binder call gets processed through the default rules again.
                dstObject = CheckArgument(srcObject, dstEEType, semantics, binderBundle: null, ref Unsafe.NullRef<ArgSetupState>());
                return dstObject;
            }
        }

        // Special coersion rules for primitives, enums and pointer.
        private static Exception ConvertOrWidenPrimitivesEnumsAndPointersIfPossible(object srcObject, EETypePtr srcEEType, EETypePtr dstEEType, CheckArgumentSemantics semantics, out object? dstObject)
        {
            if (semantics == CheckArgumentSemantics.SetFieldDirect && (srcEEType.IsEnum || dstEEType.IsEnum))
            {
                dstObject = null;
                return CreateChangeTypeException(srcEEType, dstEEType, semantics);
            }

            if (dstEEType.IsPointer)
            {
                Exception exception = ConvertPointerIfPossible(srcObject, srcEEType, dstEEType, semantics, out IntPtr dstIntPtr);
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

        private static Exception ConvertPointerIfPossible(object srcObject, EETypePtr srcEEType, EETypePtr dstEEType, CheckArgumentSemantics semantics, out IntPtr dstIntPtr)
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
            return CreateChangeTypeException(srcEEType, dstEEType, semantics);
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

        private static ArgumentException CreateChangeTypeArgumentException(EETypePtr srcEEType, EETypePtr dstEEType)
        {
            return new ArgumentException(SR.Format(SR.Arg_ObjObjEx, Type.GetTypeFromHandle(new RuntimeTypeHandle(srcEEType)), Type.GetTypeFromHandle(new RuntimeTypeHandle(dstEEType))));
        }

        private static InvalidCastException CreateChangeTypeInvalidCastException(EETypePtr srcEEType, EETypePtr dstEEType)
        {
            return new InvalidCastException(SR.InvalidCast_StoreArrayElement);
        }

        // -----------------------------------------------
        // Infrastructure and logic for Dynamic Invocation
        // -----------------------------------------------
        public enum DynamicInvokeParamType
        {
            In = 0,
            Ref = 1
        }

        public enum DynamicInvokeParamLookupType
        {
            ValuetypeObjectReturned = 0,
            IndexIntoObjectArrayReturned = 1,
        }

        public struct ArgSetupState
        {
            public bool fComplete;
            public object?[]? parameters;
            public object[] nullableCopyBackObjects;
            public int curIndex;
            public object targetMethodOrDelegate;
            public BinderBundle? binderBundle;
            public object?[] customBinderProvidedParameters;
        }

        private static object GetDefaultValue(object targetMethodOrDelegate, RuntimeTypeHandle thType, int argIndex)
        {
            if (targetMethodOrDelegate == null)
            {
                throw new ArgumentException(SR.Arg_VarMissNull);
            }

            bool hasDefaultValue = RuntimeAugments.Callbacks.TryGetDefaultParameterValue(targetMethodOrDelegate, thType, argIndex, out object defaultValue);
            if (!hasDefaultValue)
            {
                throw new ArgumentException(SR.Arg_VarMissNull, "parameters");
            }

            // Note that we might return null even for value types which cannot have null value here.
            // This case is handled in the CheckArgument method which is called after this one on the returned parameter value.
            return defaultValue;
        }

        // This is only called if we have to invoke a custom binder to coerce a parameter type. It leverages s_targetMethodOrDelegate to retrieve
        // the unaltered parameter type to pass to the binder.
        private static Type GetExactTypeForCustomBinder(in ArgSetupState argSetupState)
        {
            Debug.Assert(argSetupState.binderBundle != null && argSetupState.targetMethodOrDelegate is MethodBase);
            MethodBase method = (MethodBase)argSetupState.targetMethodOrDelegate;

            // DynamicInvokeParamHelperCore() increments s_curIndex before calling us - that's why we have to subtract 1.
            return method.GetParametersNoCopy()[argSetupState.curIndex - 1].ParameterType;
        }

        [DebuggerGuidedStepThroughAttribute]
        internal static unsafe object CallDynamicInvokeMethod(
            object thisPtr,
            IntPtr methodToCall,
            IntPtr dynamicInvokeHelperMethod,
            IntPtr dynamicInvokeHelperGenericDictionary,
            object targetMethodOrDelegate,
            object?[]? parameters,
            BinderBundle? binderBundle,
            bool wrapInTargetInvocationException,
            bool methodToCallIsThisCall = true)
        {
            // This assert is needed because we've double-purposed "targetMethodOrDelegate" (which is actually a MethodBase anytime a custom binder is used)
            // as a way of obtaining the true parameter type which we need to pass to Binder.ChangeType(). (The type normally passed to DynamicInvokeParamHelperCore
            // isn't always the exact type (byref stripped off, enums converted to int, etc.)
            Debug.Assert(!(binderBundle != null && !(targetMethodOrDelegate is MethodBase)), "The only callers that can pass a custom binder are those servicing MethodBase.Invoke() apis.");

            ArgSetupState argSetupState = new ArgSetupState
            {
                binderBundle = binderBundle,
                targetMethodOrDelegate = targetMethodOrDelegate,
            };

            {
                // If the passed in array is not an actual object[] instance, we need to copy it over to an actual object[]
                // instance so that the rest of the code can safely create managed object references to individual elements.
                if (parameters != null && EETypePtr.EETypePtrOf<object[]>() != parameters.GetEETypePtr())
                {
                    argSetupState.parameters = new object[parameters.Length];
                    Array.Copy(parameters, argSetupState.parameters, parameters.Length);
                }
                else
                {
                    argSetupState.parameters = parameters;
                }

                object result;
                try
                {
                    {
                        if (dynamicInvokeHelperGenericDictionary != IntPtr.Zero)
                        {
                            result = ((delegate*<IntPtr, object, IntPtr, ref ArgSetupState, bool, object>)dynamicInvokeHelperMethod)
                                (dynamicInvokeHelperGenericDictionary, thisPtr, methodToCall, ref argSetupState, methodToCallIsThisCall);
                            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                        }
                        else
                        {
                            result = ((delegate*<object, IntPtr, ref ArgSetupState, bool, object>)dynamicInvokeHelperMethod)
                                (thisPtr, methodToCall, ref argSetupState, methodToCallIsThisCall);
                            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                        }
                    }
                }
                catch (Exception e) when (wrapInTargetInvocationException && argSetupState.fComplete)
                {
                    throw new TargetInvocationException(e);
                }
                finally
                {
                    if (argSetupState.parameters != parameters)
                    {
                        Array.Copy(argSetupState.parameters, parameters, parameters.Length);
                    }

                    if (argSetupState.fComplete)
                    {
                        // Nullable objects can't take advantage of the ability to update the boxed value on the heap directly, so perform
                        // an update of the parameters array now.
                        if (argSetupState.nullableCopyBackObjects != null)
                        {
                            for (int i = 0; i < argSetupState.nullableCopyBackObjects.Length; i++)
                            {
                                if (argSetupState.nullableCopyBackObjects[i] != null)
                                {
                                    parameters[i] = DynamicInvokeBoxIntoNonNullable(argSetupState.nullableCopyBackObjects[i]);
                                }
                            }
                        }
                    }
                }

                return result;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void DynamicInvokeArgSetupComplete(ref ArgSetupState argSetupState)
        {
            int parametersLength = argSetupState.parameters != null ? argSetupState.parameters.Length : 0;

            if (argSetupState.curIndex != parametersLength)
            {
                throw new System.Reflection.TargetParameterCountException();
            }
            argSetupState.fComplete = true;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static unsafe void DynamicInvokeArgSetupPtrComplete(IntPtr argSetupStatePtr)
        {
            // argSetupStatePtr is a pointer to a *pinned* ArgSetupState object
            DynamicInvokeArgSetupComplete(ref Unsafe.As<byte, ArgSetupState>(ref *(byte*)argSetupStatePtr));
        }

        private static void DynamicInvokeUnboxIntoActualNullable(object actualBoxedNullable, object boxedFillObject, EETypePtr nullableType)
        {
            // get a byref to the data within the actual boxed nullable, and then call RhUnBox with the boxedFillObject as the boxed object, and nullableType as the unbox type, and unbox into the actualBoxedNullable
            RuntimeImports.RhUnbox(boxedFillObject, ref actualBoxedNullable.GetRawData(), nullableType);
        }

        private static object DynamicInvokeBoxIntoNonNullable(object actualBoxedNullable)
        {
            // grab the pointer to data, box using the MethodTable of the actualBoxedNullable, and then return the boxed object
            return RuntimeImports.RhBox(actualBoxedNullable.GetEETypePtr(), ref actualBoxedNullable.GetRawData());
        }

        [DebuggerStepThrough]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static ref IntPtr DynamicInvokeParamHelperIn(ref ArgSetupState argSetupState, RuntimeTypeHandle rth)
        {
            //
            // Call DynamicInvokeParamHelperCore as an in parameter, and return a managed byref to the interesting bit.
            //
            // This function exactly matches DynamicInvokeParamHelperRef except for the value of the enum passed to DynamicInvokeParamHelperCore
            //

            object obj = DynamicInvokeParamHelperCore(ref argSetupState, rth, out DynamicInvokeParamLookupType paramLookupType, out int index, DynamicInvokeParamType.In);

            if (paramLookupType == DynamicInvokeParamLookupType.ValuetypeObjectReturned)
            {
                return ref Unsafe.As<byte, IntPtr>(ref obj.GetRawData());
            }
            else
            {
                return ref Unsafe.As<object, IntPtr>(ref Unsafe.As<object[]>(obj)[index]);
            }
        }

        [DebuggerStepThrough]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static ref IntPtr DynamicInvokeParamHelperRef(ref ArgSetupState argSetupState, RuntimeTypeHandle rth)
        {
            //
            // Call DynamicInvokeParamHelperCore as a ref parameter, and return a managed byref to the interesting bit. As this can't actually be defined in C# there is an IL transform that fills this in.
            //
            // This function exactly matches DynamicInvokeParamHelperIn except for the value of the enum passed to DynamicInvokeParamHelperCore
            //

            object obj = DynamicInvokeParamHelperCore(ref argSetupState, rth, out DynamicInvokeParamLookupType paramLookupType, out int index, DynamicInvokeParamType.Ref);

            if (paramLookupType == DynamicInvokeParamLookupType.ValuetypeObjectReturned)
            {
                return ref Unsafe.As<byte, IntPtr>(ref obj.GetRawData());
            }
            else
            {
                return ref Unsafe.As<object, IntPtr>(ref Unsafe.As<object[]>(obj)[index]);
            }
        }

        internal static object DynamicInvokeBoxedValuetypeReturn(out DynamicInvokeParamLookupType paramLookupType, object? boxedValuetype, object?[] parameters, int index, RuntimeTypeHandle type, DynamicInvokeParamType paramType, ref object[] nullableCopyBackObjects)
        {
            object? finalObjectToReturn = boxedValuetype;
            EETypePtr eeType = type.ToEETypePtr();
            bool nullable = eeType.IsNullable;

            if (finalObjectToReturn == null || nullable || paramType == DynamicInvokeParamType.Ref)
            {
                finalObjectToReturn = RuntimeImports.RhNewObject(eeType);
                if (boxedValuetype != null)
                {
                    DynamicInvokeUnboxIntoActualNullable(finalObjectToReturn, boxedValuetype, eeType);
                }
            }

            if (nullable)
            {
                if (paramType == DynamicInvokeParamType.Ref)
                {
                    if (nullableCopyBackObjects == null)
                    {
                        nullableCopyBackObjects = new object[parameters.Length];
                    }

                    nullableCopyBackObjects[index] = finalObjectToReturn;
                    parameters[index] = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(finalObjectToReturn != null);
                if (paramType == DynamicInvokeParamType.Ref)
                    parameters[index] = finalObjectToReturn;
            }

            paramLookupType = DynamicInvokeParamLookupType.ValuetypeObjectReturned;
            return finalObjectToReturn;
        }

        internal static object DynamicInvokeUnmanagedPointerReturn(out DynamicInvokeParamLookupType paramLookupType, object boxedPointerType, int index, RuntimeTypeHandle type, DynamicInvokeParamType paramType)
        {
            object finalObjectToReturn = boxedPointerType;

            Debug.Assert(finalObjectToReturn is IntPtr);
            paramLookupType = DynamicInvokeParamLookupType.ValuetypeObjectReturned;
            return finalObjectToReturn;
        }

        public static unsafe object DynamicInvokeParamHelperCore(IntPtr argSetupState, RuntimeTypeHandle type, out DynamicInvokeParamLookupType paramLookupType, out int index, DynamicInvokeParamType paramType)
        {
            return DynamicInvokeParamHelperCore(ref Unsafe.AsRef<ArgSetupState>((void*)argSetupState), type, out paramLookupType, out index, paramType);
        }

        public static object DynamicInvokeParamHelperCore(ref ArgSetupState argSetupState, RuntimeTypeHandle type, out DynamicInvokeParamLookupType paramLookupType, out int index, DynamicInvokeParamType paramType)
        {
            index = argSetupState.curIndex++;
            int parametersLength = argSetupState.parameters != null ? argSetupState.parameters.Length : 0;

            if (index >= parametersLength)
                throw new System.Reflection.TargetParameterCountException();

            Debug.Assert(argSetupState.parameters != null);
            object? incomingParam = argSetupState.parameters[index];

            // Handle default parameters
            if ((incomingParam == System.Reflection.Missing.Value) && paramType == DynamicInvokeParamType.In)
            {
                incomingParam = GetDefaultValue(argSetupState.targetMethodOrDelegate, type, index);

                // The default value is captured into the parameters array
                argSetupState.parameters[index] = incomingParam;
            }

            RuntimeTypeHandle widenAndCompareType = type;
            bool nullable = type.ToEETypePtr().IsNullable;
            if (nullable)
            {
                widenAndCompareType = new RuntimeTypeHandle(type.ToEETypePtr().NullableType);
            }

            if (widenAndCompareType.ToEETypePtr().IsPrimitive || type.ToEETypePtr().IsEnum)
            {
                // Nullable requires exact matching
                if (incomingParam != null)
                {
                    if (nullable || paramType == DynamicInvokeParamType.Ref)
                    {
                        if (widenAndCompareType.ToEETypePtr() != incomingParam.GetEETypePtr())
                        {
                            if (argSetupState.binderBundle == null)
                                throw CreateChangeTypeArgumentException(incomingParam.GetEETypePtr(), type.ToEETypePtr());
                            Type exactDstType = GetExactTypeForCustomBinder(argSetupState);
                            incomingParam = argSetupState.binderBundle.ChangeType(incomingParam, exactDstType);
                            if (incomingParam != null && widenAndCompareType.ToEETypePtr() != incomingParam.GetEETypePtr())
                                throw CreateChangeTypeArgumentException(incomingParam.GetEETypePtr(), type.ToEETypePtr());
                        }
                    }
                    else
                    {
                        if (widenAndCompareType.ToEETypePtr().ElementType != incomingParam.GetEETypePtr().ElementType)
                        {
                            System.Diagnostics.Debug.Assert(paramType == DynamicInvokeParamType.In);
                            incomingParam = InvokeUtils.CheckArgument(incomingParam, widenAndCompareType.ToEETypePtr(), InvokeUtils.CheckArgumentSemantics.DynamicInvoke, argSetupState.binderBundle, ref argSetupState);
                        }
                    }
                }

                return DynamicInvokeBoxedValuetypeReturn(out paramLookupType, incomingParam, argSetupState.parameters, index, type, paramType, ref argSetupState.nullableCopyBackObjects);
            }
            else if (type.ToEETypePtr().IsValueType)
            {
                incomingParam = InvokeUtils.CheckArgument(incomingParam, type.ToEETypePtr(), InvokeUtils.CheckArgumentSemantics.DynamicInvoke, argSetupState.binderBundle, ref argSetupState);
                if (argSetupState.binderBundle == null)
                {
                    System.Diagnostics.Debug.Assert(argSetupState.parameters[index] == null || object.ReferenceEquals(incomingParam, argSetupState.parameters[index]));
                }
                return DynamicInvokeBoxedValuetypeReturn(out paramLookupType, incomingParam, argSetupState.parameters, index, type, paramType, ref argSetupState.nullableCopyBackObjects);
            }
            else if (type.ToEETypePtr().IsPointer)
            {
                incomingParam = InvokeUtils.CheckArgument(incomingParam, type.ToEETypePtr(), InvokeUtils.CheckArgumentSemantics.DynamicInvoke, argSetupState.binderBundle, ref argSetupState);
                return DynamicInvokeUnmanagedPointerReturn(out paramLookupType, incomingParam, index, type, paramType);
            }
            else
            {
                incomingParam = InvokeUtils.CheckArgument(incomingParam, widenAndCompareType.ToEETypePtr(), InvokeUtils.CheckArgumentSemantics.DynamicInvoke, argSetupState.binderBundle, ref argSetupState);
                paramLookupType = DynamicInvokeParamLookupType.IndexIntoObjectArrayReturned;
                if (argSetupState.binderBundle == null)
                {
                    System.Diagnostics.Debug.Assert(object.ReferenceEquals(incomingParam, argSetupState.parameters[index]));
                    return argSetupState.parameters;
                }
                else
                {
                    if (object.ReferenceEquals(incomingParam, argSetupState.parameters[index]))
                    {
                        return argSetupState.parameters;
                    }
                    else
                    {
                        // If we got here, the original argument object was superceded by invoking the custom binder.

                        if (paramType == DynamicInvokeParamType.Ref)
                        {
                            argSetupState.parameters[index] = incomingParam;
                            return argSetupState.parameters;
                        }
                        else
                        {
                            // Since this not a by-ref parameter, we don't want to bash the original user-owned argument array but the rules of DynamicInvokeParamHelperCore() require
                            // that we return non-value types as the "index"th element in an array. Thus, create an on-demand throwaway array just for this purpose.
                            if (argSetupState.customBinderProvidedParameters == null)
                            {
                                argSetupState.customBinderProvidedParameters = new object[argSetupState.parameters.Length];
                            }
                            argSetupState.customBinderProvidedParameters[index] = incomingParam;
                            return argSetupState.customBinderProvidedParameters;
                        }
                    }
                }
            }
        }
    }
}
