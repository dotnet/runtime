// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using SL = Microsoft.Build.Logging.StructuredLogger;

#nullable enable

namespace Wasm.Build.Tests
{
    public class RebuildTests : WasmTemplateTestsBase
    {
        public RebuildTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [BuildAndRun(aot: false, config: Configuration.Debug)]
        public async Task NoOpRebuild(Configuration config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "rebuild");
            UpdateFile(Path.Combine("Common", "Program.cs"), s_mainReturns42);
            PublishProject(info, config);

            BrowserRunOptions runOptions = new(config, TestScenario: "DotnetRun", ExpectedExitCode: 42);
            await RunForPublishWithWebServer(runOptions);

            if (!_buildContext.TryGetBuildFor(info, out BuildResult? result))
                throw new XunitException($"Test bug: could not get the build result in the cache");

            File.Move(result!.LogFile, Path.ChangeExtension(result.LogFile!, ".first.binlog"));

            // artificial delay to have new enough timestamps
            await Task.Delay(5000);

            _testOutput.WriteLine($"{Environment.NewLine}Rebuilding with no changes ..{Environment.NewLine}");

            // no-op Rebuild
            PublishProject(info, config, new PublishOptions(UseCache: false));
            await RunForPublishWithWebServer(runOptions);
        }

        [Theory]
        [InlineData(Configuration.Debug)]
        [InlineData(Configuration.Release)]
        public void IncrementalPublish_NoChanges_SkipsBootJson(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "incremental_pub_noop");
            UpdateFile(Path.Combine("Common", "Program.cs"), s_mainReturns42);

            (_, string firstBinlog) = BuildProjectWithoutAssert(config, info.ProjectName, new PublishOptions(Label: "first"));
            _testOutput.WriteLine($"First publish binlog: {firstBinlog}");

            // no-op publish
            (_, string secondBinlog) = BuildProjectWithoutAssert(config, info.ProjectName, new PublishOptions(UseCache: false, Label: "second"));
            _testOutput.WriteLine($"Second publish binlog: {secondBinlog}");

            AssertTargetSkipped(secondBinlog, "GeneratePublishWasmBootJson");
        }

        [Theory]
        [InlineData(Configuration.Debug)]
        [InlineData(Configuration.Release)]
        public void IncrementalPublish_SourceChange_RunsBootJson(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "incremental_pub_src");
            UpdateFile(Path.Combine("Common", "Program.cs"), s_mainReturns42);

            BuildProjectWithoutAssert(config, info.ProjectName, new PublishOptions(Label: "first"));

            // modify app source to trigger recompilation
            string programPath = Path.Combine(_projectDir, "Common", "Program.cs");
            File.AppendAllText(programPath, Environment.NewLine + "// incremental publish trigger");

            (_, string secondBinlog) = BuildProjectWithoutAssert(config, info.ProjectName, new PublishOptions(UseCache: false, Label: "second"));
            _testOutput.WriteLine($"Second publish binlog: {secondBinlog}");

            AssertTargetRan(secondBinlog, "GeneratePublishWasmBootJson");
        }

        [Theory]
        [InlineData(Configuration.Debug)]
        [InlineData(Configuration.Release)]
        public void IncrementalBuild_NoChanges_SkipsWebcilAndBootJson(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "incremental_noop");
            UpdateFile(Path.Combine("Common", "Program.cs"), s_mainReturns42);

            (_, string firstBinlog) = BuildProjectWithoutAssert(config, info.ProjectName, new BuildOptions(Label: "first"));
            _testOutput.WriteLine($"First build binlog: {firstBinlog}");

            // no-op rebuild
            (_, string secondBinlog) = BuildProjectWithoutAssert(config, info.ProjectName, new BuildOptions(UseCache: false, Label: "second"));
            _testOutput.WriteLine($"Second build binlog: {secondBinlog}");

            if (UseWebcil)
                AssertTargetSkipped(secondBinlog, "_ConvertBuildDllsToWebcil");
            AssertTargetSkipped(secondBinlog, "_WriteBuildWasmBootJsonFile");
        }

        [Theory]
        [InlineData(Configuration.Debug)]
        [InlineData(Configuration.Release)]
        public void IncrementalBuild_SourceChange_RunsWebcilForAppOnly(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "incremental_src");
            UpdateFile(Path.Combine("Common", "Program.cs"), s_mainReturns42);

            BuildProjectWithoutAssert(config, info.ProjectName, new BuildOptions(Label: "first"));

            // modify app source to trigger recompilation
            string programPath = Path.Combine(_projectDir, "Common", "Program.cs");
            File.AppendAllText(programPath, Environment.NewLine + "// incremental rebuild trigger");

            (_, string secondBinlog) = BuildProjectWithoutAssert(config, info.ProjectName, new BuildOptions(UseCache: false, Label: "second"));
            _testOutput.WriteLine($"Second build binlog: {secondBinlog}");

            // Boot JSON target must run because the app assembly changed
            AssertTargetRan(secondBinlog, "_WriteBuildWasmBootJsonFile");

            if (UseWebcil)
            {
                // Webcil conversion target must run because the app assembly changed
                AssertTargetRan(secondBinlog, "_ConvertBuildDllsToWebcil");

                // Only the app assembly should have been re-converted (not framework DLLs)
                var convertedFiles = GetConvertedWebcilFiles(secondBinlog);
                _testOutput.WriteLine($"Webcil-converted files: {string.Join(", ", convertedFiles)}");
                Assert.Single(convertedFiles);
                Assert.Contains(convertedFiles, f => f.Contains(info.ProjectName, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Theory]
        [InlineData(Configuration.Release)]
        public void IncrementalBuild_NoChanges_SkipsWebcilAndBootJson_AOT(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: true, TestAsset.WasmBasicTestApp, "incremental_aot_noop");
            UpdateFile(Path.Combine("Common", "Program.cs"), s_mainReturns42);

            (_, string firstBinlog) = BuildProjectWithoutAssert(config, info.ProjectName, new PublishOptions(AOT: true, Label: "first"));
            _testOutput.WriteLine($"First publish binlog: {firstBinlog}");

            // no-op publish
            (_, string secondBinlog) = BuildProjectWithoutAssert(config, info.ProjectName, new PublishOptions(AOT: true, UseCache: false, Label: "second"));
            _testOutput.WriteLine($"Second publish binlog: {secondBinlog}");

            AssertTargetSkipped(secondBinlog, "GeneratePublishWasmBootJson");
        }

        [Theory]
        [InlineData(Configuration.Debug)]
        [InlineData(Configuration.Release)]
        public void IncrementalBuild_PropertyChange_RunsBootJson(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "incremental_prop");
            UpdateFile(Path.Combine("Common", "Program.cs"), s_mainReturns42);

            (_, string firstBinlog) = BuildProjectWithoutAssert(config, info.ProjectName, new BuildOptions(Label: "first"));
            _testOutput.WriteLine($"First build binlog: {firstBinlog}");

            // rebuild with a property-only change (no source changes)
            (_, string secondBinlog) = BuildProjectWithoutAssert(config, info.ProjectName, new BuildOptions(UseCache: false, Label: "second", ExtraMSBuildArgs: "-p:WasmDebugLevel=-1"));
            _testOutput.WriteLine($"Second build binlog: {secondBinlog}");

            // boot JSON must be regenerated because WasmDebugLevel changed (tracked via stamp file)
            AssertTargetRan(secondBinlog, "_WriteBuildWasmBootJsonFile");

            // webcil conversion should be skipped — no assembly changes
            if (UseWebcil)
                AssertTargetSkipped(secondBinlog, "_ConvertBuildDllsToWebcil");
        }

        private static void AssertTargetSkipped(string binlogPath, string targetName)
        {
            var build = SL.BinaryLog.ReadBuild(binlogPath);
            SL.BuildAnalyzer.AnalyzeBuild(build);

            bool found = false;
            bool skipped = false;
            build.VisitAllChildren<SL.Target>(t =>
            {
                if (t.Name == targetName)
                {
                    found = true;
                    if (t.Children.OfType<SL.Message>().Any(m =>
                        m.Text is not null && m.Text.Contains("Skipping target")))
                    {
                        skipped = true;
                    }
                }
            });

            Assert.True(found, $"Target '{targetName}' was not found in the binlog '{binlogPath}'.");
            Assert.True(skipped, $"Target '{targetName}' was expected to be skipped but it ran.");
        }

        private static void AssertTargetRan(string binlogPath, string targetName)
        {
            var build = SL.BinaryLog.ReadBuild(binlogPath);
            SL.BuildAnalyzer.AnalyzeBuild(build);

            bool found = false;
            bool ran = false;
            build.VisitAllChildren<SL.Target>(t =>
            {
                if (t.Name == targetName)
                {
                    found = true;
                    if (t.Children.OfType<SL.Message>().Any(m =>
                        m.Text is not null && m.Text.Contains("Building target")))
                    {
                        ran = true;
                    }
                }
            });

            Assert.True(found, $"Target '{targetName}' was not found in the binlog '{binlogPath}'.");
            Assert.True(ran, $"Target '{targetName}' was expected to run but it was skipped.");
        }

        private static List<string> GetConvertedWebcilFiles(string binlogPath)
        {
            var build = SL.BinaryLog.ReadBuild(binlogPath);
            SL.BuildAnalyzer.AnalyzeBuild(build);

            var converted = new List<string>();
            build.VisitAllChildren<SL.Target>(t =>
            {
                if (t.Name != "_ConvertBuildDllsToWebcil")
                    return;

                foreach (var child in t.Children)
                {
                    if (child is not SL.Task { Name: "ConvertDllsToWebcil" } convertTask)
                        continue;

                    convertTask.VisitAllChildren<SL.Message>(m =>
                    {
                        if (m.Text is not null && m.Text.StartsWith("Converting to Webcil:"))
                            converted.Add(m.Text);
                    });
                }
            });

            return converted;
        }
    }
}
