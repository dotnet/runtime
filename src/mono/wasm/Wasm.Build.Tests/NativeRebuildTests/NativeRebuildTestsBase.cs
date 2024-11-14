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
    public class NativeRebuildTestsBase : WasmTemplateTestsBase
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

            return data;

            IEnumerable<object?[]> GetData(bool aot, bool nativeRelinking, bool invariant)
                => ConfigWithAOTData(aot)
                        .Multiply(new object[] { nativeRelinking, invariant })
                        // AOT in Debug is switched off
                        .Where(item => !(item.ElementAt(0) is string config && config == "Debug" && item.ElementAt(1) is bool aotValue && aotValue))
                        .UnwrapItemsAsArrays().ToList();
        }

        internal async Task<BuildPaths> FirstNativeBuildAndRun(ProjectInfo info, bool nativeRelink, bool invariant, string extraBuildArgs="")
        {
            bool isNativeBuild = nativeRelink || invariant;
            var extraArgs = new string[] {
                "-p:_WasmDevel=true",
                $"-p:WasmBuildNative={nativeRelink}",
                $"-p:InvariantGlobalization={invariant}",
                extraBuildArgs
            };
            bool isPublish = true;
            BuildTemplateProject(info,
                        new BuildProjectOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish, isNativeBuild: nativeRelink),
                            IsPublish: isPublish,
                            GlobalizationMode: invariant ? GlobalizationMode.Invariant : GlobalizationMode.Sharded
                        ),
                        extraArgs);
            await RunForPublishWithWebServer(new (info.Configuration, TestScenario: "DotnetRun"));
            return GetBuildPaths(info, isPublish);
        }

        protected string Rebuild(ProjectInfo info, bool nativeRelink, bool invariant, string extraBuildArgs="", string verbosity="normal")
        {
            if (!_buildContext.TryGetBuildFor(info, out BuildProduct? product))
                throw new XunitException($"Test bug: could not get the build product in the cache");

            File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));
            
            var extraArgs = new string[] {
                "-p:_WasmDevel=true",
                $"-p:WasmBuildNative={nativeRelink}",
                $"-p:InvariantGlobalization={invariant}",
                $"-v:{verbosity}",
                extraBuildArgs
            };

            bool isNativeBuild = nativeRelink || invariant;
            bool isPublish = true;
            (string _, string output) = BuildTemplateProject(info,
                        new BuildProjectOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish, isNativeBuild),
                            IsPublish: isPublish,
                            GlobalizationMode: invariant ? GlobalizationMode.Invariant : GlobalizationMode.Sharded,
                            UseCache: false
                        ),
                        extraArgs);

            return output;
        }

    }
}
