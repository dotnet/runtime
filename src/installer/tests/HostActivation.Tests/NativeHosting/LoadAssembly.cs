// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public class LoadAssembly : IClassFixture<LoadAssembly.SharedTestState>
    {
        private const string AppLoadAssemblyArg = "app_load_assembly";
        private const string ComponentLoadAssemblyArg = "component_load_assembly";

        private const string AppLoadAssemblyBytesArg = "app_load_assembly_bytes";
        private const string ComponentLoadAssemblyBytesArg = "component_load_assembly_bytes";

        private readonly SharedTestState sharedState;

        public LoadAssembly(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        private void ApplicationContext(bool loadAssemblyBytes, bool loadSymbolBytes)
        {
            var app = sharedState.Application;
            var component = sharedState.Component;
            IEnumerable<string> args = new[]
            {
                loadAssemblyBytes ? AppLoadAssemblyBytesArg : AppLoadAssemblyArg,
                sharedState.HostFxrPath,
                app.AppDll
            }.Concat(sharedState.GetComponentLoadArgs(loadAssemblyBytes, loadSymbolBytes));

            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForApp(app.AppDll)
                .And.ExecuteSelfContained(selfContained: false)
                .And.ExecuteInDefaultContext(component.AssemblyName)
                .And.ExecuteWithLocation(component.AssemblyName, loadAssemblyBytes ? string.Empty : component.AppDll)
                .And.ExecuteFunctionPointer(sharedState.ComponentEntryPoint1, 1, 1);
        }

        [Fact]
        public void ApplicationContext_FilePath()
        {
            ApplicationContext(loadAssemblyBytes: false, loadSymbolBytes: false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ApplicationContext_Bytes(bool loadSymbolBytes)
        {
            ApplicationContext(loadAssemblyBytes: true, loadSymbolBytes);
        }

        private void ComponentContext(bool loadAssemblyBytes, bool loadSymbolBytes)
        {
            var app = sharedState.Application;
            var component = sharedState.Component;
            IEnumerable<string> args = new[]
            {
                loadAssemblyBytes ? ComponentLoadAssemblyBytesArg : ComponentLoadAssemblyArg,
                sharedState.HostFxrPath,
                component.RuntimeConfigJson
            }.Concat(sharedState.GetComponentLoadArgs(loadAssemblyBytes, loadSymbolBytes));

            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForConfig(component.RuntimeConfigJson)
                .And.ExecuteInDefaultContext(component.AssemblyName)
                .And.ExecuteWithLocation(component.AssemblyName, loadAssemblyBytes ? string.Empty : component.AppDll)
                .And.ExecuteFunctionPointer(sharedState.ComponentEntryPoint1, 1, 1);
        }

        [Fact]
        public void ComponentContext_FilePath()
        {
            ComponentContext(loadAssemblyBytes: false, loadSymbolBytes: false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ComponentContext_Bytes(bool loadSymbolBytes)
        {
            ComponentContext(loadAssemblyBytes: true, loadSymbolBytes);
        }

        private void SelfContainedApplicationContext(bool loadAssemblyBytes, bool loadSymbolBytes)
        {
            var app = sharedState.SelfContainedApplication;
            var component = sharedState.Component;
            IEnumerable<string> args = new[]
            {
                loadAssemblyBytes ? AppLoadAssemblyBytesArg : AppLoadAssemblyArg,
                app.HostFxrDll,
                app.AppDll
            }.Concat(sharedState.GetComponentLoadArgs(loadAssemblyBytes, loadSymbolBytes));

            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForApp(app.AppDll)
                .And.ExecuteSelfContained(selfContained: true)
                .And.ExecuteInDefaultContext(component.AssemblyName)
                .And.ExecuteWithLocation(component.AssemblyName, loadAssemblyBytes ? string.Empty : component.AppDll)
                .And.ExecuteFunctionPointer(sharedState.ComponentEntryPoint1, 1, 1);
        }

        [Fact]
        public void SelfContainedApplicationContext_FilePath()
        {
            SelfContainedApplicationContext(loadAssemblyBytes: false, loadSymbolBytes: false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SelfContainedApplicationContext_Bytes(bool loadSymbolBytes)
        {
            SelfContainedApplicationContext(loadAssemblyBytes: true, loadSymbolBytes);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public string DotNetRoot { get; }

            public TestApp Application { get; }
            public TestApp Component { get; }
            public TestApp SelfContainedApplication { get; }

            public string ComponentTypeName { get; }
            public string ComponentEntryPoint1 => "ComponentEntryPoint1";

            public SharedTestState()
            {
                DotNetRoot = TestContext.BuiltDotNet.BinPath;
                HostFxrPath = TestContext.BuiltDotNet.GreatestVersionHostFxrFilePath;

                Application = TestApp.CreateEmpty("App");
                Application.PopulateFrameworkDependent(Constants.MicrosoftNETCoreApp, TestContext.MicrosoftNETCoreAppVersion);

                SelfContainedApplication = TestApp.CreateEmpty("SelfContainedApp");
                SelfContainedApplication.PopulateSelfContained(TestApp.MockedComponent.None);

                Component = TestApp.CreateFromBuiltAssets("Component");
                ComponentTypeName = $"Component.Component, {Component.AssemblyName}";
            }

            internal IEnumerable<string> GetComponentLoadArgs(bool loadAssemblyBytes, bool loadSymbolBytes)
            {
                List<string> args = new List<string>() { Component.AppDll };
                if (loadAssemblyBytes)
                    args.Add(loadSymbolBytes ? $"{Path.GetFileNameWithoutExtension(Component.AppDll)}.pdb" : "nullptr");

                args.Add(ComponentTypeName);
                args.Add(ComponentEntryPoint1);
                return args;
            }

            protected override void Dispose(bool disposing)
            {
                Application?.Dispose();
                Component?.Dispose();
                SelfContainedApplication?.Dispose();

                base.Dispose(disposing);
            }
        }
    }
}
