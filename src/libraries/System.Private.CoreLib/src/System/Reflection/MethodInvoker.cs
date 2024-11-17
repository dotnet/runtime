// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.InvokeSignatureInfo;
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
        private readonly int _argCount; // For perf, to avoid calling _signatureInfo.ParameterTypes.Length in fast path.
        private readonly IntPtr _functionPointer;
        private readonly Delegate? _invokeFunc;
        private readonly InvokerArgFlags[] _invokerArgFlags;
        private readonly RuntimeType[] _parameterTypes;
        private readonly MethodBase _method;
        private readonly InvokerStrategy _strategy;

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
                return new MethodInvoker(rmi, rmi.ArgumentTypes, rmi.ReturnType, rmi.InvocationFlags);
            }

            if (method is RuntimeConstructorInfo rci)
            {
                return new MethodInvoker(rci, rci.ArgumentTypes, typeof(void), rci.InvocationFlags);
            }

            if (method is DynamicMethod dm)
            {
                return new MethodInvoker(dm, dm.ArgumentTypes, dm.ReturnType, InvocationFlags.Unknown);
            }

            throw new ArgumentException(SR.Argument_MustBeRuntimeMethod, nameof(method));
        }

        private MethodInvoker(MethodBase method, RuntimeType[] parameterTypes, Type returnType, InvocationFlags invocationFlags)
        {
            _method = method;

            if ((invocationFlags & (InvocationFlags.NoInvoke | InvocationFlags.ContainsStackPointers)) != 0)
            {
                _invokeFunc = null!;
                _invokerArgFlags = null!;
                _parameterTypes = null!;
                return;
            }

            _parameterTypes = parameterTypes;
            _argCount = _parameterTypes.Length;

            Initialize(
                isForInvokerClasses: true,
                method,
                _parameterTypes,
                returnType,
                out _functionPointer,
                out _invokeFunc!,
                out _strategy,
                out _invokerArgFlags);

            _invokeFunc ??= CreateInvokeDelegateForInterpreted();
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
            if (_invokeFunc is null)
            {
                ThrowNoInvokeException();
            }

            if (!_method.IsStatic)
            {
                ValidateInvokeTarget(obj, _method);
            }

            if (_argCount != 0)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            if (_strategy == InvokerStrategy.Ref4)
            {
                return InvokeWithRefArgs4(obj, Span<object?>.Empty);
            }

            return ((InvokeFunc_Obj0Args)_invokeFunc!)(obj, _functionPointer);
        }

        /// <inheritdoc cref="Invoke(object?)"/>
        /// <param name="obj"> The object on which to invoke the method. If the method is static, this argument is ignored. </param>
        /// <param name="arg1">The first argument for the invoked method.</param>
        /// <exception cref="ArgumentException">
        /// The arguments do not match the signature of the invoked method.
        /// </exception>
        public object? Invoke(object? obj, object? arg1)
        {
            if (_invokeFunc is null)
            {
                ThrowNoInvokeException();
            }

            if (!_method.IsStatic)
            {
                ValidateInvokeTarget(obj, _method);
            }

            if (_argCount != 1)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            if (_strategy == InvokerStrategy.Ref4)
            {
                return InvokeWithRefArgs4(obj, new Span<object?>(new object?[] { arg1 }));
            }

            CheckArgument(ref arg1, 0);
            return ((InvokeFunc_Obj1Arg)_invokeFunc!)(obj, _functionPointer, arg1);
        }

        /// <inheritdoc cref="Invoke(object?)"/>
        /// <param name="obj"> The object on which to invoke the method. If the method is static, this argument is ignored. </param>
        /// <param name="arg1">The first argument for the invoked method.</param>
        /// <param name="arg2">The second argument for the invoked method.</param>
        public object? Invoke(object? obj, object? arg1, object? arg2)
        {
            if (_invokeFunc is null)
            {
                ThrowNoInvokeException();
            }

            if (_argCount != 2)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            if (_strategy == InvokerStrategy.Ref4)
            {
                return InvokeWithRefArgs4(obj, new Span<object?>(new object?[] { arg1, arg2 }));
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
            if (_invokeFunc is null)
            {
                ThrowNoInvokeException();
            }

            if (_argCount != 3)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            if (_strategy == InvokerStrategy.Ref4)
            {
                return InvokeWithRefArgs4(obj, new Span<object?>(new object?[] { arg1, arg2, arg3 }));
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
            if (_invokeFunc is null)
            {
                ThrowNoInvokeException();
            }

            if (_argCount != 4)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            if (_strategy == InvokerStrategy.Ref4)
            {
                return InvokeWithRefArgs4(obj, new Span<object?>(new object?[] { arg1, arg2, arg3, arg4 }));
            }

            return InvokeImpl(obj, arg1, arg2, arg3, arg4);
        }

        private object? InvokeImpl(object? obj, object? arg1, object? arg2, object? arg3, object? arg4)
        {
            if (!_method.IsStatic)
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

            return ((InvokeFunc_Obj4Args)_invokeFunc!)(obj, _functionPointer, arg1, arg2, arg3, arg4);
        }

        /// <inheritdoc cref="Invoke(object?)"/>
        /// <param name="obj"> The object on which to invoke the method. If the method is static, this argument is ignored. </param>
        /// <param name="arguments">The arguments for the invoked method.</param>
        /// <exception cref="ArgumentException">
        /// The arguments do not match the signature of the invoked method.
        /// </exception>
        public object? Invoke(object? obj, Span<object?> arguments)
        {
            if (_invokeFunc is null)
            {
                ThrowNoInvokeException();
            }

            if (!_method.IsStatic)
            {
                ValidateInvokeTarget(obj, _method);
            }

            if (arguments.Length != _argCount)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            switch (_strategy)
            {
                case InvokerStrategy.Obj0:
                    return ((InvokeFunc_Obj0Args)_invokeFunc!)(obj, _functionPointer);
                case InvokerStrategy.Obj1:
                    object? arg1 = arguments[0];
                    CheckArgument(ref arg1, 0);
                    return ((InvokeFunc_Obj1Arg)_invokeFunc!)(obj, _functionPointer, arg1);
                case InvokerStrategy.Obj4:
                    switch (_argCount)
                    {
                        case 2:
                            return InvokeImpl(obj, arguments[0], arguments[1], null, null);
                        case 3:
                            return InvokeImpl(obj, arguments[0], arguments[1], arguments[2], null);
                        default:
                            Debug.Assert(_argCount == 4);
                            return InvokeImpl(obj, arguments[0], arguments[1], arguments[2], arguments[3]);
                    }
                case InvokerStrategy.ObjSpan:
                    return InvokeWithSpanArgs(obj, arguments);
                case InvokerStrategy.Ref4:
                    return InvokeWithRefArgs4(obj, arguments);
                default:
                    Debug.Assert(_strategy == InvokerStrategy.RefMany);
                    return InvokeWithRefArgsMany(obj, arguments);
            }
        }

        [DoesNotReturn]
        private void ThrowForBadInvocationFlags()
        {
            if (_method is RuntimeMethodInfo rmi)
            {
                rmi.ThrowNoInvokeException();
            }

            Debug.Assert(_method is RuntimeConstructorInfo);
            ((RuntimeConstructorInfo)_method).ThrowNoInvokeException();
        }

        internal unsafe object? InvokeWithSpanArgs(object? obj, Span<object?> arguments)
        {
            if (_invokeFunc is null)
            {
                ThrowForBadInvocationFlags();
            }

            Span<object?> copyOfArgs;
            GCFrameRegistration regArgStorage;

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

                return ((InvokeFunc_ObjSpanArgs)_invokeFunc)(obj, _functionPointer, copyOfArgs);
                // No need to call CopyBack here since there are no ref values.
            }
            finally
            {
                GCFrameRegistration.UnregisterForGCReporting(&regArgStorage);
            }
        }

        internal unsafe object? InvokeWithRefArgs4(object? obj, Span<object?> arguments)
        {
            if (_invokeFunc is null)
            {
                ThrowForBadInvocationFlags();
            }

            Debug.Assert(_argCount <= MaxStackAllocArgCount);

            StackAllocatedArgumentsWithCopyBack stackArgStorage = default;
            Span<object?> copyOfArgs = ((Span<object?>)stackArgStorage._args).Slice(0, _argCount);
            Span<bool> shouldCopyBack = ((Span<bool>)stackArgStorage._shouldCopyBack).Slice(0, _argCount);
            StackAllocatedByRefs byrefs = default;
            IntPtr* pByRefFixedStorage = (IntPtr*)&byrefs;

            for (int i = 0; i < _argCount; i++)
            {
                object? arg = arguments[i];
                shouldCopyBack[i] = CheckArgument(ref arg, i);
                copyOfArgs[i] = arg;

                *(ByReference*)(pByRefFixedStorage + i) = (_invokerArgFlags[i] & InvokerArgFlags.IsValueType) != 0 ?
                    ByReference.Create(ref copyOfArgs[i]!.GetRawData()) :
#pragma warning disable CS9080
                    ByReference.Create(ref copyOfArgs[i]);
#pragma warning restore CS9080
            }

            object? ret = ((InvokeFunc_RefArgs)_invokeFunc)(obj, _functionPointer, pByRefFixedStorage);
            CopyBack(arguments, copyOfArgs, shouldCopyBack);
            return ret;
        }

        internal unsafe object? InvokeWithRefArgsMany(object? obj, Span<object?> arguments)
        {
            if (_invokeFunc is null)
            {
                ThrowForBadInvocationFlags();
            }

            Span<object?> copyOfArgs;
            GCFrameRegistration regArgStorage;

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
                    *(ByReference*)(pByRefStorage + i) = (_invokerArgFlags[i] & InvokerArgFlags.IsValueType) != 0 ?
                        ByReference.Create(ref Unsafe.AsRef<object>(pStorage + i).GetRawData()) :
                        ByReference.Create(ref Unsafe.AsRef<object>(pStorage + i));
                }

                object? ret = ((InvokeFunc_RefArgs)_invokeFunc)(obj, _functionPointer, pByRefStorage);
                CopyBack(arguments, copyOfArgs, shouldCopyBack);
                return ret;
            }
            finally
            {
                GCFrameRegistration.UnregisterForGCReporting(&regByRefStorage);
                GCFrameRegistration.UnregisterForGCReporting(&regArgStorage);
            }
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
            RuntimeType sigType = (RuntimeType)_parameterTypes[i];

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

        [DoesNotReturn]
        private void ThrowNoInvokeException()
        {
            if (_method is RuntimeMethodInfo rmi)
            {
                rmi.ThrowNoInvokeException();
            }

            Debug.Assert(_method is RuntimeConstructorInfo);
            ((RuntimeConstructorInfo)_method).ThrowNoInvokeException();
        }
    }
}
