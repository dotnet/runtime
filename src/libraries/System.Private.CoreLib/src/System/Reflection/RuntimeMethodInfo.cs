// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

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

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
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
            int actualCount = (parameters is null) ? 0 : parameters.Length;
            if (ArgumentTypes.Length != actualCount)
            {
                throw new TargetParameterCountException(SR.Arg_ParmCnt);
            }

            StackAllocedArguments stackArgs = default; // try to avoid intermediate array allocation if possible
            Span<object?> arguments = default;
            if (actualCount != 0)
            {
                arguments = CheckArguments(ref stackArgs, parameters!, binder, invokeAttr, culture, ArgumentTypes);
            }

            object? retValue = InvokeWorker(obj, invokeAttr, arguments);

            // copy out. This should be made only if ByRef are present.
            // n.b. cannot use Span<T>.CopyTo, as parameters.GetType() might not actually be typeof(object[])
            for (int index = 0; index < arguments.Length; index++)
            {
                parameters![index] = arguments[index];
            }

            return retValue;
        }
    }
}