// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Wasm.Build.Tests;

public class TestMainJsProjectProvider
{
    public static void AssertRuntimePackPath(string buildOutput, string targetFramework)
    {
        var match = BuildTestBase.s_runtimePackPathRegex.Match(buildOutput);
        if (!match.Success || match.Groups.Count != 2)
            throw new XunitException($"Could not find the pattern in the build output: '{BuildTestBase.s_runtimePackPathPattern}'.{Environment.NewLine}Build output: {buildOutput}");

        string expectedRuntimePackDir = BuildTestBase.s_buildEnv.GetRuntimePackDir(targetFramework);
        string actualPath = match.Groups[1].Value;
        if (string.Compare(actualPath, expectedRuntimePackDir) != 0)
            throw new XunitException($"Runtime pack path doesn't match.{Environment.NewLine}Expected: '{expectedRuntimePackDir}'{Environment.NewLine}Actual:   '{actualPath}'");
    }

    public static void AssertBasicAppBundle(AssertTestMainJsAppBundleOptions options)
    {
        var filesToExist = new List<string>()
        {
            options.mainJS,
            "dotnet.native.wasm",
            "_framework/blazor.boot.json",
            "dotnet.js",
            "dotnet.native.js",
            "dotnet.runtime.js"
        };

        if (options.isBrowserProject)
            filesToExist.Add("index.html");

        BuildTestBase.AssertFilesExist(options.bundleDir, filesToExist);

        BuildTestBase.AssertFilesExist(options.bundleDir, new[] { "run-v8.sh" }, expectToExist: options.hasV8Script);
        AssertIcuAssets();

        string managedDir = Path.Combine(options.bundleDir, "managed");
        string bundledMainAppAssembly =
            options.useWebcil ? $"{options.projectName}{BuildTestBase.WebcilInWasmExtension}" : $"{options.projectName}.dll";
        BuildTestBase.AssertFilesExist(managedDir, new[] { bundledMainAppAssembly });

        bool is_debug = options.config == "Debug";
        if (is_debug)
        {
            // Use cecil to check embedded pdb?
            // AssertFilesExist(managedDir, new[] { $"{projectName}.pdb" });

            //FIXME: um.. what about these? embedded? why is linker omitting them?
            //foreach (string file in Directory.EnumerateFiles(managedDir, "*.dll"))
            //{
            //string pdb = Path.ChangeExtension(file, ".pdb");
            //Assert.True(File.Exists(pdb), $"Could not find {pdb} for {file}");
            //}
        }

        AssertDotNetWasmJs(options.bundleDir, fromRuntimePack: options.dotnetWasmFromRuntimePack, options.targetFramework);

        void AssertIcuAssets()
        {
            bool expectEFIGS = false;
            bool expectCJK = false;
            bool expectNOCJK = false;
            bool expectFULL = false;
            bool expectHYBRID = false;
            switch (options.globalizationMode)
            {
                case GlobalizationMode.Invariant:
                    break;
                case GlobalizationMode.FullIcu:
                    expectFULL = true;
                    break;
                case GlobalizationMode.Hybrid:
                    expectHYBRID = true;
                    break;
                case GlobalizationMode.PredefinedIcu:
                    if (string.IsNullOrEmpty(options.predefinedIcudt))
                        throw new ArgumentException("WasmBuildTest is invalid, value for predefinedIcudt is required when GlobalizationMode=PredefinedIcu.");
                    BuildTestBase.AssertFilesExist(options.bundleDir, new[] { options.predefinedIcudt }, expectToExist: true);
                    // predefined ICU name can be identical with the icu files from runtime pack
                    switch (options.predefinedIcudt)
                    {
                        case "icudt.dat":
                            expectFULL = true;
                            break;
                        case "icudt_EFIGS.dat":
                            expectEFIGS = true;
                            break;
                        case "icudt_CJK.dat":
                            expectCJK = true;
                            break;
                        case "icudt_no_CJK.dat":
                            expectNOCJK = true;
                            break;
                    }
                    break;
                default:
                    // icu shard chosen based on the locale
                    expectCJK = true;
                    expectEFIGS = true;
                    expectNOCJK = true;
                    break;
            }
            BuildTestBase.AssertFilesExist(options.bundleDir, new[] { "icudt.dat" }, expectToExist: expectFULL);
            BuildTestBase.AssertFilesExist(options.bundleDir, new[] { "icudt_EFIGS.dat" }, expectToExist: expectEFIGS);
            BuildTestBase.AssertFilesExist(options.bundleDir, new[] { "icudt_CJK.dat" }, expectToExist: expectCJK);
            BuildTestBase.AssertFilesExist(options.bundleDir, new[] { "icudt_no_CJK.dat" }, expectToExist: expectNOCJK);
            BuildTestBase.AssertFilesExist(options.bundleDir, new[] { "icudt_hybrid.dat" }, expectToExist: expectHYBRID);
        }
    }

    private static void AssertDotNetWasmJs(string bundleDir, bool fromRuntimePack, string targetFramework)
    {
        BuildTestBase.AssertFile(Path.Combine(BuildTestBase.s_buildEnv.GetRuntimeNativeDir(targetFramework), "dotnet.native.wasm"),
                   Path.Combine(bundleDir, "dotnet.native.wasm"),
                   "Expected dotnet.native.wasm to be same as the runtime pack",
                   same: fromRuntimePack);

        BuildTestBase.AssertFile(Path.Combine(BuildTestBase.s_buildEnv.GetRuntimeNativeDir(targetFramework), "dotnet.native.js"),
                   Path.Combine(bundleDir, "dotnet.native.js"),
                   "Expected dotnet.native.js to be same as the runtime pack",
                   same: fromRuntimePack);
    }

}

public record AssertTestMainJsAppBundleOptions
(
   string bundleDir,
   string projectName,
   string config,
   string mainJS,
   bool hasV8Script,
   string targetFramework,
   GlobalizationMode? globalizationMode,
   string predefinedIcudt = "",
   bool dotnetWasmFromRuntimePack = true,
   bool useWebcil = true,
   bool isBrowserProject = true
);
