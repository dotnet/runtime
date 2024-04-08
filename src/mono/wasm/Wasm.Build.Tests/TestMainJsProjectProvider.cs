// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Xunit.Abstractions;

namespace Wasm.Build.Tests;

public class TestMainJsProjectProvider : ProjectProviderBase
{
    public TestMainJsProjectProvider(ITestOutputHelper _testOutput, string? _projectDir = null)
            : base(_testOutput, _projectDir)
    {
        BundleDirName = "AppBundle";
    }

    // no fingerprinting
    protected override IReadOnlyDictionary<string, bool> GetAllKnownDotnetFilesToFingerprintMap(AssertBundleOptionsBase assertOptions)
        => new SortedDictionary<string, bool>()
            {
               { "dotnet.js", false },
               { "dotnet.js.map", false },
               { "dotnet.native.js", false },
               { "dotnet.native.js.symbols", false },
               { "dotnet.native.wasm", false },
               { "dotnet.native.worker.js", false },
               { "dotnet.runtime.js", false },
               { "dotnet.runtime.js.map", false }
            };

    protected override IReadOnlySet<string> GetDotNetFilesExpectedSet(AssertBundleOptionsBase assertOptions)
    {
        SortedSet<string>? res = new();
        if (assertOptions.RuntimeType is RuntimeVariant.SingleThreaded)
        {
            res.Add("dotnet.js");
            res.Add("dotnet.native.wasm");
            res.Add("dotnet.native.js");
            res.Add("dotnet.runtime.js");
            res.Add("dotnet.js.map");
            res.Add("dotnet.runtime.js.map");
        }

        if (assertOptions.RuntimeType is RuntimeVariant.MultiThreaded)
        {
            res.Add("dotnet.js");
            res.Add("dotnet.native.wasm");
            res.Add("dotnet.native.js");
            res.Add("dotnet.runtime.js");
            res.Add("dotnet.native.worker.js");

            if (!assertOptions.IsPublish)
            {
                res.Add("dotnet.js.map");
                res.Add("dotnet.runtime.js.map");
                res.Add("dotnet.native.worker.js.map");
            }
        }

        if (assertOptions.AssertSymbolsFile && assertOptions.ExpectSymbolsFile)
            res.Add("dotnet.native.js.symbols");

        return res ?? throw new ArgumentException($"Unknown runtime type: {assertOptions.RuntimeType}");
    }

    public void AssertBundle(AssertTestMainJsAppBundleOptions assertOptions)
    {
        AssertBasicBundle(assertOptions);

        TestUtils.AssertFilesExist(assertOptions.BundleDir, new[] { assertOptions.MainJS });
        if (assertOptions.IsBrowserProject)
            TestUtils.AssertFilesExist(assertOptions.BundleDir, new[] { "index.html" });
        TestUtils.AssertFilesExist(assertOptions.BundleDir, new[] { "run-v8.sh" }, expectToExist: assertOptions.HasV8Script);

        string bundledMainAppAssembly = $"{assertOptions.ProjectName}{WasmAssemblyExtension}";
        TestUtils.AssertFilesExist(assertOptions.BinFrameworkDir, new[] { bundledMainAppAssembly });
    }

    public void AssertBundle(BuildArgs buildArgs, BuildProjectOptions buildProjectOptions)
    {
        string binFrameworkDir = buildProjectOptions.BinFrameworkDir
                                    ?? FindBinFrameworkDir(buildArgs.Config,
                                                     buildProjectOptions.Publish,
                                                     buildProjectOptions.TargetFramework);
        NativeFilesType expectedFileType = buildArgs.AOT
                                            ? NativeFilesType.AOT
                                            : buildProjectOptions.DotnetWasmFromRuntimePack == false
                                                ? NativeFilesType.Relinked
                                                : NativeFilesType.FromRuntimePack;

        var assertOptions = new AssertTestMainJsAppBundleOptions(
                                        Config: buildArgs.Config,
                                        IsPublish: buildProjectOptions.Publish,
                                        TargetFramework: buildProjectOptions.TargetFramework!,
                                        BinFrameworkDir: binFrameworkDir,
                                        ProjectName: buildArgs.ProjectName,
                                        MainJS: buildProjectOptions.MainJS ?? "test-main.js",
                                        GlobalizationMode: buildProjectOptions.GlobalizationMode,
                                        HasV8Script: buildProjectOptions.HasV8Script,
                                        PredefinedIcudt: buildProjectOptions.PredefinedIcudt ?? string.Empty,
                                        IsBrowserProject: buildProjectOptions.IsBrowserProject,
                                        ExpectedFileType: expectedFileType,
                                        ExpectSymbolsFile: !buildArgs.AOT);
        AssertBundle(assertOptions);
    }

    public override string FindBinFrameworkDir(string config, bool forPublish, string framework, string? bundleDirName = null, string? projectDir = null)
    {
        EnsureProjectDirIsSet();
        return Path.Combine(projectDir ?? ProjectDir!, "bin", config, framework, "browser-wasm", bundleDirName ?? this.BundleDirName, "_framework");
    }
}
