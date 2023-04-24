// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace System.Reflection
{
    internal sealed partial class RuntimeMethodInfo : MethodInfo
    {
        [MethodImpl(MethodImplOptions.NoInlining)] // move lazy invocation flags population out of the hot path
        private static InvocationFlags ComputeAndUpdateInvocationFlags(MethodInfo methodInfo, ref InvocationFlags flagsToUpdate)
        {
            InvocationFlags invocationFlags = InvocationFlags.Unknown;

            Type? declaringType = methodInfo.DeclaringType;

            if (methodInfo.ContainsGenericParameters // Method has unbound generics
                || IsDisallowedByRefType(methodInfo.ReturnType) // Return type is an invalid by-ref (i.e., by-ref-like or void*)
                || (methodInfo.CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs // Managed varargs
                )
            {
                invocationFlags = InvocationFlags.NoInvoke;
            }
            else
            {
                if (declaringType != null)
                {
                    if (declaringType.ContainsGenericParameters) // Enclosing type has unbound generics
                    {
                        invocationFlags = InvocationFlags.NoInvoke;
                    }
                    else if (declaringType.IsByRefLike) // Check for byref-like types
                    {
                        invocationFlags |= InvocationFlags.ContainsStackPointers;
                    }
                }

                if (methodInfo.ReturnType.IsByRefLike) // Check for byref-like types for return
                {
                    invocationFlags |= InvocationFlags.ContainsStackPointers;
                }
            }

            invocationFlags |= InvocationFlags.Initialized;
            flagsToUpdate = invocationFlags; // accesses are guaranteed atomic
            return invocationFlags;

            static bool IsDisallowedByRefType(Type type)
            {
                if (!type.IsByRef)
                    return false;

                Type elementType = type.GetElementType()!;
                return elementType.IsByRefLike || elementType == typeof(void);
            }
        }

        [DoesNotReturn]
        private void ThrowNoInvokeException()
        {
            // method is on a class that contains stack pointers
            if ((InvocationFlags & InvocationFlags.ContainsStackPointers) != 0)
            {
                throw new NotSupportedException();
            }
            // method is vararg
            else if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
            {
                throw new NotSupportedException();
            }
            // method is generic or on a generic class
            else if (DeclaringType!.ContainsGenericParameters || ContainsGenericParameters)
            {
                throw new InvalidOperationException(SR.Arg_UnboundGenParam);
            }
            // method is abstract class
            else if (IsAbstract)
            {
                throw new MemberAccessException();
            }
            else if (ReturnType.IsByRef)
            {
                Type elementType = ReturnType.GetElementType()!;
                if (elementType.IsByRefLike)
                    throw new NotSupportedException(SR.NotSupported_ByRefToByRefLikeReturn);
                if (elementType == typeof(void))
                    throw new NotSupportedException(SR.NotSupported_ByRefToVoidReturn);
            }

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
            // ContainsStackPointers means that the struct (either the declaring type or the return type)
            // contains pointers that point to the stack. This is either a ByRef or a TypedReference. These structs cannot
            // be boxed and thus cannot be invoked through reflection which only deals with boxed value type objects.
            if ((InvocationFlags & (InvocationFlags.NoInvoke | InvocationFlags.ContainsStackPointers)) != 0)
            {
                ThrowNoInvokeException();
            }

            ValidateInvokeTarget(obj);

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
                    retValue = Invoker.InlinedInvoke(obj, args: default, invokeAttr);
                }
                else if (argCount > MaxStackAllocArgCount)
                {
                    Debug.Assert(parameters != null);
                    retValue = InvokeWithManyArguments(this, argCount, obj, invokeAttr, binder, parameters, culture);
                }
                else
                {
                    Debug.Assert(parameters != null);
                    StackAllocedArguments argStorage = default;
                    Span<object?> copyOfParameters = argStorage._args.AsSpan(argCount);
                    Span<ParameterCopyBackAction> shouldCopyBackParameters = argStorage._copyBacks.AsSpan(argCount);

                    StackAllocatedByRefs byrefStorage = default;
#pragma warning disable 8500
                    IntPtr* pByRefStorage = (IntPtr*)&byrefStorage;
#pragma warning restore 8500

                    CheckArguments(
                        copyOfParameters,
                        pByRefStorage,
                        shouldCopyBackParameters,
                        parameters,
                        ArgumentTypes,
                        binder,
                        culture,
                        invokeAttr);

                    retValue = Invoker.InlinedInvoke(obj, pByRefStorage, invokeAttr);

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

            return retValue;
        }

        // Slower path that does a heap alloc for copyOfParameters and registers byrefs to those objects.
        // This is a separate method to support better performance for the faster paths.
        [DebuggerStepThrough]
        [DebuggerHidden]
        private static unsafe object? InvokeWithManyArguments(
            RuntimeMethodInfo mi,
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

            object? retValue;
            try
            {
                RegisterForGCReporting(&reg);
                mi.CheckArguments(
                    copyOfParameters,
                    pByRefStorage,
                    shouldCopyBackParameters,
                    parameters,
                    mi.ArgumentTypes,
                    binder,
                    culture,
                    invokeAttr);

                retValue = mi.Invoker.InlinedInvoke(obj, pByRefStorage, invokeAttr);
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
