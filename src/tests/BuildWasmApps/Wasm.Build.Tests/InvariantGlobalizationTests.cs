using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Wasm.Build.Tests
{
    public class InvariantGlobalizationTests : BuildTestBase
    {
        public InvariantGlobalizationTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> InvariantGlobalizationTestData(bool aot, RunHost host)
            => ConfigWithAOTData(aot)
                .Multiply(new object?[] { null, false, true })
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        // TODO: check that icu bits have been linked out
        [Theory]
        [MemberData(nameof(InvariantGlobalizationTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        [MemberData(nameof(InvariantGlobalizationTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        public void InvariantGlobalization(BuildArgs buildArgs, bool? invariantGlobalization, RunHost host, string id)
        {
            string projectName = $"invariant_{invariantGlobalization?.ToString() ?? "unset"}";
            string? extraProperties = null;
            if (invariantGlobalization != null)
                extraProperties = $"<InvariantGlobalization>{invariantGlobalization}</InvariantGlobalization>";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = GetBuildArgsWith(buildArgs, extraProperties);

            string programText = @"
                using System;
                using System.Threading.Tasks;

                public class TestClass {
                    public static int Main()
                    {
                        Console.WriteLine(""Hello, World!"");
                        return 42;
                    }
                }";

            BuildProject(buildArgs,
                        initProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                        id: id,
                        hasIcudt: invariantGlobalization == null || invariantGlobalization.Value == false);

            RunAndTestWasmApp(buildArgs, expectedExitCode: 42,
                                test: output => Assert.Contains("Hello, World!", output), host: host, id: id);
        }
    }
}
