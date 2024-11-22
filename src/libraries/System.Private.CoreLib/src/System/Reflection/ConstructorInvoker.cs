// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.InvokeSignatureInfo;
using static System.Reflection.MethodBase;
using static System.Reflection.MethodInvokerCommon;
using static System.RuntimeType;

namespace System.Reflection
{
    /// <summary>
    /// Invokes the method reflected by the provided <see cref="ConstructorInfo"/>.
    /// </summary>
    /// <remarks>
    /// Used for better performance than <seealso cref="ConstructorInfo.Invoke"/> when compatibility with that method
    /// is not necessary and when the caller can cache the ConstructorInvoker instance for additional invoke calls.<br/>
    /// Unlike <see cref="ConstructorInfo.Invoke"/>, the invoke methods do not look up default values for arguments when
    /// <see cref="Type.Missing"/> is specified. In addition, the target constructor may be inlined for performance and not
    /// appear in stack traces.
    /// </remarks>
    /// <seealso cref="MethodInvoker"/>
    public sealed partial class ConstructorInvoker
    {
        private readonly int _argCount; // For perf, to avoid calling _signatureInfo.ParameterTypes.Length in fast path.
        private readonly RuntimeType _declaringType;
        private readonly IntPtr _functionPointer;
        private readonly Delegate _invokeFunc; // todo: use GetMethodImpl and fcnptr?
        private readonly InvokerArgFlags[] _invokerArgFlags;
        private readonly RuntimeConstructorInfo _method;
        private readonly RuntimeType[] _parameterTypes;
        private readonly InvokerStrategy _strategy;

        /// <summary>
        /// Creates a new instance of ConstructorInvoker.
        /// </summary>
        /// <remarks>
        /// For performance, the resulting instance should be cached for additional calls.
        /// </remarks>
        /// <param name="constructor">The constructor that will be invoked.</param>
        /// <returns>An instance of a ConstructorInvoker.</returns>
        /// <exception cref="ArgumentException">
        /// The <paramref name="constructor"/> is not a runtime-based method.
        /// </exception>
        public static ConstructorInvoker Create(ConstructorInfo constructor)
        {
            ArgumentNullException.ThrowIfNull(constructor, nameof(constructor));

            if (constructor is not RuntimeConstructorInfo runtimeConstructor)
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeConstructorInfo, nameof(constructor));
            }

            return new ConstructorInvoker(runtimeConstructor);
        }

        private ConstructorInvoker(RuntimeConstructorInfo constructor)
        {
            _method = constructor;

            if ((constructor.InvocationFlags & (InvocationFlags.NoInvoke | InvocationFlags.ContainsStackPointers | InvocationFlags.NoConstructorInvoke)) != 0)
            {
                _declaringType = null!;
                _invokeFunc = null!;
                _invokerArgFlags = null!;
                _parameterTypes = null!;
                return;
            }

            _declaringType = (RuntimeType)constructor.DeclaringType!;
            _parameterTypes = constructor.ArgumentTypes;
            _argCount = _parameterTypes.Length;

            MethodBase _ = constructor;

            Initialize(
                isForInvokerClasses: true,
                constructor,
                _parameterTypes,
                returnType: typeof(void),
                out _functionPointer,
                out _invokeFunc!,
                out _strategy,
                out _invokerArgFlags);

            _invokeFunc ??= CreateInvokeDelegateForInterpreted();

            if (_functionPointer != IntPtr.Zero)
            {
#if MONO
                _shouldAllocate = true;
#else
                _allocator = _declaringType.GetOrCreateCacheEntry<CreateUninitializedCache>();
#endif
            }
        }

        /// <summary>
        /// Invokes the constructor.
        /// </summary>
        /// <returns>
        /// An instance of the class associated with the constructor.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The type that declares the method is an open generic type.
        /// </exception>
        /// <exception cref="TargetParameterCountException">
        /// The correct number of arguments were not provided.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The calling convention or signature is not supported.
        /// </exception>
        public object Invoke()
        {
            if (_invokeFunc is null)
            {
                _method.ThrowNoInvokeException();
            }

            if (_argCount != 0)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            if (ShouldAllocate)
            {
                object obj = CreateUninitializedObject();
                ((InvokeFunc_Obj0Args)_invokeFunc)(obj, _functionPointer);
                return obj;
            }

            if (_strategy == InvokerStrategy.Ref4)
            {
                return InvokeWithRefArgs4(Span<object?>.Empty);
            }

            return ((InvokeFunc_Obj0Args)_invokeFunc)(obj: null, _functionPointer)!;
        }

        /// <summary>
        /// Invokes the constructor using the specified arguments.
        /// </summary>
        /// <inheritdoc cref="Invoke()"/>
        /// <param name="arg1">The first argument for the invoked method.</param>
        /// <exception cref="ArgumentException">
        /// The arguments do not match the signature of the invoked constructor.
        /// </exception>
        public object Invoke(object? arg1)
        {
            if (_invokeFunc is null)
            {
                _method.ThrowNoInvokeException();
            }

            if (_argCount != 1)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            CheckArgument(ref arg1, 0);

            if (_strategy == InvokerStrategy.Ref4)
            {
                return InvokeWithRefArgs4(arg1);
            }

            if (ShouldAllocate)
            {
                object obj = CreateUninitializedObject();
                ((InvokeFunc_Obj1Arg)_invokeFunc)(obj, _functionPointer, arg1);
                return obj;
            }

            return ((InvokeFunc_Obj1Arg)_invokeFunc)(obj: null, _functionPointer, arg1)!;
        }

        /// <inheritdoc cref="Invoke(object?)"/>
        /// <param name="arg1">The first argument for the invoked method.</param>
        /// <param name="arg2">The second argument for the invoked method.</param>
        public object Invoke(object? arg1, object? arg2)
        {
            if (_invokeFunc is null)
            {
                _method.ThrowNoInvokeException();
            }

            if (_argCount != 2)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            if (_strategy == InvokerStrategy.Ref4)
            {
                return InvokeWithRefArgs4(arg1, arg2);
            }

            return InvokeImpl(arg1, arg2, null, null);
        }

        /// <inheritdoc cref="Invoke(object?)"/>
        /// <param name="arg1">The first argument for the invoked method.</param>
        /// <param name="arg2">The second argument for the invoked method.</param>
        /// <param name="arg3">The third argument for the invoked method.</param>
        public object Invoke(object? arg1, object? arg2, object? arg3)
        {
            if (_invokeFunc is null)
            {
                _method.ThrowNoInvokeException();
            }

            if (_argCount != 3)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            if (_strategy == InvokerStrategy.Ref4)
            {
                return InvokeWithRefArgs4(arg1, arg2, arg3);
            }

            return InvokeImpl(arg1, arg2, arg3, null);
        }

        /// <inheritdoc cref="Invoke(object?)"/>
        /// <param name="arg1">The first argument for the invoked method.</param>
        /// <param name="arg2">The second argument for the invoked method.</param>
        /// <param name="arg3">The third argument for the invoked method.</param>
        /// <param name="arg4">The fourth argument for the invoked method.</param>
        public object Invoke(object? arg1, object? arg2, object? arg3, object? arg4)
        {
            if (_invokeFunc is null)
            {
                _method.ThrowNoInvokeException();
            }

            if (_argCount != 4)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            if (_strategy == InvokerStrategy.Ref4)
            {
                return InvokeWithRefArgs4(arg1, arg2, arg3, arg4);
            }

            return InvokeImpl(arg1, arg2, arg3, arg4);
        }

        private object InvokeImpl(object? arg1, object? arg2, object? arg3, object? arg4)
        {
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

            if (ShouldAllocate)
            {
                object obj = CreateUninitializedObject();
                ((InvokeFunc_Obj4Args)_invokeFunc)(obj, _functionPointer, arg1, arg2, arg3, arg4);
                return obj;
            }

            return ((InvokeFunc_Obj4Args)_invokeFunc)(obj: null, _functionPointer, arg1, arg2, arg3, arg4)!;
        }

        /// <inheritdoc cref="Invoke(object?)"/>
        /// <param name="arguments">The arguments for the invoked constructor.</param>
        /// <exception cref="ArgumentException">
        /// The arguments do not match the signature of the invoked constructor.
        /// </exception>
        public object Invoke(Span<object?> arguments)
        {
            if (_invokeFunc is null)
            {
                _method.ThrowNoInvokeException();
            }

            if (arguments.Length != _argCount)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            switch (_strategy)
            {
                case InvokerStrategy.Obj0:
                    if (ShouldAllocate)
                    {
                        object obj = CreateUninitializedObject();
                        ((InvokeFunc_Obj0Args)_invokeFunc)(obj, _functionPointer);
                        return obj;
                    }
                    return ((InvokeFunc_Obj0Args)_invokeFunc)(obj: null, _functionPointer)!;
                case InvokerStrategy.Obj1:
                    object? arg1 = arguments[0];
                    CheckArgument(ref arg1, 0);

                    if (ShouldAllocate)
                    {
                        object obj = CreateUninitializedObject();
                        ((InvokeFunc_Obj1Arg)_invokeFunc)(obj, _functionPointer, arg1);
                        return obj;
                    }
                    return ((InvokeFunc_Obj1Arg)_invokeFunc)(obj: null, _functionPointer, arg1)!;
                case InvokerStrategy.Obj4:
                    switch (_argCount)
                    {
                        case 2:
                            return InvokeImpl(arguments[0], arguments[1], null, null);
                        case 3:
                            return InvokeImpl(arguments[0], arguments[1], arguments[2], null);
                        default:
                            Debug.Assert(_argCount == 4);
                            return InvokeImpl(arguments[0], arguments[1], arguments[2], arguments[3]);
                    }
                case InvokerStrategy.ObjSpan:
                    return InvokeWithSpanArgs(arguments);
                case InvokerStrategy.Ref4:
                    return InvokeWithRefArgs4(arguments);
                default:
                    Debug.Assert(_strategy == InvokerStrategy.RefMany);
                    return InvokeWithRefArgsMany(arguments);
            }
        }

        // Version with no copy-back.
        private unsafe object InvokeWithSpanArgs(Span<object?> arguments)
        {
            if (_invokeFunc is null)
            {
                _method.ThrowNoInvokeException();
            }

            object? obj;

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

                if (ShouldAllocate)
                {
                    obj = CreateUninitializedObject();
                    ((InvokeFunc_ObjSpanArgs)_invokeFunc)(obj, _functionPointer, copyOfArgs);
                }
                else
                {
                    obj = ((InvokeFunc_ObjSpanArgs)_invokeFunc)(obj: null, _functionPointer, copyOfArgs)!;
                }

                // No need to call CopyBack here since there are no ref values.
            }
            finally
            {
                GCFrameRegistration.UnregisterForGCReporting(&regArgStorage);
            }

            return obj;
        }

        // Version with no copy-back
        private unsafe object InvokeWithRefArgs4(object? arg1, object? arg2 = null, object? arg3 = null, object? arg4 = null)
        {
            if (_invokeFunc is null)
            {
                _method.ThrowNoInvokeException();
            }

            StackAllocatedArguments stackStorage = new(arg1, arg2, arg3, arg4);
            Span<object?> arguments = ((Span<object?>)(stackStorage._args)).Slice(0, _argCount);
            StackAllocatedByRefs byrefs = default;
            IntPtr* pByRefFixedStorage = (IntPtr*)&byrefs;

            for (int i = 0; i < _argCount; i++)
            {
                CheckArgument(ref arguments[i], i);

                *(ByReference*)(pByRefFixedStorage + i) = (_invokerArgFlags[i] & InvokerArgFlags.IsValueType) != 0 ?
                    ByReference.Create(ref arguments[i]!.GetRawData()) :
#pragma warning disable CS9080
                    ByReference.Create(ref arguments[i]);
#pragma warning restore CS9080
            }

            object obj;
            if (ShouldAllocate)
            {
                obj = CreateUninitializedObject();
                ((InvokeFunc_RefArgs)_invokeFunc)(obj, _functionPointer, pByRefFixedStorage);
            }
            else
            {
                obj = ((InvokeFunc_RefArgs)_invokeFunc)(obj: null, _functionPointer, pByRefFixedStorage)!;
            }

            return obj;
        }

        private unsafe object InvokeWithRefArgs4(Span<object?> arguments)
        {
            if (_invokeFunc is null)
            {
                _method.ThrowNoInvokeException();
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

            object obj;
            if (ShouldAllocate)
            {
                obj = CreateUninitializedObject();
                ((InvokeFunc_RefArgs)_invokeFunc)(obj, _functionPointer, pByRefFixedStorage);
            }
            else
            {
                obj = ((InvokeFunc_RefArgs)_invokeFunc)(obj: null, _functionPointer, pByRefFixedStorage)!;
            }

            CopyBack(arguments, copyOfArgs, shouldCopyBack);
            return obj;
        }

        private unsafe object InvokeWithRefArgsMany(Span<object?> arguments)
        {
            if (_invokeFunc is null)
            {
                _method.ThrowNoInvokeException();
            }

            object? obj;

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

                if (ShouldAllocate)
                {
                    obj = CreateUninitializedObject();
                    ((InvokeFunc_RefArgs)_invokeFunc)(obj, _functionPointer, pByRefStorage);
                }
                else
                {
                    obj = ((InvokeFunc_RefArgs)_invokeFunc)(obj: null, _functionPointer, pByRefStorage)!;
                }

                CopyBack(arguments, copyOfArgs, shouldCopyBack);
            }
            finally
            {
                GCFrameRegistration.UnregisterForGCReporting(&regByRefStorage);
                GCFrameRegistration.UnregisterForGCReporting(&regArgStorage);
            }

            return obj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Copy modified values out. This is only done with ByRef arguments.
        private void CopyBack(Span<object?> dest, ReadOnlySpan<object?> copyOfParameters, ReadOnlySpan<bool> shouldCopyBack)
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
    }
}
