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

namespace System.Reflection
{
    internal sealed partial class MethodBaseInvoker
    {
        internal const int MaxStackAllocArgCount = 4;

        private InvokeFunc_ObjSpanArgs? _invokeFunc_ObjSpanArgs;
        private InvokeFunc_RefArgs? _invokeFunc_RefArgs;
        private InvokerStrategy _strategy;
        internal readonly InvocationFlags _invocationFlags;
        private readonly InvokerArgFlags[] _invokerArgFlags;
        private readonly RuntimeType[] _argTypes;
        private readonly MethodBase _method;
        private readonly int _argCount;
        private readonly bool _needsByRefStrategy;

        private MethodBaseInvoker(MethodBase method, RuntimeType[] argumentTypes)
        {
            _method = method;
            _argTypes = argumentTypes;
            _argCount = _argTypes.Length;

            Initialize(argumentTypes, out _strategy, out _invokerArgFlags, out _needsByRefStrategy);
        }

        [DoesNotReturn]
        internal static void ThrowTargetParameterCountException()
        {
            throw new TargetParameterCountException(SR.Arg_ParmCnt);
        }


        internal unsafe object? InvokeWithNoArgs(object? obj, BindingFlags invokeAttr)
        {
            Debug.Assert(_argCount == 0);

            if ((_strategy & InvokerStrategy.StrategyDetermined_RefArgs) == 0)
            {
                DetermineStrategy_RefArgs(ref _strategy, ref _invokeFunc_RefArgs, _method, backwardsCompat: true);
            }

            try
            {
                return _invokeFunc_RefArgs!(obj, refArguments: null);
            }
            catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                throw new TargetInvocationException(e);
            }
        }

        internal unsafe object? InvokeWithOneArg(
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[] parameters,
            CultureInfo? culture)
        {
            Debug.Assert(_argCount == 1);

            object? arg = parameters[0];
            var parametersSpan = new ReadOnlySpan<object?>(in arg);

            object? copyOfArg = null;
            Span<object?> copyOfArgs = new(ref copyOfArg);

            bool copyBack = false;
            Span<bool> shouldCopyBack = new(ref copyBack);

            object? ret;
            if ((_strategy & InvokerStrategy.StrategyDetermined_ObjSpanArgs) == 0)
            {
                DetermineStrategy_ObjSpanArgs(ref _strategy, ref _invokeFunc_ObjSpanArgs, _method, _needsByRefStrategy, backwardsCompat: true);
            }

            CheckArguments(parametersSpan, copyOfArgs, shouldCopyBack, binder, culture, invokeAttr);

            if (_invokeFunc_ObjSpanArgs is not null)
            {
                try
                {
                    ret = _invokeFunc_ObjSpanArgs(obj, copyOfArgs);
                }
                catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
                {
                    throw new TargetInvocationException(e);
                }
            }
            else
            {
                ret = InvokeDirectByRefWithFewArgs(obj, copyOfArgs, invokeAttr);
            }

            CopyBack(parameters, copyOfArgs, shouldCopyBack);
            return ret;
        }

        internal unsafe object? InvokeWithFewArgs(
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[] parameters,
            CultureInfo? culture)
        {
            Debug.Assert(_argCount <= MaxStackAllocArgCount);

            StackAllocatedArgumentsWithCopyBack stackArgStorage = default;
            Span<object?> copyOfArgs = stackArgStorage._args.AsSpan(_argCount);
            Span<bool> shouldCopyBack = stackArgStorage._shouldCopyBack.AsSpan(_argCount);

            object? ret;
            if ((_strategy & InvokerStrategy.StrategyDetermined_ObjSpanArgs) == 0)
            {
                DetermineStrategy_ObjSpanArgs(ref _strategy, ref _invokeFunc_ObjSpanArgs, _method, _needsByRefStrategy, backwardsCompat: true);
            }

            CheckArguments(parameters, copyOfArgs, shouldCopyBack, binder, culture, invokeAttr);

            if (_invokeFunc_ObjSpanArgs is not null)
            {
                try
                {
                    ret = _invokeFunc_ObjSpanArgs(obj, copyOfArgs);
                }
                catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
                {
                    throw new TargetInvocationException(e);
                }
            }
            else
            {
                ret = InvokeDirectByRefWithFewArgs(obj, copyOfArgs, invokeAttr);

            }

            CopyBack(parameters, copyOfArgs, shouldCopyBack);
            return ret;
        }

        internal unsafe object? InvokeDirectByRefWithFewArgs(object? obj, Span<object?> copyOfArgs, BindingFlags invokeAttr)
        {
            Debug.Assert(_argCount <= MaxStackAllocArgCount);

            if ((_strategy & InvokerStrategy.StrategyDetermined_RefArgs) == 0)
            {
                DetermineStrategy_RefArgs(ref _strategy, ref _invokeFunc_RefArgs, _method, backwardsCompat: true);
            }

            StackAllocatedByRefs byrefs = default;
#pragma warning disable CS8500
            IntPtr* pByRefFixedStorage = (IntPtr*)&byrefs;
#pragma warning restore CS8500

            for (int i = 0; i < _argCount; i++)
            {
#pragma warning disable CS8500
                *(ByReference*)(pByRefFixedStorage + i) = (_invokerArgFlags[i] & InvokerArgFlags.IsValueType) != 0 ?
#pragma warning restore CS8500
                    ByReference.Create(ref copyOfArgs[i]!.GetRawData()) :
                    ByReference.Create(ref copyOfArgs[i]);
            }

            try
            {
                return _invokeFunc_RefArgs!(obj, pByRefFixedStorage);
            }
            catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                throw new TargetInvocationException(e);
            }
        }

        internal unsafe object? InvokeWithManyArgs(
            object? obj,
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

            if ((_strategy & InvokerStrategy.StrategyDetermined_ObjSpanArgs) == 0)
            {
                DetermineStrategy_ObjSpanArgs(ref _strategy, ref _invokeFunc_ObjSpanArgs, _method, _needsByRefStrategy, backwardsCompat: true);
            }

            if (_invokeFunc_ObjSpanArgs is not null)
            {
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
                        ret = _invokeFunc_ObjSpanArgs(obj, copyOfArgs);
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
            }
            else
            {
                if ((_strategy & InvokerStrategy.StrategyDetermined_RefArgs) == 0)
                {
                    DetermineStrategy_RefArgs(ref _strategy, ref _invokeFunc_RefArgs, _method, backwardsCompat: true);
                }

                IntPtr* pStorage = stackalloc IntPtr[3 * _argCount];
                NativeMemory.Clear(pStorage, (nuint)(3 * _argCount) * (nuint)sizeof(IntPtr));
                copyOfArgs = new(ref Unsafe.AsRef<object?>(pStorage), _argCount);
                regArgStorage = new((void**)pStorage, (uint)_argCount, areByRefs: false);
                IntPtr* pByRefStorage = pStorage + _argCount;
                GCFrameRegistration regByRefStorage = new((void**)pByRefStorage, (uint)_argCount, areByRefs: true);
                shouldCopyBack = new Span<bool>(pStorage + _argCount * 2, _argCount);

                try
                {
                    GCFrameRegistration.RegisterForGCReporting(&regArgStorage);
                    GCFrameRegistration.RegisterForGCReporting(&regByRefStorage);

                    CheckArguments(parameters, copyOfArgs, shouldCopyBack, binder, culture, invokeAttr);

                    for (int i = 0; i < _argCount; i++)
                    {
    #pragma warning disable CS8500
                        *(ByReference*)(pByRefStorage + i) = (_invokerArgFlags[i] & InvokerArgFlags.IsValueType) != 0 ?
    #pragma warning restore CS8500
                            ByReference.Create(ref Unsafe.AsRef<object>(pStorage + i).GetRawData()) :
                            ByReference.Create(ref Unsafe.AsRef<object>(pStorage + i));
                    }

                    try
                    {
                        ret = _invokeFunc_RefArgs!(obj, pByRefStorage);
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
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvokePropertySetter(
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object? parameter,
            CultureInfo? culture)
        {
            Debug.Assert(_argCount == 1);

            object? copyOfArg = null;
            Span<object?> copyOfArgs = new(ref copyOfArg, 1);

            bool copyBack = false;
            Span<bool> shouldCopyBack = new(ref copyBack, 1); // Not used for setters

            CheckArguments(new ReadOnlySpan<object?>(in parameter), copyOfArgs, shouldCopyBack, binder, culture, invokeAttr);

            if (_invokeFunc_ObjSpanArgs is not null) // Fast path check
            {
                try
                {
                    _invokeFunc_ObjSpanArgs(obj, copyOfArgs);
                }
                catch (Exception e) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
                {
                    throw new TargetInvocationException(e);
                }
            }
            else
            {
                if ((_strategy & InvokerStrategy.StrategyDetermined_ObjSpanArgs) == 0)
                {
                    // Initialize for next time.
                    DetermineStrategy_ObjSpanArgs(ref _strategy, ref _invokeFunc_ObjSpanArgs, _method, _needsByRefStrategy, backwardsCompat: true);
                }

                InvokeDirectByRefWithFewArgs(obj, copyOfArgs, invokeAttr);
            }
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
        internal void CheckArguments(
             ReadOnlySpan<object?> parameters,
             Span<object?> copyOfParameters,
             Span<bool> shouldCopyBack,
             Binder? binder,
             CultureInfo? culture,
             BindingFlags invokeAttr
         )
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                object? arg = parameters[i];
                RuntimeType sigType = _argTypes[i];

                // Convert a Type.Missing to the default value.
                if (ReferenceEquals(arg, Type.Missing))
                {
                    arg = HandleTypeMissing(_method.GetParametersAsSpan()[i], sigType);
                    shouldCopyBack[i] = true;
                }

                // Convert the type if necessary.
                if (arg is null)
                {
                    if ((_invokerArgFlags[i] & InvokerArgFlags.IsValueType_ByRef_Or_Pointer) != 0)
                    {
                        shouldCopyBack[i] = sigType.CheckValue(ref arg, binder, culture, invokeAttr);
                    }
                }
                else if (!ReferenceEquals(arg.GetType(), sigType))
                {
                    // Determine if we can use the fast path for byref types.
                    if (TryByRefFastPath(sigType, ref arg))
                    {
                        // Fast path when the value's type matches the signature type of a byref parameter.
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

        private static bool TryByRefFastPath(RuntimeType type, ref object arg)
        {
            if (RuntimeType.TryGetByRefElementType(type, out RuntimeType? sigElementType) &&
                ReferenceEquals(sigElementType, arg.GetType()))
            {
                if (sigElementType.IsValueType)
                {
                    // Make a copy to prevent the boxed instance from being directly modified by the method.
                    arg = RuntimeType.AllocateValueType(sigElementType, arg);
                }

                return true;
            }

            return false;
        }
    }
}
