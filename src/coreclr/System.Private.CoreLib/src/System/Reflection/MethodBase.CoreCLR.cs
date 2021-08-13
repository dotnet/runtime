// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Internal.Runtime.CompilerServices;

namespace System.Reflection
{
    public abstract partial class MethodBase : MemberInfo
    {
        #region Static Members
        public static MethodBase? GetMethodFromHandle(RuntimeMethodHandle handle)
        {
            if (handle.IsNullHandle())
                throw new ArgumentException(SR.Argument_InvalidHandle);

            MethodBase? m = RuntimeType.GetMethodBase(handle.GetMethodInfo());

            Type? declaringType = m?.DeclaringType;
            if (declaringType != null && declaringType.IsGenericType)
                throw new ArgumentException(SR.Format(
                    SR.Argument_MethodDeclaringTypeGeneric,
                    m, declaringType.GetGenericTypeDefinition()));

            return m;
        }

        public static MethodBase? GetMethodFromHandle(RuntimeMethodHandle handle, RuntimeTypeHandle declaringType)
        {
            if (handle.IsNullHandle())
                throw new ArgumentException(SR.Argument_InvalidHandle);

            return RuntimeType.GetMethodBase(declaringType.GetRuntimeType(), handle.GetMethodInfo());
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static MethodBase? GetCurrentMethod()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeMethodInfo.InternalGetCurrentMethod(ref stackMark);
        }
        #endregion

        #region Internal Members
        // used by EE
        private IntPtr GetMethodDesc() { return MethodHandle.Value; }

        internal virtual ParameterInfo[] GetParametersNoCopy() { return GetParameters(); }
        #endregion

        #region Internal Methods
        // helper method to construct the string representation of the parameter list

        internal virtual Type[] GetParameterTypes()
        {
            ParameterInfo[] paramInfo = GetParametersNoCopy();

            Type[] parameterTypes = new Type[paramInfo.Length];
            for (int i = 0; i < paramInfo.Length; i++)
                parameterTypes[i] = paramInfo[i].ParameterType;

            return parameterTypes;
        }

        private protected Span<object?> CheckArguments(ref StackAllocedArguments stackArgs, ReadOnlySpan<object?> parameters, Binder? binder,
            BindingFlags invokeAttr, CultureInfo? culture, Signature sig)
        {
            Debug.Assert(Unsafe.SizeOf<StackAllocedArguments>() == StackAllocedArguments.MaxStackAllocArgCount * Unsafe.SizeOf<object>(),
                "MaxStackAllocArgCount not properly defined.");
            Debug.Assert(!parameters.IsEmpty);

            // We need to perform type safety validation against the incoming arguments, but we also need
            // to be resilient against the possibility that some other thread (or even the binder itself!)
            // may mutate the array after we've validated the arguments but before we've properly invoked
            // the method. The solution is to copy the arguments to a different, not-user-visible buffer
            // as we validate them. n.b. This disallows use of ArrayPool, as ArrayPool-rented arrays are
            // considered user-visible to threads which may still be holding on to returned instances.

            Span<object?> copyOfParameters = (parameters.Length <= StackAllocedArguments.MaxStackAllocArgCount)
                    ? MemoryMarshal.CreateSpan(ref stackArgs._arg0, parameters.Length)
                    : new Span<object?>(new object?[parameters.Length]);

            ParameterInfo[]? p = null;
            for (int i = 0; i < parameters.Length; i++)
            {
                object? arg = parameters[i];
                RuntimeType argRT = sig.Arguments[i];

                if (arg == Type.Missing)
                {
                    p ??= GetParametersNoCopy();
                    if (p[i].DefaultValue == System.DBNull.Value)
                        throw new ArgumentException(SR.Arg_VarMissNull, nameof(parameters));
                    arg = p[i].DefaultValue!;
                }
                copyOfParameters[i] = argRT.CheckValue(arg, binder, culture, invokeAttr);
            }

            return copyOfParameters;
        }

        // Helper struct to avoid intermediate object[] allocation in calls to the native reflection stack.
        // Typical usage is to define a local of type default(StackAllocedArguments), then pass 'ref theLocal'
        // as the first parameter to CheckArguments. CheckArguments will try to utilize storage within this
        // struct instance if there's sufficient space; otherwise CheckArguments will allocate a temp array.
        private protected struct StackAllocedArguments
        {
            internal const int MaxStackAllocArgCount = 4;
            internal object? _arg0;
#pragma warning disable CA1823, CS0169, IDE0051 // accessed via 'CheckArguments' ref arithmetic
            private object? _arg1;
            private object? _arg2;
            private object? _arg3;
#pragma warning restore CA1823, CS0169, IDE0051
        }
        #endregion
    }
}
