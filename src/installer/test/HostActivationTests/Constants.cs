// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    internal static class Constants
    {
        public const string MicrosoftNETCoreApp = "Microsoft.NETCore.App";

        public static class ApplyPatchesSetting
        {
            public const string RuntimeConfigPropertyName = "applyPatches";
        }

        public static class RollForwardOnNoCandidateFxSetting
        {
            public const string RuntimeConfigPropertyName = "rollForwardOnNoCandidateFx";
            public const string CommandLineArgument = "--roll-forward-on-no-candidate-fx";
            public const string EnvironmentVariable = "DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX";
        }

        public static class RollForwardSetting
        {
            public const string RuntimeConfigPropertyName = "rollForward";
            public const string CommandLineArgument = "--roll-forward";
            public const string EnvironmentVariable = "DOTNET_ROLL_FORWARD";

            public const string LatestPatch = "LatestPatch";
            public const string Minor = "Minor";
            public const string Major = "Major";
            public const string LatestMinor = "LatestMinor";
            public const string LatestMajor = "LatestMajor";
            public const string Disable = "Disable";
        }

        public static class FxVersion
        {
            public const string CommandLineArgument = "--fx-version";
        }

        public static class RollForwardToPreRelease
        {
            public const string EnvironmentVariable = "DOTNET_ROLL_FORWARD_TO_PRERELEASE";
        }

        public static class TestOnlyEnvironmentVariables
        {
            public const string DefaultInstallPath = "_DOTNET_TEST_DEFAULT_INSTALL_PATH";
            public const string RegistryPath = "_DOTNET_TEST_REGISTRY_PATH";
            public const string GloballyRegisteredPath = "_DOTNET_TEST_GLOBALLY_REGISTERED_PATH";
            public const string InstallLocationFilePath = "_DOTNET_TEST_INSTALL_LOCATION_FILE_PATH";
        }
    }
}
