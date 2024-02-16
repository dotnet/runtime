// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

using Microsoft.DotNet.Cli.Build.Framework;
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
            var component = sharedState.Component;
            string[] args =
            {
                ComponentLoadAssemblyAndGetFunctionPointerArg,
                sharedState.HostFxrPath,
                component.RuntimeConfigJson,
                validPath ? component.AppDll : "BadPath...",
                validType ? sharedState.ComponentTypeName : $"Component.BadType, {component.AssemblyName}",
                validMethod ? sharedState.ComponentEntryPoint1 : "BadMethod",
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should()
                .InitializeContextForConfig(component.RuntimeConfigJson);

            if (validPath && validType && validMethod)
            {
                result.Should().Pass()
                    .And.ExecuteFunctionPointer(sharedState.ComponentEntryPoint1, 1, 1)
                    .And.ExecuteInIsolatedContext(component.AssemblyName);
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
            var component = sharedState.Component;
            string[] args =
            {
                AppLoadAssemblyAndGetFunctionPointerArg,
                sharedState.HostFxrPath,
                app.AppDll,
                validPath ? component.AppDll : "BadPath...",
                validType ? sharedState.ComponentTypeName : $"Component.BadType, {component.AssemblyName}",
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
                    .And.ExecuteInIsolatedContext(component.AssemblyName);
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
            var component = sharedState.Component;
            string[] args =
            {
                AppLoadAssemblyAndGetFunctionPointerArg,
                app.HostFxrDll,
                app.AppDll,
                component.AppDll,
                sharedState.ComponentTypeName,
                sharedState.ComponentEntryPoint1,
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForApp(app.AppDll)
                .And.ExecuteFunctionPointer(sharedState.ComponentEntryPoint1, 1, 1)
                .And.ExecuteInIsolatedContext(component.AssemblyName);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(10, false)]
        [InlineData(10, true)]
        public void CallDelegateOnComponentContext_MultipleEntryPoints(int callCount, bool callUnmanaged)
        {
            var component = sharedState.Component;
            string[] baseArgs =
            {
                ComponentLoadAssemblyAndGetFunctionPointerArg,
                sharedState.HostFxrPath,
                component.RuntimeConfigJson,
            };

            string comp1Name = callUnmanaged ? sharedState.UnmanagedComponentEntryPoint1 : sharedState.ComponentEntryPoint1;
            string[] componentInfo =
            {
                // [Unmanaged]ComponentEntryPoint1
                component.AppDll,
                sharedState.ComponentTypeName,
                comp1Name,
                // ComponentEntryPoint2
                component.AppDll,
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
                .And.InitializeContextForConfig(component.RuntimeConfigJson)
                .And.ExecuteInIsolatedContext(component.AssemblyName);

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
            var component = sharedState.Component;
            var componentCopy = component.Copy();
            string[] baseArgs =
            {
                ComponentLoadAssemblyAndGetFunctionPointerArg,
                sharedState.HostFxrPath,
                component.RuntimeConfigJson,
            };
            string[] componentInfo =
            {
                // Component
                component.AppDll,
                sharedState.ComponentTypeName,
                sharedState.ComponentEntryPoint1,
                // Component copy
                componentCopy.AppDll,
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
                .And.InitializeContextForConfig(component.RuntimeConfigJson)
                .And.ExecuteInIsolatedContext(component.AssemblyName);

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
            var component = sharedState.Component;
            string[] args =
            {
                ComponentLoadAssemblyAndGetFunctionPointerArg,
                sharedState.HostFxrPath,
                component.RuntimeConfigJson,
                component.AppDll,
                sharedState.ComponentTypeName,
                entryPoint,
            };

            sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.InitializeContextForConfig(component.RuntimeConfigJson)
                .And.ExecuteFunctionPointerWithException(entryPoint, 1);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public string DotNetRoot { get; }

            public TestApp Component { get; }
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

                Component = TestApp.CreateFromBuiltAssets("Component");

                FrameworkDependentApp = TestApp.CreateFromBuiltAssets("HelloWorld");

                SelfContainedApp = TestApp.CreateFromBuiltAssets("HelloWorld");
                SelfContainedApp.PopulateSelfContained(TestApp.MockedComponent.None);

                ComponentTypeName = $"Component.Component, {Component.AssemblyName}";
            }

            protected override void Dispose(bool disposing)
            {
                Component?.Dispose();
                FrameworkDependentApp?.Dispose();
                SelfContainedApp?.Dispose();

                base.Dispose(disposing);
            }
        }
    }
}
