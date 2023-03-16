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
    public partial class LoadAssembly : IClassFixture<LoadAssembly.SharedTestState>
    {
        private const string AppLoadAssemblyArg = "app_load_assembly";
        private const string ComponentLoadAssemblyArg = "component_load_assembly";

        private readonly SharedTestState sharedState;

        public LoadAssembly(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Fact]
        public void ApplicationContext()
        {
            var appProject = sharedState.Application;
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            string[] args =
            {
                AppLoadAssemblyArg,
                sharedState.HostFxrPath,
                appProject.AppDll,
                componentProject.AppDll,
                sharedState.ComponentTypeName,
                sharedState.ComponentEntryPoint1,
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForApp(appProject.AppDll)
                .And.ExecuteSelfContained(selfContained: false)
                .And.ExecuteInDefaultContext(componentProject.AssemblyName)
                .And.ExecuteFunctionPointer(sharedState.ComponentEntryPoint1, 1, 1);
        }

        [Fact]
        public void ComponentContext()
        {
            var appProject = sharedState.Application;
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            string[] args =
            {
                ComponentLoadAssemblyArg,
                sharedState.HostFxrPath,
                componentProject.RuntimeConfigJson,
                componentProject.AppDll,
                sharedState.ComponentTypeName,
                sharedState.ComponentEntryPoint1,
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForConfig(componentProject.RuntimeConfigJson)
                .And.ExecuteInDefaultContext(componentProject.AssemblyName)
                .And.ExecuteFunctionPointer(sharedState.ComponentEntryPoint1, 1, 1);
        }

        [Fact]
        public void SelfContainedApplicationContext()
        {
            var appProject = sharedState.SelfContainedApplication;
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            string[] args =
            {
                AppLoadAssemblyArg,
                appProject.HostFxrDll,
                appProject.AppDll,
                componentProject.AppDll,
                sharedState.ComponentTypeName,
                sharedState.ComponentEntryPoint1
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForApp(appProject.AppDll)
                .And.ExecuteSelfContained(selfContained: true)
                .And.ExecuteInDefaultContext(componentProject.AssemblyName)
                .And.ExecuteFunctionPointer(sharedState.ComponentEntryPoint1, 1, 1);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public string DotNetRoot { get; }

            public TestApp Application { get; }
            public TestApp SelfContainedApplication { get; }

            public TestProjectFixture ComponentWithNoDependenciesFixture { get; }

            public string ComponentTypeName { get; }
            public string ComponentEntryPoint1 => "ComponentEntryPoint1";
            public string UnmanagedFunctionPointerEntryPoint1 => "UnmanagedFunctionPointerEntryPoint1";

            public SharedTestState()
            {
                var dotNet = new Microsoft.DotNet.Cli.Build.DotNetCli(RepoDirectories.BuiltDotnet);
                DotNetRoot = dotNet.BinPath;
                HostFxrPath = dotNet.GreatestVersionHostFxrFilePath;

                Application = TestApp.CreateEmpty("App");
                Application.PopulateFrameworkDependent(Constants.MicrosoftNETCoreApp, RepoDirectories.MicrosoftNETCoreAppVersion);

                SelfContainedApplication = TestApp.CreateEmpty("SelfContainedApp");
                SelfContainedApplication.PopulateSelfContained(TestApp.MockedComponent.None);

                ComponentWithNoDependenciesFixture = new TestProjectFixture("ComponentWithNoDependencies", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();

                ComponentTypeName = $"Component.Component, {ComponentWithNoDependenciesFixture.TestProject.AssemblyName}";
            }

            protected override void Dispose(bool disposing)
            {
                if (Application != null)
                    Application.Dispose();

                if (SelfContainedApplication != null)
                    SelfContainedApplication.Dispose();

                if (ComponentWithNoDependenciesFixture != null)
                    ComponentWithNoDependenciesFixture.Dispose();

                base.Dispose(disposing);
            }
        }
    }
}
