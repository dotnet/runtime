// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Emit;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Concurrent;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;
using static System.Reflection.MethodInvokerCommon;

namespace System.Reflection
{
    internal sealed partial class MethodBaseInvoker
    {
        internal const int MaxStackAllocArgCount = 4;

        private static CerHashtable<InvokeSignatureInfo, MethodBaseInvoker> s_invokers;
        private static object? s_invokersLock;

        // todo: use single with cast: private Delegate _invokeFunc;
        private readonly InvokeFunc_Obj0Args? _invokeFunc_Obj0Args;
        private readonly InvokeFunc_Obj1Arg? _invokeFunc_Obj1Arg;
        private readonly InvokeFunc_ObjSpanArgs? _invokeFunc_ObjSpanArgs;
        private readonly InvokeFunc_RefArgs? _invokeFunc_RefArgs;

        private readonly int _argCount; // For perf, to avoid calling _signatureInfo.ParameterTypes.Length in fast path.
        private readonly InvokerArgFlags[] _invokerArgFlags;
        private readonly MethodBase? _method; // This will be null when using Calli.
        private readonly InvokeSignatureInfo _signatureInfo;
        private readonly InvokerStrategy _strategy;

        public static MethodBaseInvoker Create(MethodBase method, RuntimeType[] argumentTypes)
        {
            return new MethodBaseInvoker(SupportsCalli(method) ? null : method, InvokeSignatureInfo.Create(method, argumentTypes));
        }

        public static MethodBaseInvoker GetOrCreate(MethodBase method, RuntimeType returnType, RuntimeType[] argumentTypes)
        {
            if (!CanCacheInvoker(method))
            {
                return Create(method, argumentTypes);
            }

            InvokeSignatureInfo.NormalizedLookupKey key = new((RuntimeType)method.DeclaringType!, returnType, argumentTypes, method.IsStatic);

            int hashcode = key.AlternativeGetHashCode();
            MethodBaseInvoker existing;
            unsafe
            {
                existing = s_invokers.GetValue<InvokeSignatureInfo.NormalizedLookupKey>(hashcode, key, &InvokeSignatureInfo.NormalizedLookupKey.AlternativeEquals);
            }

            if (existing is not null)
            {
                return existing;
            }

            if (s_invokersLock is null)
            {
                Interlocked.CompareExchange(ref s_invokersLock!, new object(), null);
            }

            bool lockTaken = false;
            try
            {
                Monitor.Enter(s_invokersLock, ref lockTaken);

                unsafe
                {
                    existing = s_invokers.GetValue<InvokeSignatureInfo.NormalizedLookupKey>(hashcode, key, &InvokeSignatureInfo.NormalizedLookupKey.AlternativeEquals);
                }

                if (existing is not null)
                {
                    return existing;
                }

                InvokeSignatureInfo memberBaseSignatureTypes = InvokeSignatureInfo.Create(key);
                MethodBaseInvoker invoker = new MethodBaseInvoker(method: null, memberBaseSignatureTypes);
                s_invokers[memberBaseSignatureTypes] = invoker;
                return invoker;
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(s_invokersLock);
                }
            }
        }

        private static bool CanCacheInvoker(MethodBase method) =>
            CanCacheDynamicMethod(method) &&
            // Supporting default values would increase cache memory usage and slow down the common case.
            !HasDefaultParameterValues(method);

        private MethodBaseInvoker(MethodBase? method, InvokeSignatureInfo signatureInfo)
        {
            _argCount = signatureInfo.ParameterTypes.Length;
            _method = method;
            _signatureInfo = signatureInfo;
            Initialize(signatureInfo, out bool needsByRefStrategy, out _invokerArgFlags);

            _strategy = GetInvokerStrategy(_argCount, needsByRefStrategy);
            switch (_strategy)
            {
                case InvokerStrategy.Obj0:
                    _invokeFunc_Obj0Args = CreateInvokeDelegate_Obj0Args(_method, _signatureInfo, backwardsCompat: true);
                    break;
                case InvokerStrategy.Obj1:
                    _invokeFunc_Obj1Arg = CreateInvokeDelegate_Obj1Arg(_method, _signatureInfo, backwardsCompat: true);
                    break;
                case InvokerStrategy.Obj4:
                case InvokerStrategy.ObjSpan:
                    _invokeFunc_ObjSpanArgs = CreateInvokeDelegate_ObjSpanArgs(_method, _signatureInfo, backwardsCompat: true);
                    break;
                default:
                    Debug.Assert(_strategy == InvokerStrategy.Ref4 || _strategy == InvokerStrategy.RefMany);
                    _invokeFunc_RefArgs = CreateInvokeDelegate_RefArgs(_method, _signatureInfo, backwardsCompat: true);
                    break;
            }
        }

        internal InvokerStrategy Strategy => _strategy;

        [DoesNotReturn]
        internal static void ThrowTargetParameterCountException()
        {
            throw new TargetParameterCountException(SR.Arg_ParmCnt);
        }

        internal unsafe object? InvokeWith0Args(object? obj, IntPtr functionPointer, BindingFlags invokeAttr)
        {
            Debug.Assert(_argCount == 0);

            try
            {
                return _invokeFunc_Obj0Args!(obj, functionPointer);
            }
            catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                throw new TargetInvocationException(e);
            }
        }

        internal unsafe object? InvokeWith1Arg(
            object? obj,
            IntPtr functionPointer,
            BindingFlags invokeAttr,
            Binder? binder,
            object? parameter,
            CultureInfo? culture)
        {
            Debug.Assert(_argCount == 1);

            CheckArgument(ref parameter, 0, binder, culture, invokeAttr);

            try
            {
                return _invokeFunc_Obj1Arg!(obj, functionPointer, parameter);
            }
            catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                throw new TargetInvocationException(e);
            }
        }

        internal unsafe object? InvokeWith4Args(
            object? obj,
            IntPtr functionPointer,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[] parameters,
            CultureInfo? culture)
        {
            Debug.Assert(_argCount <= MaxStackAllocArgCount);

            StackAllocatedArgumentsWithCopyBack stackArgStorage = default;
            Span<object?> copyOfArgs = ((Span<object?>)stackArgStorage._args).Slice(0, _argCount);
            Span<bool> shouldCopyBack = ((Span<bool>)stackArgStorage._shouldCopyBack).Slice(0, _argCount);

            CheckArguments(parameters, copyOfArgs, shouldCopyBack, binder, culture, invokeAttr);

            try
            {
                return _invokeFunc_ObjSpanArgs!(obj, functionPointer, copyOfArgs);
            }
            catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                throw new TargetInvocationException(e);
            }
        }

        internal unsafe object? InvokeWithSpanArgs(
            object? obj,
            IntPtr functionPointer,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[] parameters,
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

                CheckArguments(parameters, copyOfArgs, shouldCopyBack, binder, culture, invokeAttr);

                try
                {
                    ret = _invokeFunc_ObjSpanArgs!(obj, functionPointer, copyOfArgs);
                }
                catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
                {
                    throw new TargetInvocationException(e);
                }

                CopyBack(parameters, copyOfArgs, shouldCopyBack);
            }
            finally
            {
                GCFrameRegistration.UnregisterForGCReporting(&regArgStorage);
            }

            return ret;
        }

        internal unsafe object? InvokeWith4RefArgs(
            object? obj,
            IntPtr functionPointer,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[] parameters,
            CultureInfo? culture)
        {
            Debug.Assert(_argCount <= MaxStackAllocArgCount);

            StackAllocatedArgumentsWithCopyBack stackArgStorage = default;
            Span<object?> copyOfArgs = ((Span<object?>)stackArgStorage._args).Slice(0, _argCount);
            Span<bool> shouldCopyBack = ((Span<bool>)stackArgStorage._shouldCopyBack).Slice(0, _argCount);

            StackAllocatedByRefs byrefs = default;
            IntPtr* pByRefFixedStorage = (IntPtr*)&byrefs;

            CheckArguments(parameters, copyOfArgs, shouldCopyBack, binder, culture, invokeAttr);

            for (int i = 0; i < _argCount; i++)
            {
                *(ByReference*)(pByRefFixedStorage + i) = (_invokerArgFlags[i] & InvokerArgFlags.IsValueType) != 0 ?
                    ByReference.Create(ref copyOfArgs[i]!.GetRawData()) :
#pragma warning disable CS9080
                    ByReference.Create(ref copyOfArgs[i]);
#pragma warning restore CS9080
            }

            object? ret;
            try
            {
                ret = _invokeFunc_RefArgs!(obj, functionPointer, pByRefFixedStorage);
            }
            catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                throw new TargetInvocationException(e);
            }

            CopyBack(parameters, copyOfArgs, shouldCopyBack);
            return ret;
        }

        internal unsafe object? InvokeWithManyRefArgs(
            object? obj,
            IntPtr functionPointer,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[] parameters,
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

                CheckArguments(parameters, copyOfArgs, shouldCopyBack, binder, culture, invokeAttr);

                for (int i = 0; i < _argCount; i++)
                {
                    *(ByReference*)(pByRefStorage + i) = (_invokerArgFlags[i] & InvokerArgFlags.IsValueType) != 0 ?
                        ByReference.Create(ref Unsafe.AsRef<object>(pStorage + i).GetRawData()) :
                        ByReference.Create(ref Unsafe.AsRef<object>(pStorage + i));
                }

                try
                {
                    ret = _invokeFunc_RefArgs!(obj, functionPointer, pByRefStorage);
                }
                catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
                {
                    throw new TargetInvocationException(e);
                }

                CopyBack(parameters, copyOfArgs, shouldCopyBack);
            }
            finally
            {
                GCFrameRegistration.UnregisterForGCReporting(&regByRefStorage);
                GCFrameRegistration.UnregisterForGCReporting(&regArgStorage);
            }

            return ret;
        }

        // Copy modified values out. This is done with ByRef, Type.Missing and parameters changed by the Binder.
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
            RuntimeType sigType = (RuntimeType)_signatureInfo.ParameterTypes[i];

            // Convert a Type.Missing to the default value.
            if (ReferenceEquals(arg, Type.Missing))
            {
                if (_method is null)
                {
                    ThrowHelperArgumentExceptionVariableMissing();
                }

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
                // Check fast path to ignore when arg type matches signature type.
                else if (!ReferenceEquals(sigType, arg.GetType()))
                {
                    // Fast path to ignore byref types.
                    if (TryByRefFastPath(sigType, ref arg!))
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

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CheckArguments(
             ReadOnlySpan<object?> parameters,
             Span<object?> copyOfParameters,
             Span<bool> shouldCopyBack,
             Binder? binder,
             CultureInfo? culture,
             BindingFlags invokeAttr)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                object? arg = parameters[i];
                RuntimeType sigType = (RuntimeType)_signatureInfo.ParameterTypes[i];

                // Convert a Type.Missing to the default value.
                if (ReferenceEquals(arg, Type.Missing))
                {
                    if (_method is null)
                    {
                        ThrowHelperArgumentExceptionVariableMissing();
                    }

                    arg = HandleTypeMissing(_method.GetParametersAsSpan()[i], sigType);
                    shouldCopyBack[i] = true;
                }

                // Convert the type if necessary.
                // Check fast path to ignore non-byref types for normalized arguments.
                if (!ReferenceEquals(sigType, typeof(object)))
                {
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
                        // Fast path to ignore byref types.
                        if (TryByRefFastPath(sigType, ref arg!))
                        {
                            shouldCopyBack[i] = true;
                        }
                        else
                        {
                            shouldCopyBack[i] = sigType.CheckValue(ref arg, binder, culture, invokeAttr);
                        }
                    }
                }

                copyOfParameters[i] = arg;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CheckArgument(
             ref object? arg,
             int index,
             Binder? binder,
             CultureInfo? culture,
             BindingFlags invokeAttr)
        {
            RuntimeType sigType = (RuntimeType)_signatureInfo.ParameterTypes[index];

            // Convert a Type.Missing to the default value.
            if (ReferenceEquals(arg, Type.Missing))
            {
                if (_method is null)
                {
                    // When _method is null, we are using Calli and previously checked if there were any default parameter values.
                    ThrowHelperArgumentExceptionVariableMissing();
                }

                arg = HandleTypeMissing(_method.GetParametersAsSpan()[index], sigType);
            }

            // Convert the type if necessary.
            if (!ReferenceEquals(sigType, typeof(object)) && !ReferenceEquals(sigType, arg?.GetType()))
            {
                sigType.CheckValue(ref arg, binder, culture, invokeAttr);
            }
        }

        private static bool TryByRefFastPath(RuntimeType type, ref object arg)
        {
            if (RuntimeType.TryGetByRefElementType(type, out RuntimeType? sigElementType) &&
                ReferenceEquals(sigElementType, arg.GetType()))
            {
                if (sigElementType.IsValueType)
                {
                    Debug.Assert(!sigElementType.IsNullableOfT, "A true boxed Nullable<T> should never be here.");
                    // Make a copy to prevent the boxed instance from being directly modified by the method.
                    arg = RuntimeType.AllocateValueType(sigElementType, arg);
                }

                return true;
            }

            return false;
        }

        private static InvokerStrategy GetInvokerStrategy(int argCount, bool needsByRefStrategy)
        {
            if (needsByRefStrategy)
            {
                return argCount <= 4 ? InvokerStrategy.Ref4 : InvokerStrategy.RefMany;
            }

            return argCount switch
            {
                0 => InvokerStrategy.Obj0,
                1 => InvokerStrategy.Obj1,
                2 or 3 or 4 => InvokerStrategy.Obj4,
                _ => InvokerStrategy.ObjSpan
            };
        }
    }
}
