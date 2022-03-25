// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
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
                    retValue = Invoker.InvokeUnsafe(obj, args: default, argsForTemporaryMonoSupport: default, invokeAttr);
                }
                else
                {
                    Debug.Assert(parameters != null);
                    Span<object?> copyOfParameters;
                    Span<bool> shouldCopyBackParameters;
                    Span<IntPtr> byrefParameters;

                    if (argCount <= MaxStackAllocArgCount)
                    {
                        StackAllocedArguments argStorage = default;
                        copyOfParameters = new Span<object?>(ref argStorage._arg0, argCount);
                        shouldCopyBackParameters = new Span<bool>(ref argStorage._copyBack0, argCount);

                        StackAllocatedByRefs byrefStorage = default;
                        byrefParameters = new Span<IntPtr>(&byrefStorage, argCount);

                        CheckArguments(
                            copyOfParameters,
                            byrefParameters,
                            shouldCopyBackParameters,
                            parameters,
                            ArgumentTypes,
                            binder,
                            culture,
                            invokeAttr);

                        retValue = Invoker.InvokeUnsafe(obj, (IntPtr*)(void*)&byrefStorage, copyOfParameters, invokeAttr);
                    }
                    else
                    {
                        object[] objHolder = new object[argCount];
                        copyOfParameters = new Span<object?>(objHolder, 0, argCount);

                        // We don't check a max stack size since we are invoking a method which
                        // natually requires a stack size that is dependent on the arg count\size.
                        IntPtr* byrefStorage = stackalloc IntPtr[argCount];
                        byrefParameters = new Span<IntPtr>(byrefStorage, argCount);

                        bool* boolHolder = stackalloc bool[argCount];
                        shouldCopyBackParameters = new Span<bool>(boolHolder, argCount);

                        GCFrameRegistration reg = new(byrefStorage, (uint)argCount, areByRefs: true);

                        try
                        {
                            RegisterForGCReporting(&reg);
                            CheckArguments(
                                copyOfParameters,
                                byrefParameters,
                                shouldCopyBackParameters,
                                parameters,
                                ArgumentTypes,
                                binder,
                                culture,
                                invokeAttr);

                            retValue = Invoker.InvokeUnsafe(obj, (IntPtr*)(void*)byrefStorage, copyOfParameters, invokeAttr);
                        }
                        finally
                        {
                            UnregisterForGCReporting(&reg);
                        }
                    }

                    // Copy modified values out. This should be done only with ByRef or Type.Missing parameters.
                    for (int i = 0; i < argCount; i++)
                    {
                        if (shouldCopyBackParameters[i])
                        {
                            parameters[i] = copyOfParameters[i];
                        }
                    }
                }
            }

            return retValue;
        }
    }
}
