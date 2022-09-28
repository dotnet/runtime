// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace System.Reflection
{
    internal sealed partial class RuntimeConstructorInfo : ConstructorInfo
    {
        [MethodImpl(MethodImplOptions.NoInlining)] // move lazy invocation flags population out of the hot path
        private static InvocationFlags ComputeAndUpdateInvocationFlags(ConstructorInfo constructorInfo, ref InvocationFlags flagsToUpdate)
        {
            InvocationFlags invocationFlags = InvocationFlags.IsConstructor; // this is a given

            Type? declaringType = constructorInfo.DeclaringType;

            if (declaringType == typeof(void)
                || declaringType != null && declaringType.ContainsGenericParameters  // Enclosing type has unbound generics
                || (constructorInfo.CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs // Managed varargs
                )
            {
                invocationFlags |= InvocationFlags.NoInvoke;
            }
            else if (constructorInfo.IsStatic)
            {
                invocationFlags |= InvocationFlags.RunClassConstructor | InvocationFlags.NoConstructorInvoke;
            }
            else if (declaringType != null && declaringType.IsAbstract)
            {
                invocationFlags |= InvocationFlags.NoConstructorInvoke;
            }
            else
            {
                // Check for byref-like types
                if (declaringType != null && declaringType.IsByRefLike)
                    invocationFlags |= InvocationFlags.ContainsStackPointers;

                // Check for attempt to create a delegate class.
                if (typeof(Delegate).IsAssignableFrom(constructorInfo.DeclaringType))
                    invocationFlags |= InvocationFlags.IsDelegateConstructor;
            }

            invocationFlags |= InvocationFlags.Initialized;
            flagsToUpdate = invocationFlags; // accesses are guaranteed atomic
            return invocationFlags;
        }

        internal static void CheckCanCreateInstance(Type declaringType, bool isVarArg)
        {
            ArgumentNullException.ThrowIfNull(declaringType);

            // ctor is declared on interface class
            if (declaringType.IsInterface)
                throw new MemberAccessException(
                    SR.Format(SR.Acc_CreateInterfaceEx, declaringType));

            // ctor is on an abstract class
            else if (declaringType.IsAbstract)
                throw new MemberAccessException(
                    SR.Format(SR.Acc_CreateAbstEx, declaringType));

            // ctor is on a class that contains stack pointers
            else if (declaringType.GetRootElementType() == typeof(ArgIterator))
                throw new NotSupportedException();

            // ctor is vararg
            else if (isVarArg)
                throw new NotSupportedException();

            // ctor is generic or on a generic class
            else if (declaringType.ContainsGenericParameters)
            {
                throw new MemberAccessException(
                    SR.Format(SR.Acc_CreateGenericEx, declaringType));
            }

            // ctor is declared on System.Void
            else if (declaringType == typeof(void))
                throw new MemberAccessException(SR.Access_Void);
        }

        [DoesNotReturn]
        internal void ThrowNoInvokeException()
        {
            CheckCanCreateInstance(DeclaringType!, (CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs);

            // ctor is .cctor
            if ((Attributes & MethodAttributes.Static) == MethodAttributes.Static)
                throw new MemberAccessException(SR.Acc_NotClassInit);

            throw new TargetException();
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public override object? Invoke(
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[]? parameters,
            CultureInfo? culture)
        {
            if ((InvocationFlags & InvocationFlags.NoInvoke) != 0)
                ThrowNoInvokeException();

            ValidateInvokeTarget(obj);

            // Correct number of arguments supplied
            int argCount = (parameters is null) ? 0 : parameters.Length;
            if (ArgumentTypes.Length != argCount)
            {
                throw new TargetParameterCountException(SR.Arg_ParmCnt);
            }

            if ((InvocationFlags & InvocationFlags.RunClassConstructor) != 0)
            {
                // Run the class constructor through the class constructor mechanism instead of the Invoke path.
                // This avoids allowing mutation of readonly static fields, and initializes the type correctly.
                InvokeClassConstructor();
                return null;
            }

            Debug.Assert(obj != null);

            unsafe
            {
                if (argCount == 0)
                {
                    Invoker.InlinedInvoke(obj, args: default, invokeAttr);
                }
                else if (argCount > MaxStackAllocArgCount)
                {
                    Debug.Assert(parameters != null);
                    InvokeWithManyArguments(this, argCount, obj, invokeAttr, binder, parameters, culture);
                }
                else
                {
                    Debug.Assert(parameters != null);
                    StackAllocedArguments argStorage = default;
                    Span<object?> copyOfParameters = new(ref argStorage._arg0, argCount);
                    Span<ParameterCopyBackAction> shouldCopyBackParameters = new(ref argStorage._copyBack0, argCount);

                    StackAllocatedByRefs byrefStorage = default;
                    IntPtr* pByRefStorage = (IntPtr*)&byrefStorage;

                    CheckArguments(
                        copyOfParameters,
                        pByRefStorage,
                        shouldCopyBackParameters,
                        parameters,
                        ArgumentTypes,
                        binder,
                        culture,
                        invokeAttr);

#if MONO // Temporary until Mono is updated.
                    Invoker.InlinedInvoke(obj, copyOfParameters, invokeAttr);
#else
                    Invoker.InlinedInvoke(obj, pByRefStorage, invokeAttr);
#endif

                    // Copy modified values out. This should be done only with ByRef or Type.Missing parameters.
                    for (int i = 0; i < argCount; i++)
                    {
                        ParameterCopyBackAction action = shouldCopyBackParameters[i];
                        if (action != ParameterCopyBackAction.None)
                        {
                            if (action == ParameterCopyBackAction.Copy)
                            {
                                parameters[i] = copyOfParameters[i];
                            }
                            else
                            {
                                Debug.Assert(action == ParameterCopyBackAction.CopyNullable);
                                Debug.Assert(copyOfParameters[i] != null);
                                Debug.Assert(((RuntimeType)copyOfParameters[i]!.GetType()).IsNullableOfT);
                                parameters[i] = RuntimeMethodHandle.ReboxFromNullable(copyOfParameters[i]);
                            }
                        }
                    }
                }
            }

            return null;
        }

        // Slower path that does a heap alloc for copyOfParameters and registers byrefs to those objects.
        // This is a separate method to support better performance for the faster paths.
        [DebuggerStepThrough]
        [DebuggerHidden]
        private static unsafe void InvokeWithManyArguments(
            RuntimeConstructorInfo ci,
            int argCount,
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[] parameters,
            CultureInfo? culture)
        {
            object[] objHolder = new object[argCount];
            Span<object?> copyOfParameters = new(objHolder, 0, argCount);

            // We don't check a max stack size since we are invoking a method which
            // naturally requires a stack size that is dependent on the arg count\size.
            IntPtr* pByRefStorage = stackalloc IntPtr[argCount];
            NativeMemory.Clear(pByRefStorage, (uint)(argCount * sizeof(IntPtr)));

            ParameterCopyBackAction* copyBackActions = stackalloc ParameterCopyBackAction[argCount];
            Span<ParameterCopyBackAction> shouldCopyBackParameters = new(copyBackActions, argCount);

            GCFrameRegistration reg = new(pByRefStorage, (uint)argCount, areByRefs: true);

            try
            {
                RegisterForGCReporting(&reg);
                ci.CheckArguments(
                    copyOfParameters,
                    pByRefStorage,
                    shouldCopyBackParameters,
                    parameters,
                    ci.ArgumentTypes,
                    binder,
                    culture,
                    invokeAttr);

#if MONO // Temporary until Mono is updated.
                ci.Invoker.InlinedInvoke(obj, copyOfParameters, invokeAttr);
#else
                ci.Invoker.InlinedInvoke(obj, pByRefStorage, invokeAttr);
#endif
            }
            finally
            {
                UnregisterForGCReporting(&reg);
            }

            // Copy modified values out. This should be done only with ByRef or Type.Missing parameters.
            for (int i = 0; i < argCount; i++)
            {
                ParameterCopyBackAction action = shouldCopyBackParameters[i];
                if (action != ParameterCopyBackAction.None)
                {
                    if (action == ParameterCopyBackAction.Copy)
                    {
                        parameters[i] = copyOfParameters[i];
                    }
                    else
                    {
                        Debug.Assert(action == ParameterCopyBackAction.CopyNullable);
                        Debug.Assert(copyOfParameters[i] != null);
                        Debug.Assert(((RuntimeType)copyOfParameters[i]!.GetType()).IsNullableOfT);
                        parameters[i] = RuntimeMethodHandle.ReboxFromNullable(copyOfParameters[i]);
                    }
                }
            }
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            if ((InvocationFlags & (InvocationFlags.NoInvoke | InvocationFlags.ContainsStackPointers | InvocationFlags.NoConstructorInvoke)) != 0)
            {
                ThrowNoInvokeException();
            }

            // We don't need to explicitly invoke the class constructor here,
            // JIT will insert the call to .cctor in the instance ctor.

            // Correct number of arguments supplied
            int argCount = (parameters is null) ? 0 : parameters.Length;
            if (ArgumentTypes.Length != argCount)
            {
                throw new TargetParameterCountException(SR.Arg_ParmCnt);
            }

            object? retValue;

            unsafe
            {
                if (argCount == 0)
                {
                    retValue = Invoker.InlinedInvoke(obj: null, args: default, invokeAttr);
                }
                else if (argCount > MaxStackAllocArgCount)
                {
                    retValue = InvokeWithManyArguments(this, argCount, invokeAttr, binder, parameters, culture);
                }
                else
                {
                    Debug.Assert(parameters != null);
                    StackAllocedArguments argStorage = default;
                    Span<object?> copyOfParameters = new(ref argStorage._arg0, argCount);
                    Span<ParameterCopyBackAction> shouldCopyBackParameters = new(ref argStorage._copyBack0, argCount);

                    StackAllocatedByRefs byrefStorage = default;
                    IntPtr* pByRefStorage = (IntPtr*)&byrefStorage;

                    CheckArguments(
                        copyOfParameters,
                        pByRefStorage,
                        shouldCopyBackParameters,
                        parameters,
                        ArgumentTypes,
                        binder,
                        culture,
                        invokeAttr);

#if MONO // Temporary until Mono is updated.
                    retValue = Invoker.InlinedInvoke(obj: null, copyOfParameters, invokeAttr);
#else
                    retValue = Invoker.InlinedInvoke(obj: null, pByRefStorage, invokeAttr);
#endif

                    // Copy modified values out. This should be done only with ByRef or Type.Missing parameters.
                    for (int i = 0; i < argCount; i++)
                    {
                        ParameterCopyBackAction action = shouldCopyBackParameters[i];
                        if (action != ParameterCopyBackAction.None)
                        {
                            if (action == ParameterCopyBackAction.Copy)
                            {
                                parameters[i] = copyOfParameters[i];
                            }
                            else
                            {
                                Debug.Assert(action == ParameterCopyBackAction.CopyNullable);
                                Debug.Assert(copyOfParameters[i] != null);
                                Debug.Assert(((RuntimeType)copyOfParameters[i]!.GetType()).IsNullableOfT);
                                parameters[i] = RuntimeMethodHandle.ReboxFromNullable(copyOfParameters[i]);
                            }
                        }
                    }
                }
            }

            Debug.Assert(retValue != null);
            return retValue;
        }

        // Slower path that does a heap alloc for copyOfParameters and registers byrefs to those objects.
        // This is a separate method to encourage more efficient IL for the faster paths.
        [DebuggerStepThrough]
        [DebuggerHidden]
        private static unsafe object? InvokeWithManyArguments(
            RuntimeConstructorInfo ci,
            int argCount,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[]? parameters,
            CultureInfo? culture)
        {
            Debug.Assert(parameters != null);

            object[] objHolder = new object[argCount];
            Span<object?> copyOfParameters = new(objHolder, 0, argCount);

            // We don't check a max stack size since we are invoking a method which
            // naturally requires a stack size that is dependent on the arg count\size.
            IntPtr* pByRefStorage = stackalloc IntPtr[argCount];
            NativeMemory.Clear(pByRefStorage, (uint)(argCount * sizeof(IntPtr)));

            ParameterCopyBackAction* copyBackActions = stackalloc ParameterCopyBackAction[argCount];
            Span<ParameterCopyBackAction> shouldCopyBackParameters = new(copyBackActions, argCount);

            GCFrameRegistration reg = new(pByRefStorage, (uint)argCount, areByRefs: true);

            object? retValue;
            try
            {
                RegisterForGCReporting(&reg);
                ci.CheckArguments(
                    copyOfParameters,
                    pByRefStorage,
                    shouldCopyBackParameters,
                    parameters,
                    ci.ArgumentTypes,
                    binder,
                    culture,
                    invokeAttr);

#if MONO // Temporary until Mono is updated.
                retValue = ci.Invoker.InlinedInvoke(obj: null, copyOfParameters, invokeAttr);
#else
                retValue = ci.Invoker.InlinedInvoke(obj: null, pByRefStorage, invokeAttr);
#endif
            }
            finally
            {
                UnregisterForGCReporting(&reg);
            }

            // Copy modified values out. This should be done only with ByRef or Type.Missing parameters.
            for (int i = 0; i < argCount; i++)
            {
                ParameterCopyBackAction action = shouldCopyBackParameters[i];
                if (action != ParameterCopyBackAction.None)
                {
                    if (action == ParameterCopyBackAction.Copy)
                    {
                        parameters[i] = copyOfParameters[i];
                    }
                    else
                    {
                        Debug.Assert(action == ParameterCopyBackAction.CopyNullable);
                        Debug.Assert(copyOfParameters[i] != null);
                        Debug.Assert(((RuntimeType)copyOfParameters[i]!.GetType()).IsNullableOfT);
                        parameters[i] = RuntimeMethodHandle.ReboxFromNullable(copyOfParameters[i]);
                    }
                }
            }

            return retValue;
        }
    }
}
