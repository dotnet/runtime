// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class RuntimeProperties : IClassFixture<RuntimeProperties.SharedTestState>
    {
        private SharedTestState sharedState;

        public RuntimeProperties(RuntimeProperties.SharedTestState fixture)
        {
            sharedState = fixture;
        }

        [Fact]
        public void AppConfigProperty_AppCanGetData()
        {
            var fixture = sharedState.RuntimePropertiesFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            dotnet.Exec(appDll, sharedState.AppTestPropertyName)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Property {sharedState.AppTestPropertyName} = {sharedState.AppTestPropertyValue}")
                .And.HaveStdOutContaining($"AppContext.GetData({sharedState.AppTestPropertyName}) = {sharedState.AppTestPropertyValue}");
        }

        [Fact]
        public void FrameworkConfigProperty_AppCanGetData()
        {
            var fixture = sharedState.RuntimePropertiesFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            dotnet.Exec(appDll, sharedState.FrameworkTestPropertyName)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Property {sharedState.FrameworkTestPropertyName} = {sharedState.FrameworkTestPropertyValue}")
                .And.HaveStdOutContaining($"AppContext.GetData({sharedState.FrameworkTestPropertyName}) = {sharedState.FrameworkTestPropertyValue}");
        }

        [Fact]
        public void DuplicateConfigProperty_AppConfigValueUsed()
        {
            var fixture = sharedState.RuntimePropertiesFixture
                .Copy();

            RuntimeConfig.FromFile(fixture.TestProject.RuntimeConfigJson)
                .WithProperty(sharedState.FrameworkTestPropertyName, sharedState.AppTestPropertyValue)
                .Save();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            dotnet.Exec(appDll, sharedState.FrameworkTestPropertyName)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Property {sharedState.FrameworkTestPropertyName} = {sharedState.AppTestPropertyValue}")
                .And.HaveStdOutContaining($"AppContext.GetData({sharedState.FrameworkTestPropertyName}) = {sharedState.AppTestPropertyValue}");
        }

        [Fact]
        public void DuplicateCommonProperty_Fails()
        {
            var fixture = sharedState.RuntimePropertiesFixture
                .Copy();

            string name = "RUNTIME_IDENTIFIER";
            RuntimeConfig.FromFile(fixture.TestProject.RuntimeConfigJson)
                .WithProperty(name, sharedState.AppTestPropertyValue)
                .Save();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"Duplicate runtime property found: {name}");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture RuntimePropertiesFixture { get; }
            public RepoDirectoriesProvider RepoDirectories { get; }

            public string AppTestPropertyName => "APP_TEST_PROPERTY";
            public string AppTestPropertyValue => "VALUE_FROM_APP";
            public string FrameworkTestPropertyName => "FRAMEWORK_TEST_PROPERTY";
            public string FrameworkTestPropertyValue => "VALUE_FROM_FRAMEWORK";

            private readonly string copiedDotnet;

            public SharedTestState()
            {
                copiedDotnet = Path.Combine(TestArtifact.TestArtifactsPath, "runtimeProperties");
                SharedFramework.CopyDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"), copiedDotnet);

                RepoDirectories = new RepoDirectoriesProvider(builtDotnet: copiedDotnet);

                RuntimePropertiesFixture = new TestProjectFixture("RuntimeProperties", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .BuildProject();

                RuntimeConfig.FromFile(RuntimePropertiesFixture.TestProject.RuntimeConfigJson)
                    .WithProperty(AppTestPropertyName, AppTestPropertyValue)
                    .Save();

                RuntimeConfig.FromFile(Path.Combine(RuntimePropertiesFixture.BuiltDotnet.GreatestVersionSharedFxPath, "Microsoft.NETCore.App.runtimeconfig.json"))
                    .WithProperty(FrameworkTestPropertyName, FrameworkTestPropertyValue)
                    .Save();
            }

            public void Dispose()
            {
                RuntimePropertiesFixture.Dispose();
                if (!TestArtifact.PreserveTestRuns() && Directory.Exists(copiedDotnet))
                {
                    Directory.Delete(copiedDotnet, true);
                }
            }
        }
    }
}
