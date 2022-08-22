// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Runtime.General;
using System.Runtime.CompilerServices;

using Internal.LowLevelLinq;
using Internal.Reflection.Augments;
using Internal.Reflection.Core.Execution;

namespace Internal.Reflection.Core.Execution
{
    [ReflectionBlocked]
    [CLSCompliant(false)]
    public static class ReflectionCoreExecution
    {
        //
        // One time initialization to supply the information needed to initialize the execution environment.
        //
        public static void InitializeExecutionDomain(ReflectionDomainSetup executionDomainSetup, ExecutionEnvironment executionEnvironment)
        {
            ExecutionDomain executionDomain = new ExecutionDomain(executionDomainSetup, executionEnvironment);
            //@todo: This check has a race window but since this is a private api targeted by the toolchain, perhaps this is not so critical.
            if (s_executionDomain != null)
                throw new InvalidOperationException(); // Multiple Initializes not allowed.
            s_executionDomain = executionDomain;

            ReflectionCoreCallbacks reflectionCallbacks = new ReflectionCoreCallbacksImplementation();
            ReflectionAugments.Initialize(reflectionCallbacks);
            return;
        }

        public static ExecutionDomain ExecutionDomain
        {
            get
            {
                return s_executionDomain;
            }
        }

        internal static ExecutionEnvironment ExecutionEnvironment
        {
            get
            {
                return ExecutionDomain.ExecutionEnvironment;
            }
        }

        private static volatile ExecutionDomain s_executionDomain;
    }
}
