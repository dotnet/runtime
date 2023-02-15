// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Cli.Build;
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
                .EnableTracingAndCaptureOutputs()
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
                .EnableTracingAndCaptureOutputs()
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
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Property {sharedState.FrameworkTestPropertyName} = {sharedState.AppTestPropertyValue}")
                .And.HaveStdOutContaining($"AppContext.GetData({sharedState.FrameworkTestPropertyName}) = {sharedState.AppTestPropertyValue}");
        }

        [Fact]
        public void HostFxrPathProperty_SetWhenRunningSDKCommand()
        {
            var dotnet = sharedState.MockSDK;
            dotnet.Exec("--info")
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Property {sharedState.HostFxrPathPropertyName} = {dotnet.GreatestVersionHostFxrFilePath}");
        }

        [Fact]
        public void HostFxrPathProperty_NotVisibleFromApp()
        {
            var fixture = sharedState.RuntimePropertiesFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            dotnet.Exec(appDll, sharedState.HostFxrPathPropertyName)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining($"Property '{sharedState.HostFxrPathPropertyName}' was not found.");
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
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"Duplicate runtime property found: {name}");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture RuntimePropertiesFixture { get; }
            public RepoDirectoriesProvider RepoDirectories { get; }
            public DotNetCli MockSDK { get; }

            public string AppTestPropertyName => "APP_TEST_PROPERTY";
            public string AppTestPropertyValue => "VALUE_FROM_APP";
            public string FrameworkTestPropertyName => "FRAMEWORK_TEST_PROPERTY";
            public string FrameworkTestPropertyValue => "VALUE_FROM_FRAMEWORK";
            public string HostFxrPathPropertyName => "HOSTFXR_PATH";

            private readonly TestArtifact copiedDotnet;

            public SharedTestState()
            {
                copiedDotnet = new TestArtifact(Path.Combine(TestArtifact.TestArtifactsPath, "runtimeProperties"));
                SharedFramework.CopyDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"), copiedDotnet.Location);

                MockSDK = new DotNetBuilder(copiedDotnet.Location, Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"), "exe")
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr("9999.0.0")
                    .AddMockSDK("9999.0.0-dev", "9999.0.0")
                    .Build();

                File.WriteAllText(Path.Combine(MockSDK.BinPath, "global.json"),
                    @"
{
    ""sdk"": {
      ""version"": ""9999.0.0-dev""
    }
}");

                RepoDirectories = new RepoDirectoriesProvider(builtDotnet: copiedDotnet.Location);

                RuntimePropertiesFixture = new TestProjectFixture("RuntimeProperties", RepoDirectories)
                    .EnsureRestored()
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
                copiedDotnet.Dispose();
            }
        }
    }
}
