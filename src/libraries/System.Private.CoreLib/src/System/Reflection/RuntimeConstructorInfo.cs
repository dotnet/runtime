// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

        internal static void CheckCanCreateInstance(Type declaringType!!, bool isVarArg)
        {
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
                    Invoker.InvokeUnsafe(obj, args: default, argsForTemporaryMonoSupport: default, invokeAttr);
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

                        Invoker.InvokeUnsafe(obj, (IntPtr*)(void**)&byrefStorage, copyOfParameters, invokeAttr);
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

                            Invoker.InvokeUnsafe(obj, (IntPtr*)(void*)byrefStorage, copyOfParameters, invokeAttr);
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
            return null;
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
                    retValue = Invoker.InvokeUnsafe(obj: null, args: default, argsForTemporaryMonoSupport: default, invokeAttr);
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

                        retValue = Invoker.InvokeUnsafe(obj: null, (IntPtr*)(void**)&byrefStorage, copyOfParameters, invokeAttr);
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

                            retValue = Invoker.InvokeUnsafe(obj: null, (IntPtr*)(void*)byrefStorage, copyOfParameters, invokeAttr);
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

            Debug.Assert(retValue != null);
            return retValue;
        }
    }
}
