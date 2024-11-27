// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;
using static System.Reflection.MethodInvokerCommon;
using static System.RuntimeType;

namespace System.Reflection
{
    /// <summary>
    /// Provides the implementation of the Invoke() methods on MethodInfo, ConstructorInfo and DynamicMethod.
    /// </summary>
    /// <remarks>
    /// This class is known by the runtime in order to ignore reflection frames during stack walks.
    /// </remarks>
    internal sealed partial class MethodBaseInvoker
    {
        internal const int MaxStackAllocArgCount = 4;

        private readonly int _argCount; // For perf, to avoid calling _signatureInfo.ParameterTypes.Length in fast path.
        private readonly RuntimeType? _declaringType;
        private readonly IntPtr _functionPointer; // This will be Zero when not using calli.
        private readonly Delegate _invokeFunc;
        private volatile MethodBaseInvoker? _invokerForCallingCtorAsMethod;
        private readonly InvokerArgFlags[] _invokerArgFlags;
        private readonly MethodBase _method;
        private readonly RuntimeType[] _parameterTypes;
        private readonly InvokerStrategy _strategy;

        public MethodBaseInvoker(MethodBase method, RuntimeType[] parameterTypes, Type returnType, bool callCtorAsMethod = false)
        {
            _method = method;
            _declaringType = (RuntimeType?)_method.DeclaringType;
            _parameterTypes = parameterTypes;
            _argCount = parameterTypes.Length;

            Initialize(
                isForInvokerClasses: false,
                method,
                parameterTypes,
                returnType,
                out _functionPointer,
                out _invokeFunc!,
                out _strategy,
                out _invokerArgFlags);

            _invokeFunc ??= CreateInvokeDelegateForInterpreted();

            if (_functionPointer != IntPtr.Zero && method is RuntimeConstructorInfo)
            {
#if MONO
                _shouldAllocate = true;
#else
                _allocator = _declaringType!.GetOrCreateCacheEntry<CreateUninitializedCache>();
#endif
            }
        }

        // A clone constructor for calling a constructor as a method where the incoming obj parameter is specified.
        private MethodBaseInvoker(MethodBaseInvoker other)
        {
            Debug.Assert(other._method is RuntimeConstructorInfo);

            _argCount = other._argCount;
            _declaringType = other._declaringType;
            _functionPointer = other._functionPointer;
            _invokeFunc = other._invokeFunc;
            _invokerArgFlags = other._invokerArgFlags;
            _method = other._method;
            _parameterTypes = other._parameterTypes;
            _strategy = other._strategy;

            if (_functionPointer != IntPtr.Zero)
            {
#if MONO
                _shouldAllocate = false;
#else
                _allocator = null;
#endif
            }
            else if (UseInterpretedPath)
            {
                // The same interpreted func can be used; the incoming 'obj' parameter checked for null to determine if an alloc is needed.
                _invokeFunc = other._invokeFunc;
            }
            else
            {
                InvokeSignatureInfoKey signatureInfo = new((RuntimeType?)_method.DeclaringType, _parameterTypes, returnType: typeof(void), isStatic: false);
                _invokeFunc = CreateIlInvokeFunc(isForInvokerClasses: false, _method, callCtorAsMethod: true, signatureInfo, _strategy);
            }
        }

        public MethodBaseInvoker GetInvokerForCallingCtorAsMethod()
        {
            Debug.Assert(_method is RuntimeConstructorInfo);

            MethodBaseInvoker? invoker = _invokerForCallingCtorAsMethod;
            if (invoker is null)
            {
                _invokerForCallingCtorAsMethod = invoker = new MethodBaseInvoker(this);
            }

            return invoker;
        }

        internal InvokerStrategy Strategy => _strategy;

        [DoesNotReturn]
        internal static void ThrowTargetParameterCountException()
        {
            throw new TargetParameterCountException(SR.Arg_ParmCnt);
        }

        internal unsafe object? InvokeWithNoArgs(object? obj, BindingFlags invokeAttr)
        {
            Debug.Assert(_argCount == 0);

            try
            {
                if (ShouldAllocate)
                {
                    Debug.Assert(obj is null);
                    obj = CreateUninitializedObject();
                    ((InvokeFunc_Obj0Args)_invokeFunc)(obj, _functionPointer);
                    return obj;
                }

#if MONO
                // Mono calls this method when invoking a constructor with no arguments.
                if (_strategy == InvokerStrategy.Ref4)
                {
                    return ((InvokeFunc_RefArgs)_invokeFunc)(obj, _functionPointer, refArguments: null);
                }
#endif
                return ((InvokeFunc_Obj0Args)_invokeFunc)(obj, _functionPointer);
            }
            catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                throw new TargetInvocationException(e);
            }
        }

        internal unsafe object? InvokePropertySetter(
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object? arg,
            CultureInfo? culture)
        {
            Debug.Assert(_argCount == 1);

            bool _ = false;
            CheckArgument(ref arg, ref _, 0, binder, culture, invokeAttr);

            try
            {
                if (_strategy == InvokerStrategy.Obj1)
                {
                    return ((InvokeFunc_Obj1Arg)_invokeFunc)!(obj, _functionPointer, arg);
                }

                // This method may be called directly, and the interpreted path needs to use the byref strategies.
                return InvokePropertySetter(obj, arg);
            }
            catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                throw new TargetInvocationException(e);
            }
        }

        // Slower path that removes the stack allocs from the caller.
        private unsafe object? InvokePropertySetter(object? obj, object? arg)
        {
            Debug.Assert(UseInterpretedPath);
            Debug.Assert(_strategy == InvokerStrategy.Ref4);

            StackAllocatedByRefs byrefs = default;
            IntPtr* pByRefFixedStorage = (IntPtr*)&byrefs;

            *(ByReference*)(pByRefFixedStorage) = (_invokerArgFlags[0] & InvokerArgFlags.IsValueType) != 0 ?
                ByReference.Create(ref arg!.GetRawData()) :
                ByReference.Create(ref Unsafe.AsRef<object?>(ref arg));

            return ((InvokeFunc_RefArgs) _invokeFunc) (obj, _functionPointer, pByRefFixedStorage);
        }

        internal unsafe object? InvokeWith1Arg(
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[] arguments,
            CultureInfo? culture)
        {
            Debug.Assert(_argCount == 1);

            bool copyBack = false;
            object? arg1 = arguments[0];
            CheckArgument(ref arg1, ref copyBack, 0, binder, culture, invokeAttr);

            try
            {
                if (ShouldAllocate)
                {
                    Debug.Assert(obj is null);
                    obj = CreateUninitializedObject();
                    ((InvokeFunc_Obj1Arg)_invokeFunc)!(obj, _functionPointer, arg1);
                }
                else
                {
                    obj = ((InvokeFunc_Obj1Arg)_invokeFunc)!(obj, _functionPointer, arg1);
                }
            }
            catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                throw new TargetInvocationException(e);
            }

            if (copyBack)
            {
                CopyBack(arguments, new Span<object?>(ref arg1), new Span<bool>(ref copyBack));
            }

            return obj;
        }

        internal unsafe object? InvokeWith4Args(
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[] arguments,
            CultureInfo? culture)
        {
            Debug.Assert(_argCount <= MaxStackAllocArgCount);

            StackAllocatedArgumentsWithCopyBack stackArgStorage = default;
            Span<object?> copyOfArgs = (Span<object?>)stackArgStorage._args;
            Span<bool> shouldCopyBack = (Span<bool>)stackArgStorage._shouldCopyBack;

            for (int i = 0; i < arguments.Length; i++)
            {
                copyOfArgs[i] = arguments[i];
                CheckArgument(ref copyOfArgs[i], ref shouldCopyBack[i], i, binder, culture, invokeAttr);
            }

            try
            {
                if (ShouldAllocate)
                {
                    Debug.Assert(obj is null);
                    obj = CreateUninitializedObject();
                    ((InvokeFunc_Obj4Args)_invokeFunc)(obj, _functionPointer, copyOfArgs[0], copyOfArgs[1], copyOfArgs[2], copyOfArgs[3]);
                }
                else
                {
                    obj = ((InvokeFunc_Obj4Args)_invokeFunc)(obj, _functionPointer, copyOfArgs[0], copyOfArgs[1], copyOfArgs[2], copyOfArgs[3]);
                }
            }
            catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                throw new TargetInvocationException(e);
            }

            CopyBack(arguments, copyOfArgs, shouldCopyBack);

            return obj;
        }

        internal unsafe object? InvokeWithSpanArgs(
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[] arguments,
            CultureInfo? culture)
        {
            Debug.Assert(_argCount > MaxStackAllocArgCount);

            Span<object?> copyOfArgs;
            object? ret;
            GCFrameRegistration regArgStorage;
            Span<bool> shouldCopyBack;

            IntPtr* pArgStorage = stackalloc IntPtr[_argCount * 2];
            NativeMemory.Clear(pArgStorage, (nuint)_argCount * (nuint)sizeof(IntPtr) * 2);
            copyOfArgs = new(ref Unsafe.AsRef<object?>(pArgStorage), _argCount);
            regArgStorage = new((void**)pArgStorage, (uint)_argCount, areByRefs: false);
            shouldCopyBack = new Span<bool>(pArgStorage + _argCount, _argCount);

            try
            {
                GCFrameRegistration.RegisterForGCReporting(&regArgStorage);
                CheckArguments(arguments, copyOfArgs, shouldCopyBack, binder, culture, invokeAttr);

                try
                {
                    if (ShouldAllocate)
                    {
                        Debug.Assert(obj is null);
                        obj = CreateUninitializedObject();
                        ((InvokeFunc_ObjSpanArgs)_invokeFunc)(obj, _functionPointer, copyOfArgs);
                        ret = obj;
                    }
                    else
                    {
                        ret = ((InvokeFunc_ObjSpanArgs)_invokeFunc)(obj, _functionPointer, copyOfArgs);
                    }
                }
                catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
                {
                    throw new TargetInvocationException(e);
                }

                CopyBack(arguments, copyOfArgs, shouldCopyBack);
                return ret;
            }
            finally
            {
                GCFrameRegistration.UnregisterForGCReporting(&regArgStorage);
            }
        }

        internal unsafe object? InvokeWith4RefArgs(
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[]? arguments,
            CultureInfo? culture)
        {
            Debug.Assert(_argCount <= MaxStackAllocArgCount);

            if (_argCount == 0)
            {
                // This method may be called from the interpreted path for a property getter with arguments==null.
                Debug.Assert(UseInterpretedPath);
                Debug.Assert(_strategy == InvokerStrategy.Ref4);
                try
                {
                    return ((InvokeFunc_RefArgs)_invokeFunc)(obj, _functionPointer, refArguments: null);
                }
                catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
                {
                    throw new TargetInvocationException(e);
                }
            }

            StackAllocatedArgumentsWithCopyBack stackArgStorage = default;
            Span<object?> copyOfArgs = (Span<object?>)stackArgStorage._args;
            Span<bool> shouldCopyBack = (Span<bool>)stackArgStorage._shouldCopyBack;

            StackAllocatedByRefs byrefs = default;
            IntPtr* pByRefFixedStorage = (IntPtr*)&byrefs;

            CheckArguments(arguments, copyOfArgs, shouldCopyBack, binder, culture, invokeAttr);

            for (int i = 0; i < _argCount; i++)
            {
                *(ByReference*)(pByRefFixedStorage + i) = (_invokerArgFlags[i] & InvokerArgFlags.IsValueType) != 0 ?
                    ByReference.Create(ref copyOfArgs[i]!.GetRawData()) :
                    ByReference.Create(ref Unsafe.AsRef<object?>(ref copyOfArgs[i]));
            }

            object? ret;
            try
            {
                if (ShouldAllocate)
                {
                    Debug.Assert(obj is null);
                    obj = CreateUninitializedObject();
                    ((InvokeFunc_RefArgs)_invokeFunc)(obj, _functionPointer, pByRefFixedStorage);
                    ret = obj;
                }
                else
                {
                    ret = ((InvokeFunc_RefArgs)_invokeFunc)(obj, _functionPointer, pByRefFixedStorage);
                }
            }
            catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                throw new TargetInvocationException(e);
            }

            CopyBack(arguments!, copyOfArgs, shouldCopyBack);
            return ret;
        }

        internal unsafe object? InvokeWithManyRefArgs(
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[] arguments,
            CultureInfo? culture)
        {
            Debug.Assert(_argCount > MaxStackAllocArgCount);

            object? ret;

            IntPtr* pStorage = stackalloc IntPtr[3 * _argCount];
            NativeMemory.Clear(pStorage, (nuint)(3 * _argCount) * (nuint)sizeof(IntPtr));
            IntPtr* pByRefStorage = pStorage + _argCount;
            Span<object?> copyOfArgs = new(ref Unsafe.AsRef<object?>(pStorage), _argCount);
            GCFrameRegistration regArgStorage = new((void**)pStorage, (uint)_argCount, areByRefs: false);
            GCFrameRegistration regByRefStorage = new((void**)pByRefStorage, (uint)_argCount, areByRefs: true);
            Span<bool> shouldCopyBack = new Span<bool>(pStorage + _argCount * 2, _argCount);

            try
            {
                GCFrameRegistration.RegisterForGCReporting(&regArgStorage);
                GCFrameRegistration.RegisterForGCReporting(&regByRefStorage);

                CheckArguments(arguments, copyOfArgs, shouldCopyBack, binder, culture, invokeAttr);

                for (int i = 0; i < _argCount; i++)
                {
                    *(ByReference*)(pByRefStorage + i) = (_invokerArgFlags[i] & InvokerArgFlags.IsValueType) != 0 ?
                        ByReference.Create(ref Unsafe.AsRef<object>(pStorage + i).GetRawData()) :
                        ByReference.Create(ref Unsafe.AsRef<object>(pStorage + i));
                }

                try
                {
                    if (ShouldAllocate)
                    {
                        Debug.Assert(obj is null);
                        obj = CreateUninitializedObject();
                        ((InvokeFunc_RefArgs)_invokeFunc)(obj, _functionPointer, pByRefStorage);
                        ret = obj;
                    }
                    else
                    {
                        ret = ((InvokeFunc_RefArgs)_invokeFunc)(obj, _functionPointer, pByRefStorage);
                    }
                }
                catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
                {
                    throw new TargetInvocationException(e);
                }

                CopyBack(arguments, copyOfArgs, shouldCopyBack);
            }
            finally
            {
                GCFrameRegistration.UnregisterForGCReporting(&regByRefStorage);
                GCFrameRegistration.UnregisterForGCReporting(&regArgStorage);
            }

            return ret;
        }

        // Copy modified values out. This is done with ByRef, Type.Missing and arguments changed by the Binder.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyBack(object?[] dest, Span<object?> copyOfParameters, Span<bool> shouldCopyBack)
        {
            for (int i = 0; i < dest.Length; i++)
            {
                if (shouldCopyBack[i])
                {
                    if ((_invokerArgFlags[i] & InvokerArgFlags.IsNullableOfT) != 0)
                    {
                        Debug.Assert(copyOfParameters[i] != null);
                        Debug.Assert(((RuntimeType)copyOfParameters[i]!.GetType()).IsNullableOfT);
                        dest![i] = RuntimeMethodHandle.ReboxFromNullable(copyOfParameters[i]);
                    }
                    else
                    {
                        dest![i] = copyOfParameters[i];
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CheckArgument(
             ref object? arg,
             ref bool shouldCopyBack,
             int i,
             Binder? binder,
             CultureInfo? culture,
             BindingFlags invokeAttr)
        {
            RuntimeType sigType = (RuntimeType)_parameterTypes[i];

            // Convert a Type.Missing to the default value.
            if (ReferenceEquals(arg, Type.Missing))
            {
                arg = HandleTypeMissing(_method.GetParametersAsSpan()[i], sigType);
                shouldCopyBack = true;
            }

            // Convert the type if necessary.
            // Check fast path to ignore non-byref types for normalized arguments.
            if (!ReferenceEquals(sigType, typeof(object)))
            {
                if (arg is null)
                {
                    if ((_invokerArgFlags[i] & InvokerArgFlags.IsValueType_ByRef_Or_Pointer) != 0)
                    {
                        shouldCopyBack = sigType.CheckValue(ref arg, binder, culture, invokeAttr);
                    }
                }
                else if (!ReferenceEquals(sigType, arg.GetType()))
                {
                    if (((_invokerArgFlags[i] & InvokerArgFlags.IsByRefForValueType) != 0) && HandleByRefForValueType(sigType, ref arg))
                    {
                        shouldCopyBack = true;
                    }
                    else
                    {
                        shouldCopyBack = sigType.CheckValue(ref arg, binder, culture, invokeAttr);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CheckArguments(
             ReadOnlySpan<object?> arguments,
             Span<object?> copyOfParameters,
             Span<bool> shouldCopyBack,
             Binder? binder,
             CultureInfo? culture,
             BindingFlags invokeAttr)
        {
            for (int i = 0; i < arguments.Length; i++)
            {
                object? arg = arguments[i];
                RuntimeType sigType = (RuntimeType)_parameterTypes[i];

                // Convert a Type.Missing to the default value.
                if (ReferenceEquals(arg, Type.Missing))
                {
                    arg = HandleTypeMissing(_method.GetParametersAsSpan()[i], sigType);
                    shouldCopyBack[i] = true;
                }

                // Convert the type if necessary.
                // Check fast path to ignore non-byref types for normalized arguments.
                if (arg is null)
                {
                    if ((_invokerArgFlags[i] & InvokerArgFlags.IsValueType_ByRef_Or_Pointer) != 0)
                    {
                        shouldCopyBack[i] = sigType.CheckValue(ref arg, binder, culture, invokeAttr);
                    }
                }
                // Check fast path to ignore when arg type matches signature type.
                else if (!ReferenceEquals(sigType, arg.GetType()))
                {
                    if (((_invokerArgFlags[i] & InvokerArgFlags.IsByRefForValueType) != 0) && HandleByRefForValueType(sigType, ref arg))
                    {
                        shouldCopyBack[i] = true;
                    }
                    else
                    {
                        shouldCopyBack[i] = sigType.CheckValue(ref arg, binder, culture, invokeAttr);
                    }
                }

                copyOfParameters[i] = arg;
            }
        }

        private static bool HandleByRefForValueType(RuntimeType type, ref object arg)
        {
            RuntimeType elementType = RuntimeTypeHandle.GetElementType(type)!;
            Debug.Assert(RuntimeTypeHandle.IsByRef(type) && elementType.IsValueType);
            if (ReferenceEquals(elementType, arg.GetType()))
            {
                Debug.Assert(!elementType.IsNullableOfT, "A true boxed Nullable<T> should never be here.");
                // Make a copy to prevent the boxed instance from being directly modified by the method.
                arg = RuntimeType.AllocateValueType(elementType, arg);
                return true;
            }

            return false;
        }
    }
}
