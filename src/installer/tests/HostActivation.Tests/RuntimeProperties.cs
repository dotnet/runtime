// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.TestUtils;
using Xunit;

namespace HostActivation.Tests
{
    public class RuntimeProperties : IClassFixture<RuntimeProperties.SharedTestState>
    {
        private const string PrintProperties = "print_properties";

        private readonly SharedTestState sharedState;

        public RuntimeProperties(RuntimeProperties.SharedTestState fixture)
        {
            sharedState = fixture;
        }

        [Fact]
        public void AppConfigProperty_AppCanGetData()
        {
            sharedState.DotNet.Exec(sharedState.App.AppDll, PrintProperties, SharedTestState.AppTestPropertyName)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Property {SharedTestState.AppTestPropertyName} = {SharedTestState.AppTestPropertyValue}")
                .And.HavePropertyValue(SharedTestState.AppTestPropertyName, SharedTestState.AppTestPropertyValue);
        }

        [Fact]
        public void FrameworkConfigProperty_AppCanGetData()
        {
            sharedState.DotNet.Exec(sharedState.App.AppDll, PrintProperties, SharedTestState.FrameworkTestPropertyName)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Property {SharedTestState.FrameworkTestPropertyName} = {SharedTestState.FrameworkTestPropertyValue}")
                .And.HavePropertyValue(SharedTestState.FrameworkTestPropertyName, SharedTestState.FrameworkTestPropertyValue);
        }

        [Fact]
        public void DuplicateConfigProperty_AppConfigValueUsed()
        {
            var app = sharedState.App.Copy();
            RuntimeConfig.FromFile(app.RuntimeConfigJson)
                .WithProperty(SharedTestState.FrameworkTestPropertyName, SharedTestState.AppTestPropertyValue)
                .Save();

            sharedState.DotNet.Exec(app.AppDll, PrintProperties, SharedTestState.FrameworkTestPropertyName)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Property {SharedTestState.FrameworkTestPropertyName} = {SharedTestState.AppTestPropertyValue}")
                .And.HavePropertyValue(SharedTestState.FrameworkTestPropertyName, SharedTestState.AppTestPropertyValue);
        }

        [Fact]
        public void HostFxrPathProperty_SetWhenRunningSDKCommand()
        {
            var dotnet = sharedState.MockSDK;
            dotnet.Exec("--info")
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Property {SharedTestState.HostFxrPathPropertyName} = {dotnet.GreatestVersionHostFxrFilePath}");
        }

        [Fact]
        public void HostFxrPathProperty_NotVisibleFromApp()
        {
            sharedState.DotNet.Exec(sharedState.App.AppDll, PrintProperties, SharedTestState.HostFxrPathPropertyName)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.NotFindProperty(SharedTestState.HostFxrPathPropertyName);
        }

        [Fact]
        public void DuplicateCommonProperty_Fails()
        {
            var app = sharedState.App.Copy();

            string name = "RUNTIME_IDENTIFIER";
            RuntimeConfig.FromFile(app.RuntimeConfigJson)
                .WithProperty(name, SharedTestState.AppTestPropertyValue)
                .Save();

            sharedState.DotNet.Exec(app.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"Duplicate runtime property found: {name}");
        }

        [Fact]
        public void SpecifiedInConfigAndDevConfig_DevConfigWins()
        {
            var app = sharedState.App.Copy();

            const string devConfigValue = "VALUE_FROM_DEV_CONFIG";
            RuntimeConfig.FromFile(app.RuntimeDevConfigJson)
                .WithProperty(SharedTestState.AppTestPropertyName, devConfigValue)
                .Save();

            sharedState.DotNet.Exec(app.AppDll, PrintProperties, SharedTestState.AppTestPropertyName)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Property {SharedTestState.AppTestPropertyName} = {devConfigValue}")
                .And.HavePropertyValue(SharedTestState.AppTestPropertyName, devConfigValue);
        }

        [Fact]
        public void KnownHostProperty_AppCanGetData()
        {
            sharedState.DotNet.Exec(sharedState.App.AppDll, PrintProperties,
                    "TRUSTED_PLATFORM_ASSEMBLIES", "NATIVE_DLL_SEARCH_DIRECTORIES",
                    "PLATFORM_RESOURCE_ROOTS", "APP_PATHS")
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HavePropertyContaining("TRUSTED_PLATFORM_ASSEMBLIES",
                    Path.Combine(sharedState.DotNet.GreatestVersionSharedFxPath, "System.Private.CoreLib.dll"))
                .And.HavePropertyContaining("NATIVE_DLL_SEARCH_DIRECTORIES",
                    sharedState.DotNet.GreatestVersionSharedFxPath)
                .And.NotFindProperty("PLATFORM_RESOURCE_ROOTS")
                .And.NotFindProperty("APP_PATHS");
        }

        public class SharedTestState : IDisposable
        {
            public const string AppTestPropertyName = "APP_TEST_PROPERTY";
            public const string AppTestPropertyValue = "VALUE_FROM_APP";
            public const string FrameworkTestPropertyName = "FRAMEWORK_TEST_PROPERTY";
            public const string FrameworkTestPropertyValue = "VALUE_FROM_FRAMEWORK";
            public const string HostFxrPathPropertyName = "HOSTFXR_PATH";

            public TestApp App { get; }
            public DotNetCli MockSDK { get; }

            public DotNetCli DotNet { get; }
            private readonly TestArtifact copiedDotnet;

            public SharedTestState()
            {
                // Make a copy of the built .NET, as we will update the framework's runtime config
                copiedDotnet = TestArtifact.CreateFromCopy("runtimeProperties", HostTestContext.BuiltDotNet.BinPath);

                MockSDK = new DotNetBuilder(copiedDotnet.Location, HostTestContext.BuiltDotNet.BinPath, "mocksdk")
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr("9999.0.0")
                    .AddMockSDK("9999.0.0-dev", "9999.0.0")
                    .Build();

                GlobalJson.CreateWithVersion(MockSDK.BinPath, "9999.0.0-dev");

                App = TestApp.CreateFromBuiltAssets("HelloWorld");
                RuntimeConfig.FromFile(App.RuntimeConfigJson)
                    .WithProperty(AppTestPropertyName, AppTestPropertyValue)
                    .Save();

                DotNet = new DotNetCli(copiedDotnet.Location);
                RuntimeConfig.FromFile(Path.Combine(DotNet.GreatestVersionSharedFxPath, "Microsoft.NETCore.App.runtimeconfig.json"))
                    .WithProperty(FrameworkTestPropertyName, FrameworkTestPropertyValue)
                    .Save();
            }

            public void Dispose()
            {
                App?.Dispose();
                copiedDotnet.Dispose();
            }
        }
    }

    internal static class RuntimePropertyCommandResultExtensions
    {
        public static AndConstraint<CommandResultAssertions> NotFindProperty(this CommandResultAssertions assertion, string propertyName)
        {
            return assertion.HaveStdOutContaining($"Property '{propertyName}' was not found.");
        }

        public static AndConstraint<CommandResultAssertions> HavePropertyValue(this CommandResultAssertions assertion, string propertyName, string expectedValue)
        {
            return assertion.HaveStdOutContaining($"AppContext.GetData({propertyName}) = {expectedValue}");
        }

        public static AndConstraint<CommandResultAssertions> HavePropertyContaining(this CommandResultAssertions assertion, string propertyName, string expectedSubstring)
        {
            return assertion.HaveStdOutMatching(
                $@"AppContext\.GetData\({Regex.Escape(propertyName)}\) = .*{Regex.Escape(expectedSubstring)}");
        }
    }
}
