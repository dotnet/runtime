// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Text;

#nullable enable

namespace Wasm.Build.NativeRebuild.Tests
{
    // TODO: test for runtime components
    public class NativeRebuildTestsBase : BuildTestBase
    {
        public NativeRebuildTestsBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
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
                        .WithRunHosts(RunHost.Chrome)
                        .UnwrapItemsAsArrays().ToList();
        }

        internal (BuildArgs BuildArgs, BuildPaths paths) FirstNativeBuild(string programText, bool nativeRelink, bool invariant, BuildArgs buildArgs, string id, string extraProperties="")
        {
            buildArgs = GenerateProjectContents(buildArgs, nativeRelink, invariant, extraProperties);
            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                                DotnetWasmFromRuntimePack: false,
                                GlobalizationMode: invariant ? GlobalizationMode.Invariant : null,
                                CreateProject: true));

            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: RunHost.Chrome, id: id);
            return (buildArgs, GetBuildPaths(buildArgs));
        }

        protected string Rebuild(bool nativeRelink, bool invariant, BuildArgs buildArgs, string id, string extraProperties="", string extraBuildArgs="", string? verbosity=null)
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
                                            new BuildProjectOptions(
                                                DotnetWasmFromRuntimePack: false,
                                                GlobalizationMode: invariant ? GlobalizationMode.Invariant : null,
                                                CreateProject: false,
                                                UseCache: false,
                                                Verbosity: verbosity));

            return output;
        }

        protected BuildArgs GenerateProjectContents(BuildArgs buildArgs, bool nativeRelink, bool invariant, string extraProperties)
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

        internal void CompareStat(IDictionary<string, FileStat> oldStat, IDictionary<string, FileStat> newStat, IEnumerable<(string fullpath, bool unchanged)> expected)
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

        internal IDictionary<string, (string fullPath, bool unchanged)> GetFilesTable(bool unchanged, params string[] baseDirs)
        {
            var dict = new Dictionary<string, (string fullPath, bool unchanged)>();
            foreach (var baseDir in baseDirs)
            {
                foreach (var file in Directory.EnumerateFiles(baseDir, "*", new EnumerationOptions { RecurseSubdirectories = true }))
                    dict[Path.GetFileName(file)] = (file, unchanged);
            }

            return dict;
        }

        internal IDictionary<string, (string fullPath, bool unchanged)> GetFilesTable(BuildArgs buildArgs, BuildPaths paths, bool unchanged)
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
    }
}
