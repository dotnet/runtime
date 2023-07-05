// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Threading;

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
        [DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
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
    }
}
