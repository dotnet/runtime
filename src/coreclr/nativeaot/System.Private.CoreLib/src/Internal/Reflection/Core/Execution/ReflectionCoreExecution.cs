// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Runtime.CompilerServices;

using Internal.LowLevelLinq;
using Internal.Reflection.Augments;
using Internal.Reflection.Core.Execution;

namespace Internal.Reflection.Core.Execution
{
    [CLSCompliant(false)]
    public static class ReflectionCoreExecution
    {
        //
        // One time initialization to supply the information needed to initialize the execution environment.
        //
        public static void InitializeExecutionDomain(ExecutionEnvironment executionEnvironment)
        {
            Debug.Assert(s_executionEnvironment == null);
            s_executionEnvironment = executionEnvironment;

            ReflectionCoreCallbacks reflectionCallbacks = new ReflectionCoreCallbacksImplementation();
            ReflectionAugments.Initialize(reflectionCallbacks);
        }

        internal static ExecutionEnvironment ExecutionEnvironment
        {
            get
            {
                return s_executionEnvironment;
            }
        }

        private static volatile ExecutionEnvironment s_executionEnvironment;
    }
}
