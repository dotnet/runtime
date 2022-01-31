// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Reflection;

using global::Internal.Runtime.TypeLoader;
using global::Internal.Runtime.Augments;
using global::Internal.Runtime.CompilerServices;
using global::Internal.Reflection.Execution;
using global::Internal.Reflection.Core.Execution;

using System.Reflection.Runtime.General;

namespace Internal.Reflection.Extensions.NonPortable
{
    public static class DelegateMethodInfoRetriever
    {
        public static MethodInfo GetDelegateMethodInfo(Delegate del)
        {
            if (del == null)
                throw new ArgumentException();
            Delegate[] invokeList = del.GetInvocationList();
            del = invokeList[invokeList.Length - 1];
            IntPtr originalLdFtnResult = RuntimeAugments.GetDelegateLdFtnResult(del, out RuntimeTypeHandle typeOfFirstParameterIfInstanceDelegate, out bool isOpenResolver, out bool isInterpreterEntrypoint);

            if (isInterpreterEntrypoint)
            {
                // This is a special kind of delegate where the invoke method is "ObjectArrayThunk". Typically,
                // this will be a delegate that points the the LINQ Expression interpreter. We could manufacture
                // a MethodInfo based on the delegate's Invoke signature, but let's just throw for now.
                throw new PlatformNotSupportedException(SR.DelegateGetMethodInfo_ObjectArrayDelegate);
            }

            if (originalLdFtnResult == (IntPtr)0)
                return null;

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

                        RuntimeTypeHandle declaringTypeHandleIgnored;
                        MethodNameAndSignature nameAndSignatureIgnored;
                        if (!TypeLoaderEnvironment.Instance.TryGetRuntimeMethodHandleComponents(resolver->GVMMethodHandle, out declaringTypeHandleIgnored, out nameAndSignatureIgnored, out genericMethodTypeArgumentHandles))
                            throw new MissingRuntimeArtifactException(SR.DelegateGetMethodInfo_NoInstantiation);
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
                        throw new MissingRuntimeArtifactException(SR.DelegateGetMethodInfo_NoDynamic);
                    else
                        throw new MissingRuntimeArtifactException(SR.Format(SR.DelegateGetMethodInfo_NoDynamic_WithDisplayString, methodDisplayString));
                }
            }
            MethodBase methodBase = ReflectionCoreExecution.ExecutionDomain.GetMethod(typeOfFirstParameterIfInstanceDelegate, methodHandle, genericMethodTypeArgumentHandles);
            MethodInfo methodInfo = methodBase as MethodInfo;
            if (methodInfo != null)
                return methodInfo;
            return null; // GetMethod() returned a ConstructorInfo.
        }
    }
}
