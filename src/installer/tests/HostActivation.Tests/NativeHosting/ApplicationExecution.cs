// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public partial class ApplicationExecution : IClassFixture<ApplicationExecution.SharedTestState>
    {
        private const string ApplicationExecutionArg = "run_app";

        private readonly SharedTestState sharedState;

        public ApplicationExecution(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Fact]
        public void RunApp()
        {
            var app = sharedState.App;
            string[] args =
            {
                ApplicationExecutionArg,
                sharedState.HostFxrPath,
                app.AppDll
            };

            sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute()
                .Should().Pass()
                .And.InitializeContextForApp(app.AppDll)
                .And.ExecuteApplication(sharedState.NativeHostPath, app.AppDll);
        }

        [Fact]
        public void RunApp_UnhandledException()
        {
            var app = sharedState.App;
            string[] args =
            {
                ApplicationExecutionArg,
                sharedState.HostFxrPath,
                app.AppDll,
                "throw_exception"
            };

            sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.InitializeContextForApp(app.AppDll)
                .And.ExecuteApplicationWithException(sharedState.NativeHostPath, app.AppDll);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public string DotNetRoot { get; }

            public TestApp App { get; }

            public SharedTestState()
            {
                DotNetRoot = TestContext.BuiltDotNet.BinPath;
                HostFxrPath = TestContext.BuiltDotNet.GreatestVersionHostFxrFilePath;

                App = TestApp.CreateFromBuiltAssets("HelloWorld");
            }

            protected override void Dispose(bool disposing)
            {
                App?.Dispose();

                base.Dispose(disposing);
            }
        }
    }

    internal static class ApplicationExecutionResultExtensions
    {
        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteApplication(this CommandResultAssertions assertion, string hostPath, string appPath)
        {
            return assertion.HaveStdErrContaining($"Launch host: {hostPath}, app: {appPath}")
                .And.HaveStdOutContaining("Hello World!");
        }

        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteApplicationWithException(this CommandResultAssertions assertion, string hostPath, string appPath)
        {
            var constraint = assertion.ExecuteApplication(hostPath, appPath);
            if (OperatingSystem.IsWindows())
            {
                return constraint.And.HaveStdOutContaining($"hostfxr_run_app threw exception: 0x{Constants.ErrorCode.COMPlusException.ToString("x")}");
            }
            else
            {
                // Exception is unhandled by native host on non-Windows systems
                return constraint.And.ExitWith(Constants.ErrorCode.SIGABRT)
                    .And.HaveStdErrContaining("Unhandled exception. System.Exception: Goodbye World!");
            }
        }
    }
}
