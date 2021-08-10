// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Text;

#nullable enable

namespace Wasm.Build.Tests
{
    // TODO: test for runtime components
    public class NativeRebuildTests : BuildTestBase
    {
        public NativeRebuildTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
            _enablePerTestCleanup = true;
        }

        public static IEnumerable<object?[]> NativeBuildData()
        {
            List<object?[]> data = new();
            // relinking
            data.AddRange(GetData(aot: false, nativeRelinking: true, invariant: false));
            data.AddRange(GetData(aot: false, nativeRelinking: true, invariant: true));

            // aot
            data.AddRange(GetData(aot: true, nativeRelinking: false, invariant: false));
            data.AddRange(GetData(aot: true, nativeRelinking: false, invariant: true));

            return data;

            IEnumerable<object?[]> GetData(bool aot, bool nativeRelinking, bool invariant)
                => ConfigWithAOTData(aot)
                        .Multiply(new object[] { nativeRelinking, invariant })
                        .WithRunHosts(RunHost.V8)
                        .UnwrapItemsAsArrays().ToList().Dump();
        }

        [Theory]
        [MemberData(nameof(NativeBuildData))]
        public void NoOpRebuildForNativeBuilds(BuildArgs buildArgs, bool nativeRelink, bool invariant, RunHost host, string id)
        {
            buildArgs = buildArgs with { ProjectName = $"rebuild_noop_{buildArgs.Config}" };
            (buildArgs, BuildPaths paths) = FirstNativeBuild(s_mainReturns42, nativeRelink: nativeRelink, invariant: invariant, buildArgs, id);

            var pathsDict = GetFilesTable(buildArgs, paths, unchanged: true);
            var originalStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            Rebuild(nativeRelink, invariant, buildArgs, id);
            var newStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            CompareStat(originalStat, newStat, pathsDict.Values);
            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
        }

        [Theory]
        [MemberData(nameof(NativeBuildData))]
        public void SimpleStringChangeInSource(BuildArgs buildArgs, bool nativeRelink, bool invariant, RunHost host, string id)
        {
            buildArgs = buildArgs with { ProjectName = $"rebuild_simple_{buildArgs.Config}" };
            (buildArgs, BuildPaths paths) = FirstNativeBuild(s_mainReturns42, nativeRelink, invariant: invariant, buildArgs, id);

            string mainAssembly = $"{buildArgs.ProjectName}.dll";
            var pathsDict = GetFilesTable(buildArgs, paths, unchanged: true);
            pathsDict.UpdateTo(unchanged: false, mainAssembly);
            pathsDict.UpdateTo(unchanged: !buildArgs.AOT, "dotnet.wasm", "dotnet.js");

            if (buildArgs.AOT)
                pathsDict.UpdateTo(unchanged: false, $"{mainAssembly}.bc", $"{mainAssembly}.o");

            var originalStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            // Changes
            string mainResults55 = @"
                public class TestClass {
                    public static int Main()
                    {
                        return 55;
                    }
                }";
            File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), mainResults55);

            // Rebuild
            Rebuild(nativeRelink, invariant, buildArgs, id);
            var newStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            CompareStat(originalStat, newStat, pathsDict.Values);
            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 55, host: host, id: id);
        }

        [Theory]
        [MemberData(nameof(NativeBuildData))]
        public void ReferenceNewAssembly(BuildArgs buildArgs, bool nativeRelink, bool invariant, RunHost host, string id)
        {
            buildArgs = buildArgs with { ProjectName = $"rebuild_tasks_{buildArgs.Config}" };
            (buildArgs, BuildPaths paths) = FirstNativeBuild(s_mainReturns42, nativeRelink, invariant: invariant, buildArgs, id);

            var pathsDict = GetFilesTable(buildArgs, paths, unchanged: false);
            pathsDict.UpdateTo(unchanged: true, "corebindings.o");
            if (!buildArgs.AOT) // relinking
                pathsDict.UpdateTo(unchanged: true, "driver-gen.c");

            var originalStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            string programText =
            @$"
                using System;
                using System.Text.Json;
                public class Test
                {{
                    public static int Main()
                    {{" +
             @"          string json = ""{ \""name\"": \""value\"" }"";" +
             @"          var jdoc = JsonDocument.Parse($""{json}"", new JsonDocumentOptions());" +
            @$"          Console.WriteLine($""json: {{jdoc}}"");
                        return 42;
                    }}
                }}";
            File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText);

            Rebuild(nativeRelink, invariant, buildArgs, id);
            var newStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            CompareStat(originalStat, newStat, pathsDict.Values);
            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
        }

        public static IEnumerable<object?[]> FlagsChangesForNativeRelinkingData(bool aot)
            => ConfigWithAOTData(aot, config: "Release").Multiply(
                        new object[] { /*cflags*/ "/p:EmccExtraCFlags=-g", /*ldflags*/ "" },
                        new object[] { /*cflags*/ "",                      /*ldflags*/ "/p:EmccExtraLDFlags=-g" },
                        new object[] { /*cflags*/ "/p:EmccExtraCFlags=-g", /*ldflags*/ "/p:EmccExtraLDFlags=-g" }
            ).WithRunHosts(RunHost.V8).UnwrapItemsAsArrays().Dump();

        [Theory]
        [MemberData(nameof(FlagsChangesForNativeRelinkingData), parameters: /*aot*/ false)]
        [MemberData(nameof(FlagsChangesForNativeRelinkingData), parameters: /*aot*/ true)]
        public void ExtraEmccFlagsSetButNoRealChange(BuildArgs buildArgs, string extraCFlags, string extraLDFlags, RunHost host, string id)
        {
            buildArgs = buildArgs with { ProjectName = $"rebuild_flags_{buildArgs.Config}" };
            (buildArgs, BuildPaths paths) = FirstNativeBuild(s_mainReturns42, nativeRelink: true, invariant: false, buildArgs, id);
            var pathsDict = GetFilesTable(buildArgs, paths, unchanged: true);
            if (extraLDFlags.Length > 0)
                pathsDict.UpdateTo(unchanged: false, "dotnet.wasm", "dotnet.js");

            var originalStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            // Rebuild

            string mainAssembly = $"{buildArgs.ProjectName}.dll";
            string extraBuildArgs = $" {extraCFlags} {extraLDFlags}";
            string output = Rebuild(nativeRelink: true, invariant: false, buildArgs, id, extraBuildArgs: extraBuildArgs, verbosity: "normal");

            var newStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));
            CompareStat(originalStat, newStat, pathsDict.Values);

            // cflags: pinvoke get's compiled, but doesn't overwrite pinvoke.o
            // and thus doesn't cause relinking
            AssertSubstring("pinvoke.c -> pinvoke.o", output, contains: extraCFlags.Length > 0);

            // ldflags: link step args change, so it should trigger relink
            AssertSubstring("wasm-opt", output, contains: extraLDFlags.Length > 0);

            if (buildArgs.AOT)
            {
                // ExtraEmccLDFlags does not affect .bc files
                Assert.DoesNotContain("Compiling assembly bitcode files", output);
            }

            string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
            AssertSubstring($"Found statically linked AOT module '{Path.GetFileNameWithoutExtension(mainAssembly)}'", runOutput,
                                contains: buildArgs.AOT);
        }

        public static IEnumerable<object?[]> FlagsOnlyChangeData(bool aot)
            => ConfigWithAOTData(aot, config: "Release").Multiply(
                        new object[] { /*cflags*/ "/p:EmccCompileOptimizationFlag=-O1", /*ldflags*/ "" },
                        new object[] { /*cflags*/ "",                                   /*ldflags*/ "/p:EmccLinkOptimizationFlag=-O0" }
            ).WithRunHosts(RunHost.V8).UnwrapItemsAsArrays().Dump();

        [Theory]
        [MemberData(nameof(FlagsOnlyChangeData), parameters: /*aot*/ false)]
        [MemberData(nameof(FlagsOnlyChangeData), parameters: /*aot*/ true)]
        public void OptimizationFlagChange(BuildArgs buildArgs, string cflags, string ldflags, RunHost host, string id)
        {
            // force _WasmDevel=false, so we don't get -O0
            buildArgs = buildArgs with { ProjectName = $"rebuild_flags_{buildArgs.Config}", ExtraBuildArgs = "/p:_WasmDevel=false" };
            (buildArgs, BuildPaths paths) = FirstNativeBuild(s_mainReturns42, nativeRelink: true, invariant: false, buildArgs, id);

            string mainAssembly = $"{buildArgs.ProjectName}.dll";
            var pathsDict = GetFilesTable(buildArgs, paths, unchanged: false);
            pathsDict.UpdateTo(unchanged: true, mainAssembly, "icall-table.h", "pinvoke-table.h", "driver-gen.c");
            if (cflags.Length == 0)
                pathsDict.UpdateTo(unchanged: true, "pinvoke.o", "corebindings.o", "driver.o");

            pathsDict.Remove(mainAssembly);
            if (buildArgs.AOT)
            {
                // link optimization flag change affects .bc->.o files too, but
                // it might result in only *some* files being *changed,
                // so, don't check for those
                // Link optimization flag is set to Compile optimization flag, if unset
                // so, it affects .bc files too!
                foreach (string key in pathsDict.Keys.ToArray())
                {
                    if (key.EndsWith(".dll.bc", StringComparison.Ordinal) || key.EndsWith(".dll.o", StringComparison.Ordinal))
                        pathsDict.Remove(key);
                }
            }

            var originalStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            // Rebuild

            string output = Rebuild(nativeRelink: true, invariant: false, buildArgs, id, extraBuildArgs: $" {cflags} {ldflags}", verbosity: "normal");
            var newStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));
            CompareStat(originalStat, newStat, pathsDict.Values);

            string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
            AssertSubstring($"Found statically linked AOT module '{Path.GetFileNameWithoutExtension(mainAssembly)}'", runOutput,
                                contains: buildArgs.AOT);
        }

        private (BuildArgs BuildArgs, BuildPaths paths) FirstNativeBuild(string programText, bool nativeRelink, bool invariant, BuildArgs buildArgs, string id, string extraProperties="")
        {
            buildArgs = GenerateProjectContents(buildArgs, nativeRelink, invariant, extraProperties);
            BuildProject(buildArgs,
                        initProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                        dotnetWasmFromRuntimePack: false,
                        hasIcudt: !invariant,
                        id: id,
                        createProject: true);

            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: RunHost.V8, id: id);
            return (buildArgs, GetBuildPaths(buildArgs));
        }

        private string Rebuild(bool nativeRelink, bool invariant, BuildArgs buildArgs, string id, string extraProperties="", string extraBuildArgs="", string? verbosity=null)
        {
            if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
                throw new XunitException($"Test bug: could not get the build product in the cache");

            File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));

            buildArgs = buildArgs with { ExtraBuildArgs = $"{buildArgs.ExtraBuildArgs} {extraBuildArgs}" };
            var newBuildArgs = GenerateProjectContents(buildArgs, nativeRelink, invariant, extraProperties);

            // key(buildArgs) being changed
            _buildContext.RemoveFromCache(product.ProjectDir);
            _buildContext.CacheBuild(newBuildArgs, product);

            if (buildArgs.ProjectFileContents != newBuildArgs.ProjectFileContents)
                File.WriteAllText(Path.Combine(_projectDir!, $"{buildArgs.ProjectName}.csproj"), buildArgs.ProjectFileContents);
            buildArgs = newBuildArgs;

            _testOutput.WriteLine($"{Environment.NewLine}Rebuilding with no changes ..{Environment.NewLine}");
            (_, string output) = BuildProject(buildArgs,
                                            id: id,
                                            dotnetWasmFromRuntimePack: false,
                                            hasIcudt: !invariant,
                                            createProject: false,
                                            useCache: false,
                                            verbosity: verbosity);

            return output;
        }

        private BuildArgs GenerateProjectContents(BuildArgs buildArgs, bool nativeRelink, bool invariant, string extraProperties)
        {
            StringBuilder propertiesBuilder = new();
            propertiesBuilder.Append("<_WasmDevel>true</_WasmDevel>");
            if (nativeRelink)
                propertiesBuilder.Append($"<WasmBuildNative>true</WasmBuildNative>");
            if (invariant)
                propertiesBuilder.Append($"<InvariantGlobalization>true</InvariantGlobalization>");
            propertiesBuilder.Append(extraProperties);

            return ExpandBuildArgs(buildArgs, propertiesBuilder.ToString());
        }

        private void CompareStat(IDictionary<string, FileStat> oldStat, IDictionary<string, FileStat> newStat, IEnumerable<(string fullpath, bool unchanged)> expected)
        {
            StringBuilder msg = new();
            foreach (var expect in expected)
            {
                string expectFilename = Path.GetFileName(expect.fullpath);
                if (!oldStat.TryGetValue(expectFilename, out FileStat? oldFs))
                {
                    msg.AppendLine($"Could not find an entry for {expectFilename} in old files");
                    continue;
                }

                if (!newStat.TryGetValue(expectFilename, out FileStat? newFs))
                {
                    msg.AppendLine($"Could not find an entry for {expectFilename} in new files");
                    continue;
                }

                bool actualUnchanged = oldFs == newFs;
                if (expect.unchanged && !actualUnchanged)
                {
                    msg.AppendLine($"[Expected unchanged file: {expectFilename}]{Environment.NewLine}" +
                                   $"   old: {oldFs}{Environment.NewLine}" +
                                   $"   new: {newFs}");
                }
                else if (!expect.unchanged && actualUnchanged)
                {
                    msg.AppendLine($"[Expected changed file: {expectFilename}]{Environment.NewLine}" +
                                   $"   {newFs}");
                }
            }

            if (msg.Length > 0)
                throw new XunitException($"CompareStat failed:{Environment.NewLine}{msg}");
        }

        private IDictionary<string, FileStat> StatFiles(IEnumerable<string> fullpaths)
        {
            Dictionary<string, FileStat> table = new();
            foreach (string file in fullpaths)
            {
                if (File.Exists(file))
                    table.Add(Path.GetFileName(file), new FileStat(FullPath: file, Exists: true, LastWriteTimeUtc: File.GetLastWriteTimeUtc(file), Length: new FileInfo(file).Length));
                else
                    table.Add(Path.GetFileName(file), new FileStat(FullPath: file, Exists: false, LastWriteTimeUtc: DateTime.MinValue, Length: 0));
            }

            return table;
        }

        private BuildPaths GetBuildPaths(BuildArgs buildArgs)
        {
            string objDir = GetObjDir(buildArgs.Config);
            string bundleDir = Path.Combine(GetBinDir(baseDir: _projectDir, config: buildArgs.Config), "AppBundle");
            string wasmDir = Path.Combine(objDir, "wasm");

            return new BuildPaths(wasmDir, objDir, GetBinDir(buildArgs.Config), bundleDir);
        }

        private IDictionary<string, (string fullPath, bool unchanged)> GetFilesTable(BuildArgs buildArgs, BuildPaths paths, bool unchanged)
        {
            List<string> files = new()
            {
                Path.Combine(paths.BinDir, "publish", $"{buildArgs.ProjectName}.dll"),
                Path.Combine(paths.ObjWasmDir, "driver.o"),
                Path.Combine(paths.ObjWasmDir, "corebindings.o"),
                Path.Combine(paths.ObjWasmDir, "pinvoke.o"),

                Path.Combine(paths.ObjWasmDir, "icall-table.h"),
                Path.Combine(paths.ObjWasmDir, "pinvoke-table.h"),
                Path.Combine(paths.ObjWasmDir, "driver-gen.c"),

                Path.Combine(paths.BundleDir, "dotnet.wasm"),
                Path.Combine(paths.BundleDir, "dotnet.js")
            };

            if (buildArgs.AOT)
            {
                files.AddRange(new[]
                {
                    Path.Combine(paths.ObjWasmDir, $"{buildArgs.ProjectName}.dll.bc"),
                    Path.Combine(paths.ObjWasmDir, $"{buildArgs.ProjectName}.dll.o"),

                    Path.Combine(paths.ObjWasmDir, "System.Private.CoreLib.dll.bc"),
                    Path.Combine(paths.ObjWasmDir, "System.Private.CoreLib.dll.o"),
                });
            }

            var dict = new Dictionary<string, (string fullPath, bool unchanged)>();
            foreach (var file in files)
                dict[Path.GetFileName(file)] = (file, unchanged);

            return dict;
        }

        private void AssertSubstring(string substring, string full, bool contains)
        {
            if (contains)
                Assert.Contains(substring, full);
            else
                Assert.DoesNotContain(substring, full);
        }
    }

    internal record FileStat (bool Exists, DateTime LastWriteTimeUtc, long Length, string FullPath);
    internal record BuildPaths(string ObjWasmDir, string ObjDir, string BinDir, string BundleDir);
}
