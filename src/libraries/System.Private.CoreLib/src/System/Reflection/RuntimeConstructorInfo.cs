// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal sealed partial class RuntimeConstructorInfo : ConstructorInfo
    {
        [MethodImpl(MethodImplOptions.NoInlining)] // move lazy invocation flags population out of the hot path
        internal InvocationFlags ComputeAndUpdateInvocationFlags()
        {
            InvocationFlags invocationFlags = InvocationFlags.IsConstructor; // this is a given

            Type? declaringType = DeclaringType;

            if (declaringType == typeof(void)
                || declaringType != null && declaringType.ContainsGenericParameters  // Enclosing type has unbound generics
                || (CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs // Managed varargs
                )
            {
                invocationFlags |= InvocationFlags.NoInvoke;
            }
            else if (IsStatic)
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
                if (typeof(Delegate).IsAssignableFrom(DeclaringType))
                    invocationFlags |= InvocationFlags.IsDelegateConstructor;
            }

            invocationFlags |= InvocationFlags.Initialized;
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
        // This is a rarely-used Invoke since it calls a constructor on an existing instance.
        public override object? Invoke(
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[]? parameters,
            CultureInfo? culture)
        {
            if ((InvocationFlags & InvocationFlags.NoInvoke) != 0)
                ThrowNoInvokeException();

            if (!IsStatic)
            {
                MethodInvokerCommon.ValidateInvokeTarget(obj, this);
            }

            // Correct number of arguments supplied
            int argCount = (parameters is null) ? 0 : parameters.Length;
            if (ArgumentTypes.Length != argCount)
            {
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            if ((InvocationFlags & InvocationFlags.RunClassConstructor) != 0)
            {
                // Run the class constructor through the class constructor mechanism instead of the Invoke path.
                // This avoids allowing mutation of readonly static fields, and initializes the type correctly.
                InvokeClassConstructor();
                return null;
            }

            return argCount == 0 ?
                Invoker.InvokeConstructorWithoutAlloc(obj!, (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0) :
                Invoker.InvokeConstructorWithoutAlloc(obj!, invokeAttr, binder, parameters!, culture);
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
                MethodBaseInvoker.ThrowTargetParameterCountException();
            }

            return argCount switch
            {
                0 => Invoker.InvokeWithNoArgs(obj: null, invokeAttr)!,
                1 => Invoker.InvokeWithOneArg(obj: null, invokeAttr, binder, parameters!, culture)!,
                2 or 3 or 4 => Invoker.InvokeWithFewArgs(obj: null, invokeAttr, binder, parameters!, culture)!,
                _ => Invoker.InvokeWithManyArgs(obj: null, invokeAttr, binder, parameters!, culture)!,
            };
        }
    }
}
