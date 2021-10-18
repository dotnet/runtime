// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        [RequiresUnreferencedCode("Metadata for the method might be incomplete or removed")]
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

        private protected Span<object?> CheckArguments(Span<object?> stackArgs, ReadOnlySpan<object?> parameters, Binder? binder,
            BindingFlags invokeAttr, CultureInfo? culture, Signature sig)
        {
            Debug.Assert(!parameters.IsEmpty);

            // We need to perform type safety validation against the incoming arguments, but we also need
            // to be resilient against the possibility that some other thread (or even the binder itself!)
            // may mutate the array after we've validated the arguments but before we've properly invoked
            // the method. The solution is to copy the arguments to a different, not-user-visible buffer
            // as we validate them. n.b. This disallows use of ArrayPool, as ArrayPool-rented arrays are
            // considered user-visible to threads which may still be holding on to returned instances.

            Span<object?> copyOfParameters = stackArgs;
            if (parameters.Length > copyOfParameters.Length)
            {
                copyOfParameters = new object?[parameters.Length];
            }

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
        #endregion
    }
}
