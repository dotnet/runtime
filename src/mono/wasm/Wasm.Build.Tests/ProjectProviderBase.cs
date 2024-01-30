// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.NET.Sdk.WebAssembly;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Wasm.Build.Tests;

// For projects using WasmAppBuilder
public abstract class ProjectProviderBase(ITestOutputHelper _testOutput, string? _projectDir)
{
    public static string WasmAssemblyExtension = BuildTestBase.s_buildEnv.UseWebcil ? ".wasm" : ".dll";
    protected const string s_dotnetVersionHashRegex = @"\.(?<version>[0-9]+\.[0-9]+\.[a-zA-Z0-9\.-]+)\.(?<hash>[a-zA-Z0-9]+)\.";

    private const string s_runtimePackPathPattern = "\\*\\* MicrosoftNetCoreAppRuntimePackDir : '([^ ']*)'";
    private static Regex s_runtimePackPathRegex = new Regex(s_runtimePackPathPattern);
    private static string[] s_dotnetExtensionsToIgnore = new[]
    {
        ".gz",
        ".br"
    };

    public string? ProjectDir { get; set; } = _projectDir;
    protected ITestOutputHelper _testOutput = new TestOutputWrapper(_testOutput);
    protected BuildEnvironment _buildEnv = BuildTestBase.s_buildEnv;
    public string BundleDirName { get; set; } = "wwwroot";

    // Returns the actual files on disk
    public IReadOnlyDictionary<string, DotNetFileName> AssertBasicBundle(AssertBundleOptionsBase assertOptions)
    {
        EnsureProjectDirIsSet();
        var dotnetFiles = FindAndAssertDotnetFiles(assertOptions);

        TestUtils.AssertFilesExist(assertOptions.BinFrameworkDir,
                                   new[] { "System.Private.CoreLib.dll" },
                                   expectToExist: !BuildTestBase.UseWebcil);
        TestUtils.AssertFilesExist(assertOptions.BinFrameworkDir,
                                   new[] { "System.Private.CoreLib.wasm" },
                                   expectToExist: BuildTestBase.UseWebcil);

        AssertBootJson(assertOptions);

        // icu
        if (assertOptions.AssertIcuAssets)
        {
            AssertIcuAssets(assertOptions);
        }
        else
        {
            _testOutput.WriteLine("Skipping asserting icu assets");
        }

        // symbols
        if (assertOptions.AssertSymbolsFile)
        {
            _testOutput.WriteLine("Skipping asserting symbols file");
            AssertDotNetJsSymbols(assertOptions);
        }

        return dotnetFiles;
    }

    public IReadOnlyDictionary<string, DotNetFileName> FindAndAssertDotnetFiles(AssertBundleOptionsBase assertOptions)
    {
        EnsureProjectDirIsSet();
        return FindAndAssertDotnetFiles(binFrameworkDir: assertOptions.BinFrameworkDir,
                                        expectFingerprintOnDotnetJs: assertOptions.ExpectFingerprintOnDotnetJs,
                                        superSet: GetAllKnownDotnetFilesToFingerprintMap(assertOptions),
                                        expected: GetDotNetFilesExpectedSet(assertOptions));
    }

    protected abstract IReadOnlyDictionary<string, bool> GetAllKnownDotnetFilesToFingerprintMap(AssertBundleOptionsBase assertOptions);
    protected abstract IReadOnlySet<string> GetDotNetFilesExpectedSet(AssertBundleOptionsBase assertOptions);

    public IReadOnlyDictionary<string, DotNetFileName> FindAndAssertDotnetFiles(
        string binFrameworkDir,
        bool expectFingerprintOnDotnetJs,
        IReadOnlyDictionary<string, bool> superSet,
        IReadOnlySet<string>? expected)
    {
        EnsureProjectDirIsSet();
        var actual = new SortedDictionary<string, DotNetFileName>();

        if (!Directory.Exists(binFrameworkDir))
            throw new XunitException($"Could not find bundle directory {binFrameworkDir}");

        IList<string> dotnetFiles = Directory.EnumerateFiles(binFrameworkDir,
                                                             "dotnet.*",
                                                             SearchOption.TopDirectoryOnly)
                                                .Order()
                                                .ToList();
        foreach ((string expectedFilename, bool expectFingerprint) in superSet.OrderByDescending(kvp => kvp.Key))
        {
            string prefix = Path.GetFileNameWithoutExtension(expectedFilename);
            string extension = Path.GetExtension(expectedFilename).Substring(1);

            dotnetFiles = dotnetFiles
                .Where(actualFile =>
                {
                    if (s_dotnetExtensionsToIgnore.Contains(Path.GetExtension(actualFile)))
                        return false;

                    string actualFilename = Path.GetFileName(actualFile);
                    // _testOutput.WriteLine($"Comparing {expectedFilename} with {actualFile}, expectFingerprintOnDotnetJs: {expectFingerprintOnDotnetJs}, expectFingerprint: {expectFingerprint}");
                    if (ShouldCheckFingerprint(expectedFilename: expectedFilename,
                                               expectFingerprintOnDotnetJs: expectFingerprintOnDotnetJs,
                                               expectFingerprintForThisFile: expectFingerprint))
                    {
                        string pattern = $"^{prefix}{s_dotnetVersionHashRegex}{extension}$";
                        var match = Regex.Match(actualFilename, pattern);
                        if (!match.Success)
                            return true;

                        actual[expectedFilename] = new(ExpectedFilename: expectedFilename,
                                                       Version: match.Groups[1].Value,
                                                       Hash: match.Groups[2].Value,
                                                       ActualPath: actualFile);
                    }
                    else
                    {
                        if (actualFilename != expectedFilename)
                            return true;

                        actual[expectedFilename] = new(ExpectedFilename: expectedFilename,
                                                       Version: null,
                                                       Hash: null,
                                                       ActualPath: actualFile);
                    }

                    return false;
                }).ToList();
        }

        // _testOutput.WriteLine($"Accepted count: {actual.Count}");
        // foreach (var kvp in actual)
        //     _testOutput.WriteLine($"Accepted: \t[{kvp.Key}] = {kvp.Value}");

        if (dotnetFiles.Any())
        {
            throw new XunitException($"Found unknown files in {binFrameworkDir}:{Environment.NewLine}    " +
                    $"{string.Join($"{Environment.NewLine}  ", dotnetFiles.Select(f => Path.GetRelativePath(binFrameworkDir, f)))}{Environment.NewLine}" +
                    $"Add these to {nameof(GetAllKnownDotnetFilesToFingerprintMap)} method");
        }

        if (expected is not null)
            AssertDotNetFilesSet(expected, superSet, actual, expectFingerprintOnDotnetJs, binFrameworkDir);
        return actual;
    }

    private void AssertDotNetFilesSet(
        IReadOnlySet<string> expected,
        IReadOnlyDictionary<string, bool> superSet,
        IReadOnlyDictionary<string, DotNetFileName> actualReadOnly,
        bool expectFingerprintOnDotnetJs,
        string bundleDir)
    {
        EnsureProjectDirIsSet();

        var actual = new Dictionary<string, DotNetFileName>(actualReadOnly);
        foreach (string expectedFilename in expected)
        {
            bool expectFingerprint = superSet[expectedFilename];

            Assert.True(actual.ContainsKey(expectedFilename), $"Could not find {expectedFilename} in bundle directory: {bundleDir}. Actual files on disk: {string.Join(", ", actual.Keys)}");

            // Check that the version and hash are present or not present as expected
            if (ShouldCheckFingerprint(expectedFilename: expectedFilename,
                                       expectFingerprintOnDotnetJs: expectFingerprintOnDotnetJs,
                                       expectFingerprintForThisFile: expectFingerprint))
            {
                if (string.IsNullOrEmpty(actual[expectedFilename].Version))
                    throw new XunitException($"Expected version in filename: {actual[expectedFilename].ActualPath}");
                if (string.IsNullOrEmpty(actual[expectedFilename].Hash))
                    throw new XunitException($"Expected hash in filename: {actual[expectedFilename].ActualPath}");
            }
            else
            {
                if (!string.IsNullOrEmpty(actual[expectedFilename].Version))
                    throw new XunitException($"Expected no version in filename: {actual[expectedFilename].ActualPath}");
                if (!string.IsNullOrEmpty(actual[expectedFilename].Hash))
                    throw new XunitException($"Expected no hash in filename: {actual[expectedFilename].ActualPath}");
            }
            actual.Remove(expectedFilename);
        }

        if (actual.Any())
        {
            var actualFileNames = actual.Values.Select(x => x.ActualPath).Order();
            throw new XunitException($"Found unexpected files: {string.Join(", ", actualFileNames)}");
        }
    }

    public void CompareStat(IDictionary<string, FileStat> oldStat, IDictionary<string, FileStat> newStat, IEnumerable<(string fullpath, bool unchanged)> expected)
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

    public IDictionary<string, FileStat> StatFiles(IEnumerable<string> fullpaths)
    {
        Dictionary<string, FileStat> table = new();
        foreach (string file in fullpaths)
        {
            if (File.Exists(file))
                table.Add(Path.GetFileName(file), new FileStat(FullPath: file, Exists: true, LastWriteTimeUtc: File.GetLastWriteTimeUtc(file), Length: new FileInfo(file).Length));
            else
                table.Add(Path.GetFileName(file), new FileStat(FullPath: file, Exists: false, LastWriteTimeUtc: DateTime.MinValue, Length: 0));
        }

        return table;
    }

    public static string FindSubDirIgnoringCase(string parentDir, string dirName)
    {
        IEnumerable<string> matchingDirs = Directory.EnumerateDirectories(parentDir,
                                                        dirName,
                                                        new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

        string? first = matchingDirs.FirstOrDefault();
        if (matchingDirs.Count() > 1)
            throw new Exception($"Found multiple directories with names that differ only in case. {string.Join(", ", matchingDirs.ToArray())}");

        return first ?? Path.Combine(parentDir, dirName);
    }

    public IDictionary<string, (string fullPath, bool unchanged)> GetFilesTable(bool unchanged, params string[] baseDirs)
    {
        var dict = new Dictionary<string, (string fullPath, bool unchanged)>();
        foreach (var baseDir in baseDirs)
        {
            foreach (var file in Directory.EnumerateFiles(baseDir, "*", new EnumerationOptions { RecurseSubdirectories = true }))
                dict[Path.GetFileName(file)] = (file, unchanged);
        }

        return dict;
    }

    public IDictionary<string, (string fullPath, bool unchanged)> GetFilesTable(BuildArgs buildArgs, BuildPaths paths, bool unchanged)
    {
        List<string> files = new()
        {
            Path.Combine(paths.BinDir, "publish", $"{buildArgs.ProjectName}.dll"),
            Path.Combine(paths.ObjWasmDir, "driver.o"),
            Path.Combine(paths.ObjWasmDir, "runtime.o"),
            Path.Combine(paths.ObjWasmDir, "corebindings.o"),
            Path.Combine(paths.ObjWasmDir, "pinvoke.o"),

            Path.Combine(paths.ObjWasmDir, "icall-table.h"),
            Path.Combine(paths.ObjWasmDir, "pinvoke-table.h"),
            Path.Combine(paths.ObjWasmDir, "driver-gen.c"),

            Path.Combine(paths.BundleDir, "_framework", "dotnet.native.wasm"),
            Path.Combine(paths.BundleDir, "_framework", "dotnet.native.js"),
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

        // those files do not change on re-link
        dict["dotnet.js"]=(Path.Combine(paths.BundleDir, "_framework", "dotnet.js"), true);
        dict["dotnet.js.map"]=(Path.Combine(paths.BundleDir, "_framework", "dotnet.js.map"), true);
        dict["dotnet.runtime.js"]=(Path.Combine(paths.BundleDir, "_framework", "dotnet.runtime.js"), true);
        dict["dotnet.runtime.js.map"]=(Path.Combine(paths.BundleDir, "_framework", "dotnet.runtime.js.map"), true);

        return dict;
    }

    public static bool ShouldCheckFingerprint(string expectedFilename, bool expectFingerprintOnDotnetJs, bool expectFingerprintForThisFile) =>
        (expectedFilename == "dotnet.js" && expectFingerprintOnDotnetJs) || expectFingerprintForThisFile;


    public static void AssertRuntimePackPath(string buildOutput, string targetFramework, RuntimeVariant runtimeType = RuntimeVariant.SingleThreaded)
    {
        var match = s_runtimePackPathRegex.Match(buildOutput);
        if (!match.Success || match.Groups.Count != 2)
            throw new XunitException($"Could not find the pattern in the build output: '{s_runtimePackPathPattern}'.{Environment.NewLine}Build output: {buildOutput}");

        string expectedRuntimePackDir = BuildTestBase.s_buildEnv.GetRuntimePackDir(targetFramework, runtimeType);
        string actualPath = match.Groups[1].Value;
        if (string.Compare(actualPath, expectedRuntimePackDir) != 0)
            throw new XunitException($"Runtime pack path doesn't match.{Environment.NewLine}Expected: '{expectedRuntimePackDir}'{Environment.NewLine}Actual:   '{actualPath}'");
    }

    public static void AssertDotNetJsSymbols(AssertBundleOptionsBase assertOptions)
    {
        TestUtils.AssertFilesExist(assertOptions.BinFrameworkDir, new[] { "dotnet.native.js.symbols" }, expectToExist: assertOptions.ExpectSymbolsFile);

        if (assertOptions.ExpectedFileType == NativeFilesType.FromRuntimePack)
        {
            TestUtils.AssertFile(
                    Path.Combine(BuildTestBase.s_buildEnv.GetRuntimeNativeDir(assertOptions.TargetFramework, assertOptions.RuntimeType), "dotnet.native.js.symbols"),
                    Path.Combine(assertOptions.BinFrameworkDir, "dotnet.native.js.symbols"),
                    same: true);
        }
    }

    public void AssertIcuAssets(AssertBundleOptionsBase assertOptions)
    {
        List<string> expected = new();
        switch (assertOptions.GlobalizationMode)
        {
            case GlobalizationMode.Invariant:
                break;
            case GlobalizationMode.FullIcu:
                expected.Add("icudt.dat");
                break;
            case GlobalizationMode.Hybrid:
                expected.Add("icudt_hybrid.dat");
                expected.Add("segmentation-rules.json");
                break;
            case GlobalizationMode.PredefinedIcu:
                if (string.IsNullOrEmpty(assertOptions.PredefinedIcudt))
                    throw new ArgumentException("WasmBuildTest is invalid, value for predefinedIcudt is required when GlobalizationMode=PredefinedIcu.");

                // predefined ICU name can be identical with the icu files from runtime pack
                expected.Add(assertOptions.PredefinedIcudt);
                break;
            case GlobalizationMode.Sharded:
                // icu shard chosen based on the locale
                expected.Add("icudt_CJK.dat");
                expected.Add("icudt_EFIGS.dat");
                expected.Add("icudt_no_CJK.dat");
                break;
            default:
                throw new NotImplementedException($"Unknown {nameof(assertOptions.GlobalizationMode)} = {assertOptions.GlobalizationMode}");
        }

        IEnumerable<string> actual = Directory.EnumerateFiles(assertOptions.BinFrameworkDir, "icudt*dat");
        if (assertOptions.GlobalizationMode == GlobalizationMode.Hybrid)
            actual = actual.Union(Directory.EnumerateFiles(assertOptions.BinFrameworkDir, "segmentation-rules.json"));
        AssertFileNames(expected, actual);
        if (assertOptions.GlobalizationMode is GlobalizationMode.PredefinedIcu)
        {
            string srcPath = assertOptions.PredefinedIcudt!;
            string runtimePackDir = BuildTestBase.s_buildEnv.GetRuntimeNativeDir(assertOptions.TargetFramework, assertOptions.RuntimeType);
            if (!Path.IsPathRooted(srcPath))
                srcPath = Path.Combine(runtimePackDir, assertOptions.PredefinedIcudt!);
            TestUtils.AssertSameFile(srcPath, actual.Single());
        }
    }

    public void AssertBootJson(AssertBundleOptionsBase options)
    {
        EnsureProjectDirIsSet();
        // string binFrameworkDir = FindBinFrameworkDir(options.Config, options.IsPublish, options.TargetFramework);
        string binFrameworkDir = options.BinFrameworkDir;
        string bootJsonPath = Path.Combine(binFrameworkDir, options.BootJsonFileName);
        Assert.True(File.Exists(bootJsonPath), $"Expected to find {bootJsonPath}");

        BootJsonData bootJson = ParseBootData(bootJsonPath);
        string spcExpectedFilename = $"System.Private.CoreLib{WasmAssemblyExtension}";
        string? spcActualFilename = bootJson.resources.assembly.Keys
                                        .Where(a => Path.GetFileNameWithoutExtension(a) == "System.Private.CoreLib")
                                        .SingleOrDefault();
        if (spcActualFilename is null)
            throw new XunitException($"Could not find an assembly named System.Private.CoreLib.* in {bootJsonPath}");
        if (spcExpectedFilename != spcActualFilename)
            throw new XunitException($"Expected to find {spcExpectedFilename} but found {spcActualFilename} in {bootJsonPath}");

        var bootJsonEntries = bootJson.resources.jsModuleNative.Keys
            .Union(bootJson.resources.jsModuleRuntime.Keys)
            .Union(bootJson.resources.jsModuleWorker?.Keys ?? Enumerable.Empty<string>())
            .Union(bootJson.resources.wasmSymbols?.Keys ?? Enumerable.Empty<string>())
            .Union(bootJson.resources.wasmNative.Keys)
            .ToArray();

        var expectedEntries = new SortedDictionary<string, Action<string>>();
        IReadOnlySet<string> expected = GetDotNetFilesExpectedSet(options);

        var knownSet = GetAllKnownDotnetFilesToFingerprintMap(options);
        foreach (string expectedFilename in expected)
        {
            // FIXME: Find a systematic solution for skipping dotnet.js from boot json check
            if (expectedFilename == "dotnet.js" || Path.GetExtension(expectedFilename) == ".map")
                continue;

            bool expectFingerprint = knownSet[expectedFilename];
            expectedEntries[expectedFilename] = item =>
            {
                string prefix = Path.GetFileNameWithoutExtension(expectedFilename);
                string extension = Path.GetExtension(expectedFilename).Substring(1);

                if (ShouldCheckFingerprint(expectedFilename: expectedFilename,
                                           expectFingerprintOnDotnetJs: options.ExpectFingerprintOnDotnetJs,
                                           expectFingerprintForThisFile: expectFingerprint))
                {
                    Assert.Matches($"{prefix}{s_dotnetVersionHashRegex}{extension}", item);
                }
                else
                {
                    Assert.Equal(expectedFilename, item);
                }

                string absolutePath = Path.Combine(binFrameworkDir, item);
                Assert.True(File.Exists(absolutePath), $"Expected to find '{absolutePath}'");
            };
        }
        // FIXME: maybe use custom code so the details can show up in the log
        bootJsonEntries = bootJsonEntries.Order().ToArray();
        if (bootJsonEntries.Length != expectedEntries.Count)
        {
            throw new XunitException($"In {bootJsonPath}{Environment.NewLine}" +
                                        $"  Expected: {string.Join(", ", expectedEntries.Keys.ToArray())}{Environment.NewLine}" +
                                        $"  Actual  : {string.Join(", ", bootJsonEntries)}");


        }
        Assert.Collection(bootJsonEntries.Order(), expectedEntries.Values.ToArray());
    }

    public static BootJsonData ParseBootData(string bootJsonPath)
    {
        using FileStream stream = File.OpenRead(bootJsonPath);
        stream.Position = 0;
        var serializer = new DataContractJsonSerializer(
            typeof(BootJsonData),
            new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });

        var config = (BootJsonData?)serializer.ReadObject(stream);
        Assert.NotNull(config);
        return config;
    }

    private void AssertFileNames(IEnumerable<string> expected, IEnumerable<string> actual)
    {
        expected = expected.Order().Select(f => Path.GetFileName(f)).Distinct();
        var actualFileNames = actual.Order().Select(f => Path.GetFileName(f));
        if (expected.Count() != actualFileNames.Count())
        {
            throw new XunitException(
                    $"Expected: {string.Join(", ", expected)}{Environment.NewLine}" +
                    $"Actual:   {string.Join(", ", actualFileNames)}");
        }

        Assert.Equal(expected, actualFileNames);
    }

    public virtual string FindBinFrameworkDir(string config, bool forPublish, string framework, string? bundleDirName = null)
    {
        EnsureProjectDirIsSet();
        string basePath = Path.Combine(ProjectDir!, "bin", config, framework);
        if (forPublish)
            basePath = FindSubDirIgnoringCase(basePath, "publish");

        return Path.Combine(basePath, bundleDirName ?? this.BundleDirName, "_framework");
    }

    [MemberNotNull(nameof(ProjectDir))]
    protected void EnsureProjectDirIsSet()
    {
        if (string.IsNullOrEmpty(ProjectDir))
            throw new Exception($"{nameof(ProjectDir)} is not set");
    }
}
