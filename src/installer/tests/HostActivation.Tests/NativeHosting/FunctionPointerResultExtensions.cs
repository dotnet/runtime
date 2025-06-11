// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    internal static class FunctionPointerResultExtensions
    {
        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteFunctionPointer(this CommandResultAssertions assertion, string methodName, int callCount, int returnValue)
        {
            return assertion.ExecuteFunctionPointer(methodName, callCount)
                .And.HaveStdOutContaining($"{methodName} delegate result: 0x{returnValue.ToString("x")}");
        }

        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteFunctionPointerWithException(this CommandResultAssertions assertion, string methodName, int callCount)
        {
            var constraint = assertion.ExecuteFunctionPointer(methodName, callCount);
            if (OperatingSystem.IsWindows())
            {
                return constraint.And.HaveStdOutContaining($"{methodName} delegate threw exception: 0x{Constants.ErrorCode.COMPlusException.ToString("x")}");
            }
            else
            {
                // Exception is unhandled by native host on non-Windows systems
                return constraint.And.ExitWith(Constants.ErrorCode.SIGABRT)
                    .And.HaveStdErrContaining($"Unhandled exception. System.InvalidOperationException: {methodName}");
            }
        }

        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteFunctionPointer(this CommandResultAssertions assertion, string methodName, int callCount)
        {
            return assertion.HaveStdOutContaining($"Called {methodName}(0xdeadbeef, 42) - call count: {callCount}");
        }

        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteInIsolatedContext(this CommandResultAssertions assertion, string assemblyName)
        {
            return assertion.HaveStdOutContaining($"{assemblyName}: AssemblyLoadContext = \"IsolatedComponentLoadContext(");
        }

        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteInDefaultContext(this CommandResultAssertions assertion, string assemblyName)
        {
            return assertion.HaveStdOutContaining($"{assemblyName}: AssemblyLoadContext = \"Default\" System.Runtime.Loader.DefaultAssemblyLoadContext");
        }

        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteWithLocation(this CommandResultAssertions assertion, string assemblyName, string location)
        {
            return assertion.HaveStdOutContaining($"{assemblyName}: Location = '{location}'");
        }
    }
}
