// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Runtime.InteropServices;
using global::Internal.Reflection.Core.Execution;

namespace Internal.Reflection.Execution
{
    //==========================================================================================================
    // These ExecutionEnvironment entrypoints provide access to the Interop\MCG information that
    // enables Reflection invoke
    //==========================================================================================================
    internal sealed partial class ExecutionEnvironmentImplementation : ExecutionEnvironment
    {
        public sealed override bool IsCOMObject(Type type)
        {
#if PROJECTN
            return McgMarshal.IsComObject(type);
#else
            return false;
#endif
        }
    }
}
