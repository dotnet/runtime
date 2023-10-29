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
        public ReflectionExecutionDomainCallbacksImplementation()
        {
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
            return ExecutionDomain.GetMethod(declaringTypeHandle, methodHandle, genericMethodTypeArgumentHandles: null);
        }

        public sealed override Assembly GetAssemblyForHandle(RuntimeTypeHandle typeHandle)
        {
            return Type.GetTypeFromHandle(typeHandle).Assembly;
        }

        public sealed override IntPtr TryGetStaticClassConstructionContext(RuntimeTypeHandle runtimeTypeHandle)
        {
            return ExecutionEnvironmentImplementation.TryGetStaticClassConstructionContext(runtimeTypeHandle);
        }

        public sealed override MethodInfo GetDelegateMethod(Delegate del)
        {
            return DelegateMethodInfoRetriever.GetDelegateMethodInfo(del);
        }
    }
}
