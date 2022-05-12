// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmTemplateTests : BuildTestBase
    {
        public WasmTemplateTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void BrowserBuildThenPublish(string config)
        {
            string id = $"{config}_{Path.GetRandomFileName()}";
            string projectName = $"browser";
            CreateWasmTemplateProject(id, "wasmbrowser");

            var buildArgs = new BuildArgs(projectName, config, false, id, null);
            buildArgs = ExpandBuildArgs(buildArgs);

            BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            DotnetWasmFromRuntimePack: false,
                            CreateProject: false,
                            HasV8Script: false,
                            MainJS: "main.js",
                            Publish: false,
                            TargetFramework: "net7.0"
                        ));

            if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
                throw new XunitException($"Test bug: could not get the build product in the cache");

            File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));

            _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");
            _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");

            BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            DotnetWasmFromRuntimePack: false,
                            CreateProject: false,
                            HasV8Script: false,
                            MainJS: "main.js",
                            Publish: true,
                            TargetFramework: "net7.0",
                            UseCache: false));
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void ConsoleBuildThenPublish(string config)
        {
            string id = $"{config}_{Path.GetRandomFileName()}";
            string projectName = $"console";
            CreateWasmTemplateProject(id, "wasmconsole");

            var buildArgs = new BuildArgs(projectName, config, false, id, null);
            buildArgs = ExpandBuildArgs(buildArgs);

            BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: true,
                        CreateProject: false,
                        HasV8Script: false,
                        MainJS: "main.cjs",
                        Publish: false,
                        TargetFramework: "net7.0"
                        ));

            if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
                throw new XunitException($"Test bug: could not get the build product in the cache");

            File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));

            _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");
            _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");

            bool expectRelinking = config == "Release";
            BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            DotnetWasmFromRuntimePack: !expectRelinking,
                            CreateProject: false,
                            HasV8Script: false,
                            MainJS: "main.cjs",
                            Publish: true,
                            TargetFramework: "net7.0",
                            UseCache: false));
        }
    }
}
