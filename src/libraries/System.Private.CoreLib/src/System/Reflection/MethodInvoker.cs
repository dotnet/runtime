// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;
using static System.Reflection.MethodInvokerCommon;

namespace System.Reflection
{
    /// <summary>
    /// Invokes the method reflected by the provided <see cref="MethodBase"/>.
    /// </summary>
    /// <remarks>
    /// Used for better performance than <seealso cref="MethodBase.Invoke"/> when compatibility with that method
    /// is not necessary and when the caller can cache the MethodInvoker instance for additional invoke calls.<br/>
    /// Unlike <see cref="MethodBase.Invoke"/>, the invoke methods do not look up default values for arguments when
    /// <see cref="Type.Missing"/> is specified. In addition, the target method may be inlined for performance and not
    /// appear in stack traces.
    /// </remarks>
    /// <seealso cref="ConstructorInvoker"/>
    public sealed partial class MethodInvoker
    {
        private InvokeFunc_ObjSpanArgs? _invokeFunc_ObjSpanArgs;
        private InvokeFunc_Obj4Args? _invokeFunc_Obj4Args;
        private InvokeFunc_RefArgs? _invokeFunc_RefArgs;
        private InvokerStrategy _strategy;
        private readonly int _argCount;
        private readonly RuntimeType[] _argTypes;
        private readonly InvocationFlags _invocationFlags;
        private readonly InvokerArgFlags[] _invokerArgFlags;
        private readonly MethodBase _method;
        private readonly bool _needsByRefStrategy;
        private readonly bool _isStatic;

        /// <summary>
        /// Creates a new instance of MethodInvoker.
        /// </summary>
        /// <remarks>
        /// For performance, the resulting instance should be cached for additional calls.
        /// </remarks>
        /// <param name="method">The method that will be invoked.</param>
        /// <returns>An instance of a MethodInvoker.</returns>
        /// <exception cref="ArgumentException">
        /// The <paramref name="method"/> is not a runtime-based method.
        /// </exception>
        public static MethodInvoker Create(MethodBase method)
        {
            ArgumentNullException.ThrowIfNull(method, nameof(method));

            if (method is RuntimeMethodInfo rmi)
            {
                return new MethodInvoker(rmi);
            }

            if (method is DynamicMethod dm)
            {
                return new MethodInvoker(dm);
            }

            if (method is RuntimeConstructorInfo rci)
            {
                // This is useful for calling a constructor on an already-initialized object
                // such as created from RuntimeHelpers.GetUninitializedObject(Type).
                MethodInvoker invoker = new MethodInvoker(rci);

                // Use the interpreted version to avoid having to generate a new method that doesn't allocate.
                invoker._strategy = GetStrategyForUsingInterpreted();

                return invoker;
            }

            throw new ArgumentException(SR.Argument_MustBeRuntimeMethod, nameof(method));
        }

        private MethodInvoker(MethodBase method, RuntimeType[] argumentTypes)
        {
            _method = method;
            _argTypes = argumentTypes;
            _argCount = _argTypes.Length;
            _isStatic = _method.IsStatic;

            Initialize(argumentTypes, out _strategy, out _invokerArgFlags, out _needsByRefStrategy);
        }

        /// <summary>
        /// Invokes the method using the specified parameters.
        /// </summary>
        /// <param name="obj">
        /// The object on which to invoke the method. If the method is static, this argument is ignored.
        /// </param>
        /// <returns>
        /// An object containing the return value of the invoked method,
        /// or <c>null</c> if the invoked method does not have a return value.
        /// </returns>
        /// <exception cref="TargetException">
        /// The <para>obj</para> parameter is <c>null</c> and the method is not static.
        ///
        /// -or-
        ///
        /// The method is not declared or inherited by the class of <para>obj</para>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The type that declares the method is an open generic type.
        /// </exception>
        /// <exception cref="TargetParameterCountException">
        /// The correct number of arguments were not provided.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The calling convention or signature is not supported.
        /// </exception>
        public object? Invoke(object? obj)
        {
            if (_argCount != 0)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            return InvokeImpl(obj, null, null, null, null);
        }

        /// <inheritdoc cref="Invoke(object?)"/>
        /// <param name="obj"> The object on which to invoke the method. If the method is static, this argument is ignored. </param>
        /// <param name="arg1">The first argument for the invoked method.</param>
        /// <exception cref="ArgumentException">
        /// The arguments do not match the signature of the invoked method.
        /// </exception>
        public object? Invoke(object? obj, object? arg1)
        {
            if (_argCount != 1)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            return InvokeImpl(obj, arg1, null, null, null);
        }

        /// <inheritdoc cref="Invoke(object?)"/>
        /// <param name="obj"> The object on which to invoke the method. If the method is static, this argument is ignored. </param>
        /// <param name="arg1">The first argument for the invoked method.</param>
        /// <param name="arg2">The second argument for the invoked method.</param>
        public object? Invoke(object? obj, object? arg1, object? arg2)
        {
            if (_argCount != 2)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            return InvokeImpl(obj, arg1, arg2, null, null);
        }

        /// <inheritdoc cref="Invoke(object?)"/>
        /// <param name="obj"> The object on which to invoke the method. If the method is static, this argument is ignored. </param>
        /// <param name="arg1">The first argument for the invoked method.</param>
        /// <param name="arg2">The second argument for the invoked method.</param>
        /// <param name="arg3">The third argument for the invoked method.</param>
        public object? Invoke(object? obj, object? arg1, object? arg2, object? arg3)
        {
            if (_argCount != 3)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            return InvokeImpl(obj, arg1, arg2, arg3, null);
        }

        /// <inheritdoc cref="Invoke(object?)"/>
        /// <param name="obj"> The object on which to invoke the method. If the method is static, this argument is ignored. </param>
        /// <param name="arg1">The first argument for the invoked method.</param>
        /// <param name="arg2">The second argument for the invoked method.</param>
        /// <param name="arg3">The third argument for the invoked method.</param>
        /// <param name="arg4">The fourth argument for the invoked method.</param>
        public object? Invoke(object? obj, object? arg1, object? arg2, object? arg3, object? arg4)
        {
            if (_argCount != 4)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            return InvokeImpl(obj, arg1, arg2, arg3, arg4);
        }

        private object? InvokeImpl(object? obj, object? arg1, object? arg2, object? arg3, object? arg4)
        {
            if ((_invocationFlags & (InvocationFlags.NoInvoke | InvocationFlags.ContainsStackPointers | InvocationFlags.NoConstructorInvoke)) != 0)
            {
                ThrowForBadInvocationFlags();
            }

            if (!_isStatic)
            {
                ValidateInvokeTarget(obj, _method);
            }

            switch (_argCount)
            {
                case 4:
                    CheckArgument(ref arg4, 3);
                    goto case 3;
                case 3:
                    CheckArgument(ref arg3, 2);
                    goto case 2;
                case 2:
                    CheckArgument(ref arg2, 1);
                    goto case 1;
                case 1:
                    CheckArgument(ref arg1, 0);
                    break;
            }

            // Check fast path first.
            if (_invokeFunc_Obj4Args is not null)
            {
                return _invokeFunc_Obj4Args(obj, arg1, arg2, arg3, arg4);
            }

            if ((_strategy & InvokerStrategy.StrategyDetermined_Obj4Args) == 0)
            {
                DetermineStrategy_Obj4Args(ref _strategy, ref _invokeFunc_Obj4Args, _method, _needsByRefStrategy, backwardsCompat: false);
                if (_invokeFunc_Obj4Args is not null)
                {
                    return _invokeFunc_Obj4Args(obj, arg1, arg2, arg3, arg4);
                }
            }

            return InvokeDirectByRef(obj, arg1, arg2, arg3, arg4);
        }

        /// <inheritdoc cref="Invoke(object?)"/>
        /// <param name="obj"> The object on which to invoke the method. If the method is static, this argument is ignored. </param>
        /// <param name="arguments">The arguments for the invoked method.</param>
        /// <exception cref="ArgumentException">
        /// The arguments do not match the signature of the invoked method.
        /// </exception>
        public object? Invoke(object? obj, Span<object?> arguments)
        {
            int argLen = arguments.Length;
            if (argLen != _argCount)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            if (!_needsByRefStrategy)
            {
                // Switch to fast path if possible.
                switch (_argCount)
                {
                    case 0:
                        return InvokeImpl(obj, null, null, null, null);
                    case 1:
                        return InvokeImpl(obj, arguments[0], null, null, null);
                    case 2:
                        return InvokeImpl(obj, arguments[0], arguments[1], null, null);
                    case 3:
                        return InvokeImpl(obj, arguments[0], arguments[1], arguments[2], null);
                    case 4:
                        return InvokeImpl(obj, arguments[0], arguments[1], arguments[2], arguments[3]);
                    default:
                        break;
                }
            }

            if ((_invocationFlags & (InvocationFlags.NoInvoke | InvocationFlags.ContainsStackPointers)) != 0)
            {
                ThrowForBadInvocationFlags();
            }

            if (!_isStatic)
            {
                ValidateInvokeTarget(obj, _method);
            }

            if (argLen > MaxStackAllocArgCount)
            {
                return InvokeWithManyArgs(obj, arguments);
            }

            return InvokeWithFewArgs(obj, arguments);
        }

        private void ThrowForBadInvocationFlags()
        {
            if (_method is RuntimeMethodInfo rmi)
            {
                rmi.ThrowNoInvokeException();
            }

            Debug.Assert(_method is RuntimeConstructorInfo);
            ((RuntimeConstructorInfo)_method).ThrowNoInvokeException();
        }

        internal object? InvokeWithFewArgs(object? obj, Span<object?> arguments)
        {
            Debug.Assert(_argCount <= MaxStackAllocArgCount);

            StackAllocatedArgumentsWithCopyBack stackArgStorage = default;
            Span<object?> copyOfArgs = stackArgStorage._args.AsSpan(_argCount);
            scoped Span<bool> shouldCopyBack = stackArgStorage._shouldCopyBack.AsSpan(_argCount);

            for (int i = 0; i < _argCount; i++)
            {
                object? arg = arguments[i];
                shouldCopyBack[i] = CheckArgument(ref arg, i);
                copyOfArgs[i] = arg;
            }

            // Check fast path first.
            if (_invokeFunc_ObjSpanArgs is not null)
            {
                return _invokeFunc_ObjSpanArgs(obj, copyOfArgs);
                // No need to call CopyBack here since there are no ref values.
            }

            if ((_strategy & InvokerStrategy.StrategyDetermined_ObjSpanArgs) == 0)
            {
                DetermineStrategy_ObjSpanArgs(ref _strategy, ref _invokeFunc_ObjSpanArgs, _method, _needsByRefStrategy, backwardsCompat: false);
                if (_invokeFunc_ObjSpanArgs is not null)
                {
                    return _invokeFunc_ObjSpanArgs(obj, copyOfArgs);
                }
            }

            object? ret = InvokeDirectByRefWithFewArgs(obj, copyOfArgs);
            CopyBack(arguments, copyOfArgs, shouldCopyBack);
            return ret;
        }

        internal object? InvokeDirectByRef(object? obj, object? arg1 = null, object? arg2 = null, object? arg3 = null, object? arg4 = null)
        {
            StackAllocatedArguments stackStorage = new(arg1, arg2, arg3, arg4);
            return InvokeDirectByRefWithFewArgs(obj, stackStorage._args.AsSpan(_argCount));
        }

        internal unsafe object? InvokeDirectByRefWithFewArgs(object? obj, Span<object?> copyOfArgs)
        {
            if ((_strategy & InvokerStrategy.StrategyDetermined_RefArgs) == 0)
            {
                DetermineStrategy_RefArgs(ref _strategy, ref _invokeFunc_RefArgs, _method, backwardsCompat: false);
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

            return _invokeFunc_RefArgs!(obj, pByRefFixedStorage);
        }

        internal unsafe object? InvokeWithManyArgs(object? obj, Span<object?> arguments)
        {
            Span<object?> copyOfArgs;
            GCFrameRegistration regArgStorage;
            object? ret;

            if ((_strategy & InvokerStrategy.StrategyDetermined_ObjSpanArgs) == 0)
            {
                DetermineStrategy_ObjSpanArgs(ref _strategy, ref _invokeFunc_ObjSpanArgs, _method, _needsByRefStrategy, backwardsCompat: false);
            }

            if (_invokeFunc_ObjSpanArgs is not null)
            {
                IntPtr* pArgStorage = stackalloc IntPtr[_argCount];
                NativeMemory.Clear(pArgStorage, (nuint)_argCount * (nuint)sizeof(IntPtr));
                copyOfArgs = new(ref Unsafe.AsRef<object?>(pArgStorage), _argCount);
                regArgStorage = new((void**)pArgStorage, (uint)_argCount, areByRefs: false);

                try
                {
                    GCFrameRegistration.RegisterForGCReporting(&regArgStorage);

                    for (int i = 0; i < _argCount; i++)
                    {
                        object? arg = arguments[i];
                        CheckArgument(ref arg, i);
                        copyOfArgs[i] = arg;
                    }

                    ret = _invokeFunc_ObjSpanArgs(obj, copyOfArgs);
                    // No need to call CopyBack here since there are no ref values.
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
                    DetermineStrategy_RefArgs(ref _strategy, ref _invokeFunc_RefArgs, _method, backwardsCompat: false);
                }

                IntPtr* pStorage = stackalloc IntPtr[2 * _argCount];
                NativeMemory.Clear(pStorage, (nuint)(2 * _argCount) * (nuint)sizeof(IntPtr));
                copyOfArgs = new(ref Unsafe.AsRef<object?>(pStorage), _argCount);

                IntPtr* pByRefStorage = pStorage + _argCount;
                scoped Span<bool> shouldCopyBack = stackalloc bool[_argCount];

                regArgStorage = new((void**)pStorage, (uint)_argCount, areByRefs: false);
                GCFrameRegistration regByRefStorage = new((void**)pByRefStorage, (uint)_argCount, areByRefs: true);

                try
                {
                    GCFrameRegistration.RegisterForGCReporting(&regArgStorage);
                    GCFrameRegistration.RegisterForGCReporting(&regByRefStorage);

                    for (int i = 0; i < _argCount; i++)
                    {
                        object? arg = arguments[i];
                        shouldCopyBack[i] = CheckArgument(ref arg, i);
                        copyOfArgs[i] = arg;
    #pragma warning disable CS8500
                        *(ByReference*)(pByRefStorage + i) = (_invokerArgFlags[i] & InvokerArgFlags.IsValueType) != 0 ?
    #pragma warning restore CS8500
                            ByReference.Create(ref Unsafe.AsRef<object>(pStorage + i).GetRawData()) :
                            ByReference.Create(ref Unsafe.AsRef<object>(pStorage + i));
                    }

                    ret = _invokeFunc_RefArgs!(obj, pByRefStorage);
                    CopyBack(arguments, copyOfArgs, shouldCopyBack);
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
        // Copy modified values out. This is only done with ByRef parameters.
        internal void CopyBack(Span<object?> dest, Span<object?> copyOfParameters, Span<bool> shouldCopyBack)
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
        private bool CheckArgument(ref object? arg, int i)
        {
            RuntimeType sigType = _argTypes[i];

            // Convert the type if necessary.
            // Note that Type.Missing is not supported.
            if (arg is null)
            {
                if ((_invokerArgFlags[i] & InvokerArgFlags.IsValueType_ByRef_Or_Pointer) != 0)
                {
                    return sigType.CheckValue(ref arg);
                }
            }
            else if (!ReferenceEquals(arg.GetType(), sigType))
            {
                return sigType.CheckValue(ref arg);
            }

            return false;
        }
    }
}
