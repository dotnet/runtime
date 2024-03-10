// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime;

namespace System.Reflection
{
    // caches information required for efficient argument validation and type coercion for reflection Invoke.
    public unsafe class DynamicInvokeInfo
    {
        // Public state
        public MethodBase Method { get; }
        public IntPtr InvokeThunk { get; }

        // Private cached information
        private readonly int _argumentCount;
        private readonly bool _isStatic;
        // private readonly bool _isValueTypeInstanceMethod;
        private readonly bool _needsCopyBack;
        private readonly Transform _returnTransform;
        private readonly MethodTable* _returnType;
        private readonly ArgumentInfo[] _arguments;

        // We use negative argument count to signal unsupported invoke signatures
        private const int ArgumentCount_NotSupported = -1;
        private const int ArgumentCount_NotSupported_ByRefLike = -2;

        [Flags]
        private enum Transform
        {
            ByRef = 0x0001,
            Nullable = 0x0002,
            Pointer = 0x0004,
            Reference = 0x0008,
            FunctionPointer = 0x0010,
            AllocateReturnBox = 0x0020,
        }

        private readonly unsafe struct ArgumentInfo
        {
            internal ArgumentInfo(Transform transform, MethodTable* type)
            {
                Transform = transform;
                Type = type;
            }

            internal Transform Transform { get; }
            internal MethodTable* Type { get; }
        }

        public unsafe DynamicInvokeInfo(MethodBase method, IntPtr invokeThunk)
        {
            Method = method;
            InvokeThunk = invokeThunk;

            _isStatic = method.IsStatic;

            // _isValueTypeInstanceMethod = method.DeclaringType?.IsValueType ?? false;

            ReadOnlySpan<ParameterInfo> parameters = method.GetParametersAsSpan();

            _argumentCount = parameters.Length;

            if (_argumentCount != 0)
            {
                ArgumentInfo[] arguments = new ArgumentInfo[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    Transform transform = default;

                    var argumentType = (RuntimeType)parameters[i].ParameterType;
                    if (argumentType.IsByRef)
                    {
                        _needsCopyBack = true;
                        transform |= Transform.ByRef;
                        argumentType = (RuntimeType)argumentType.GetElementType()!;
                    }
                    Debug.Assert(!argumentType.IsByRef);

                    // This can return a null MethodTable for reference types.
                    // The compiler makes sure it returns a non-null MT for everything else.
                    MethodTable* eeArgumentType = argumentType.ToMethodTableMayBeNull();
                    if (argumentType.IsValueType)
                    {
                        Debug.Assert(eeArgumentType->IsValueType);

                        if (eeArgumentType->IsByRefLike)
                            _argumentCount = ArgumentCount_NotSupported_ByRefLike;

                        if (eeArgumentType->IsNullable)
                            transform |= Transform.Nullable;
                    }
                    else if (argumentType.IsPointer)
                    {
                        Debug.Assert(eeArgumentType->IsPointer);

                        transform |= Transform.Pointer;
                    }
                    else if (argumentType.IsFunctionPointer)
                    {
                        Debug.Assert(eeArgumentType->IsFunctionPointer);

                        transform |= Transform.FunctionPointer;
                    }
                    else
                    {
                        transform |= Transform.Reference;
                    }

                    arguments[i] = new ArgumentInfo(transform, eeArgumentType);
                }
                _arguments = arguments;
            }

            if (method is MethodInfo methodInfo)
            {
                Transform transform = default;

                var returnType = (RuntimeType)methodInfo.ReturnType;
                if (returnType.IsByRef)
                {
                    transform |= Transform.ByRef;
                    returnType = (RuntimeType)returnType.GetElementType()!;
                }
                Debug.Assert(!returnType.IsByRef);

                MethodTable* eeReturnType = returnType.ToMethodTableMayBeNull();
                if (returnType.IsValueType)
                {
                    Debug.Assert(eeReturnType->IsValueType);

                    if (returnType != typeof(void))
                    {
                        if (eeReturnType->IsByRefLike)
                            _argumentCount = ArgumentCount_NotSupported_ByRefLike;

                        if ((transform & Transform.ByRef) == 0)
                            transform |= Transform.AllocateReturnBox;

                        if (eeReturnType->IsNullable)
                            transform |= Transform.Nullable;
                    }
                    else
                    {
                        if ((transform & Transform.ByRef) != 0)
                            _argumentCount = ArgumentCount_NotSupported; // ByRef to void return
                    }
                }
                else if (returnType.IsPointer)
                {
                    Debug.Assert(eeReturnType->IsPointer);

                    transform |= Transform.Pointer;
                    if ((transform & Transform.ByRef) == 0)
                        transform |= Transform.AllocateReturnBox;
                }
                else if (returnType.IsFunctionPointer)
                {
                    Debug.Assert(eeReturnType->IsFunctionPointer);

                    transform |= Transform.FunctionPointer;
                    if ((transform & Transform.ByRef) == 0)
                        transform |= Transform.AllocateReturnBox;
                }
                else
                {
                    transform |= Transform.Reference;
                }

                _returnTransform = transform;
                _returnType = eeReturnType;
            }
        }

        public bool IsSupportedSignature => _argumentCount >= 0;

        [DebuggerGuidedStepThrough]
        public unsafe object? Invoke(
            object? thisPtr,
            IntPtr methodToCall,
            object?[]? parameters,
            BinderBundle? binderBundle,
            bool wrapInTargetInvocationException)
        {
            int argCount = parameters?.Length ?? 0;
            if (argCount != _argumentCount)
            {
                ThrowForArgCountMismatch();
            }

            object? returnObject = null;

            scoped ref byte thisArg = ref Unsafe.NullRef<byte>();
            if (!_isStatic)
            {
                // The caller is expected to validate this
                Debug.Assert(thisPtr != null);

                // See TODO comment in DynamicInvokeMethodThunk.NormalizeSignature
                // if (_isValueTypeInstanceMethod)
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
            if ((_returnTransform & Transform.AllocateReturnBox) != 0)
            {
                returnObject = RuntimeImports.RhNewObject(
                    (_returnTransform & (Transform.Pointer | Transform.FunctionPointer)) != 0 ?
                        MethodTable.Of<IntPtr>() : _returnType);
                ret = ref returnObject.GetRawData();
            }

            if (argCount == 0)
            {
                try
                {
                    ret = ref RawCalliHelper.Call(InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, null);
                    DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                }
                catch (Exception e) when (wrapInTargetInvocationException)
                {
                    throw new TargetInvocationException(e);
                }
            }
            else if (argCount > MaxStackAllocArgCount)
            {
                ret = ref InvokeWithManyArguments(methodToCall, ref thisArg, ref ret,
                    parameters, binderBundle, wrapInTargetInvocationException);
            }
            else
            {
                ret = ref InvokeWithFewArguments(methodToCall, ref thisArg, ref ret,
                    parameters, binderBundle, wrapInTargetInvocationException);
            }

            return ((_returnTransform & (Transform.Nullable | Transform.Pointer | Transform.FunctionPointer | Transform.ByRef)) != 0) ?
                ReturnTransform(ref ret, wrapInTargetInvocationException) : returnObject;
        }

        [DebuggerGuidedStepThrough]
        public unsafe object? Invoke(
            object? thisPtr,
            IntPtr methodToCall,
            Span<object?> parameters)
        {
            int argCount = parameters.Length;
            if (argCount != _argumentCount)
            {
                ThrowForArgCountMismatch();
            }

            object? returnObject = null;

            scoped ref byte thisArg = ref Unsafe.NullRef<byte>();
            if (!_isStatic)
            {
                // The caller is expected to validate this
                Debug.Assert(thisPtr != null);

                // See TODO comment in DynamicInvokeMethodThunk.NormalizeSignature
                // if (_isValueTypeInstanceMethod)
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
            if ((_returnTransform & Transform.AllocateReturnBox) != 0)
            {
                returnObject = RuntimeImports.RhNewObject(
                    (_returnTransform & (Transform.Pointer | Transform.FunctionPointer)) != 0 ?
                        MethodTable.Of<IntPtr>() : _returnType);
                ret = ref returnObject.GetRawData();
            }

            if (argCount == 0)
            {
                ret = ref RawCalliHelper.Call(InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, null);
                DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            }
            else if (argCount > MaxStackAllocArgCount)
            {
                ret = ref InvokeWithManyArguments(methodToCall, ref thisArg, ref ret, parameters);
            }
            else
            {
                ret = ref InvokeWithFewArguments(methodToCall, ref thisArg, ref ret, parameters);
            }

            return ((_returnTransform & (Transform.Nullable | Transform.Pointer | Transform.FunctionPointer | Transform.ByRef)) != 0) ?
                ReturnTransform(ref ret, wrapInTargetInvocationException: false) : returnObject;
        }


        [DebuggerGuidedStepThrough]
        // This method is equivalent to the one above except that it takes 'Span<object>' instead of 'object[]'
        // for the parameters, does not require a copy of the parameters or CopyBack, and does not require
        // re-throw capability.
        public unsafe object? InvokeDirectWithFewArgs(
            object? thisPtr,
            IntPtr methodToCall,
            Span<object?> parameters)
        {
            int argCount = parameters.Length;

            if (argCount != _argumentCount)
            {
                ThrowForArgCountMismatch();
            }

            Debug.Assert(_argumentCount <= MaxStackAllocArgCount);

            object? returnObject = null;

            scoped ref byte thisArg = ref Unsafe.NullRef<byte>();
            if (!_isStatic)
            {
                // The caller is expected to validate this
                Debug.Assert(thisPtr != null);

                // See TODO comment in DynamicInvokeMethodThunk.NormalizeSignature
                // if (_isValueTypeInstanceMethod)
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
            if ((_returnTransform & Transform.AllocateReturnBox) != 0)
            {
                returnObject = RuntimeImports.RhNewObject(
                    (_returnTransform & (Transform.Pointer | Transform.FunctionPointer)) != 0 ?
                        MethodTable.Of<IntPtr>() : _returnType);
                ret = ref returnObject.GetRawData();
            }

            if (argCount == 0)
            {
                ret = ref RawCalliHelper.Call(InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, null);
                DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            }
            else if (argCount == 1)
            {
                ByReference br = ByReference.Create(ref parameters[0]);
#pragma warning disable CS8500
                void* pByrefStorage = &br;
#pragma warning restore CS8500

                // Since no copy of args is required, pass 'parameters' for both arguments.
                CheckArguments(parameters, pByrefStorage, parameters);

                ret = ref RawCalliHelper.Call(InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, pByrefStorage);
                DebugAnnotations.PreviousCallContainsDebuggerStepInCode();

                // No need to call CopyBack here since there are no ref values.
            }
            else
            {
                ret = ref InvokeDirectWithFewArguments(methodToCall, ref thisArg, ref ret, parameters);
            }

            return ((_returnTransform & (Transform.Nullable | Transform.Pointer | Transform.FunctionPointer | Transform.ByRef)) != 0) ?
                ReturnTransform(ref ret, wrapInTargetInvocationException: false) : returnObject;
        }

        private void ThrowForArgCountMismatch()
        {
            if (_argumentCount < 0)
            {
                if (_argumentCount == ArgumentCount_NotSupported_ByRefLike)
                    throw new NotSupportedException(SR.NotSupported_ByRefLike);

                throw new NotSupportedException();
            }

            throw new TargetParameterCountException(SR.Arg_ParmCnt);
        }

        private unsafe ref byte InvokeWithManyArguments(
            IntPtr methodToCall, ref byte thisArg, ref byte ret,
            object?[] parameters, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            int argCount = _argumentCount;

            // We don't check a max stack size since we are invoking a method which
            // naturally requires a stack size that is dependent on the arg count\size.
            IntPtr* pStorage = stackalloc IntPtr[2 * argCount];
            NativeMemory.Clear(pStorage, (nuint)(2 * argCount) * (nuint)sizeof(IntPtr));

#pragma warning disable 8500
            void* pByRefStorage = (ByReference*)(pStorage + argCount);
#pragma warning restore 8500

            GCFrameRegistration regArgStorage = new((void**)pStorage, (uint)argCount, areByRefs: false);
            GCFrameRegistration regByRefStorage = new((void**)pByRefStorage, (uint)argCount, areByRefs: true);

            try
            {
                RuntimeImports.RhRegisterForGCReporting(&regArgStorage);
                RuntimeImports.RhRegisterForGCReporting(&regByRefStorage);

                Span<object?> copyOfParameters = new(ref Unsafe.As<IntPtr, object?>(ref *pStorage), argCount);
                CheckArguments(copyOfParameters, pByRefStorage, parameters, binderBundle);

                try
                {
                    ret = ref RawCalliHelper.Call(InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, pByRefStorage);
                    DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                }
                catch (Exception e) when (wrapInTargetInvocationException)
                {
                    throw new TargetInvocationException(e);
                }
                finally
                {
                    if (_needsCopyBack)
                        CopyBackToArray(ref Unsafe.As<IntPtr, object?>(ref *pStorage), parameters);
                }
            }
            finally
            {
                RuntimeImports.RhUnregisterForGCReporting(&regByRefStorage);
                RuntimeImports.RhUnregisterForGCReporting(&regArgStorage);
            }

            return ref ret;
        }

        // This method is equivalent to the one above except that it takes 'Span<object>' instead of 'object[]'
        // for the parameters and does not require re-throw capability.
        private unsafe ref byte InvokeWithManyArguments(
            IntPtr methodToCall, ref byte thisArg, ref byte ret, Span<object?> parameters)
        {
            int argCount = _argumentCount;

            // We don't check a max stack size since we are invoking a method which
            // naturally requires a stack size that is dependent on the arg count\size.
            IntPtr* pStorage = stackalloc IntPtr[2 * argCount];
            NativeMemory.Clear(pStorage, (nuint)(2 * argCount) * (nuint)sizeof(IntPtr));

#pragma warning disable 8500
            void* pByRefStorage = (ByReference*)(pStorage + argCount);
#pragma warning restore 8500

            GCFrameRegistration regArgStorage = new((void**)pStorage, (uint)argCount, areByRefs: false);
            GCFrameRegistration regByRefStorage = new((void**)pByRefStorage, (uint)argCount, areByRefs: true);

            try
            {
                RuntimeImports.RhRegisterForGCReporting(&regArgStorage);
                RuntimeImports.RhRegisterForGCReporting(&regByRefStorage);

                Span<object?> copyOfParameters = new(ref Unsafe.As<IntPtr, object?>(ref *pStorage), argCount);
                CheckArguments(copyOfParameters, pByRefStorage, parameters);

                ret = ref RawCalliHelper.Call(InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, pByRefStorage);
                DebugAnnotations.PreviousCallContainsDebuggerStepInCode();

                if (_needsCopyBack)
                    CopyBackToSpan(copyOfParameters, parameters);
            }
            finally
            {
                RuntimeImports.RhUnregisterForGCReporting(&regByRefStorage);
                RuntimeImports.RhUnregisterForGCReporting(&regArgStorage);
            }

            return ref ret;
        }

        // This is a separate method to localize the overhead of stack allocs for 'StackAllocatedByRefs' and 'StackAllocatedByRefs'.
        private unsafe ref byte InvokeWithFewArguments(
            IntPtr methodToCall, ref byte thisArg, ref byte ret,
            object?[] parameters, BinderBundle? binderBundle, bool wrapInTargetInvocationException)
        {
            Debug.Assert(_argumentCount <= MaxStackAllocArgCount);
            int argCount = _argumentCount;

            StackAllocatedArguments argStorage = default;
            Span<object?> copyOfParameters = argStorage._args.AsSpan(argCount);
            StackAllocatedByRefs byrefStorage = default;
#pragma warning disable CS8500
            void* pByRefStorage = (ByReference*)&byrefStorage;
#pragma warning restore CS8500

            CheckArguments(copyOfParameters, pByRefStorage, parameters, binderBundle);

            try
            {
                ret = ref RawCalliHelper.Call(InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, pByRefStorage);
                DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            }
            catch (Exception e) when (wrapInTargetInvocationException)
            {
                throw new TargetInvocationException(e);
            }
            finally
            {
                if (_needsCopyBack)
                    CopyBackToArray(ref copyOfParameters[0], parameters);
            }

            return ref ret;
        }

        // This method is equivalent to the one above except that it takes 'Span<object>' instead of 'object[]'
        // for the parameters and does not require 'BinderBundle' or re-throw capability.
        private unsafe ref byte InvokeWithFewArguments(
            IntPtr methodToCall, ref byte thisArg, ref byte ret, Span<object?> parameters)
        {
            Debug.Assert(_argumentCount <= MaxStackAllocArgCount);
            int argCount = _argumentCount;

            StackAllocatedArguments argStorage = default;
            Span<object?> copyOfParameters = argStorage._args.AsSpan(argCount);
            StackAllocatedByRefs byrefStorage = default;
#pragma warning disable CS8500
            void* pByRefStorage = (ByReference*)&byrefStorage;
#pragma warning restore CS8500

            CheckArguments(copyOfParameters, pByRefStorage, parameters);

            try
            {
                ret = ref RawCalliHelper.Call(InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, pByRefStorage);
                DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            }
            finally
            {
                if (_needsCopyBack)
                    CopyBackToSpan(copyOfParameters, parameters);
            }

            return ref ret;
        }

        // This method is equivalent to the one above except that it does not require a copy of the args or CopyBack.
        private unsafe ref byte InvokeDirectWithFewArguments(
            IntPtr methodToCall, ref byte thisArg, ref byte ret, Span<object?> parameters)
        {
            Debug.Assert(_argumentCount <= MaxStackAllocArgCount);

            StackAllocatedByRefs byrefStorage = default;
#pragma warning disable CS8500
            void* pByRefStorage = (ByReference*)&byrefStorage;
#pragma warning restore CS8500

            // Since no copy of args is required, pass 'parameters' for both arguments.
            CheckArguments(parameters, pByRefStorage, parameters);

            ret = ref RawCalliHelper.Call(InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, pByRefStorage);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();

            // No need to call CopyBack here since there are no ref values.

            return ref ret;
        }

        private unsafe object? GetCoercedDefaultValue(int index, in ArgumentInfo argumentInfo)
        {
            object? defaultValue = Method.GetParametersAsSpan()[index].DefaultValue;
            if (defaultValue == DBNull.Value)
                throw new ArgumentException(SR.Arg_VarMissNull, "parameters");

            if (defaultValue != null && (argumentInfo.Transform & Transform.Nullable) != 0)
            {
                // In case if the parameter is nullable Enum type the ParameterInfo.DefaultValue returns a raw value which
                // needs to be parsed to the Enum type, for more info: https://github.com/dotnet/runtime/issues/12924
                MethodTable* nullableType = argumentInfo.Type->NullableType;
                if (nullableType->IsEnum)
                {
                    defaultValue = Enum.ToObject(Type.GetTypeFromMethodTable(nullableType), defaultValue);
                }
            }

            return defaultValue;
        }

        private void ThrowForNeverValidNonNullArgument(MethodTable* srcEEType, int index)
        {
            Debug.Assert(index != 0 || _isStatic);
            throw InvokeUtils.CreateChangeTypeArgumentException(srcEEType, Method.GetParametersAsSpan()[index - (_isStatic ? 0 : 1)].ParameterType, destinationIsByRef: false);
        }

        private unsafe void CheckArguments(
            Span<object?> copyOfParameters,
            void* byrefParameters,
            object?[] parameters,
            BinderBundle? binderBundle)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                object? arg = parameters[i];

                ref readonly ArgumentInfo argumentInfo = ref _arguments[i];

            Again:
                if (arg is null)
                {
                    // null is substituded by zero-initialized value for non-reference type
                    if ((argumentInfo.Transform & Transform.Reference) == 0)
                        arg = RuntimeImports.RhNewObject(
                            (argumentInfo.Transform & (Transform.Pointer | Transform.FunctionPointer)) != 0 ?
                                MethodTable.Of<IntPtr>() : argumentInfo.Type);
                }
                else
                {
                    // Check for Missing by comparing the type. It will save us from allocating the Missing instance
                    // unless it is needed.
                    if (arg.GetType() == typeof(Missing))
                    {
                        // Missing is substited by metadata default value
                        arg = GetCoercedDefaultValue(i, in argumentInfo);

                        // The metadata default value is written back into the parameters array
                        parameters[i] = arg;
                        if (arg is null)
                            goto Again; // Redo the argument handling to deal with null
                    }

                    MethodTable* srcEEType = arg.GetMethodTable();
                    MethodTable* dstEEType = argumentInfo.Type;

                    if (srcEEType != dstEEType)
                    {
                        // Destination type can be null if we don't have a MethodTable for this type. This means one cannot
                        // possibly pass a valid non-null object instance here.
                        if (dstEEType == null)
                        {
                            ThrowForNeverValidNonNullArgument(srcEEType, i);
                        }

                        if (!(RuntimeImports.AreTypesAssignable(srcEEType, dstEEType) ||
                            (dstEEType->IsInterface && arg is System.Runtime.InteropServices.IDynamicInterfaceCastable castable
                            && castable.IsInterfaceImplemented(new RuntimeTypeHandle(dstEEType), throwIfNotImplemented: false))))
                        {
                            // ByRefs have to be exact match
                            if ((argumentInfo.Transform & Transform.ByRef) != 0)
                                throw InvokeUtils.CreateChangeTypeArgumentException(srcEEType, argumentInfo.Type, destinationIsByRef: true);

                            arg = InvokeUtils.CheckArgumentConversions(arg, argumentInfo.Type, InvokeUtils.CheckArgumentSemantics.DynamicInvoke, binderBundle);
                        }
                    }

                    if ((argumentInfo.Transform & Transform.Reference) == 0)
                    {
                        if ((argumentInfo.Transform & (Transform.ByRef | Transform.Nullable)) != 0)
                        {
                            // Rebox the value to avoid mutating the original box. This also takes care of
                            // T -> Nullable<T> transformation as side-effect.
                            object box = RuntimeImports.RhNewObject(argumentInfo.Type);
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

                copyOfParameters[i] = arg!;

#pragma warning disable 8500, 9094
                ((ByReference*)byrefParameters)[i] = new ByReference(ref (argumentInfo.Transform & Transform.Reference) != 0 ?
                    ref Unsafe.As<object?, byte>(ref copyOfParameters[i]) : ref arg.GetRawData());
#pragma warning restore 8500, 9094
            }
        }

        // This method is equivalent to the one above except that it takes 'Span<object>' instead of 'object[]'
        // for the parameters and does not require the use of 'BinderBundle' or check for 'Type.Missing'.
        private unsafe void CheckArguments(
            Span<object?> copyOfParameters,
            void* byrefParameters,
            Span<object?> parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                object? arg = parameters[i];

                ref readonly ArgumentInfo argumentInfo = ref _arguments[i];

                if (arg is null)
                {
                    // null is substituded by zero-initialized value for non-reference type
                    if ((argumentInfo.Transform & Transform.Reference) == 0)
                        arg = RuntimeImports.RhNewObject(
                            (argumentInfo.Transform & (Transform.Pointer | Transform.FunctionPointer)) != 0 ?
                                MethodTable.Of<IntPtr>() : argumentInfo.Type);
                }
                else
                {
                    MethodTable* srcEEType = arg.GetMethodTable();
                    MethodTable* dstEEType = argumentInfo.Type;

                    if (srcEEType != dstEEType)
                    {
                        // Destination type can be null if we don't have a MethodTable for this type. This means one cannot
                        // possibly pass a valid non-null object instance here.
                        if (dstEEType == null)
                        {
                            ThrowForNeverValidNonNullArgument(srcEEType, i);
                        }

                        if (!(RuntimeImports.AreTypesAssignable(srcEEType, dstEEType) ||
                            (dstEEType->IsInterface && arg is System.Runtime.InteropServices.IDynamicInterfaceCastable castable
                            && castable.IsInterfaceImplemented(new RuntimeTypeHandle(dstEEType), throwIfNotImplemented: false))))
                        {
                            // ByRefs have to be exact match
                            if ((argumentInfo.Transform & Transform.ByRef) != 0)
                                throw InvokeUtils.CreateChangeTypeArgumentException(srcEEType, argumentInfo.Type, destinationIsByRef: true);

                            arg = InvokeUtils.CheckArgumentConversions(arg, argumentInfo.Type, InvokeUtils.CheckArgumentSemantics.DynamicInvoke, binderBundle: null);
                        }
                    }

                    if ((argumentInfo.Transform & Transform.Reference) == 0)
                    {
                        if ((argumentInfo.Transform & (Transform.ByRef | Transform.Nullable)) != 0)
                        {
                            // Rebox the value to avoid mutating the original box. This also takes care of
                            // T -> Nullable<T> transformation as side-effect.
                            object box = RuntimeImports.RhNewObject(argumentInfo.Type);
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

                copyOfParameters[i] = arg;

#pragma warning disable 8500, 9094
                ((ByReference*)byrefParameters)[i] = new ByReference(ref (argumentInfo.Transform & Transform.Reference) != 0 ?
                    ref Unsafe.As<object?, byte>(ref copyOfParameters[i]) : ref arg.GetRawData());
#pragma warning restore 8500, 9094
            }
        }

        private unsafe void CopyBackToArray(ref object? src, object?[] dest)
        {
            ArgumentInfo[] arguments = _arguments;

            for (int i = 0; i < arguments.Length; i++)
            {
                ref readonly ArgumentInfo argumentInfo = ref arguments[i];

                Transform transform = argumentInfo.Transform;

                if ((transform & Transform.ByRef) == 0)
                    continue;

                object? obj = Unsafe.Add(ref src, i);

                if ((transform & (Transform.Pointer | Transform.FunctionPointer | Transform.Nullable)) != 0)
                {
                    if ((transform & Transform.Pointer) != 0)
                    {
                        Type type = Type.GetTypeFromMethodTable(argumentInfo.Type);
                        Debug.Assert(type.IsPointer);
                        obj = Pointer.Box((void*)Unsafe.As<byte, IntPtr>(ref obj.GetRawData()), type);
                    }
                    else
                    {
                        obj = RuntimeImports.RhBox(
                            (transform & Transform.FunctionPointer) != 0 ? MethodTable.Of<IntPtr>() : argumentInfo.Type,
                            ref obj.GetRawData());
                    }
                }

                dest[i] = obj;
            }
        }

        private unsafe void CopyBackToSpan(Span<object?> src, Span<object?> dest)
        {
            ArgumentInfo[] arguments = _arguments;

            for (int i = 0; i < arguments.Length; i++)
            {
                ref readonly ArgumentInfo argumentInfo = ref arguments[i];

                Transform transform = argumentInfo.Transform;

                if ((transform & Transform.ByRef) == 0)
                    continue;

                object? obj = src[i];

                if ((transform & (Transform.Pointer | Transform.FunctionPointer | Transform.Nullable)) != 0)
                {
                    if ((transform & Transform.Pointer) != 0)
                    {
                        Type type = Type.GetTypeFromMethodTable(argumentInfo.Type);
                        Debug.Assert(type.IsPointer);
                        obj = Pointer.Box((void*)Unsafe.As<byte, IntPtr>(ref obj.GetRawData()), type);
                    }
                    else
                    {
                        obj = RuntimeImports.RhBox(
                            (transform & Transform.FunctionPointer) != 0 ? MethodTable.Of<IntPtr>() : argumentInfo.Type,
                            ref obj.GetRawData());
                    }
                }

                dest[i] = obj;
            }
        }

        private unsafe object ReturnTransform(ref byte byref, bool wrapInTargetInvocationException)
        {
            if (Unsafe.IsNullRef(ref byref))
            {
                Debug.Assert((_returnTransform & Transform.ByRef) != 0);
                Exception exception = new NullReferenceException(SR.NullReference_InvokeNullRefReturned);
                if (wrapInTargetInvocationException)
                    exception = new TargetInvocationException(exception);
                throw exception;
            }

            object obj;
            if ((_returnTransform & Transform.Pointer) != 0)
            {
                Type type = Type.GetTypeFromMethodTable(_returnType);
                Debug.Assert(type.IsPointer);
                obj = Pointer.Box((void*)Unsafe.As<byte, IntPtr>(ref byref), type);
            }
            else if ((_returnTransform & Transform.FunctionPointer) != 0)
            {
                Debug.Assert(Type.GetTypeFromMethodTable(_returnType).IsFunctionPointer);
                obj = RuntimeImports.RhBox(MethodTable.Of<IntPtr>(), ref byref);
            }
            else if ((_returnTransform & Transform.Reference) != 0)
            {
                Debug.Assert((_returnTransform & Transform.ByRef) != 0);
                obj = Unsafe.As<byte, object>(ref byref);
            }
            else
            {
                Debug.Assert((_returnTransform & (Transform.ByRef | Transform.Nullable)) != 0);
                obj = RuntimeImports.RhBox(_returnType, ref byref);
            }
            return obj;
        }

        private const int MaxStackAllocArgCount = 4;

        [InlineArray(MaxStackAllocArgCount)]
        internal struct ArgumentData<T>
        {
            private T _arg0;

            [UnscopedRef]
            public Span<T> AsSpan(int length)
            {
                Debug.Assert((uint)length <= MaxStackAllocArgCount);
                return new Span<T>(ref _arg0, length);
            }

            public void Set(int index, T value)
            {
                Debug.Assert((uint)index < MaxStackAllocArgCount);
                Unsafe.Add(ref _arg0, index) = value;
            }
        }

        // Helper struct to avoid intermediate object[] allocation in calls to the native reflection stack.
        // When argument count <= MaxStackAllocArgCount, define a local of type default(StackAllocatedByRefs)
        // and pass it to CheckArguments().
        // For argument count > MaxStackAllocArgCount, do a stackalloc of void* pointers along with
        // GCReportingRegistration to safely track references.
        [StructLayout(LayoutKind.Sequential)]
        internal ref struct StackAllocatedArguments
        {
            internal ArgumentData<object?> _args;
        }

        // Helper struct to avoid intermediate IntPtr[] allocation and RegisterForGCReporting in calls to the native reflection stack.
        [InlineArray(MaxStackAllocArgCount)]
        internal ref struct StackAllocatedByRefs
        {
            // We're intentionally taking advantage of the runtime functionality, even if the language functionality won't work
            // CS9184: 'Inline arrays' language feature is not supported for inline array types with element field which is either a 'ref' field, or has type that is not valid as a type argument.

#pragma warning disable CS9184
            internal ref byte _arg0;
#pragma warning restore CS9184
        }
    }
}
