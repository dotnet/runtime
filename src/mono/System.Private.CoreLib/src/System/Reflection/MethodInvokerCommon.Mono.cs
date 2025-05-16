// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    internal static partial class MethodInvokerCommon
    {
        internal static bool UseInterpretedPath => LocalAppContextSwitches.ForceInterpretedInvoke || !RuntimeFeature.IsDynamicCodeSupported;

#pragma warning disable IDE0060 // Unused parameters - the Clr partial class implementation may use the parameter name.
        // When Mono supports well-known funcs, remove this stub and then move the CoreClr implementation to the shared code.
        private static unsafe bool TryGetCalliFunc(MethodBase method, RuntimeType[] parameterTypes, RuntimeType returnType, InvokerStrategy strategy, out Delegate? invokeFunc)
        {
            invokeFunc = null;
            return false;
        }
#pragma warning restore IDE0060
    }
}
