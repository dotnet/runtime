// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Linq;
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
            public const string NonContextMixed = "non_context_mixed";
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
        private const int CoreHostIncompatibleConfig = unchecked((int)0x800080a5);
        private const int Success_HostAlreadyInitialized = 0x00000001;
        private const int Success_DifferentRuntimeProperties = 0x00000002;

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
        [InlineData(CheckProperties.GetActive)]
        [InlineData(CheckProperties.GetAllActive)]
        public void RunApp(string checkProperties)
        {
            string newPropertyName = "HOST_TEST_PROPERTY";
            string[] args =
            {
                HostContextArg,
                Scenario.App,
                checkProperties,
                sharedState.HostFxrPath,
                sharedState.AppPath
            };
            string[] appArgs =
            {
                SharedTestState.AppPropertyName,
                newPropertyName
            };
            CommandResult result = Command.Create(sharedState.NativeHostPath, args.Concat(appArgs))
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROOT", sharedState.DotNetRoot)
                .EnvironmentVariable("DOTNET_ROOT(x86)", sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForApp(sharedState.AppPath)
                .And.ExecuteAssemblyMock(sharedState.AppPath, appArgs);

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
            CommandResult result = Command.Create(sharedState.NativeHostPath, args)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROOT", sharedState.DotNetRoot)
                .EnvironmentVariable("DOTNET_ROOT(x86)", sharedState.DotNetRoot)
                .Execute();

            result.Should().Pass()
                .And.InitializeContextForConfig(sharedState.RuntimeConfigPath)
                .And.CreateDelegateMock_COM();

            CheckPropertiesValidation propertyValidation = new CheckPropertiesValidation(checkProperties, LogPrefix.Config, SharedTestState.ConfigPropertyName, SharedTestState.ConfigPropertyValue);
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
            CommandResult result = Command.Create(sharedState.NativeHostPath, args)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROOT", sharedState.DotNetRoot)
                .EnvironmentVariable("DOTNET_ROOT(x86)", sharedState.DotNetRoot)
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
        [InlineData(Scenario.NonContextMixed, CheckProperties.None)]
        [InlineData(Scenario.NonContextMixed, CheckProperties.Get)]
        [InlineData(Scenario.NonContextMixed, CheckProperties.Set)]
        [InlineData(Scenario.NonContextMixed, CheckProperties.Remove)]
        [InlineData(Scenario.NonContextMixed, CheckProperties.GetAll)]
        [InlineData(Scenario.NonContextMixed, CheckProperties.GetActive)]
        [InlineData(Scenario.NonContextMixed, CheckProperties.GetAllActive)]
        public void RunApp_GetDelegate(string scenario, string checkProperties)
        {
            if (scenario != Scenario.Mixed && scenario != Scenario.NonContextMixed)
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
            CommandResult result = Command.Create(sharedState.NativeHostPath, args.Concat(appArgs))
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROOT", sharedState.DotNetRoot)
                .EnvironmentVariable("DOTNET_ROOT(x86)", sharedState.DotNetRoot)
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
        [InlineData(Scenario.ConfigMultiple, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.LatestPatch, false)]
        [InlineData(Scenario.ConfigMultiple, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.Minor, false)]
        [InlineData(Scenario.ConfigMultiple, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.LatestMinor, false)]
        [InlineData(Scenario.ConfigMultiple, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.Major, true)]
        [InlineData(Scenario.ConfigMultiple, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.LatestMajor, true)]
        [InlineData(Scenario.ConfigMultiple, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.LatestPatch, false)]
        [InlineData(Scenario.ConfigMultiple, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.Minor, true)]
        [InlineData(Scenario.ConfigMultiple, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.LatestMinor, true)]
        [InlineData(Scenario.ConfigMultiple, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.Major, true)]
        [InlineData(Scenario.ConfigMultiple, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.LatestMajor, true)]
        [InlineData(Scenario.ConfigMultiple, Constants.MicrosoftNETCoreApp, "2.2.0", Constants.RollForwardSetting.Disable, true)]
        [InlineData(Scenario.ConfigMultiple, Constants.MicrosoftNETCoreApp, "3.1.0", Constants.RollForwardSetting.LatestMinor, false)]
        [InlineData(Scenario.ConfigMultiple, "UnknownFramework", "2.2.0", null, null)]
        [InlineData(Scenario.Mixed, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.LatestPatch, false)]
        [InlineData(Scenario.Mixed, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.Minor, false)]
        [InlineData(Scenario.Mixed, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.LatestMinor, false)]
        [InlineData(Scenario.Mixed, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.Major, true)]
        [InlineData(Scenario.Mixed, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.LatestMajor, true)]
        [InlineData(Scenario.Mixed, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.LatestPatch, false)]
        [InlineData(Scenario.Mixed, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.Minor, true)]
        [InlineData(Scenario.Mixed, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.LatestMinor, true)]
        [InlineData(Scenario.Mixed, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.Major, true)]
        [InlineData(Scenario.Mixed, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.LatestMajor, true)]
        [InlineData(Scenario.Mixed, Constants.MicrosoftNETCoreApp, "2.2.0", Constants.RollForwardSetting.Disable, true)]
        [InlineData(Scenario.Mixed, Constants.MicrosoftNETCoreApp, "3.1.0", Constants.RollForwardSetting.LatestMinor, false)]
        [InlineData(Scenario.Mixed, "UnknownFramework", "2.2.0", null, null)]
        [InlineData(Scenario.NonContextMixed, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.LatestPatch, false)]
        [InlineData(Scenario.NonContextMixed, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.Minor, false)]
        [InlineData(Scenario.NonContextMixed, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.LatestMinor, false)]
        [InlineData(Scenario.NonContextMixed, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.Major, true)]
        [InlineData(Scenario.NonContextMixed, Constants.MicrosoftNETCoreApp, "1.1.0", Constants.RollForwardSetting.LatestMajor, true)]
        [InlineData(Scenario.NonContextMixed, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.LatestPatch, false)]
        [InlineData(Scenario.NonContextMixed, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.Minor, true)]
        [InlineData(Scenario.NonContextMixed, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.LatestMinor, true)]
        [InlineData(Scenario.NonContextMixed, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.Major, true)]
        [InlineData(Scenario.NonContextMixed, Constants.MicrosoftNETCoreApp, "2.1.0", Constants.RollForwardSetting.LatestMajor, true)]
        [InlineData(Scenario.NonContextMixed, Constants.MicrosoftNETCoreApp, "2.2.0", Constants.RollForwardSetting.Disable, true)]
        [InlineData(Scenario.NonContextMixed, Constants.MicrosoftNETCoreApp, "3.1.0", Constants.RollForwardSetting.LatestMinor, false)]
        [InlineData(Scenario.NonContextMixed, "UnknownFramework", "2.2.0", null, null)]
        public void CompatibilityCheck_Frameworks(string scenario, string frameworkName, string version, string rollForward, bool? isCompatibleVersion)
        {
            if (scenario != Scenario.ConfigMultiple && scenario != Scenario.Mixed && scenario != Scenario.NonContextMixed)
                throw new Exception($"Unexpected scenario: {scenario}");

            string frameworkCompatConfig = Path.Combine(sharedState.BaseDirectory, "frameworkCompat.runtimeconfig.json");
            RuntimeConfig.FromFile(frameworkCompatConfig)
                .WithFramework(new RuntimeConfig.Framework(frameworkName, version))
                .WithRollForward(rollForward)
                .Save();

            string appOrConfigPath = scenario == Scenario.ConfigMultiple ? sharedState.RuntimeConfigPath : sharedState.AppPath;
            string[] args =
            {
                HostContextArg,
                scenario,
                CheckProperties.None,
                sharedState.HostFxrPath,
                appOrConfigPath,
                frameworkCompatConfig
            };

            CommandResult result;
            try
            {
                result = Command.Create(sharedState.NativeHostPath, args)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .EnvironmentVariable("COREHOST_TRACE", "1")
                    .EnvironmentVariable("DOTNET_ROOT", sharedState.DotNetRoot)
                    .EnvironmentVariable("DOTNET_ROOT(x86)", sharedState.DotNetRoot)
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
                case Scenario.NonContextMixed:
                    result.Should()
                        .ExecuteAssemblyMock(appOrConfigPath, new string[0]);
                    break;
            }

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
                        .And.HaveStdErrMatching($".*The specified framework '{frameworkName}', version '{version}', apply_patches=[0-1], roll_forward=[^ ]* is incompatible with the previously loaded version '{SharedTestState.NetCoreAppVersion}'.*");
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
        [MemberData(nameof(GetPropertyCompatibilityTestData), parameters: new object[] { Scenario.NonContextMixed, false })]
        [MemberData(nameof(GetPropertyCompatibilityTestData), parameters: new object[] { Scenario.NonContextMixed, true })]
        public void CompatibilityCheck_Properties(string scenario, bool hasMultipleProperties, PropertyTestData[] properties)
        {
            if (scenario != Scenario.ConfigMultiple && scenario != Scenario.Mixed && scenario != Scenario.NonContextMixed)
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
                result = Command.Create(sharedState.NativeHostPath, args)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .EnvironmentVariable("COREHOST_TRACE", "1")
                    .EnvironmentVariable("DOTNET_ROOT", sharedState.DotNetRoot)
                    .EnvironmentVariable("DOTNET_ROOT(x86)", sharedState.DotNetRoot)
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
                case Scenario.NonContextMixed:
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
        }
    }
}
