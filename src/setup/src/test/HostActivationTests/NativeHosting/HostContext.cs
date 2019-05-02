// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public class HostContext : IClassFixture<HostContext.SharedTestState>
    {
        public class Scenario
        {
            public const string App = "app";
            public const string Config = "config";
            public const string ConfigMultiple = "config_multiple";
            public const string Mixed = "mixed";
        }

        public class CheckProperties
        {
            public const string None = "none";
            public const string Get = "get";
            public const string Set = "set";
            public const string Remove = "remove";
            public const string GetAll = "get_all";
            public const string GetActive = "get_active";
            public const string GetAllActive = "get_all_active";
        }

        public class LogPrefix
        {
            public const string App = "[APP] ";
            public const string Config = "[CONFIG] ";
            public const string Secondary = "[SECONDARY] ";
        }

        private const string HostContextArg = "host_context";
        private const string PropertyValueFromHost = "VALUE_FROM_HOST";

        private const int InvalidArgFailure = unchecked((int)0x80008081);
        private const int HostInvalidState = unchecked((int)0x800080a3);
        private const int HostPropertyNotFound = unchecked((int)0x800080a4);

        private readonly SharedTestState sharedState;

        public HostContext(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Theory]
        [InlineData(CheckProperties.None)]
        [InlineData(CheckProperties.Get)]
        [InlineData(CheckProperties.Set)]
        [InlineData(CheckProperties.Remove)]
        [InlineData(CheckProperties.GetAll)]
        public void RunApp(string checkProperties)
        {
            string newPropertyName = "HOST_TEST_PROPERTY";
            string[] args =
            {
                HostContextArg,
                Scenario.App,
                checkProperties,
                sharedState.HostFxrPath,
                sharedState.AppPath,
                sharedState.AppPropertyName,
                newPropertyName
            };
            CommandResult result = Command.Create(sharedState.NativeHostPath, args)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROOT", sharedState.DotNetRoot)
                .EnvironmentVariable("DOTNET_ROOT(x86)", sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForApp(sharedState.AppPath)
                .And.ExecuteAssemblyMock(sharedState.AppPath);

            switch (checkProperties)
            {
                case CheckProperties.None:
                    int appArgIndex = 5;
                    result.Should()
                        .HavePropertyMock(sharedState.AppPropertyName, sharedState.AppPropertyValue)
                        .And.HaveStdOutContaining($"mock argc:{args.Length - appArgIndex}");
                    for (int i = appArgIndex; i < args.Length; ++i)
                    {
                        result.Should().HaveStdOutContaining($"mock argv[{i - appArgIndex}] = {args[i]}");
                    }
                    break;
                case CheckProperties.Get:
                    result.Should()
                        .GetRuntimePropertyValue(LogPrefix.App, sharedState.AppPropertyName, sharedState.AppPropertyValue)
                        .And.FailToGetRuntimePropertyValue(LogPrefix.App, newPropertyName, HostPropertyNotFound)
                        .And.HavePropertyMock(sharedState.AppPropertyName, sharedState.AppPropertyValue)
                        .And.NotHavePropertyMock(newPropertyName);
                    break;
                case CheckProperties.Set:
                    result.Should()
                        .SetRuntimePropertyValue(LogPrefix.App, sharedState.AppPropertyName)
                        .And.SetRuntimePropertyValue(LogPrefix.App, newPropertyName)
                        .And.HavePropertyMock(sharedState.AppPropertyName, PropertyValueFromHost)
                        .And.HavePropertyMock(newPropertyName, PropertyValueFromHost);
                    break;
                case CheckProperties.Remove:
                    result.Should()
                        .SetRuntimePropertyValue(LogPrefix.App, sharedState.AppPropertyName)
                        .And.SetRuntimePropertyValue(LogPrefix.App, newPropertyName)
                        .And.NotHavePropertyMock(sharedState.AppPropertyName)
                        .And.NotHavePropertyMock(newPropertyName);
                    break;
                case CheckProperties.GetAll:
                    result.Should()
                        .GetRuntimePropertiesIncludes(LogPrefix.App, sharedState.AppPropertyName, sharedState.AppPropertyValue)
                        .And.HavePropertyMock(sharedState.AppPropertyName, sharedState.AppPropertyValue);
                    break;
                default:
                    throw new Exception($"Unknown option: {checkProperties}");
            }
        }

        [Theory]
        [InlineData(CheckProperties.None)]
        [InlineData(CheckProperties.Get)]
        [InlineData(CheckProperties.Set)]
        [InlineData(CheckProperties.Remove)]
        [InlineData(CheckProperties.GetAll)]
        public void GetDelegate(string checkProperties)
        {
            string newPropertyName = "HOST_TEST_PROPERTY";
            string[] args =
            {
                HostContextArg,
                Scenario.Config,
                checkProperties,
                sharedState.HostFxrPath,
                sharedState.RuntimeConfigPath,
                sharedState.ConfigPropertyName,
                newPropertyName
            };
            CommandResult result = Command.Create(sharedState.NativeHostPath, args)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROOT", sharedState.DotNetRoot)
                .EnvironmentVariable("DOTNET_ROOT(x86)", sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForConfig(sharedState.RuntimeConfigPath)
                .And.CreateDelegateMock();

            switch (checkProperties)
            {
                case CheckProperties.None:
                    result.Should()
                        .HavePropertyMock(sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue);
                    break;
                case CheckProperties.Get:
                    result.Should()
                        .GetRuntimePropertyValue(LogPrefix.Config, sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue)
                        .And.FailToGetRuntimePropertyValue(LogPrefix.Config, newPropertyName, HostPropertyNotFound)
                        .And.HavePropertyMock(sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue);
                    break;
                case CheckProperties.Set:
                    result.Should()
                        .SetRuntimePropertyValue(LogPrefix.Config, sharedState.ConfigPropertyName)
                        .And.SetRuntimePropertyValue(LogPrefix.Config, newPropertyName)
                        .And.HavePropertyMock(sharedState.ConfigPropertyName, PropertyValueFromHost)
                        .And.HavePropertyMock(newPropertyName, PropertyValueFromHost);
                    break;
                case CheckProperties.Remove:
                    result.Should()
                        .SetRuntimePropertyValue(LogPrefix.Config, sharedState.ConfigPropertyName)
                        .And.SetRuntimePropertyValue(LogPrefix.Config, newPropertyName)
                        .And.NotHavePropertyMock(sharedState.ConfigPropertyName)
                        .And.NotHavePropertyMock(newPropertyName);
                    break;
                case CheckProperties.GetAll:
                    result.Should()
                        .GetRuntimePropertiesIncludes(LogPrefix.Config, sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue)
                        .And.HavePropertyMock(sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue);
                    break;
                default:
                    throw new Exception($"Unknown option: {checkProperties}");
            }
        }

        [Theory]
        [InlineData(CheckProperties.None)]
        [InlineData(CheckProperties.Get)]
        [InlineData(CheckProperties.Set)]
        [InlineData(CheckProperties.Remove)]
        [InlineData(CheckProperties.GetAll)]
        [InlineData(CheckProperties.GetActive)]
        [InlineData(CheckProperties.GetAllActive)]
        public void GetDelegate_Multiple(string checkProperties)
        {
            string[] args =
            {
                HostContextArg,
                Scenario.ConfigMultiple,
                checkProperties,
                sharedState.HostFxrPath,
                sharedState.RuntimeConfigPath,
                sharedState.SecondaryRuntimeConfigPath,
                sharedState.ConfigPropertyName,
                sharedState.SecondaryConfigPropertyName
            };
            CommandResult result = Command.Create(sharedState.NativeHostPath, args)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROOT", sharedState.DotNetRoot)
                .EnvironmentVariable("DOTNET_ROOT(x86)", sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForConfig(sharedState.RuntimeConfigPath)
                .And.InitializeSecondaryContext(sharedState.SecondaryRuntimeConfigPath)
                .And.CreateDelegateMock();

            switch (checkProperties)
            {
                case CheckProperties.None:
                    result.Should()
                        .HavePropertyMock(sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue);
                    break;
                case CheckProperties.Get:
                    result.Should()
                        .GetRuntimePropertyValue(LogPrefix.Config, sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue)
                        .And.FailToGetRuntimePropertyValue(LogPrefix.Config, sharedState.SecondaryConfigPropertyName, HostPropertyNotFound)
                        .And.FailToGetRuntimePropertyValue(LogPrefix.Secondary, sharedState.ConfigPropertyName, HostPropertyNotFound)
                        .And.GetRuntimePropertyValue(LogPrefix.Secondary, sharedState.SecondaryConfigPropertyName, sharedState.SecondaryConfigPropertyValue)
                        .And.HavePropertyMock(sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue)
                        .And.NotHavePropertyMock(sharedState.SecondaryConfigPropertyName);
                    break;
                case CheckProperties.Set:
                    result.Should()
                        .SetRuntimePropertyValue(LogPrefix.Config, sharedState.ConfigPropertyName)
                        .And.SetRuntimePropertyValue(LogPrefix.Config, sharedState.SecondaryConfigPropertyName)
                        .And.FailToSetRuntimePropertyValue(LogPrefix.Secondary, sharedState.ConfigPropertyName, InvalidArgFailure)
                        .And.FailToSetRuntimePropertyValue(LogPrefix.Secondary, sharedState.SecondaryConfigPropertyName, InvalidArgFailure)
                        .And.HavePropertyMock(sharedState.ConfigPropertyName, PropertyValueFromHost)
                        .And.HavePropertyMock(sharedState.SecondaryConfigPropertyName, PropertyValueFromHost);
                    break;
                case CheckProperties.Remove:
                    result.Should()
                        .SetRuntimePropertyValue(LogPrefix.Config, sharedState.ConfigPropertyName)
                        .And.SetRuntimePropertyValue(LogPrefix.Config, sharedState.SecondaryConfigPropertyName)
                        .And.FailToSetRuntimePropertyValue(LogPrefix.Secondary, sharedState.ConfigPropertyName, InvalidArgFailure)
                        .And.FailToSetRuntimePropertyValue(LogPrefix.Secondary, sharedState.SecondaryConfigPropertyName, InvalidArgFailure)
                        .And.NotHavePropertyMock(sharedState.ConfigPropertyName)
                        .And.NotHavePropertyMock(sharedState.SecondaryConfigPropertyName);
                    break;
                case CheckProperties.GetAll:
                    result.Should()
                        .GetRuntimePropertiesIncludes(LogPrefix.Config, sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue)
                        .And.GetRuntimePropertiesIncludes(LogPrefix.Secondary, sharedState.SecondaryConfigPropertyName, sharedState.SecondaryConfigPropertyValue)
                        .And.GetRuntimePropertiesExcludes(LogPrefix.Secondary, sharedState.ConfigPropertyName)
                        .And.HavePropertyMock(sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue);
                    break;
                case CheckProperties.GetActive:
                    result.Should()
                        .FailToGetRuntimePropertyValue(LogPrefix.Config, sharedState.ConfigPropertyName, HostInvalidState)
                        .And.FailToGetRuntimePropertyValue(LogPrefix.Config, sharedState.SecondaryConfigPropertyName, HostInvalidState)
                        .And.GetRuntimePropertyValue(LogPrefix.Secondary, sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue)
                        .And.FailToGetRuntimePropertyValue(LogPrefix.Secondary, sharedState.SecondaryConfigPropertyName, HostPropertyNotFound)
                        .And.HavePropertyMock(sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue)
                        .And.NotHavePropertyMock(sharedState.SecondaryConfigPropertyName);
                    break;
                case CheckProperties.GetAllActive:
                    result.Should()
                        .FailToGetRuntimeProperties(LogPrefix.Config, HostInvalidState)
                        .And.GetRuntimePropertiesIncludes(LogPrefix.Secondary, sharedState.ConfigPropertyName,sharedState.ConfigPropertyValue)
                        .And.GetRuntimePropertiesExcludes(LogPrefix.Secondary, sharedState.SecondaryConfigPropertyName)
                        .And.HavePropertyMock(sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue);
                    break;
                default:
                    throw new Exception($"Unknown option: {checkProperties}");
            }
        }

        [Theory]
        [InlineData(CheckProperties.None)]
        [InlineData(CheckProperties.Get)]
        [InlineData(CheckProperties.Set)]
        [InlineData(CheckProperties.Remove)]
        [InlineData(CheckProperties.GetAll)]
        [InlineData(CheckProperties.GetActive)]
        [InlineData(CheckProperties.GetAllActive)]
        public void RunApp_GetDelegate(string checkProperties)
        {
            string[] args =
            {
                HostContextArg,
                Scenario.Mixed,
                checkProperties,
                sharedState.HostFxrPath,
                sharedState.AppPath,
                sharedState.RuntimeConfigPath,
                sharedState.AppPropertyName,
                sharedState.ConfigPropertyName
            };
            CommandResult result = Command.Create(sharedState.NativeHostPath, args)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROOT", sharedState.DotNetRoot)
                .EnvironmentVariable("DOTNET_ROOT(x86)", sharedState.DotNetRoot)
                .EnvironmentVariable("TEST_BLOCK_MOCK_EXECUTE_ASSEMBLY", $"{sharedState.AppPath}.block")
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForApp(sharedState.AppPath)
                .And.ExecuteAssemblyMock(sharedState.AppPath)
                .And.InitializeSecondaryContext(sharedState.RuntimeConfigPath)
                .And.CreateDelegateMock();

            switch(checkProperties)
            {
                case CheckProperties.None:
                    result.Should()
                        .HavePropertyMock(sharedState.AppPropertyName, sharedState.AppPropertyValue);
                    break;
                case CheckProperties.Get:
                    result.Should()
                        .GetRuntimePropertyValue(LogPrefix.App, sharedState.AppPropertyName, sharedState.AppPropertyValue)
                        .And.FailToGetRuntimePropertyValue(LogPrefix.App, sharedState.ConfigPropertyName, HostPropertyNotFound)
                        .And.FailToGetRuntimePropertyValue(LogPrefix.Secondary, sharedState.AppPropertyName, HostPropertyNotFound)
                        .And.GetRuntimePropertyValue(LogPrefix.Secondary, sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue)
                        .And.HavePropertyMock(sharedState.AppPropertyName, sharedState.AppPropertyValue)
                        .And.NotHavePropertyMock(sharedState.ConfigPropertyName);
                    break;
                case CheckProperties.Set:
                    result.Should()
                        .SetRuntimePropertyValue(LogPrefix.App, sharedState.AppPropertyName)
                        .And.SetRuntimePropertyValue(LogPrefix.App, sharedState.ConfigPropertyName)
                        .And.FailToSetRuntimePropertyValue(LogPrefix.Secondary, sharedState.AppPropertyName, InvalidArgFailure)
                        .And.FailToSetRuntimePropertyValue(LogPrefix.Secondary, sharedState.ConfigPropertyName, InvalidArgFailure)
                        .And.HavePropertyMock(sharedState.AppPropertyName, PropertyValueFromHost)
                        .And.HavePropertyMock(sharedState.ConfigPropertyName, PropertyValueFromHost);
                    break;
                case CheckProperties.Remove:
                    result.Should()
                        .SetRuntimePropertyValue(LogPrefix.App, sharedState.AppPropertyName)
                        .And.SetRuntimePropertyValue(LogPrefix.App, sharedState.ConfigPropertyName)
                        .And.FailToSetRuntimePropertyValue(LogPrefix.Secondary, sharedState.AppPropertyName, InvalidArgFailure)
                        .And.FailToSetRuntimePropertyValue(LogPrefix.Secondary, sharedState.ConfigPropertyName, InvalidArgFailure)
                        .And.NotHavePropertyMock(sharedState.AppPropertyName)
                        .And.NotHavePropertyMock(sharedState.ConfigPropertyName);
                    break;
                case CheckProperties.GetAll:
                    result.Should()
                        .GetRuntimePropertiesIncludes(LogPrefix.App, sharedState.AppPropertyName, sharedState.AppPropertyValue)
                        .And.GetRuntimePropertiesIncludes(LogPrefix.Secondary, sharedState.ConfigPropertyName, sharedState.ConfigPropertyValue)
                        .And.GetRuntimePropertiesExcludes(LogPrefix.Secondary, sharedState.AppPropertyName)
                        .And.HavePropertyMock(sharedState.AppPropertyName, sharedState.AppPropertyValue)
                        .And.NotHavePropertyMock(sharedState.ConfigPropertyName);
                    break;
                case CheckProperties.GetActive:
                    result.Should()
                        .FailToGetRuntimePropertyValue(LogPrefix.App, sharedState.AppPropertyName, HostInvalidState)
                        .And.FailToGetRuntimePropertyValue(LogPrefix.App, sharedState.ConfigPropertyName, HostInvalidState)
                        .And.GetRuntimePropertyValue(LogPrefix.Secondary, sharedState.AppPropertyName, sharedState.AppPropertyValue)
                        .And.FailToGetRuntimePropertyValue(LogPrefix.Secondary, sharedState.ConfigPropertyName, HostPropertyNotFound)
                        .And.HavePropertyMock(sharedState.AppPropertyName, sharedState.AppPropertyValue)
                        .And.NotHavePropertyMock(sharedState.ConfigPropertyName);
                    break;
                case CheckProperties.GetAllActive:
                    result.Should()
                        .FailToGetRuntimeProperties(LogPrefix.App, HostInvalidState)
                        .And.GetRuntimePropertiesIncludes(LogPrefix.Secondary, sharedState.AppPropertyName, sharedState.AppPropertyValue)
                        .And.GetRuntimePropertiesExcludes(LogPrefix.Secondary, sharedState.ConfigPropertyName)
                        .And.HavePropertyMock(sharedState.AppPropertyName, sharedState.AppPropertyValue)
                        .And.NotHavePropertyMock(sharedState.ConfigPropertyName);
                    break;
                default:
                    throw new Exception($"Unknown option: {checkProperties}");
            }
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public string DotNetRoot { get; }

            public string AppPath { get; }
            public string RuntimeConfigPath { get; }
            public string SecondaryRuntimeConfigPath { get; }

            public string AppPropertyName => "APP_TEST_PROPERTY";
            public string AppPropertyValue => "VALUE_FROM_APP";

            public string ConfigPropertyName => "CONFIG_TEST_PROPERTY";
            public string ConfigPropertyValue => "VALUE_FROM_CONFIG";

            public string SecondaryConfigPropertyName => "SECONDARY_CONFIG_TEST_PROPERTY";
            public string SecondaryConfigPropertyValue => "VALUE_FROM_SECONDARY_CONFIG";

            public SharedTestState()
            {
                var dotNet = new DotNetBuilder(BaseDirectory, Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"), "mockRuntime")
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr(RepoDirectories.MicrosoftNETCoreAppVersion)
                    .Build();
                DotNetRoot = dotNet.BinPath;

                HostFxrPath = Path.Combine(
                    dotNet.GreatestVersionHostFxrPath,
                    RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostfxr"));

                string appDir = Path.Combine(BaseDirectory, "app");
                Directory.CreateDirectory(appDir);
                AppPath = Path.Combine(appDir, "App.dll");
                File.WriteAllText(AppPath, string.Empty);

                RuntimeConfig.FromFile(Path.Combine(appDir, "App.runtimeconfig.json"))
                    .WithFramework(new RuntimeConfig.Framework("Microsoft.NETCore.App", RepoDirectories.MicrosoftNETCoreAppVersion))
                    .WithProperty(AppPropertyName, AppPropertyValue)
                    .Save();

                string configDir = Path.Combine(BaseDirectory, "config");
                Directory.CreateDirectory(configDir);
                RuntimeConfigPath = Path.Combine(configDir, "Component.runtimeconfig.json");
                RuntimeConfig.FromFile(RuntimeConfigPath)
                    .WithFramework(new RuntimeConfig.Framework("Microsoft.NETCore.App", RepoDirectories.MicrosoftNETCoreAppVersion))
                    .WithProperty(ConfigPropertyName, ConfigPropertyValue)
                    .Save();

                string secondaryDir = Path.Combine(BaseDirectory, "secondary");
                Directory.CreateDirectory(secondaryDir);
                SecondaryRuntimeConfigPath = Path.Combine(secondaryDir, "Secondary.runtimeconfig.json");
                RuntimeConfig.FromFile(SecondaryRuntimeConfigPath)
                    .WithFramework(new RuntimeConfig.Framework("Microsoft.NETCore.App", RepoDirectories.MicrosoftNETCoreAppVersion))
                    .WithProperty(SecondaryConfigPropertyName, SecondaryConfigPropertyValue)
                    .Save();
            }
        }
    }
}
