// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

using Microsoft.DotNet.Cli.Build.Framework;
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
            var app = sharedState.App;
            string[] args =
            {
                AppGetFunctionPointerArg,
                sharedState.HostFxrPath,
                app.AppDll,
                validType ? sharedState.FunctionPointerTypeName : $"Component.BadType, {app.AssemblyName}",
                validMethod ? sharedState.FunctionPointerEntryPoint1 : "BadMethod",
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should()
                .InitializeContextForApp(app.AppDll);

            if (validType && validMethod)
            {
                result.Should().Pass()
                    .And.ExecuteFunctionPointer(sharedState.FunctionPointerEntryPoint1, 1, 1)
                    .And.ExecuteInDefaultContext(app.AssemblyName);
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
            var component = sharedState.Component;
            string[] args =
            {
                ComponentGetFunctionPointerArg,
                sharedState.HostFxrPath,
                component.RuntimeConfigJson,
                validType ? sharedState.ComponentTypeName : $"Component.BadType, {component.AssemblyName}",
                validMethod ? sharedState.ComponentEntryPoint1 : "BadMethod",
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should()
                .InitializeContextForConfig(component.RuntimeConfigJson);

            // This should fail even with the valid type and valid method,
            // because the type is not resolvable from the default AssemblyLoadContext.
            result.Should().Fail();
        }

        [Fact]
        public void CallDelegateOnSelfContainedApplicationContext()
        {
            var app = sharedState.SelfContainedApp;
            string[] args =
            {
                AppGetFunctionPointerArg,
                app.HostFxrDll,
                app.AppDll,
                sharedState.FunctionPointerTypeName,
                sharedState.FunctionPointerEntryPoint1,
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForApp(app.AppDll)
                .And.ExecuteFunctionPointer(sharedState.FunctionPointerEntryPoint1, 1, 1)
                .And.ExecuteInDefaultContext(app.AssemblyName);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(10, false)]
        [InlineData(10, true)]
        public void CallDelegateOnApplicationContext_MultipleEntryPoints(int callCount, bool callUnmanaged)
        {
            var app = sharedState.App;
            string[] baseArgs =
            {
                AppGetFunctionPointerArg,
                sharedState.HostFxrPath,
                app.AppDll,
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
                .And.InitializeContextForApp(app.AppDll)
                .And.ExecuteInDefaultContext(app.AssemblyName);

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
            var app = sharedState.App;
            string[] baseArgs =
            {
                AppGetFunctionPointerArg,
                sharedState.HostFxrPath,
                app.AppDll,
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
                .And.InitializeContextForApp(app.AppDll)
                .And.ExecuteInDefaultContext(app.AssemblyName);

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
            var app = sharedState.App;
            string[] args =
            {
                AppGetFunctionPointerArg,
                sharedState.HostFxrPath,
                app.AppDll,
                sharedState.FunctionPointerTypeName,
                entryPoint,
            };

            sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.InitializeContextForApp(app.AppDll)
                .And.ExecuteFunctionPointerWithException(entryPoint, 1);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public string DotNetRoot { get; }

            public TestApp App { get; }
            public TestApp Component { get; }
            public TestApp SelfContainedApp { get; }

            public string ComponentTypeName { get; }
            public string ComponentEntryPoint1 => "ComponentEntryPoint1";
            public string FunctionPointerTypeName { get; }
            public string FunctionPointerEntryPoint1 => "FunctionPointerEntryPoint1";
            public string FunctionPointerEntryPoint2 => "FunctionPointerEntryPoint2";
            public string UnmanagedFunctionPointerEntryPoint1 => "UnmanagedFunctionPointerEntryPoint1";

            public SharedTestState()
            {
                DotNetRoot = TestContext.BuiltDotNet.BinPath;
                HostFxrPath = TestContext.BuiltDotNet.GreatestVersionHostFxrFilePath;

                App = TestApp.CreateFromBuiltAssets("AppWithCustomEntryPoints");
                Component = TestApp.CreateFromBuiltAssets("Component");
                SelfContainedApp = TestApp.CreateFromBuiltAssets("AppWithCustomEntryPoints");
                SelfContainedApp.PopulateSelfContained(TestApp.MockedComponent.None);

                ComponentTypeName = $"Component.Component, {Component.AssemblyName}";
                FunctionPointerTypeName = $"AppWithCustomEntryPoints.Program, {App.AssemblyName}";
            }

            protected override void Dispose(bool disposing)
            {
                App?.Dispose();
                Component?.Dispose();
                SelfContainedApp?.Dispose();

                base.Dispose(disposing);
            }
        }
    }
}
