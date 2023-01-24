// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public partial class HostContext : IClassFixture<HostContext.SharedTestState>
    {
        public class Scenario
        {
            public const string App = "app";
            public const string Config = "config";
            public const string ConfigMultiple = "config_multiple";
            public const string Mixed = "mixed";
            public const string NonContextMixedAppHost = "non_context_mixed_apphost";
            public const string NonContextMixedDotnet = "non_context_mixed_dotnet";
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

        public enum CommandLine
        {
            AppPath,
            Exec,
        }

        private const string HostContextArg = "host_context";
        private const string PropertyValueFromHost = "VALUE_FROM_HOST";

        private const int InvalidArgFailure = unchecked((int)0x80008081);
        private const int HostInvalidState = unchecked((int)0x800080a3);
        private const int HostPropertyNotFound = unchecked((int)0x800080a4);
        private const int CoreHostIncompatibleConfig = unchecked((int)0x800080a5);
        private const int Success_HostAlreadyInitialized = 0x00000001;
        private const int Success_DifferentRuntimeProperties = 0x00000002;

        private readonly SharedTestState sharedState;

        public HostContext(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Theory]
        [InlineData(CommandLine.AppPath, false, CheckProperties.None)]
        [InlineData(CommandLine.AppPath, false, CheckProperties.Get)]
        [InlineData(CommandLine.AppPath, false, CheckProperties.Set)]
        [InlineData(CommandLine.AppPath, false, CheckProperties.Remove)]
        [InlineData(CommandLine.AppPath, false, CheckProperties.GetAll)]
        [InlineData(CommandLine.AppPath, false, CheckProperties.GetActive)]
        [InlineData(CommandLine.AppPath, false, CheckProperties.GetAllActive)]
        [InlineData(CommandLine.AppPath, true, CheckProperties.None)]
        [InlineData(CommandLine.AppPath, true, CheckProperties.Get)]
        [InlineData(CommandLine.AppPath, true, CheckProperties.Set)]
        [InlineData(CommandLine.AppPath, true, CheckProperties.Remove)]
        [InlineData(CommandLine.AppPath, true, CheckProperties.GetAll)]
        [InlineData(CommandLine.AppPath, true, CheckProperties.GetActive)]
        [InlineData(CommandLine.AppPath, true, CheckProperties.GetAllActive)]
        [InlineData(CommandLine.Exec, false, CheckProperties.None)]
        [InlineData(CommandLine.Exec, false, CheckProperties.Get)]
        [InlineData(CommandLine.Exec, false, CheckProperties.Set)]
        [InlineData(CommandLine.Exec, false, CheckProperties.Remove)]
        [InlineData(CommandLine.Exec, false, CheckProperties.GetAll)]
        [InlineData(CommandLine.Exec, false, CheckProperties.GetActive)]
        [InlineData(CommandLine.Exec, false, CheckProperties.GetAllActive)]
        [InlineData(CommandLine.Exec, true, CheckProperties.None)]
        [InlineData(CommandLine.Exec, true, CheckProperties.Get)]
        [InlineData(CommandLine.Exec, true, CheckProperties.Set)]
        [InlineData(CommandLine.Exec, true, CheckProperties.Remove)]
        [InlineData(CommandLine.Exec, true, CheckProperties.GetAll)]
        [InlineData(CommandLine.Exec, true, CheckProperties.GetActive)]
        [InlineData(CommandLine.Exec, true, CheckProperties.GetAllActive)]
        public void RunApp(CommandLine commandLine, bool isSelfContained, string checkProperties)
        {
            string expectedAppPath;
            string hostFxrPath;
            if (isSelfContained)
            {
                expectedAppPath = sharedState.SelfContainedAppPath;
                hostFxrPath = sharedState.SelfContainedHostFxrPath;
            }
            else
            {
                expectedAppPath = sharedState.AppPath;
                hostFxrPath = sharedState.HostFxrPath;
            }

            string newPropertyName = "HOST_TEST_PROPERTY";
            string[] args =
            {
                HostContextArg,
                Scenario.App,
                checkProperties,
                hostFxrPath
            };

            string[] commandArgs = { };
            switch (commandLine)
            {
                case CommandLine.AppPath:
                    commandArgs = new string[]
                    {
                        expectedAppPath
                    };
                    break;
                case CommandLine.Exec:
                    commandArgs = new string[]
                    {
                        "exec",
                        expectedAppPath
                    };
                    break;
            }

            string[] appArgs =
            {
                SharedTestState.AppPropertyName,
                newPropertyName
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args.Concat(commandArgs).Concat(appArgs), sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForApp(expectedAppPath)
                .And.ExecuteAssemblyMock(expectedAppPath, appArgs)
                .And.HaveStdErrContaining($"Executing as a {(isSelfContained ? "self-contained" : "framework-dependent")} app");

            CheckPropertiesValidation propertyValidation = new CheckPropertiesValidation(checkProperties, LogPrefix.App, SharedTestState.AppPropertyName, SharedTestState.AppPropertyValue);
            propertyValidation.ValidateActiveContext(result, newPropertyName);
        }

        [Theory]
        [InlineData(CheckProperties.None)]
        [InlineData(CheckProperties.Get)]
        [InlineData(CheckProperties.Set)]
        [InlineData(CheckProperties.Remove)]
        [InlineData(CheckProperties.GetAll)]
        [InlineData(CheckProperties.GetActive)]
        [InlineData(CheckProperties.GetAllActive)]
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
                SharedTestState.ConfigPropertyName,
                newPropertyName
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForConfig(sharedState.RuntimeConfigPath)
                .And.CreateDelegateMock_COM();

            CheckPropertiesValidation propertyValidation = new CheckPropertiesValidation(checkProperties, LogPrefix.Config, SharedTestState.ConfigPropertyName, SharedTestState.ConfigPropertyValue);
            propertyValidation.ValidateActiveContext(result, newPropertyName);
        }

        [Fact]
        public void InitializeConfig_SelfContained_Fails()
        {
            string[] args =
            {
                HostContextArg,
                Scenario.Config,
                CheckProperties.None,
                sharedState.SelfContainedHostFxrPath,
                sharedState.SelfContainedConfigPath
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Fail()
                .And.HaveStdErrContaining("Initialization for self-contained components is not supported");
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
                SharedTestState.ConfigPropertyName,
                SharedTestState.SecondaryConfigPropertyName
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForConfig(sharedState.RuntimeConfigPath)
                .And.InitializeSecondaryContext(sharedState.SecondaryRuntimeConfigPath, Success_DifferentRuntimeProperties)
                .And.CreateDelegateMock_COM()
                .And.CreateDelegateMock_InMemoryAssembly();

            CheckPropertiesValidation propertyValidation = new CheckPropertiesValidation(checkProperties, LogPrefix.Config, SharedTestState.ConfigPropertyName, SharedTestState.ConfigPropertyValue);
            propertyValidation.ValidateActiveContext(result, SharedTestState.SecondaryConfigPropertyName);
            propertyValidation.ValidateSecondaryContext(result, SharedTestState.SecondaryConfigPropertyName, SharedTestState.SecondaryConfigPropertyValue);
        }

        [Theory]
        [InlineData(Scenario.Mixed, CheckProperties.None)]
        [InlineData(Scenario.Mixed, CheckProperties.Get)]
        [InlineData(Scenario.Mixed, CheckProperties.Set)]
        [InlineData(Scenario.Mixed, CheckProperties.Remove)]
        [InlineData(Scenario.Mixed, CheckProperties.GetAll)]
        [InlineData(Scenario.Mixed, CheckProperties.GetActive)]
        [InlineData(Scenario.Mixed, CheckProperties.GetAllActive)]
        [InlineData(Scenario.NonContextMixedAppHost, CheckProperties.None)]
        [InlineData(Scenario.NonContextMixedAppHost, CheckProperties.Get)]
        [InlineData(Scenario.NonContextMixedAppHost, CheckProperties.Set)]
        [InlineData(Scenario.NonContextMixedAppHost, CheckProperties.Remove)]
        [InlineData(Scenario.NonContextMixedAppHost, CheckProperties.GetAll)]
        [InlineData(Scenario.NonContextMixedAppHost, CheckProperties.GetActive)]
        [InlineData(Scenario.NonContextMixedAppHost, CheckProperties.GetAllActive)]
        [InlineData(Scenario.NonContextMixedDotnet, CheckProperties.None)]
        [InlineData(Scenario.NonContextMixedDotnet, CheckProperties.Get)]
        [InlineData(Scenario.NonContextMixedDotnet, CheckProperties.Set)]
        [InlineData(Scenario.NonContextMixedDotnet, CheckProperties.Remove)]
        [InlineData(Scenario.NonContextMixedDotnet, CheckProperties.GetAll)]
        [InlineData(Scenario.NonContextMixedDotnet, CheckProperties.GetActive)]
        [InlineData(Scenario.NonContextMixedDotnet, CheckProperties.GetAllActive)]
        public void RunApp_GetDelegate(string scenario, string checkProperties)
        {
            if (scenario != Scenario.Mixed && scenario != Scenario.NonContextMixedAppHost && scenario != Scenario.NonContextMixedDotnet)
                throw new Exception($"Unexpected scenario: {scenario}");

            string[] args =
            {
                HostContextArg,
                scenario,
                checkProperties,
                sharedState.HostFxrPath,
                sharedState.AppPath,
                sharedState.RuntimeConfigPath
            };
            string[] appArgs =
            {
                SharedTestState.AppPropertyName,
                SharedTestState.ConfigPropertyName
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args.Concat(appArgs), sharedState.DotNetRoot)
                .EnvironmentVariable(Constants.HostTracing.VerbosityEnvironmentVariable, "3")
                .EnvironmentVariable("TEST_BLOCK_MOCK_EXECUTE_ASSEMBLY", $"{sharedState.AppPath}.block")
                .EnvironmentVariable("TEST_SIGNAL_MOCK_EXECUTE_ASSEMBLY", $"{sharedState.AppPath}.signal")
                .Execute();

            result.Should().Pass()
                .And.ExecuteAssemblyMock(sharedState.AppPath, appArgs)
                .And.InitializeSecondaryContext(sharedState.RuntimeConfigPath, Success_DifferentRuntimeProperties)
                .And.CreateDelegateMock_InMemoryAssembly();

            CheckPropertiesValidation propertyValidation = new CheckPropertiesValidation(checkProperties, LogPrefix.App, SharedTestState.AppPropertyName, SharedTestState.AppPropertyValue);
            if (scenario == Scenario.Mixed)
            {
                result.Should().InitializeContextForApp(sharedState.AppPath);
                propertyValidation.ValidateActiveContext(result, SharedTestState.ConfigPropertyName);
            }

            propertyValidation.ValidateSecondaryContext(result, SharedTestState.ConfigPropertyName, SharedTestState.ConfigPropertyValue);
        }

        [Theory]
        [MemberData(nameof(GetFrameworkCompatibilityTestData), parameters: Scenario.ConfigMultiple)]
        [MemberData(nameof(GetFrameworkCompatibilityTestData), parameters: Scenario.Mixed)]
        [MemberData(nameof(GetFrameworkCompatibilityTestData), parameters: Scenario.NonContextMixedAppHost)]
        [MemberData(nameof(GetFrameworkCompatibilityTestData), parameters: Scenario.NonContextMixedDotnet)]
        public void CompatibilityCheck_Frameworks(string scenario, FrameworkCompatibilityTestData testData)
        {
            if (scenario != Scenario.ConfigMultiple && scenario != Scenario.Mixed && scenario != Scenario.NonContextMixedAppHost && scenario != Scenario.NonContextMixedDotnet)
                throw new Exception($"Unexpected scenario: {scenario}");

            string frameworkName = testData.Name;
            string version = testData.Version;
            string frameworkCompatConfig = Path.Combine(sharedState.BaseDirectory, "frameworkCompat.runtimeconfig.json");
            RuntimeConfig.FromFile(frameworkCompatConfig)
                .WithFramework(new RuntimeConfig.Framework(frameworkName, version))
                .WithRollForward(testData.RollForward)
                .Save();

            string appOrConfigPath = scenario == Scenario.ConfigMultiple
                ? sharedState.RuntimeConfigPath
                : testData.ExistingContext switch
                    {
                        ExistingContextType.FrameworkDependent => sharedState.AppPath,
                        ExistingContextType.SelfContained_NoIncludedFrameworks => sharedState.SelfContainedAppPath,
                        ExistingContextType.SelfContained_WithIncludedFrameworks => sharedState.SelfContainedWithIncludedFrameworksAppPath,
                        _ => throw new Exception($"Unexpected test data {nameof(testData.ExistingContext)}: {testData.ExistingContext}")
                    };

            string hostfxrPath = scenario == Scenario.NonContextMixedDotnet
                ? sharedState.HostFxrPath // Imitating dotnet - always use the non-self-contained hostfxr
                : testData.ExistingContext switch
                    {
                        ExistingContextType.FrameworkDependent => sharedState.HostFxrPath,
                        ExistingContextType.SelfContained_NoIncludedFrameworks => sharedState.SelfContainedHostFxrPath,
                        ExistingContextType.SelfContained_WithIncludedFrameworks => sharedState.SelfContainedWithIncludedFrameworksHostFxrPath,
                        _ => throw new Exception($"Unexpected test data {nameof(testData.ExistingContext)}: {testData.ExistingContext}")
                    };

            string[] args =
            {
                HostContextArg,
                scenario,
                CheckProperties.None,
                hostfxrPath,
                appOrConfigPath,
                frameworkCompatConfig
            };

            CommandResult result;
            try
            {
                result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                    .EnvironmentVariable(Constants.HostTracing.VerbosityEnvironmentVariable, "3")
                    .EnvironmentVariable("TEST_BLOCK_MOCK_EXECUTE_ASSEMBLY", $"{sharedState.AppPath}.block")
                    .EnvironmentVariable("TEST_SIGNAL_MOCK_EXECUTE_ASSEMBLY", $"{sharedState.AppPath}.signal")
                    .Execute();
            }
            finally
            {
                File.Delete(frameworkCompatConfig);
            }

            switch (scenario)
            {
                case Scenario.ConfigMultiple:
                    result.Should()
                        .InitializeContextForConfig(appOrConfigPath)
                        .And.CreateDelegateMock_COM();
                    break;
                case Scenario.Mixed:
                    result.Should()
                        .InitializeContextForApp(appOrConfigPath)
                        .And.ExecuteAssemblyMock(appOrConfigPath, new string[0]);
                    break;
                case Scenario.NonContextMixedAppHost:
                case Scenario.NonContextMixedDotnet:
                    result.Should()
                        .ExecuteAssemblyMock(appOrConfigPath, new string[0])
                        .And.HaveStdErrContaining($"Mode: {(scenario == Scenario.NonContextMixedAppHost ? "apphost" : "muxer")}");
                    break;
            }

            bool? isCompatibleVersion = testData.IsCompatible;
            if (isCompatibleVersion.HasValue)
            {
                if (isCompatibleVersion.Value)
                {
                    result.Should().Pass()
                        .And.InitializeSecondaryContext(frameworkCompatConfig, Success_HostAlreadyInitialized)
                        .And.CreateDelegateMock_InMemoryAssembly();
                }
                else
                {
                    result.Should().Fail()
                        .And.FailToInitializeContextForConfig(CoreHostIncompatibleConfig)
                        .And.HaveStdErrMatching($".*The specified framework '{frameworkName}', version '{version}', apply_patches=[0-1], version_compatibility_range=[^ ]* is incompatible with the previously loaded version '{SharedTestState.NetCoreAppVersion}'.*");
                }
            }
            else
            {
                result.Should().Fail()
                    .And.FailToInitializeContextForConfig(CoreHostIncompatibleConfig)
                    .And.HaveStdErrContaining($"The specified framework '{frameworkName}' is not present in the previously loaded runtime");
            }
        }

        [Theory]
        [MemberData(nameof(GetPropertyCompatibilityTestData), parameters: new object[] { Scenario.ConfigMultiple, false })]
        [MemberData(nameof(GetPropertyCompatibilityTestData), parameters: new object[] { Scenario.ConfigMultiple, true })]
        [MemberData(nameof(GetPropertyCompatibilityTestData), parameters: new object[] { Scenario.Mixed, false })]
        [MemberData(nameof(GetPropertyCompatibilityTestData), parameters: new object[] { Scenario.Mixed, true })]
        [MemberData(nameof(GetPropertyCompatibilityTestData), parameters: new object[] { Scenario.NonContextMixedAppHost, false })]
        [MemberData(nameof(GetPropertyCompatibilityTestData), parameters: new object[] { Scenario.NonContextMixedAppHost, true })]
        [MemberData(nameof(GetPropertyCompatibilityTestData), parameters: new object[] { Scenario.NonContextMixedDotnet, false })]
        [MemberData(nameof(GetPropertyCompatibilityTestData), parameters: new object[] { Scenario.NonContextMixedDotnet, true })]
        public void CompatibilityCheck_Properties(string scenario, bool hasMultipleProperties, PropertyTestData[] properties)
        {
            if (scenario != Scenario.ConfigMultiple && scenario != Scenario.Mixed && scenario != Scenario.NonContextMixedAppHost && scenario != Scenario.NonContextMixedDotnet)
                throw new Exception($"Unexpected scenario: {scenario}");

            string propertyCompatConfig = Path.Combine(sharedState.BaseDirectory, "propertyCompat.runtimeconfig.json");
            var config = RuntimeConfig.FromFile(propertyCompatConfig)
                .WithFramework(new RuntimeConfig.Framework(Constants.MicrosoftNETCoreApp, SharedTestState.NetCoreAppVersion));

            foreach (var kv in properties)
            {
                config.WithProperty(kv.Name, kv.NewValue);
            }

            config.Save();

            string appOrConfigPath = scenario == Scenario.ConfigMultiple
                ? hasMultipleProperties ? sharedState.RuntimeConfigPath_MultiProperty : sharedState.RuntimeConfigPath
                : hasMultipleProperties ? sharedState.AppPath_MultiProperty : sharedState.AppPath;
            string[] args =
            {
                HostContextArg,
                scenario,
                CheckProperties.None,
                sharedState.HostFxrPath,
                appOrConfigPath,
                propertyCompatConfig
            };

            CommandResult result;
            try
            {
                result = sharedState.CreateNativeHostCommand(args, sharedState.DotNetRoot)
                    .EnvironmentVariable(Constants.HostTracing.VerbosityEnvironmentVariable, "3")
                    .EnvironmentVariable("TEST_BLOCK_MOCK_EXECUTE_ASSEMBLY", $"{sharedState.AppPath}.block")
                    .EnvironmentVariable("TEST_SIGNAL_MOCK_EXECUTE_ASSEMBLY", $"{sharedState.AppPath}.signal")
                    .Execute();
            }
            finally
            {
                File.Delete(propertyCompatConfig);
            }

            result.Should().Pass()
                .And.CreateDelegateMock_InMemoryAssembly();

            switch (scenario)
            {
                case Scenario.ConfigMultiple:
                    result.Should()
                        .InitializeContextForConfig(appOrConfigPath)
                        .And.CreateDelegateMock_COM();
                    break;
                case Scenario.Mixed:
                    result.Should()
                        .InitializeContextForApp(appOrConfigPath)
                        .And.ExecuteAssemblyMock(appOrConfigPath, new string[0]);
                    break;
                case Scenario.NonContextMixedAppHost:
                case Scenario.NonContextMixedDotnet:
                    result.Should()
                        .ExecuteAssemblyMock(appOrConfigPath, new string[0]);
                    break;
            }

            bool shouldHaveDifferentProperties = false;
            foreach(var prop in properties)
            {
                if (prop.ExistingValue == null)
                {
                    shouldHaveDifferentProperties = true;
                    result.Should()
                        .HaveStdErrContaining($"The property [{prop.Name}] is not present in the previously loaded runtime");
                }
                else if (!prop.ExistingValue.Equals(prop.NewValue))
                {
                    shouldHaveDifferentProperties = true;
                    result.Should()
                        .InitializeSecondaryContext(propertyCompatConfig, Success_DifferentRuntimeProperties)
                        .And.HaveStdErrContaining($"The property [{prop.Name}] has a different value [{prop.NewValue}] from that in the previously loaded runtime [{prop.ExistingValue}]");
                }
            }

            if (shouldHaveDifferentProperties)
            {
                result.Should()
                    .InitializeSecondaryContext(propertyCompatConfig, Success_DifferentRuntimeProperties);
            }
            else
            {
                result.Should()
                    .InitializeSecondaryContext(propertyCompatConfig, Success_HostAlreadyInitialized);

                if (properties.Length > 0)
                {
                    result.Should()
                        .HaveStdErrContaining("All specified properties match those in the previously loaded runtime");
                }
            }
        }

        public class CheckPropertiesValidation
        {
            public readonly string PropertyName;

            private readonly string logPrefix;
            private readonly string propertyValue;
            private readonly string checkProperties;

            public CheckPropertiesValidation(string checkProperties, string logPrefix, string propertyName, string propertyValue)
            {
                this.checkProperties = checkProperties;
                this.logPrefix = logPrefix;
                this.PropertyName = propertyName;
                this.propertyValue = propertyValue;
            }

            public void ValidateActiveContext(CommandResult result, string newPropertyName)
            {
                switch (checkProperties)
                {
                    case CheckProperties.None:
                        result.Should()
                            .HavePropertyMock(PropertyName, propertyValue);
                        break;
                    case CheckProperties.Get:
                        result.Should()
                            .GetRuntimePropertyValue(logPrefix, PropertyName, propertyValue)
                            .And.FailToGetRuntimePropertyValue(logPrefix, newPropertyName, HostPropertyNotFound)
                            .And.HavePropertyMock(PropertyName, propertyValue);
                        break;
                    case CheckProperties.Set:
                        result.Should()
                            .SetRuntimePropertyValue(logPrefix, PropertyName)
                            .And.SetRuntimePropertyValue(logPrefix, newPropertyName)
                            .And.HavePropertyMock(PropertyName, PropertyValueFromHost)
                            .And.HavePropertyMock(newPropertyName, PropertyValueFromHost);
                        break;
                    case CheckProperties.Remove:
                        result.Should()
                            .SetRuntimePropertyValue(logPrefix, PropertyName)
                            .And.SetRuntimePropertyValue(logPrefix, newPropertyName)
                            .And.NotHavePropertyMock(PropertyName)
                            .And.NotHavePropertyMock(newPropertyName);
                        break;
                    case CheckProperties.GetAll:
                        result.Should()
                            .GetRuntimePropertiesIncludes(logPrefix, PropertyName, propertyValue)
                            .And.HavePropertyMock(PropertyName, propertyValue);
                        break;
                    case CheckProperties.GetActive:
                        result.Should()
                            .FailToGetRuntimePropertyValue(logPrefix, PropertyName, HostInvalidState)
                            .And.FailToGetRuntimePropertyValue(logPrefix, newPropertyName, HostInvalidState)
                            .And.HavePropertyMock(PropertyName, propertyValue);
                        break;
                    case CheckProperties.GetAllActive:
                        result.Should()
                            .FailToGetRuntimeProperties(logPrefix, HostInvalidState)
                            .And.HavePropertyMock(PropertyName, propertyValue);
                        break;
                    default:
                        throw new Exception($"Unknown option: {checkProperties}");
                }
            }

            public void ValidateSecondaryContext(CommandResult result, string secondaryPropertyName, string secondaryPropertyValue)
            {
                switch (checkProperties)
                {
                    case CheckProperties.None:
                        break;
                    case CheckProperties.Get:
                        result.Should()
                            .FailToGetRuntimePropertyValue(LogPrefix.Secondary, PropertyName, HostPropertyNotFound)
                            .And.GetRuntimePropertyValue(LogPrefix.Secondary, secondaryPropertyName, secondaryPropertyValue);
                        break;
                    case CheckProperties.Set:
                        result.Should()
                            .FailToSetRuntimePropertyValue(LogPrefix.Secondary, PropertyName, InvalidArgFailure)
                            .And.FailToSetRuntimePropertyValue(LogPrefix.Secondary, secondaryPropertyName, InvalidArgFailure);
                        break;
                    case CheckProperties.Remove:
                        result.Should()
                            .FailToSetRuntimePropertyValue(LogPrefix.Secondary, PropertyName, InvalidArgFailure)
                            .And.FailToSetRuntimePropertyValue(LogPrefix.Secondary, secondaryPropertyName, InvalidArgFailure);
                        break;
                    case CheckProperties.GetAll:
                        result.Should()
                            .GetRuntimePropertiesIncludes(LogPrefix.Secondary, secondaryPropertyName, secondaryPropertyValue)
                            .And.GetRuntimePropertiesExcludes(LogPrefix.Secondary, PropertyName);
                        break;
                    case CheckProperties.GetActive:
                        result.Should()
                            .GetRuntimePropertyValue(LogPrefix.Secondary, PropertyName, propertyValue)
                            .And.FailToGetRuntimePropertyValue(LogPrefix.Secondary, secondaryPropertyName, HostPropertyNotFound);
                        break;
                    case CheckProperties.GetAllActive:
                        result.Should()
                            .GetRuntimePropertiesIncludes(LogPrefix.Secondary, PropertyName, propertyValue)
                            .And.GetRuntimePropertiesExcludes(LogPrefix.Secondary, secondaryPropertyName);
                        break;
                    default:
                        throw new Exception($"Unknown option: {checkProperties}");
                }
            }
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public string DotNetRoot { get; }

            public string AppPath { get; }
            public string RuntimeConfigPath { get; }
            public string SecondaryRuntimeConfigPath { get; }

            public string SelfContainedAppPath { get; }
            public string SelfContainedConfigPath { get; }
            public string SelfContainedHostFxrPath { get; }

            public string SelfContainedWithIncludedFrameworksAppPath { get; }
            public string SelfContainedWithIncludedFrameworksConfigPath { get; }
            public string SelfContainedWithIncludedFrameworksHostFxrPath { get; }

            public string AppPath_MultiProperty { get; }
            public string RuntimeConfigPath_MultiProperty { get; }

            public const string AppPropertyName = "APP_TEST_PROPERTY";
            public const string AppPropertyValue = "VALUE_FROM_APP";

            public const string AppMultiPropertyName = "APP_TEST_PROPERTY_2";
            public const string AppMultiPropertyValue = "VALUE_FROM_APP_2";

            public const string ConfigPropertyName = "CONFIG_TEST_PROPERTY";
            public const string ConfigPropertyValue = "VALUE_FROM_CONFIG";

            public const string ConfigMultiPropertyName = "CONFIG_TEST_PROPERTY_2";
            public const string ConfigMultiPropertyValue = "VALUE_FROM_CONFIG_2";

            public const string SecondaryConfigPropertyName = "SECONDARY_CONFIG_TEST_PROPERTY";
            public const string SecondaryConfigPropertyValue = "VALUE_FROM_SECONDARY_CONFIG";

            public const string NetCoreAppVersion = "2.2.0";

            public SharedTestState()
            {
                var dotNet = new DotNetBuilder(BaseDirectory, Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"), "mockRuntime")
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr(NetCoreAppVersion)
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
                    .WithFramework(new RuntimeConfig.Framework(Constants.MicrosoftNETCoreApp, NetCoreAppVersion))
                    .WithProperty(AppPropertyName, AppPropertyValue)
                    .Save();

                AppPath_MultiProperty = Path.Combine(appDir, "App_MultiProperty.dll");
                File.WriteAllText(AppPath_MultiProperty, string.Empty);

                RuntimeConfig.FromFile(Path.Combine(appDir, "App_MultiProperty.runtimeconfig.json"))
                    .WithFramework(new RuntimeConfig.Framework(Constants.MicrosoftNETCoreApp, NetCoreAppVersion))
                    .WithProperty(AppPropertyName, AppPropertyValue)
                    .WithProperty(AppMultiPropertyName, AppMultiPropertyValue)
                    .Save();

                CreateSelfContainedApp(dotNet, "SelfContained", out string selfContainedAppPath, out string selfContainedHostFxrPath, out string selfContainedConfigPath);
                SelfContainedAppPath = selfContainedAppPath;
                SelfContainedHostFxrPath = selfContainedHostFxrPath;
                SelfContainedConfigPath = selfContainedConfigPath;

                CreateSelfContainedApp(dotNet, "SelfContainedWithIncludedFrameworks", out selfContainedAppPath, out selfContainedHostFxrPath, out selfContainedConfigPath);
                SelfContainedWithIncludedFrameworksAppPath = selfContainedAppPath;
                SelfContainedWithIncludedFrameworksHostFxrPath = selfContainedHostFxrPath;
                SelfContainedWithIncludedFrameworksConfigPath = selfContainedConfigPath;
                RuntimeConfig.FromFile(SelfContainedWithIncludedFrameworksConfigPath)
                    .WithIncludedFramework(Constants.MicrosoftNETCoreApp, NetCoreAppVersion)
                    .Save();

                string configDir = Path.Combine(BaseDirectory, "config");
                Directory.CreateDirectory(configDir);
                RuntimeConfigPath = Path.Combine(configDir, "Component.runtimeconfig.json");
                RuntimeConfig.FromFile(RuntimeConfigPath)
                    .WithFramework(new RuntimeConfig.Framework(Constants.MicrosoftNETCoreApp, NetCoreAppVersion))
                    .WithProperty(ConfigPropertyName, ConfigPropertyValue)
                    .Save();

                RuntimeConfigPath_MultiProperty = Path.Combine(configDir, "Component_MultiProperty.runtimeconfig.json");
                RuntimeConfig.FromFile(RuntimeConfigPath_MultiProperty)
                    .WithFramework(new RuntimeConfig.Framework(Constants.MicrosoftNETCoreApp, NetCoreAppVersion))
                    .WithProperty(ConfigPropertyName, ConfigPropertyValue)
                    .WithProperty(ConfigMultiPropertyName, ConfigMultiPropertyValue)
                    .Save();

                string secondaryDir = Path.Combine(BaseDirectory, "secondary");
                Directory.CreateDirectory(secondaryDir);
                SecondaryRuntimeConfigPath = Path.Combine(secondaryDir, "Secondary.runtimeconfig.json");
                RuntimeConfig.FromFile(SecondaryRuntimeConfigPath)
                    .WithFramework(new RuntimeConfig.Framework(Constants.MicrosoftNETCoreApp, NetCoreAppVersion))
                    .WithProperty(SecondaryConfigPropertyName, SecondaryConfigPropertyValue)
                    .Save();
            }

            public void CreateSelfContainedApp(DotNetCli dotNet, string name, out string appPath, out string hostFxrPath, out string configPath)
            {
                string selfContainedDir = Path.Combine(BaseDirectory, name);
                Directory.CreateDirectory(selfContainedDir);
                appPath = Path.Combine(selfContainedDir, name + ".dll");
                File.WriteAllText(appPath, string.Empty);
                var toCopy = Directory.GetFiles(dotNet.GreatestVersionSharedFxPath)
                    .Concat(Directory.GetFiles(dotNet.GreatestVersionHostFxrPath));
                foreach (string file in toCopy)
                {
                    File.Copy(file, Path.Combine(selfContainedDir, Path.GetFileName(file)));
                }

                hostFxrPath = Path.Combine(selfContainedDir, Path.GetFileName(dotNet.GreatestVersionHostFxrFilePath));
                configPath = Path.Combine(selfContainedDir, name + ".runtimeconfig.json");
                RuntimeConfig.FromFile(configPath)
                    .WithProperty(AppPropertyName, AppPropertyValue)
                    .Save();
            }
        }
    }
}
