// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.NET.Sdk.WebAssembly;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Linq;

#nullable enable

namespace Wasm.Build.Tests;

public class WasmSdkBasedProjectProvider : ProjectProviderBase
{
    public string DefaultTargetFramework { get; }

    public WasmSdkBasedProjectProvider(ITestOutputHelper _testOutput, string defaultTargetFramework, string? _projectDir = null)
            : base(_testOutput, _projectDir)
    {
        DefaultTargetFramework = defaultTargetFramework;
    }

    protected override string BundleDirName { get { return "wwwroot"; } }

    /// <summary>
    /// Discovers the single materialized framework directory produced by
    /// UpdatePackageStaticWebAssets for a built (non-published) project:
    /// <paramref name="objDir"/>/fx/&lt;source-id&gt;/_framework/.
    ///
    /// The per-project folder name under obj/.../fx/ is derived from static web assets
    /// metadata (SourceId / PackageId), so tests should not assume it matches the
    /// project directory name.
    /// </summary>
    public static string GetMaterializedFrameworkDir(string objDir)
    {
        string fxBaseDir = Path.Combine(objDir, "fx");
        Assert.True(Directory.Exists(fxBaseDir), $"Expected materialized framework base directory: {fxBaseDir}");
        string[] fxSubDirs = Directory.GetDirectories(fxBaseDir);
        Assert.True(fxSubDirs.Length == 1, $"Expected exactly one subdirectory under {fxBaseDir}, found: {string.Join(", ", fxSubDirs.Select(Path.GetFileName))}");
        string fxFrameworkDir = Path.Combine(fxSubDirs[0], "_framework");
        Assert.True(Directory.Exists(fxFrameworkDir), $"Expected materialized framework dir: {fxFrameworkDir}");
        return fxFrameworkDir;
    }

    protected override IReadOnlyDictionary<string, bool> GetAllKnownDotnetFilesToFingerprintMap(AssertBundleOptions assertOptions)
    {
        var result = new SortedDictionary<string, bool>()
        {
            { "dotnet.js", true },
            { "dotnet.js.map", false },
            { "dotnet.native.js", true },
            { "dotnet.native.js.symbols", true },
            { "dotnet.native.wasm", true },
            { "dotnet.native.worker.mjs", true },
            { "dotnet.runtime.js", true },
            { "dotnet.runtime.js.map", false },
            { "dotnet.diagnostics.js", true },
            { "dotnet.diagnostics.js.map", false },
        };

        if (assertOptions.ExpectDotnetJsFingerprinting == false)
            result["dotnet.js"] = false;

        return result;
    }

    protected override IReadOnlySet<string> GetDotNetFilesExpectedSet(AssertBundleOptions assertOptions)
    {
        SortedSet<string> res = new()
        {
           "dotnet.js",
           "dotnet.native.wasm",
           "dotnet.native.js",
           "dotnet.runtime.js",
        };
        if (assertOptions.BuildOptions.RuntimeType is RuntimeVariant.MultiThreaded)
        {
            res.Add("dotnet.native.worker.mjs");
        }

        if (!assertOptions.BuildOptions.IsPublish)
        {
            res.Add("dotnet.js.map");
            res.Add("dotnet.runtime.js.map");
        }

        if (assertOptions.AssertSymbolsFile && assertOptions.ExpectSymbolsFile)
            res.Add("dotnet.native.js.symbols");

        if (assertOptions.BuildOptions.EnableDiagnostics || EnvironmentVariables.RuntimeFlavor == "CoreCLR")
        {
            res.Add("dotnet.diagnostics.js");
            if (!assertOptions.BuildOptions.IsPublish)
                res.Add("dotnet.diagnostics.js.map");
        }

        return res;
    }

    public NativeFilesType GetExpectedFileType(Configuration config, bool isAOT, bool isPublish, bool isUsingWorkloads, bool? isNativeBuild=null) =>
        isNativeBuild == true ? NativeFilesType.Relinked : // precedence over build/publish check: build with -p:WasmBuildNative=true should use relinked
        !isPublish ? NativeFilesType.FromRuntimePack : // precedence over AOT check: build with AOT should use runtime pack
        isAOT ? NativeFilesType.AOT : // precedence over -p:WasmBuildNative=false check: publish with AOT relinks regardless of WasmBuildNative value
        isNativeBuild == false ? NativeFilesType.FromRuntimePack :
        (config == Configuration.Release) ? NativeFilesType.Relinked :
        NativeFilesType.FromRuntimePack;

    public void AssertBundle(Configuration config, MSBuildOptions buildOptions, bool isUsingWorkloads, bool? isNativeBuild = null, bool? wasmFingerprintDotnetJs = null)
    {
        string frameworkDir = string.IsNullOrEmpty(buildOptions.NonDefaultFrameworkDir) ?
            GetBinFrameworkDir(config, buildOptions.IsPublish, DefaultTargetFramework) :
            buildOptions.NonDefaultFrameworkDir;

        AssertBundle(new AssertBundleOptions(
            config,
            BuildOptions: buildOptions,
            ExpectedFileType: GetExpectedFileType(config, buildOptions.AOT, buildOptions.IsPublish, isUsingWorkloads, isNativeBuild),
            BinFrameworkDir: frameworkDir,
            ExpectSymbolsFile: true,
            AssertIcuAssets: true,
            AssertSymbolsFile: false,
            ExpectDotnetJsFingerprinting: wasmFingerprintDotnetJs
        ));
    }

    private void AssertBundle(AssertBundleOptions assertOptions)
    {
        IReadOnlyDictionary<string, DotNetFileName> actualDotnetFiles = AssertBasicBundle(assertOptions);

        if (assertOptions.BuildOptions.IsPublish)
        {
            string publishPath = Path.GetFullPath(Path.Combine(assertOptions.BinFrameworkDir, "..", ".."));
            Assert.Equal("publish", Path.GetFileName(publishPath));

            var dlls = Directory.EnumerateFiles(publishPath, "*.dll");
            Assert.False(dlls.Any(), $"Did not expect to find any .dll in {publishPath} but found {string.Join(",", dlls)}");

            var wasmAssemblies = Directory.EnumerateFiles(publishPath, "*.wasm");
            Assert.False(wasmAssemblies.Any(), $"Did not expect to find any .wasm files in {publishPath} but found {string.Join(",", wasmAssemblies)}");
        }

        if (!BuildTestBase.IsUsingWorkloads)
            return;

        // Compare files with the runtime pack
        string objBuildDir = Path.Combine(ProjectDir!, "obj", assertOptions.Configuration.ToString(), assertOptions.BuildOptions.TargetFramework, "wasm", assertOptions.BuildOptions.IsPublish ? "for-publish" : "for-build");

        string runtimeNativeDir = BuildTestBase.s_buildEnv.GetRuntimeNativeDir(assertOptions.BuildOptions.TargetFramework, assertOptions.BuildOptions.RuntimeType);

        string srcDirForNativeFileToCompareAgainst = assertOptions.ExpectedFileType switch
        {
            NativeFilesType.FromRuntimePack => runtimeNativeDir,
            NativeFilesType.Relinked => objBuildDir,
            NativeFilesType.AOT => objBuildDir,
            _ => throw new ArgumentOutOfRangeException(nameof(assertOptions.BuildOptions.ExpectedFileType))
        };

        string buildType = assertOptions.BuildOptions.IsPublish ? "publish" : "build";
        var nativeFilesToCheck = new List<string>() { "dotnet.native.wasm", "dotnet.native.js" };
        if (assertOptions.BuildOptions.RuntimeType == RuntimeVariant.MultiThreaded)
        {
            nativeFilesToCheck.Add("dotnet.native.worker.mjs");
        }

        foreach (string nativeFilename in nativeFilesToCheck)
        {
            if (!actualDotnetFiles.TryGetValue(nativeFilename, out DotNetFileName? dotnetFile))
            {
                throw new XunitException($"Could not find {nativeFilename}. Actual files on disk: {string.Join($"{Environment.NewLine}  ", actualDotnetFiles.Values.Select(a => a.ActualPath).Order())}");
            }
            // For any *type*, check against the expected path
            TestUtils.AssertSameFile(Path.Combine(srcDirForNativeFileToCompareAgainst, nativeFilename),
                           actualDotnetFiles[nativeFilename].ActualPath,
                           buildType);

            if (assertOptions.BuildOptions.ExpectedFileType != NativeFilesType.FromRuntimePack)
            {
                if (nativeFilename == "dotnet.native.worker.mjs")
                {
                    Console.WriteLine($"Skipping the verification whether {nativeFilename} is from the runtime pack. The check wouldn't be meaningful as the runtime pack file has the same size as the relinked file");
                    continue;
                }
                // Confirm that it doesn't match the file from the runtime pack
                TestUtils.AssertNotSameFile(Path.Combine(runtimeNativeDir, nativeFilename),
                                   actualDotnetFiles[nativeFilename].ActualPath,
                                   buildType);
            }
        }
    }

    public void AssertWasmSdkBundle(Configuration config, MSBuildOptions buildOptions, bool isUsingWorkloads, bool? isNativeBuild = null, bool? wasmFingerprintDotnetJs = null, string? buildOutput = null)
    {
        if (isUsingWorkloads && buildOutput is not null)
        {
            // In no-workload case, the path would be from a restored nuget
            ProjectProviderBase.AssertRuntimePackPath(buildOutput, buildOptions.TargetFramework ?? DefaultTargetFramework, buildOptions.RuntimeType);
        }

        if (buildOptions.IsPublish)
        {
            AssertBundle(config, buildOptions, isUsingWorkloads, isNativeBuild, wasmFingerprintDotnetJs);
        }
        else if (string.IsNullOrEmpty(buildOptions.NonDefaultFrameworkDir))
        {
            AssertBuildBundle(config, buildOptions, isUsingWorkloads, isNativeBuild);
        }
        else
        {
            // When NonDefaultFrameworkDir is set (e.g. UseArtifactsOutput), the obj/ layout
            // may not follow the standard path convention, so skip build bundle assertions.
            _testOutput.WriteLine(
                $"Skipping build bundle assertions: NonDefaultFrameworkDir='{buildOptions.NonDefaultFrameworkDir}' " +
                "points to a non-standard obj/ layout. File-layout verification is not performed for this build.");
        }
    }

    /// <summary>
    /// Asserts that build-time framework assets are in their expected obj/ subdirectories
    /// and NOT in the wrong directories. With CopyToOutputDirectory=Never, framework files
    /// live in obj/ subdirectories instead of being collected in bin/_framework/:
    ///
    ///   obj/{config}/{tfm}/                          → dotnet.js (Computed, boot config entry point)
    ///   obj/{config}/{tfm}/fx/{name}/_framework/     → all materialized framework files: dotnet.runtime.js, dotnet.native.*, ICU, maps
    ///   obj/{config}/{tfm}/webcil/                   → assembly .wasm files (webcil-converted)
    ///   obj/{config}/{tfm}/wasm/for-build/           → native assets only when native build (AOT/relink)
    /// </summary>
    private void AssertBuildBundle(Configuration config, MSBuildOptions buildOptions, bool isUsingWorkloads, bool? isNativeBuild)
    {
        EnsureProjectDirIsSet();

        string tfm = buildOptions.TargetFramework;
        string objDir = Path.Combine(ProjectDir!, "obj", config.ToString(), tfm);
        string webcilDir = Path.Combine(objDir, "webcil");

        // Discover the materialized framework directory: obj/{config}/{tfm}/fx/{source-id}/_framework/
        string fxFrameworkDir = GetMaterializedFrameworkDir(objDir);

        // --- Computed assets: dotnet.js lives in objDir root ---
        AssertFileExists(objDir, "dotnet.js");
        AssertFileNotExists(fxFrameworkDir, "dotnet.js", "fx/_framework");

        // --- Materialized framework assets: JS modules and source maps in fx/_framework/ ---
        string[] materializedFiles = ["dotnet.runtime.js", "dotnet.runtime.js.map", "dotnet.js.map"];
        if (buildOptions.EnableDiagnostics || EnvironmentVariables.RuntimeFlavor == "CoreCLR")
        {
            materializedFiles = [.. materializedFiles, "dotnet.diagnostics.js", "dotnet.diagnostics.js.map"];
        }
        foreach (string file in materializedFiles)
        {
            AssertFileExists(fxFrameworkDir, file);
            AssertFileNotExists(objDir, file, "obj root");
        }

        // --- Native assets: dotnet.native.* ---
        // For non-native builds, native files are materialized in fx/_framework/ from runtime pack.
        // For native builds (AOT/relink), they are rebuilt and placed in wasm/for-build/.
        string[] nativeFiles = ["dotnet.native.js", "dotnet.native.wasm"];
        var expectedFileType = isUsingWorkloads
            ? GetExpectedFileType(config, buildOptions.AOT, isPublish: false, isUsingWorkloads: isUsingWorkloads, isNativeBuild: isNativeBuild)
            : NativeFilesType.FromRuntimePack;
        bool isNativeRebuild = expectedFileType is NativeFilesType.Relinked or NativeFilesType.AOT;
        string nativeDir = isNativeRebuild
            ? Path.Combine(objDir, "wasm", "for-build")
            : fxFrameworkDir;

        foreach (string file in nativeFiles)
        {
            AssertFileExists(nativeDir, file);
            AssertFileNotExists(objDir, file, "obj root");
            if (!isNativeRebuild)
                AssertFileNotExists(Path.Combine(objDir, "wasm", "for-build"), file, "wasm/for-build");
        }

        if (buildOptions.RuntimeType == RuntimeVariant.MultiThreaded)
        {
            // dotnet.native.worker.mjs is validated for location only and not compared against
            // the runtime pack — the publish-path AssertBundle skips the runtime-pack comparison
            // for the same reason (the runtime-pack file has the same size as the relinked file,
            // so the check is not meaningful).
            const string multiThreadedWorkerFile = "dotnet.native.worker.mjs";
            AssertFileExists(nativeDir, multiThreadedWorkerFile);
            AssertFileNotExists(objDir, multiThreadedWorkerFile, "obj root");
            if (!isNativeRebuild)
                AssertFileNotExists(Path.Combine(objDir, "wasm", "for-build"), multiThreadedWorkerFile, "wasm/for-build");
        }

        // --- Assembly files: webcil-converted in webcil/ or materialized DLLs in fx/_framework/ ---
        if (BuildTestBase.UseWebcil)
        {
            Assert.True(Directory.Exists(webcilDir), $"Expected webcil directory: {webcilDir}");
            AssertFileExists(webcilDir, "System.Private.CoreLib.wasm");
            AssertFileNotExists(fxFrameworkDir, "System.Private.CoreLib.wasm", "fx/_framework");
        }
        else
        {
            // When webcil is disabled, assembly DLLs are framework pass-through candidates
            // and get materialized alongside other framework files in fx/_framework/.
            AssertFileExists(fxFrameworkDir, "System.Private.CoreLib.dll");
        }

        // --- Boot config: parse from obj/dotnet.js to validate boot JSON is well-formed ---
        string bootConfigPath = GetBootConfigPath(objDir, "dotnet.js");
        BootJsonData bootJson = GetBootJson(bootConfigPath);
        Assert.NotNull(bootJson.resources);

        // --- Framework files (native, runtime, assemblies) must NOT be in bin/_framework/ ---
        // dotnet.js (boot config) IS expected in bin/_framework/ since it keeps CopyToOutputDirectory=PreserveNewest.
        string binFrameworkDir = GetBinFrameworkDir(config, forPublish: false, tfm);
        if (Directory.Exists(binFrameworkDir))
        {
            foreach (string file in nativeFiles.Concat(materializedFiles))
            {
                AssertFileNotExists(binFrameworkDir, file, "bin/_framework");
            }
        }

        // --- Native file comparison against runtime pack ---
        if (isUsingWorkloads)
        {
            string runtimeNativeDir = BuildTestBase.s_buildEnv.GetRuntimeNativeDir(tfm, buildOptions.RuntimeType);
            foreach (string nativeFilename in nativeFiles)
            {
                string actualPath = Path.Combine(nativeDir, nativeFilename);
                if (expectedFileType == NativeFilesType.FromRuntimePack)
                {
                    TestUtils.AssertSameFile(Path.Combine(runtimeNativeDir, nativeFilename), actualPath, "build");
                }
            }
        }
    }

    private static void AssertFileExists(string dir, string filename)
    {
        Assert.True(File.Exists(Path.Combine(dir, filename)),
            $"Expected {filename} in {dir}");
    }

    private static void AssertFileNotExists(string dir, string filename, string dirLabel)
    {
        Assert.False(File.Exists(Path.Combine(dir, filename)),
            $"Did not expect {filename} in {dirLabel} ({dir})");
    }

    public BuildPaths GetBuildPaths(Configuration configuration, bool forPublish, string? projectDir = null)
    {
        projectDir ??= ProjectDir!;
        Assert.NotNull(projectDir);
        string configStr = configuration.ToString();
        string objDir = Path.Combine(projectDir, "obj", configStr, DefaultTargetFramework);
        string binDir = Path.Combine(projectDir, "bin", configStr, DefaultTargetFramework);
        string binFrameworkDir = GetBinFrameworkDir(configuration, forPublish, DefaultTargetFramework);

        string objWasmDir = Path.Combine(objDir, "wasm", forPublish ? "for-publish" : "for-build");
        // for build: we should take from runtime pack?
        return new BuildPaths(objWasmDir, objDir, binDir, binFrameworkDir);
    }

    public override string GetBinFrameworkDir(Configuration config, bool forPublish, string framework, string? projectDir = null)
    {
        EnsureProjectDirIsSet();
        string basePath = Path.Combine(projectDir ?? ProjectDir!, "bin", config.ToString(), framework);
        if (forPublish)
            basePath = Path.Combine(basePath, "publish");

        return Path.Combine(basePath, BundleDirName, "_framework");
    }
}
