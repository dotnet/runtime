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
    public partial class GetFunctionPointer : IClassFixture<GetFunctionPointer.SharedTestState>
    {
        private const string ComponentGetFunctionPointerArg = "component_get_function_pointer";
        private const string AppGetFunctionPointerArg = "app_get_function_pointer";

        private readonly SharedTestState sharedState;

        public GetFunctionPointer(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void CallDelegateOnApplicationContext(bool validType, bool validMethod)
        {
            var appProject = sharedState.ApplicationFixture.TestProject;
            string[] args =
            {
                AppGetFunctionPointerArg,
                sharedState.HostFxrPath,
                appProject.AppDll,
                validType ? sharedState.FunctionPointerTypeName : $"Component.BadType, {appProject.AssemblyName}",
                validMethod ? sharedState.FunctionPointerEntryPoint1 : "BadMethod",
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should()
                .InitializeContextForApp(appProject.AppDll);

            if (validType && validMethod)
            {
                result.Should().Pass()
                    .And.ExecuteFunctionPointer(sharedState.FunctionPointerEntryPoint1, 1, 1);
            }
            else
            {
                result.Should().Fail();
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void CallDelegateOnComponentContext(bool validType, bool validMethod)
        {
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            string[] args =
            {
                ComponentGetFunctionPointerArg,
                sharedState.HostFxrPath,
                componentProject.RuntimeConfigJson,
                validType ? sharedState.ComponentTypeName : $"Component.BadType, {componentProject.AssemblyName}",
                validMethod ? sharedState.ComponentEntryPoint1 : "BadMethod",
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should()
                .InitializeContextForConfig(componentProject.RuntimeConfigJson);

            // This should fail even with the valid type and valid method,
            // because the type is not resolvable from the default AssemblyLoadContext.
            result.Should().Fail();
        }

        [Fact]
        public void CallDelegateOnSelfContainedApplicationContext()
        {
            var appProject = sharedState.SelfContainedApplicationFixture.TestProject;
            string[] args =
            {
                AppGetFunctionPointerArg,
                appProject.HostFxrDll,
                appProject.AppDll,
                sharedState.FunctionPointerTypeName,
                sharedState.FunctionPointerEntryPoint1,
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should()
                .InitializeContextForApp(appProject.AppDll)
                .And.Pass()
                .And.ExecuteFunctionPointer(sharedState.FunctionPointerEntryPoint1, 1, 1);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(10, false)]
        [InlineData(10, true)]
        public void CallDelegateOnApplicationContext_MultipleEntryPoints(int callCount, bool callUnmanaged)
        {
            var appProject = sharedState.ApplicationFixture.TestProject;
            string[] baseArgs =
            {
                AppGetFunctionPointerArg,
                sharedState.HostFxrPath,
                appProject.AppDll,
            };

            string functionPointer1Name = callUnmanaged ? sharedState.UnmanagedFunctionPointerEntryPoint1 : sharedState.FunctionPointerEntryPoint1;
            string[] componentInfo =
            {
                // [Unmanaged]FunctionPointerEntryPoint1
                sharedState.FunctionPointerTypeName,
                functionPointer1Name,
                // FunctionPointerEntryPoint2
                sharedState.FunctionPointerTypeName,
                sharedState.FunctionPointerEntryPoint2,
            };

            IEnumerable<string> args = baseArgs;
            for (int i = 0; i < callCount; ++i)
            {
                args = args.Concat(componentInfo);
            }

            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForApp(appProject.AppDll);

            for (int i = 1; i <= callCount; ++i)
            {
                result.Should()
                    .ExecuteFunctionPointer(functionPointer1Name, i * 2 - 1, i)
                    .And.ExecuteFunctionPointer(sharedState.FunctionPointerEntryPoint2, i * 2, i);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void CallDelegateOnApplicationContext_MultipleFunctionPointers(int callCount)
        {
            var appProject = sharedState.ApplicationFixture.TestProject;
            string[] baseArgs =
            {
                AppGetFunctionPointerArg,
                sharedState.HostFxrPath,
                appProject.AppDll,
            };
            string[] componentInfo =
            {
                // FunctionPointer
                sharedState.FunctionPointerTypeName,
                sharedState.FunctionPointerEntryPoint1,
                // FunctionPointer copy
                sharedState.FunctionPointerTypeName,
                sharedState.FunctionPointerEntryPoint2,
            };
            IEnumerable<string> args = baseArgs;
            for (int i = 0; i < callCount; ++i)
            {
                args = args.Concat(componentInfo);
            }

            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForApp(appProject.AppDll);

            for (int i = 1; i <= callCount; ++i)
            {
                result.Should()
                    .ExecuteFunctionPointer(sharedState.FunctionPointerEntryPoint1, i * 2 - 1, i)
                    .And.ExecuteFunctionPointer(sharedState.FunctionPointerEntryPoint2, i * 2, i);
            }
        }

        [Fact]
        public void CallDelegateOnApplicationContext_UnhandledException()
        {
            string entryPoint = "ThrowException";
            var appProject = sharedState.ApplicationFixture.TestProject;
            string[] args =
            {
                AppGetFunctionPointerArg,
                sharedState.HostFxrPath,
                appProject.AppDll,
                sharedState.FunctionPointerTypeName,
                entryPoint,
            };

            sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute()
                .Should().Fail()
                .And.InitializeContextForApp(appProject.AppDll)
                .And.ExecuteFunctionPointerWithException(entryPoint, 1);
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
            public string FunctionPointerTypeName { get; }
            public string FunctionPointerEntryPoint1 => "FunctionPointerEntryPoint1";
            public string FunctionPointerEntryPoint2 => "FunctionPointerEntryPoint2";
            public string UnmanagedFunctionPointerEntryPoint1 => "UnmanagedFunctionPointerEntryPoint1";

            public SharedTestState()
            {
                var dotNet = new Microsoft.DotNet.Cli.Build.DotNetCli(Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"));
                DotNetRoot = dotNet.BinPath;
                HostFxrPath = dotNet.GreatestVersionHostFxrFilePath;

                ApplicationFixture = new TestProjectFixture("AppWithCustomEntryPoints", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject(selfContained: false);
                ComponentWithNoDependenciesFixture = new TestProjectFixture("ComponentWithNoDependencies", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
                SelfContainedApplicationFixture = new TestProjectFixture("AppWithCustomEntryPoints", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject(selfContained: true);
                ComponentTypeName = $"Component.Component, {ComponentWithNoDependenciesFixture.TestProject.AssemblyName}";
                FunctionPointerTypeName = $"AppWithCustomEntryPoints.Program, {ApplicationFixture.TestProject.AssemblyName}";
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

    internal static class FunctionPointerLoadingResultExtensions
    {
        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteFunctionPointer(this CommandResultAssertions assertion, string methodName, int functionPointerCallCount, int returnValue)
        {
            return assertion.ExecuteFunctionPointer(methodName, functionPointerCallCount)
                .And.HaveStdOutContaining($"{methodName} delegate result: 0x{returnValue.ToString("x")}");
        }

        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteFunctionPointerWithException(this CommandResultAssertions assertion, string methodName, int functionPointerCallCount)
        {
            var constraint = assertion.ExecuteFunctionPointer(methodName, functionPointerCallCount);
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

        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteFunctionPointer(this CommandResultAssertions assertion, string methodName, int functionPointerCallCount)
        {
            return assertion.HaveStdOutContaining($"Called {methodName}(0xdeadbeef, 42) - function pointer call count: {functionPointerCallCount}");
        }
    }
}
