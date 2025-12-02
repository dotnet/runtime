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
                        .Where(item => !(item.ElementAt(0) is Configuration config && config == Configuration.Debug && item.ElementAt(1) is bool aotValue && aotValue))
                        .UnwrapItemsAsArrays().ToList();
        }

        internal async Task<BuildPaths> FirstNativeBuildAndRun(ProjectInfo info, Configuration config, bool aot, bool requestNativeRelink, bool invariant, string extraBuildArgs="")
        {
            var extraArgs = $"-p:_WasmDevel=true {extraBuildArgs}";
            if (requestNativeRelink)
                extraArgs += $" -p:WasmBuildNative={requestNativeRelink}";
            if (invariant)
                extraArgs += $" -p:InvariantGlobalization={invariant}";
            bool? nativeBuildValue = (requestNativeRelink || invariant) ? true : null;
            PublishProject(info,
                config,
                new PublishOptions(AOT: aot, GlobalizationMode: invariant ? GlobalizationMode.Invariant : GlobalizationMode.Sharded, ExtraMSBuildArgs: extraArgs),
                isNativeBuild: nativeBuildValue);
            await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "DotnetRun"));
            return GetBuildPaths(config, forPublish: true);
        }

        protected string Rebuild(
            ProjectInfo info, Configuration config, bool aot, bool requestNativeRelink, bool invariant, string extraBuildArgs="", string verbosity="normal", bool assertAppBundle=true)
        {
            if (!_buildContext.TryGetBuildFor(info, out BuildResult? result))
                throw new XunitException($"Test bug: could not get the build result in the cache");

            File.Move(result!.LogFile, Path.ChangeExtension(result.LogFile!, ".first.binlog"));
            
            var extraArgs = $"-p:_WasmDevel=true -v:{verbosity} {extraBuildArgs}";
            if (requestNativeRelink)
                extraArgs += $" -p:WasmBuildNative={requestNativeRelink}";
            if (invariant)
                extraArgs += $" -p:InvariantGlobalization={invariant}";

            // artificial delay to have new enough timestamps
            Thread.Sleep(5000);

            bool? nativeBuildValue = (requestNativeRelink || invariant) ? true : null;
            var globalizationMode = invariant ? GlobalizationMode.Invariant : GlobalizationMode.Sharded;
            var options = new PublishOptions(AOT: aot, GlobalizationMode: globalizationMode, ExtraMSBuildArgs: extraArgs, UseCache: false, AssertAppBundle: assertAppBundle);
            (string _, string output) = PublishProject(info, config, options, isNativeBuild: nativeBuildValue);
            return output;
        }

    }
}
