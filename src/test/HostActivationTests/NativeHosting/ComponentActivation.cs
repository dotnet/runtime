// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public partial class ComponentActivation : IClassFixture<ComponentActivation.SharedTestState>
    {
        private const string ComponentActivationArg = "load_assembly_and_get_function_pointer";

        private readonly SharedTestState sharedState;

        public ComponentActivation(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        public void CallDelegate(bool validPath, bool validType, bool validMethod)
        {
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            string[] args =
            {
                ComponentActivationArg,
                sharedState.HostFxrPath,
                componentProject.RuntimeConfigJson,
                validPath ? componentProject.AppDll : "BadPath...",
                validType ? sharedState.ComponentTypeName : $"Component.BadType, {componentProject.AssemblyName}",
                validMethod ? sharedState.ComponentEntryPoint1 : "BadMethod",
            };
            CommandResult result = Command.Create(sharedState.NativeHostPath, args)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROOT", sharedState.DotNetRoot)
                .EnvironmentVariable("DOTNET_ROOT(x86)", sharedState.DotNetRoot)
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
        [InlineData(1)]
        [InlineData(10)]
        public void CallDelegate_MultipleEntryPoints(int callCount)
        {
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            string[] baseArgs =
            {
                ComponentActivationArg,
                sharedState.HostFxrPath,
                componentProject.RuntimeConfigJson,
            };
            string[] componentInfo =
            {
                // ComponentEntryPoint1
                componentProject.AppDll,
                sharedState.ComponentTypeName,
                sharedState.ComponentEntryPoint1,
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

            CommandResult result = Command.Create(sharedState.NativeHostPath, args)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROOT", sharedState.DotNetRoot)
                .EnvironmentVariable("DOTNET_ROOT(x86)", sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForConfig(componentProject.RuntimeConfigJson);

            for (int i = 1; i <= callCount; ++i)
            {
                result.Should()
                    .ExecuteComponentEntryPoint(sharedState.ComponentEntryPoint1, i * 2 - 1, i)
                    .And.ExecuteComponentEntryPoint(sharedState.ComponentEntryPoint2, i * 2, i);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void CallDelegate_MultipleComponents(int callCount)
        {
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            var componentProjectCopy = componentProject.Copy();
            string[] baseArgs =
            {
                ComponentActivationArg,
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

            CommandResult result = Command.Create(sharedState.NativeHostPath, args)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROOT", sharedState.DotNetRoot)
                .EnvironmentVariable("DOTNET_ROOT(x86)", sharedState.DotNetRoot)
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

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public string DotNetRoot { get; }

            public TestProjectFixture ComponentWithNoDependenciesFixture { get; }
            public string ComponentTypeName { get; }
            public string ComponentEntryPoint1 => "ComponentEntryPoint1";
            public string ComponentEntryPoint2 => "ComponentEntryPoint2";

            public SharedTestState()
            {
                var dotNet = new Microsoft.DotNet.Cli.Build.DotNetCli(Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"));
                DotNetRoot = dotNet.BinPath;
                HostFxrPath = dotNet.GreatestVersionHostFxrFilePath;

                ComponentWithNoDependenciesFixture = new TestProjectFixture("ComponentWithNoDependencies", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
                ComponentTypeName = $"Component.Component, {ComponentWithNoDependenciesFixture.TestProject.AssemblyName}";
            }

            protected override void Dispose(bool disposing)
            {
                if (ComponentWithNoDependenciesFixture != null)
                    ComponentWithNoDependenciesFixture.Dispose();

                base.Dispose(disposing);
            }
        }
    }

    internal static class ComponentActivationResultExtensions
    {
        public static FluentAssertions.AndConstraint<CommandResultAssertions> ExecuteComponentEntryPoint(this CommandResultAssertions assertion, string methodName, int componentCallCount, int returnValue)
        {
            return assertion.HaveStdOutContaining($"Called {methodName}(0xdeadbeef, 42) - component call count: {componentCallCount}")
                .And.HaveStdOutContaining($"{methodName} delegate result: 0x{returnValue.ToString("x")}");
        }
    }
}
