// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class NativeLibraryTests : BuildTestBase
    {
        public NativeLibraryTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [BuildAndRun(aot: false)]
        [BuildAndRun(aot: true)]
        public void ProjectWithNativeReference(BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = $"AppUsingNativeLib-a";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, extraItems: "<NativeFileReference Include=\"native-lib.o\" />");

            if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? _))
            {
                InitPaths(id);
                if (Directory.Exists(_projectDir))
                    Directory.Delete(_projectDir, recursive: true);

                Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "AppUsingNativeLib"), _projectDir);
                File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "native-lib.o"), Path.Combine(_projectDir, "native-lib.o"));
            }

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(DotnetWasmFromRuntimePack: false));

            string output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 0,
                                test: output => {},
                                host: host, id: id);

            Assert.Contains("print_line: 100", output);
            Assert.Contains("from pinvoke: 142", output);
        }

        [Theory]
        [BuildAndRun(aot: false)]
        [BuildAndRun(aot: true)]
        public void ProjectUsingSkiaSharp(BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = $"AppUsingSkiaSharp";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs,
                            extraItems: @$"
                                <PackageReference Include=""SkiaSharp"" Version=""2.80.3"" />
                                <PackageReference Include=""SkiaSharp.NativeAssets.WebAssembly"" Version=""2.80.3"" />

                                <NativeFileReference Include=""$(SkiaSharpStaticLibraryPath)\2.0.9\*.a"" />
                                <WasmFilesToIncludeInFileSystem Include=""{Path.Combine(BuildEnvironment.TestAssetsPath, "mono.png")}"" />
                            ");

            string programText = @"
using System;
using SkiaSharp;

public class Test
{
    public static int Main()
    {
        using SKFileStream skfs = new SKFileStream(""mono.png"");
        using SKImage img = SKImage.FromEncodedData(skfs);

        Console.WriteLine ($""Size: {skfs.Length} Height: {img.Height}, Width: {img.Width}"");
        return 0;
    }
}";

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                                DotnetWasmFromRuntimePack: false));

            string output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 0,
                                test: output => {},
                                host: host, id: id,
                                args: "mono.png");

            Assert.Contains("Size: 26462 Height: 599, Width: 499", output);
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [BuildAndRun(aot: false)]
        [BuildAndRun(aot: true)]
        public void ProjectUsingBrowserNativeCrypto(BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = $"AppUsingBrowserNativeCrypto";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs);

            string programText = @"
using System;
using System.Security.Cryptography;

public class Test
{
    public static int Main()
    {
        using (SHA256 mySHA256 = SHA256.Create())
        {
            byte[] data = { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };
            byte[] hashed = mySHA256.ComputeHash(data);
            string asStr = string.Join(' ', hashed);
            Console.WriteLine(""Hashed: "" + asStr);
            return 0;
        }
    }
}";

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                                DotnetWasmFromRuntimePack: !buildArgs.AOT && buildArgs.Config != "Release"));

            string output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 0,
                                test: output => {},
                                host: host, id: id);

            Assert.Contains(
                "Hashed: 24 95 141 179 34 113 254 37 245 97 166 252 147 139 46 38 67 6 236 48 78 218 81 128 7 209 118 72 38 56 25 105",
                output);

            string cryptoInitMsg = "MONO_WASM: Initializing Crypto WebWorker";
            if (host == RunHost.V8 || host == RunHost.NodeJS)
            {
                Assert.DoesNotContain(cryptoInitMsg, output);
            }
            else
            {
                Assert.Contains(cryptoInitMsg, output);
            }
        }
    }
}
