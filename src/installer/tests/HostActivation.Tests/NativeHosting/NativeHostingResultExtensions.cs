// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    internal static class NativeHostingResultExtensions
    {
        public static CommandResultAssertions ExecuteFunctionPointer(this CommandResultAssertions assertion, string methodName, int callCount, int returnValue)
        {
            return assertion.ExecuteFunctionPointer(methodName, callCount)
                .HaveStdOutContaining($"{methodName} delegate result: 0x{returnValue.ToString("x")}");
        }

        public static CommandResultAssertions ExecuteFunctionPointerWithException(this CommandResultAssertions assertion, string methodName, int callCount)
        {
            var constraint = assertion.ExecuteFunctionPointer(methodName, callCount);
            if (OperatingSystem.IsWindows())
            {
                return constraint.HaveStdOutContaining($"{methodName} delegate threw exception: 0x{Constants.ErrorCode.COMPlusException.ToString("x")}");
            }
            else
            {
                // Exception is unhandled by native host on non-Windows systems
                return constraint.ExitWith(Constants.ErrorCode.SIGABRT)
                    .HaveStdErrContaining($"Unhandled exception. System.InvalidOperationException: {methodName}");
            }
        }

        public static CommandResultAssertions ExecuteFunctionPointer(this CommandResultAssertions assertion, string methodName, int callCount)
        {
            return assertion.HaveStdOutContaining($"Called {methodName}(0xdeadbeef, 42) - call count: {callCount}");
        }

        public static CommandResultAssertions ExecuteInIsolatedContext(this CommandResultAssertions assertion, string assemblyName)
        {
            return assertion.HaveStdOutContaining($"{assemblyName}: AssemblyLoadContext = \"IsolatedComponentLoadContext(");
        }

        public static CommandResultAssertions ExecuteInDefaultContext(this CommandResultAssertions assertion, string assemblyName)
        {
            return assertion.HaveStdOutContaining($"{assemblyName}: AssemblyLoadContext = \"Default\" System.Runtime.Loader.DefaultAssemblyLoadContext");
        }

        public static CommandResultAssertions ExecuteWithLocation(this CommandResultAssertions assertion, string assemblyName, string location)
        {
            return assertion.HaveStdOutContaining($"{assemblyName}: Location = '{location}'");
        }

        public static CommandResultAssertions ResolveHostFxr(this CommandResultAssertions assertion, Microsoft.DotNet.Cli.Build.DotNetCli dotnet)
        {
            return assertion.HaveStdErrContaining($"Resolved fxr [{dotnet.GreatestVersionHostFxrFilePath}]");
        }

        public static CommandResultAssertions ResolveHostPolicy(this CommandResultAssertions assertion, Microsoft.DotNet.Cli.Build.DotNetCli dotnet)
        {
            return assertion.HaveStdErrContaining($"{Binaries.HostPolicy.FileName} directory is [{dotnet.GreatestVersionSharedFxPath}]");
        }

        public static CommandResultAssertions ResolveCoreClr(this CommandResultAssertions assertion, Microsoft.DotNet.Cli.Build.DotNetCli dotnet)
        {
            return assertion.HaveStdErrContaining($"CoreCLR path = '{Path.Combine(dotnet.GreatestVersionSharedFxPath, Binaries.CoreClr.FileName)}'")
                .HaveStdErrContaining($"CoreCLR dir = '{dotnet.GreatestVersionSharedFxPath}{Path.DirectorySeparatorChar}'");
        }
    }
}
