using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    internal static class Constants
    {
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

        public static class FxVersion
        {
            public const string CommandLineArgument = "--fx-version";
        }

        public static class TestOnlyEnvironmentVariables
        {
            public const string RegistryPath = "_DOTNET_TEST_REGISTRY_PATH";
            public const string GloballyRegisteredPath = "_DOTNET_TEST_GLOBALLY_REGISTERED_PATH";
        }
    }
}
