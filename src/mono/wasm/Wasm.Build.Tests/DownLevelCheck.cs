using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Wasm.Build.Tests
{
    public class DownlevelCheck : BuildTestBase
    {
        public DownlevelCheck(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
                    : base(new TestMainJsProjectProvider(output), output, buildContext)
        {
        }

        private const string ProjectTemplate =
            """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>{TargetFramework}</TargetFramework>
                    <OutputType>Exe</OutputType>
                </PropertyGroup>
                <Target Name="PrintResolveFrameworkReferences" AfterTargets="ResolveFrameworkReferences">
                    <Message Text="{TestMessage}" Importance="High" />
                </Target>
            </Project>
            """;

        private string FormatTestMessage(string version) => $"TEST::TargetingPackVersion::{version}::TEST";

        public static IEnumerable<object[]> GetData() => new[]
        {
            new object[] { "net6.0", DownlevelVersions.PackageVersionNet6 },
            new object[] { "net7.0", DownlevelVersions.PackageVersionNet7 },
            new object[] { "net8.0", DownlevelVersions.PackageVersionNet8 },
        };

        [Theory]
        [MemberData(nameof(GetData))]
        public void TestComputedVersions(string targetFramework, Version computedVersion)
        {
            string id = $"DownlevelCheck_{targetFramework}_{GetRandomId()}";
            string projectContent = ProjectTemplate
                .Replace("{TargetFramework}", targetFramework)
                .Replace("{TestMessage}", FormatTestMessage("@(ResolvedFrameworkReference->'%(TargetingPackVersion)')"));

            InitPaths(id);
            InitProjectDir(_projectDir);
            File.WriteAllText(Path.Combine(_projectDir, $"{id}.csproj"), projectContent);
            File.WriteAllText(Path.Combine(_projectDir, $"Program.cs"), "System.Console.WriteLine(\"Hello, World!\");"); // no-op

            (var result, _) = BuildProjectWithoutAssert(id, "Release", new(
                AssertAppBundle: false,
                CreateProject: false,
                Publish: false,
                TargetFramework: targetFramework
            ));

            var versionExtractor = new Regex(FormatTestMessage("([0-9]+.[0-9]+.[0-9]+)"));
            var match = versionExtractor.Match(result.Output);
            Assert.True(match.Success, $"Expected to find a version in the output, but got: {result.Output}");

            if (!Version.TryParse(match.Groups[1].Value, out var actualVersion))
                Assert.Fail($"Unable to parse version from regex match '{match.Groups[1].Value}'");

            Assert.True(actualVersion >= computedVersion, $"Actually used version is lower then computed version in repository: (actually used version) '{actualVersion}' >= (computed version in repository) '{Versions.PackageVersionNet8}'");
        }
    }
}
