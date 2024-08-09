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
using System.Threading.Tasks;
using System.Threading;

#nullable enable

namespace Wasm.Build.NativeRebuild.Tests
{
    // TODO: test for runtime components
    public class NativeRebuildTestsBase : TestMainJsTestBase
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
                                GlobalizationMode: invariant ? GlobalizationMode.Invariant : GlobalizationMode.Sharded,
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

            // artificial delay to have new enough timestamps
            Thread.Sleep(5000);

            _testOutput.WriteLine($"{Environment.NewLine}Rebuilding with no changes ..{Environment.NewLine}");
            (_, string output) = BuildProject(buildArgs,
                                            id: id,
                                            new BuildProjectOptions(
                                                DotnetWasmFromRuntimePack: false,
                                                GlobalizationMode: invariant ? GlobalizationMode.Invariant : GlobalizationMode.Sharded,
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

        // appending UTF-8 char makes sure project build&publish under all types of paths is supported
        protected string GetTestProjectPath(string prefix, string config, bool appendUnicode=true) =>
            appendUnicode ? $"{prefix}_{config}_{s_unicodeChars}" : $"{prefix}_{config}";

    }
}
