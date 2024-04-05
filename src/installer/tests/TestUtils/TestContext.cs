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
            BuildRID = GetTestContextVariable("BUILD_RID");
            Configuration = GetTestContextVariable("BUILD_CONFIGURATION");

            MicrosoftNETCoreAppVersion = GetTestContextVariable("MNA_VERSION");
            Tfm = GetTestContextVariable("MNA_TFM");

            TestAssetsOutput = GetTestContextVariable("TEST_ASSETS_OUTPUT");
            TestArtifactsPath = GetTestContextVariable("TEST_ARTIFACTS");
            Directory.CreateDirectory(TestArtifactsPath);

            BuiltDotNet = new DotNetCli(Path.Combine(TestAssetsOutput, "sharedFrameworkPublish"));
        }

        public static string GetTestContextVariable(string name)
        {
            // Allow env var override, although normally the test context variables file is used.
            if (Environment.GetEnvironmentVariable(name) is string envValue)
            {
                return envValue;
            }

            if (_testContextVariables.TryGetValue(name, out string value))
            {
                return value;
            }

            throw new ArgumentException($"Unable to find variable '{name}' in test context variable file '{_testContextVariableFilePath}'");
        }
    }
}
