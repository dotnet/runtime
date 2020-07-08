// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public partial class FrameworkResolutionBase
    {
        public class TestSettings
        {
            public Func<RuntimeConfig, RuntimeConfig> RuntimeConfigCustomizer { get; set; }
            public IDictionary<string, string> Environment { get; set; }
            public IEnumerable<string> CommandLine { get; set; }
            public Action<DotNetCliExtensions.DotNetCliCustomizer> DotnetCustomizer { get; set; }
            public string WorkingDirectory { get; set; }

            public TestSettings WithRuntimeConfigCustomizer(Func<RuntimeConfig, RuntimeConfig> customizer)
            {
                Func<RuntimeConfig, RuntimeConfig> previousCustomizer = RuntimeConfigCustomizer;
                if (previousCustomizer == null)
                {
                    RuntimeConfigCustomizer = customizer;
                }
                else
                {
                    RuntimeConfigCustomizer = runtimeConfig => customizer(previousCustomizer(runtimeConfig));
                }

                return this;
            }

            public TestSettings WithEnvironment(string key, string value)
            {
                Environment = Environment == null ?
                    new Dictionary<string, string>() { { key, value } } :
                    new Dictionary<string, string>(Environment.Append(new KeyValuePair<string, string>(key, value)));
                return this;
            }

            public TestSettings WithCommandLine(params string[] args)
            {
                CommandLine = CommandLine == null ? args : CommandLine.Concat(args);
                return this;
            }

            public TestSettings WithWorkingDirectory(string path)
            {
                WorkingDirectory = path;
                return this;
            }

            public TestSettings WithDotnetCustomizer(Action<DotNetCliExtensions.DotNetCliCustomizer> customizer)
            {
                Action<DotNetCliExtensions.DotNetCliCustomizer> previousCustomizer = DotnetCustomizer;
                if (previousCustomizer == null)
                {
                    DotnetCustomizer = customizer;
                }
                else
                {
                    DotnetCustomizer = dotnet => { previousCustomizer(dotnet); customizer(dotnet); };
                }

                return this;
            }

            public TestSettings With(Func<TestSettings, TestSettings> modifier)
            {
                return modifier(this);
            }
        }

        public enum SettingLocation
        {
            None,
            CommandLine,
            Environment,
            RuntimeOptions,
            FrameworkReference
        }

        public static Func<TestSettings, TestSettings> RollForwardSetting(
            SettingLocation location,
            string value,
            string frameworkReferenceName = MicrosoftNETCoreApp)
        {
            if (value == null || location == SettingLocation.None)
            {
                return testSettings => testSettings;
            }

            switch (location)
            {
                case SettingLocation.Environment:
                    return testSettings => testSettings.WithEnvironment(Constants.RollForwardSetting.EnvironmentVariable, value);
                case SettingLocation.CommandLine:
                    return testSettings => testSettings.WithCommandLine(Constants.RollForwardSetting.CommandLineArgument, value);
                case SettingLocation.RuntimeOptions:
                    return testSettings => testSettings.WithRuntimeConfigCustomizer(rc => rc.WithRollForward(value));
                case SettingLocation.FrameworkReference:
                    return testSettings => testSettings.WithRuntimeConfigCustomizer(rc =>
                    {
                        rc.GetFramework(frameworkReferenceName).WithRollForward(value);
                        return rc;
                    });
                default:
                    throw new Exception($"RollForward forward doesn't support setting location {location}.");
            }
        }

        public static Func<TestSettings, TestSettings> RollForwardOnNoCandidateFxSetting(
            SettingLocation location,
            int? value,
            string frameworkReferenceName = MicrosoftNETCoreApp)
        {
            if (!value.HasValue || location == SettingLocation.None)
            {
                return testSettings => testSettings;
            }

            switch (location)
            {
                case SettingLocation.Environment:
                    return testSettings => testSettings.WithEnvironment(Constants.RollForwardOnNoCandidateFxSetting.EnvironmentVariable, value.ToString());
                case SettingLocation.CommandLine:
                    return testSettings => testSettings.WithCommandLine(Constants.RollForwardOnNoCandidateFxSetting.CommandLineArgument, value.ToString());
                case SettingLocation.RuntimeOptions:
                    return testSettings => testSettings.WithRuntimeConfigCustomizer(rc => rc.WithRollForwardOnNoCandidateFx(value));
                case SettingLocation.FrameworkReference:
                    return testSettings => testSettings.WithRuntimeConfigCustomizer(rc =>
                    {
                        rc.GetFramework(frameworkReferenceName).WithRollForwardOnNoCandidateFx(value);
                        return rc;
                    });
                default:
                    throw new Exception($"RollForwardOnNoCandidateFx doesn't support setting location {location}.");
            }
        }

        public static Func<TestSettings, TestSettings> ApplyPatchesSetting(
            SettingLocation location,
            bool? value,
            string frameworkReferenceName = MicrosoftNETCoreApp)
        {
            if (!value.HasValue || location == SettingLocation.None)
            {
                return testSettings => testSettings;
            }

            switch (location)
            {
                case SettingLocation.RuntimeOptions:
                    return testSettings => testSettings.WithRuntimeConfigCustomizer(rc => rc.WithApplyPatches(value));
                case SettingLocation.FrameworkReference:
                    return testSettings => testSettings.WithRuntimeConfigCustomizer(rc =>
                    {
                        rc.GetFramework(frameworkReferenceName).WithApplyPatches(value);
                        return rc;
                    });
                default:
                    throw new Exception($"ApplyPatches doesn't support setting location {location}.");
            }
        }
    }
}
