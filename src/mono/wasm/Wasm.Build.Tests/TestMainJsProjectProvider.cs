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
    {}

    // no fingerprinting
    protected override IReadOnlyDictionary<string, bool> GetAllKnownDotnetFilesToFingerprintMap(RuntimeVariant runtimeType)
        => new SortedDictionary<string, bool>()
            {
               { "dotnet.js", false },
               { "dotnet.js.map", false },
               { "dotnet.native.js", false },
               { "dotnet.native.wasm", false },
               { "dotnet.native.worker.js", false },
               { "dotnet.runtime.js", false },
               { "dotnet.runtime.js.map", false }
            };

    protected override IReadOnlySet<string> GetDotNetFilesExpectedSet(RuntimeVariant runtimeType, bool isPublish)
    {
        SortedSet<string>? res = null;
        if (runtimeType is RuntimeVariant.SingleThreaded)
        {
            res = new SortedSet<string>()
            {
               "dotnet.js",
               "dotnet.native.wasm",
               "dotnet.native.js",
               "dotnet.runtime.js",
            };

            res.Add("dotnet.js.map");
            res.Add("dotnet.runtime.js.map");
        }

        if (runtimeType is RuntimeVariant.MultiThreaded)
        {
            res = new SortedSet<string>()
            {
               "dotnet.js",
               "dotnet.native.js",
               "dotnet.native.wasm",
               "dotnet.native.worker.js",
               "dotnet.runtime.js",
            };
            if (!isPublish)
            {
                res.Add("dotnet.js.map");
                res.Add("dotnet.runtime.js.map");
                res.Add("dotnet.native.worker.js.map");
            }
        }

        return res ?? throw new ArgumentException($"Unknown runtime type: {runtimeType}");
    }

    public void AssertBasicAppBundle(AssertTestMainJsAppBundleOptions options)
    {
        EnsureProjectDirIsSet();
        new TestMainJsProjectProvider(_testOutput, ProjectDir)
                .FindAndAssertDotnetFiles(
                    Path.Combine(options.BundleDir, "_framework"),
                    isPublish: options.IsPublish,
                    expectFingerprintOnDotnetJs: false,
                    runtimeType: RuntimeVariant.SingleThreaded);

        var filesToExist = new List<string>()
        {
            options.MainJS,
            "_framework/blazor.boot.json",
            "_framework/dotnet.js.map",
            "_framework/dotnet.runtime.js.map",
        };

        if (options.IsBrowserProject)
            filesToExist.Add("index.html");

        TestUtils.AssertFilesExist(options.BundleDir, filesToExist);

        TestUtils.AssertFilesExist(options.BundleDir, new[] { "run-v8.sh" }, expectToExist: options.HasV8Script);
        AssertIcuAssets();

        string managedDir = Path.Combine(options.BundleDir, "_framework");
        string bundledMainAppAssembly =
            options.UseWebcil ? $"{options.ProjectName}{WebcilInWasmExtension}" : $"{options.ProjectName}.dll";
        TestUtils.AssertFilesExist(managedDir, new[] { bundledMainAppAssembly });

        bool is_debug = options.Config == "Debug";
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

        void AssertIcuAssets()
        {
            bool expectEFIGS = false;
            bool expectCJK = false;
            bool expectNOCJK = false;
            bool expectFULL = false;
            bool expectHYBRID = false;
            switch (options.GlobalizationMode)
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
                    if (string.IsNullOrEmpty(options.PredefinedIcudt))
                        throw new ArgumentException("WasmBuildTest is invalid, value for predefinedIcudt is required when GlobalizationMode=PredefinedIcu.");
                    TestUtils.AssertFilesExist(options.BundleDir, new[] { Path.Combine("_framework", options.PredefinedIcudt) }, expectToExist: true);
                    // predefined ICU name can be identical with the icu files from runtime pack
                    switch (options.PredefinedIcudt)
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

            var frameworkDir = Path.Combine(options.BundleDir, "_framework");
            TestUtils.AssertFilesExist(frameworkDir, new[] { "icudt.dat" }, expectToExist: expectFULL);
            TestUtils.AssertFilesExist(frameworkDir, new[] { "icudt_EFIGS.dat" }, expectToExist: expectEFIGS);
            TestUtils.AssertFilesExist(frameworkDir, new[] { "icudt_CJK.dat" }, expectToExist: expectCJK);
            TestUtils.AssertFilesExist(frameworkDir, new[] { "icudt_no_CJK.dat" }, expectToExist: expectNOCJK);
            TestUtils.AssertFilesExist(frameworkDir, new[] { "icudt_hybrid.dat" }, expectToExist: expectHYBRID);
        }
    }
}
