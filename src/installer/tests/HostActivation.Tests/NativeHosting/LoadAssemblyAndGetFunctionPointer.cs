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
                    .And.ExecuteFunctionPointer(sharedState.ComponentEntryPoint1, 1, 1)
                    .And.ExecuteInIsolatedContext(componentProject.AssemblyName);
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
            var app = sharedState.FrameworkDependentApp;
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            string[] args =
            {
                AppLoadAssemblyAndGetFunctionPointerArg,
                sharedState.HostFxrPath,
                app.AppDll,
                validPath ? componentProject.AppDll : "BadPath...",
                validType ? sharedState.ComponentTypeName : $"Component.BadType, {componentProject.AssemblyName}",
                validMethod ? sharedState.ComponentEntryPoint1 : "BadMethod",
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should()
                .InitializeContextForApp(app.AppDll);

            if (validPath && validType && validMethod)
            {
                result.Should().Pass()
                    .And.ExecuteFunctionPointer(sharedState.ComponentEntryPoint1, 1, 1)
                    .And.ExecuteInIsolatedContext(componentProject.AssemblyName);
            }
            else
            {
                result.Should().Fail();
            }
        }

        [Fact]
        public void CallDelegateOnSelfContainedApplicationContext()
        {
            var app = sharedState.SelfContainedApp;
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            string[] args =
            {
                AppLoadAssemblyAndGetFunctionPointerArg,
                app.HostFxrDll,
                app.AppDll,
                componentProject.AppDll,
                sharedState.ComponentTypeName,
                sharedState.ComponentEntryPoint1,
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should()
                .InitializeContextForApp(app.AppDll)
                .And.Pass()
                .And.ExecuteFunctionPointer(sharedState.ComponentEntryPoint1, 1, 1)
                .And.ExecuteInIsolatedContext(componentProject.AssemblyName);
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
                .And.InitializeContextForConfig(componentProject.RuntimeConfigJson)
                .And.ExecuteInIsolatedContext(componentProject.AssemblyName);

            for (int i = 1; i <= callCount; ++i)
            {
                result.Should()
                    .ExecuteFunctionPointer(comp1Name, i * 2 - 1, i)
                    .And.ExecuteFunctionPointer(sharedState.ComponentEntryPoint2, i * 2, i);
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
                .And.InitializeContextForConfig(componentProject.RuntimeConfigJson)
                .And.ExecuteInIsolatedContext(componentProject.AssemblyName);

            for (int i = 1; i <= callCount; ++i)
            {
                result.Should()
                    .ExecuteFunctionPointer(sharedState.ComponentEntryPoint1, i, i)
                    .And.ExecuteFunctionPointer(sharedState.ComponentEntryPoint2, i, i);
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
                .And.ExecuteFunctionPointerWithException(entryPoint, 1);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public string DotNetRoot { get; }

            public TestProjectFixture ComponentWithNoDependenciesFixture { get; }
            public TestApp FrameworkDependentApp { get; }
            public TestApp SelfContainedApp { get; }

            public string ComponentTypeName { get; }
            public string ComponentEntryPoint1 => "ComponentEntryPoint1";
            public string ComponentEntryPoint2 => "ComponentEntryPoint2";
            public string UnmanagedComponentEntryPoint1 => "UnmanagedComponentEntryPoint1";

            public SharedTestState()
            {
                DotNetRoot = TestContext.BuiltDotNet.BinPath;
                HostFxrPath = TestContext.BuiltDotNet.GreatestVersionHostFxrFilePath;

                ComponentWithNoDependenciesFixture = new TestProjectFixture("ComponentWithNoDependencies", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();

                FrameworkDependentApp = TestApp.CreateFromBuiltAssets("HelloWorld");

                SelfContainedApp = TestApp.CreateFromBuiltAssets("HelloWorld");
                SelfContainedApp.PopulateSelfContained(TestApp.MockedComponent.None);

                ComponentTypeName = $"Component.Component, {ComponentWithNoDependenciesFixture.TestProject.AssemblyName}";
            }

            protected override void Dispose(bool disposing)
            {
                if (ComponentWithNoDependenciesFixture != null)
                    ComponentWithNoDependenciesFixture.Dispose();

                FrameworkDependentApp?.Dispose();
                SelfContainedApp?.Dispose();

                base.Dispose(disposing);
            }
        }
    }
}
