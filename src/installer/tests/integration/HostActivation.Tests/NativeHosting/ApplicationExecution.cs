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
            var project = sharedState.PortableAppFixture.TestProject;
            string[] args =
            {
                ApplicationExecutionArg,
                sharedState.HostFxrPath,
                project.AppDll
            };

            sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute()
                .Should().Pass()
                .And.InitializeContextForApp(project.AppDll)
                .And.ExecuteApplication(sharedState.NativeHostPath, project.AppDll);
        }

        [Fact]
        public void RunApp_UnhandledException()
        {
            var project = sharedState.PortableAppWithExceptionFixture.TestProject;
            string[] args =
            {
                ApplicationExecutionArg,
                sharedState.HostFxrPath,
                project.AppDll
            };

            sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute()
                .Should().Fail()
                .And.InitializeContextForApp(project.AppDll)
                .And.ExecuteApplicationWithException(sharedState.NativeHostPath, project.AppDll);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public string DotNetRoot { get; }

            public TestProjectFixture PortableAppFixture { get; }
            public TestProjectFixture PortableAppWithExceptionFixture { get; }

            public SharedTestState()
            {
                var dotNet = new Microsoft.DotNet.Cli.Build.DotNetCli(Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"));
                DotNetRoot = dotNet.BinPath;
                HostFxrPath = dotNet.GreatestVersionHostFxrFilePath;

                PortableAppFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();

                PortableAppWithExceptionFixture = new TestProjectFixture("PortableAppWithException", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
            }

            protected override void Dispose(bool disposing)
            {
                PortableAppFixture.Dispose();
                PortableAppWithExceptionFixture.Dispose();

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
