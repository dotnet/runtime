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
    public partial class LoadAssemblyAndGetFunctionPointer : IClassFixture<LoadAssemblyAndGetFunctionPointer.SharedTestState>
    {
        private const string ComponentLoadAssemblyAndGetFunctionPointerArg = "component_load_assembly_and_get_function_pointer";
        private const string AppLoadAssemblyAndGetFunctionPointerArg = "app_load_assembly_and_get_function_pointer";

        private readonly SharedTestState sharedState;

        public LoadAssemblyAndGetFunctionPointer(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        public void CallDelegateOnComponentContext(bool validPath, bool validType, bool validMethod)
        {
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            string[] args =
            {
                ComponentLoadAssemblyAndGetFunctionPointerArg,
                sharedState.HostFxrPath,
                componentProject.RuntimeConfigJson,
                validPath ? componentProject.AppDll : "BadPath...",
                validType ? sharedState.ComponentTypeName : $"Component.BadType, {componentProject.AssemblyName}",
                validMethod ? sharedState.ComponentEntryPoint1 : "BadMethod",
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should()
                .InitializeContextForConfig(componentProject.RuntimeConfigJson);

            if (validPath && validType && validMethod)
            {
                result.Should().Pass()
                    .And.ExecuteComponentEntryPoint(sharedState.ComponentEntryPoint1, 1, 1);
            }
            else
            {
                result.Should().Fail();
            }
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        public void CallDelegateOnApplicationContext(bool validPath, bool validType, bool validMethod)
        {
            var appProject = sharedState.ApplicationFixture.TestProject;
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            string[] args =
            {
                AppLoadAssemblyAndGetFunctionPointerArg,
                sharedState.HostFxrPath,
                appProject.AppDll,
                validPath ? componentProject.AppDll : "BadPath...",
                validType ? sharedState.ComponentTypeName : $"Component.BadType, {componentProject.AssemblyName}",
                validMethod ? sharedState.ComponentEntryPoint1 : "BadMethod",
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should()
                .InitializeContextForApp(appProject.AppDll);

            if (validPath && validType && validMethod)
            {
                result.Should().Pass()
                    .And.ExecuteComponentEntryPoint(sharedState.ComponentEntryPoint1, 1, 1);
            }
            else
            {
                result.Should().Fail();
            }
        }

        [Fact]
        public void CallDelegateOnSelfContainedApplicationContext()
        {
            var appProject = sharedState.SelfContainedApplicationFixture.TestProject;
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            string[] args =
            {
                AppLoadAssemblyAndGetFunctionPointerArg,
                appProject.HostFxrDll,
                appProject.AppDll,
                componentProject.AppDll,
                sharedState.ComponentTypeName,
                sharedState.ComponentEntryPoint1,
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should()
                .InitializeContextForApp(appProject.AppDll)
                .And.Pass()
                .And.ExecuteComponentEntryPoint(sharedState.ComponentEntryPoint1, 1, 1);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(10, false)]
        [InlineData(10, true)]
        public void CallDelegateOnComponentContext_MultipleEntryPoints(int callCount, bool callUnmanaged)
        {
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            string[] baseArgs =
            {
                ComponentLoadAssemblyAndGetFunctionPointerArg,
                sharedState.HostFxrPath,
                componentProject.RuntimeConfigJson,
            };

            string comp1Name = callUnmanaged ? sharedState.UnmanagedComponentEntryPoint1 : sharedState.ComponentEntryPoint1;
            string[] componentInfo =
            {
                // [Unmanaged]ComponentEntryPoint1
                componentProject.AppDll,
                sharedState.ComponentTypeName,
                comp1Name,
                // ComponentEntryPoint2
                componentProject.AppDll,
                sharedState.ComponentTypeName,
                sharedState.ComponentEntryPoint2,
            };

            IEnumerable<string> args = baseArgs;
            for (int i = 0; i < callCount; ++i)
            {
                args = args.Concat(componentInfo);
            }

            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForConfig(componentProject.RuntimeConfigJson);

            for (int i = 1; i <= callCount; ++i)
            {
                result.Should()
                    .ExecuteComponentEntryPoint(comp1Name, i * 2 - 1, i)
                    .And.ExecuteComponentEntryPoint(sharedState.ComponentEntryPoint2, i * 2, i);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void CallDelegateOnComponentContext_MultipleComponents(int callCount)
        {
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            var componentProjectCopy = componentProject.Copy();
            string[] baseArgs =
            {
                ComponentLoadAssemblyAndGetFunctionPointerArg,
                sharedState.HostFxrPath,
                componentProject.RuntimeConfigJson,
            };
            string[] componentInfo =
            {
                // Component
                componentProject.AppDll,
                sharedState.ComponentTypeName,
                sharedState.ComponentEntryPoint1,
                // Component copy
                componentProjectCopy.AppDll,
                sharedState.ComponentTypeName,
                sharedState.ComponentEntryPoint2,
            };
            IEnumerable<string> args = baseArgs;
            for (int i = 0; i < callCount; ++i)
            {
                args = args.Concat(componentInfo);
            }

            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForConfig(componentProject.RuntimeConfigJson);

            for (int i = 1; i <= callCount; ++i)
            {
                result.Should()
                    .ExecuteComponentEntryPoint(sharedState.ComponentEntryPoint1, i, i)
                    .And.ExecuteComponentEntryPoint(sharedState.ComponentEntryPoint2, i, i);
            }
        }

        [Fact]
        public void CallDelegateOnComponentContext_UnhandledException()
        {
            string entryPoint = "ThrowException";
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            string[] args =
            {
                ComponentLoadAssemblyAndGetFunctionPointerArg,
                sharedState.HostFxrPath,
                componentProject.RuntimeConfigJson,
                componentProject.AppDll,
                sharedState.ComponentTypeName,
                entryPoint,
            };

            sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.InitializeContextForConfig(componentProject.RuntimeConfigJson)
                .And.ExecuteComponentEntryPointWithException(entryPoint, 1);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public string DotNetRoot { get; }

            public TestProjectFixture ApplicationFixture { get; }
            public TestProjectFixture ComponentWithNoDependenciesFixture { get; }
            public TestProjectFixture SelfContainedApplicationFixture { get; }
            public string ComponentTypeName { get; }
            public string ComponentEntryPoint1 => "ComponentEntryPoint1";
            public string ComponentEntryPoint2 => "ComponentEntryPoint2";
            public string UnmanagedComponentEntryPoint1 => "UnmanagedComponentEntryPoint1";

            public SharedTestState()
            {
                var dotNet = new Microsoft.DotNet.Cli.Build.DotNetCli(Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"));
                DotNetRoot = dotNet.BinPath;
                HostFxrPath = dotNet.GreatestVersionHostFxrFilePath;

                ApplicationFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
                ComponentWithNoDependenciesFixture = new TestProjectFixture("ComponentWithNoDependencies", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
                SelfContainedApplicationFixture = new TestProjectFixture("StandaloneApp", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject(selfContained: true);
                ComponentTypeName = $"Component.Component, {ComponentWithNoDependenciesFixture.TestProject.AssemblyName}";
            }

            protected override void Dispose(bool disposing)
            {
                if (ApplicationFixture != null)
                    ApplicationFixture.Dispose();
                if (ComponentWithNoDependenciesFixture != null)
                    ComponentWithNoDependenciesFixture.Dispose();
                if (SelfContainedApplicationFixture != null)
                    SelfContainedApplicationFixture.Dispose();

                base.Dispose(disposing);
            }
        }
    }

    internal static class ComponentActivationResultExtensions
    {
        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteComponentEntryPoint(this CommandResultAssertions assertion, string methodName, int componentCallCount, int returnValue)
        {
            return assertion.ExecuteComponentEntryPoint(methodName, componentCallCount)
                .And.HaveStdOutContaining($"{methodName} delegate result: 0x{returnValue.ToString("x")}");
        }

        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteComponentEntryPointWithException(this CommandResultAssertions assertion, string methodName, int componentCallCount)
        {
            var constraint = assertion.ExecuteComponentEntryPoint(methodName, componentCallCount);
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

        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteComponentEntryPoint(this CommandResultAssertions assertion, string methodName, int componentCallCount)
        {
            return assertion.HaveStdOutContaining($"Called {methodName}(0xdeadbeef, 42) - component call count: {componentCallCount}");
        }
    }
}
