using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.DotNet.Cli.Build;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public sealed class TestContext
    {
        public static string BuildArchitecture { get; }
        public static string BuildRID { get; }
        public static string Configuration { get; }
        public static string TargetRID { get; }

        public static string MicrosoftNETCoreAppVersion { get; }
        public static string Tfm { get; }

        public static string TestAssetsOutput { get; }
        public static string TestArtifactsPath { get; }

        public static DotNetCli BuiltDotNet { get; }

        private static string _testContextVariableFilePath { get; }
        private static ImmutableDictionary<string, string> _testContextVariables { get; }

        static TestContext()
        {
            _testContextVariableFilePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "TestContextVariables.txt");

            _testContextVariables = File.ReadAllLines(_testContextVariableFilePath)
                .ToImmutableDictionary(
                    line => line.Substring(0, line.IndexOf('=')),
                    line => line.Substring(line.IndexOf('=') + 1),
                    StringComparer.OrdinalIgnoreCase);

            BuildArchitecture = GetTestContextVariable("BUILD_ARCHITECTURE");
            BuildRID = GetTestContextVariable("BUILDRID");
            Configuration = GetTestContextVariable("BUILD_CONFIGURATION");
            TargetRID = GetTestContextVariable("TEST_TARGETRID");

            MicrosoftNETCoreAppVersion = GetTestContextVariable("MNA_VERSION");
            Tfm = GetTestContextVariable("MNA_TFM");

            TestAssetsOutput = GetTestContextVariable("TEST_ASSETS_OUTPUT");
            TestArtifactsPath = GetTestContextVariable("TEST_ARTIFACTS");

            BuiltDotNet = new DotNetCli(Path.Combine(TestArtifactsPath, "sharedFrameworkPublish"));
        }

        public static string GetTestContextVariable(string name)
        {
            return GetTestContextVariableOrNull(name) ?? throw new ArgumentException(
                $"Unable to find variable '{name}' in test context variable file '{_testContextVariableFilePath}'");
        }

        public static string GetTestContextVariableOrNull(string name)
        {
            // Allow env var override, although normally the test context variables file is used.
            // Don't accept NUGET_PACKAGES env override specifically: Arcade sets this and it leaks
            // in during build.cmd/sh runs, replacing the test-specific dir.
            if (!name.Equals("NUGET_PACKAGES", StringComparison.OrdinalIgnoreCase))
            {
                if (Environment.GetEnvironmentVariable(name) is string envValue)
                {
                    return envValue;
                }
            }

            if (_testContextVariables.TryGetValue(name, out string value))
            {
                return value;
            }

            return null;
        }
    }
}
