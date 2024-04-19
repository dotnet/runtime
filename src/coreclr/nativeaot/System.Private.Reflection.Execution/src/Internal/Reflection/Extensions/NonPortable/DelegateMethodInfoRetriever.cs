// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Runtime.General;

using global::Internal.Reflection.Core.Execution;
using global::Internal.Reflection.Execution;
using global::Internal.Runtime.Augments;
using global::Internal.Runtime.CompilerServices;
using global::Internal.Runtime.TypeLoader;
using global::System;
using global::System.Reflection;

namespace Internal.Reflection.Extensions.NonPortable
{
    public static class DelegateMethodInfoRetriever
    {
        public static MethodInfo GetDelegateMethodInfo(Delegate del)
        {
            IntPtr originalLdFtnResult = RuntimeAugments.GetDelegateLdFtnResult(del, out RuntimeTypeHandle typeOfFirstParameterIfInstanceDelegate, out bool isOpenResolver);

            QMethodDefinition methodHandle = default(QMethodDefinition);
            RuntimeTypeHandle[] genericMethodTypeArgumentHandles = null;

            bool callTryGetMethod = true;

            unsafe
            {
                if (isOpenResolver)
                {
                    OpenMethodResolver* resolver = (OpenMethodResolver*)originalLdFtnResult;
                    if (resolver->IsOpenNonVirtualResolve)
                    {
                        originalLdFtnResult = resolver->CodePointer;
                        // And go on to do normal ldftn processing.
                    }
                    else if (resolver->ResolverType == OpenMethodResolver.DispatchResolve)
                    {
                        callTryGetMethod = false;
                        methodHandle = QMethodDefinition.FromObjectAndInt(resolver->Reader, resolver->Handle);
                        genericMethodTypeArgumentHandles = null;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(resolver->ResolverType == OpenMethodResolver.GVMResolve);

                        callTryGetMethod = false;
                        methodHandle = QMethodDefinition.FromObjectAndInt(resolver->Reader, resolver->Handle);

                        if (!TypeLoaderEnvironment.Instance.TryGetRuntimeMethodHandleComponents(resolver->GVMMethodHandle, out _, out _, out genericMethodTypeArgumentHandles))
                            throw new NotSupportedException(SR.DelegateGetMethodInfo_NoInstantiation);
                    }
                }
            }

            if (callTryGetMethod)
            {
                if (!ReflectionExecution.ExecutionEnvironment.TryGetMethodForOriginalLdFtnResult(originalLdFtnResult, ref typeOfFirstParameterIfInstanceDelegate, out methodHandle, out genericMethodTypeArgumentHandles))
                {
                    ReflectionExecution.ExecutionEnvironment.GetFunctionPointerAndInstantiationArgumentForOriginalLdFtnResult(originalLdFtnResult, out IntPtr ip, out IntPtr _);

                    string methodDisplayString = RuntimeAugments.TryGetMethodDisplayStringFromIp(ip);
                    if (methodDisplayString == null)
                        throw new NotSupportedException(SR.DelegateGetMethodInfo_NoDynamic);
                    else
                        throw new NotSupportedException(SR.Format(SR.DelegateGetMethodInfo_NoDynamic_WithDisplayString, methodDisplayString));
                }
            }
            return (MethodInfo)ExecutionDomain.GetMethod(typeOfFirstParameterIfInstanceDelegate, methodHandle, genericMethodTypeArgumentHandles);
        }
    }
}
