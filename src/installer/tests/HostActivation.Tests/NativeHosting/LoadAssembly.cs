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

        private const string DefaultLoadContextArg = "<default>";

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
                component.RuntimeConfigJson,
                DefaultLoadContextArg
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

        private void ComponentContext_LoadContextIdentifier(bool loadAssemblyBytes, bool useSameIdentifier)
        {
            TestApp component = sharedState.Component;
            using TestApp componentCopy = component.Copy();
            string testName = loadAssemblyBytes
                ? nameof(ComponentContext_Bytes_LoadContextIdentifier)
                : nameof(ComponentContext_FilePath_LoadContextIdentifier);
            string identifierA = $"{nameof(LoadAssembly)}.{testName}.A";
            string identifierB = useSameIdentifier ? identifierA : $"{nameof(LoadAssembly)}.{testName}.B";
            string entryPoint = sharedState.ComponentEntryPoint1;
            List<string> args = new()
            {
                loadAssemblyBytes ? ComponentLoadAssemblyBytesArg : ComponentLoadAssemblyArg,
                sharedState.HostFxrPath,
                component.RuntimeConfigJson,
                identifierA,
                component.AppDll,
            };
            if (loadAssemblyBytes)
                args.Add("nullptr");

            args.AddRange(
            [
                sharedState.ComponentTypeName,
                entryPoint,
                identifierB,
                componentCopy.AppDll,
            ]);
            if (loadAssemblyBytes)
                args.Add("nullptr");

            args.Add(sharedState.ComponentTypeName);
            args.Add(entryPoint);

            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForConfig(component.RuntimeConfigJson)
                .And.ExecuteInNamedContext(component.AssemblyName, identifierA);

            if (loadAssemblyBytes)
                result.Should().ExecuteWithLocation(component.AssemblyName, string.Empty);

            if (useSameIdentifier)
            {
                result.Should().ExecuteFunctionPointer(methodName: entryPoint, callCount: 1, returnValue: 1)
                    .And.ExecuteFunctionPointer(methodName: entryPoint, callCount: 2, returnValue: 2);
            }
            else
            {
                result.Should().ExecuteInNamedContext(component.AssemblyName, identifierB)
                    .And.ExecuteFunctionPointer(methodName: entryPoint, callCount: 1, returnValue: 1);
                Assert.Equal(2, result.StdOut.Split($"{entryPoint} delegate result: 0x1").Length - 1);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ComponentContext_FilePath_LoadContextIdentifier(bool useSameIdentifier)
        {
            ComponentContext_LoadContextIdentifier(loadAssemblyBytes: false, useSameIdentifier);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ComponentContext_Bytes_LoadContextIdentifier(bool useSameIdentifier)
        {
            ComponentContext_LoadContextIdentifier(loadAssemblyBytes: true, useSameIdentifier);
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
                DotNetRoot = HostTestContext.BuiltDotNet.BinPath;
                HostFxrPath = HostTestContext.BuiltDotNet.GreatestVersionHostFxrFilePath;

                Application = TestApp.CreateEmpty("App");
                Application.PopulateFrameworkDependent(Constants.MicrosoftNETCoreApp, HostTestContext.MicrosoftNETCoreAppVersion);

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
