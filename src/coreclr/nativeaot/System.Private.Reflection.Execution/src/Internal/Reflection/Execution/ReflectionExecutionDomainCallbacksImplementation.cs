// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Internal.Runtime.Augments;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;
using Internal.Reflection.Execution.PayForPlayExperience;
using Internal.Reflection.Extensions.NonPortable;

using System.Reflection.Runtime.General;

using Debug = System.Diagnostics.Debug;

namespace Internal.Reflection.Execution
{
    //==========================================================================================================================
    // This class provides various services down to System.Private.CoreLib. (Though we forward most or all of them directly up to Reflection.Core.)
    //==========================================================================================================================
    internal sealed class ReflectionExecutionDomainCallbacksImplementation : ReflectionExecutionDomainCallbacks
    {
        public ReflectionExecutionDomainCallbacksImplementation(ExecutionDomain executionDomain, ExecutionEnvironmentImplementation executionEnvironment)
        {
            _executionDomain = executionDomain;
            _executionEnvironment = executionEnvironment;
        }

        //=======================================================================================
        // This group of methods jointly service the Type.GetTypeFromHandle() path. The caller
        // is responsible for analyzing the RuntimeTypeHandle to figure out which flavor to call.
        //=======================================================================================
        public sealed override Type GetNamedTypeForHandle(RuntimeTypeHandle typeHandle)
        {
            return _executionDomain.GetNamedTypeForHandle(typeHandle);
        }

        public sealed override Type GetArrayTypeForHandle(RuntimeTypeHandle typeHandle)
        {
            return _executionDomain.GetArrayTypeForHandle(typeHandle);
        }

        public sealed override Type GetMdArrayTypeForHandle(RuntimeTypeHandle typeHandle, int rank)
        {
            return _executionDomain.GetMdArrayTypeForHandle(typeHandle, rank);
        }

        public sealed override Type GetPointerTypeForHandle(RuntimeTypeHandle typeHandle)
        {
            return _executionDomain.GetPointerTypeForHandle(typeHandle);
        }

        public sealed override Type GetFunctionPointerTypeForHandle(RuntimeTypeHandle typeHandle)
        {
            return _executionDomain.GetFunctionPointerTypeForHandle(typeHandle);
        }

        public sealed override Type GetByRefTypeForHandle(RuntimeTypeHandle typeHandle)
        {
            return _executionDomain.GetByRefTypeForHandle(typeHandle);
        }

        public sealed override Type GetConstructedGenericTypeForHandle(RuntimeTypeHandle typeHandle)
        {
            return _executionDomain.GetConstructedGenericTypeForHandle(typeHandle);
        }

        //=======================================================================================
        // Missing metadata exception support.
        //=======================================================================================
        public sealed override Exception CreateMissingMetadataException(Type pertainant)
        {
            return _executionDomain.CreateMissingMetadataException(pertainant);
        }

        public sealed override MethodBase GetMethodBaseFromStartAddressIfAvailable(IntPtr methodStartAddress)
        {
            RuntimeTypeHandle declaringTypeHandle = default(RuntimeTypeHandle);
            QMethodDefinition methodHandle;
            if (!ReflectionExecution.ExecutionEnvironment.TryGetMethodForStartAddress(methodStartAddress,
                ref declaringTypeHandle, out methodHandle))
            {
                return null;
            }

            // We don't use the type argument handles as we want the uninstantiated method info
            return ReflectionCoreExecution.ExecutionDomain.GetMethod(declaringTypeHandle, methodHandle, genericMethodTypeArgumentHandles: null);
        }

        public sealed override Assembly GetAssemblyForHandle(RuntimeTypeHandle typeHandle)
        {
            return Type.GetTypeFromHandle(typeHandle).Assembly;
        }

        public sealed override IntPtr TryGetStaticClassConstructionContext(RuntimeTypeHandle runtimeTypeHandle)
        {
            return ExecutionEnvironmentImplementation.TryGetStaticClassConstructionContext(runtimeTypeHandle);
        }

        public sealed override RuntimeTypeHandle GetTypeHandleIfAvailable(Type type)
        {
            return _executionDomain.GetTypeHandleIfAvailable(type);
        }

        public sealed override MethodInfo GetDelegateMethod(Delegate del)
        {
            return DelegateMethodInfoRetriever.GetDelegateMethodInfo(del);
        }

        public sealed override Exception GetExceptionForHR(int hr)
        {
            return Marshal.GetExceptionForHR(hr);
        }

        private ExecutionDomain _executionDomain;
        private ExecutionEnvironmentImplementation _executionEnvironment;
    }
}
