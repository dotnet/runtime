// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Wasm.Build.Tests;

public abstract class ProjectProviderBase(ITestOutputHelper _testOutput, string? _projectDir)
{
    public const string WebcilInWasmExtension = ".wasm";
    protected const string s_dotnetVersionHashRegex = @"\.(?<version>.+)\.(?<hash>[a-zA-Z0-9]+)\.";
    private static string[] s_dotnetExtensionsToIgnore = new[]
    {
        ".gz",
        ".br",
        ".symbols"
    };
    private const string s_runtimePackPathPattern = "\\*\\* MicrosoftNetCoreAppRuntimePackDir : '([^ ']*)'";
    private static Regex s_runtimePackPathRegex = new Regex(s_runtimePackPathPattern);

    public string? ProjectDir { get; set; } = _projectDir;
    protected ITestOutputHelper _testOutput = _testOutput;
    protected BuildEnvironment _buildEnv = BuildTestBase.s_buildEnv;

    public IReadOnlyDictionary<string, DotNetFileName> FindAndAssertDotnetFiles(
        string dir,
        bool isPublish,
        bool expectFingerprintOnDotnetJs,
        RuntimeVariant runtimeType)
    {
        return FindAndAssertDotnetFiles(dir: dir,
                                        expectFingerprintOnDotnetJs: expectFingerprintOnDotnetJs,
                                        superSet: GetAllKnownDotnetFilesToFingerprintMap(runtimeType),
                                        expected: GetDotNetFilesExpectedSet(runtimeType, isPublish));
    }

    protected abstract IReadOnlyDictionary<string, bool> GetAllKnownDotnetFilesToFingerprintMap(RuntimeVariant runtimeType);
    protected abstract IReadOnlySet<string> GetDotNetFilesExpectedSet(RuntimeVariant runtimeType, bool isPublish);

    public IReadOnlyDictionary<string, DotNetFileName> FindAndAssertDotnetFiles(
        string dir,
        bool expectFingerprintOnDotnetJs,
        IReadOnlyDictionary<string, bool> superSet,
        IReadOnlySet<string>? expected)
    {
        EnsureProjectDirIsSet();
        var actual = new SortedDictionary<string, DotNetFileName>();

        IList<string> dotnetFiles = Directory.EnumerateFiles(dir,
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
                    _testOutput.WriteLine($"Comparing {expectedFilename} with {actualFile}, expectFingerprintOnDotnetJs: {expectFingerprintOnDotnetJs}, expectFingerprint: {expectFingerprint}");
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

        _testOutput.WriteLine($"Accepted count: {actual.Count}");
        foreach (var kvp in actual)
            _testOutput.WriteLine($"Accepted: \t[{kvp.Key}] = {kvp.Value}");

        if (dotnetFiles.Any())
        {
            throw new XunitException($"Found unknown files in {dir}:{Environment.NewLine}    {string.Join($"{Environment.NewLine}  ", dotnetFiles)}");
        }

        if (expected is not null)
            AssertDotNetFilesSet(expected, superSet, actual, expectFingerprintOnDotnetJs);
        return actual;
    }

    public void AssertDotNetFilesSet(
        IReadOnlySet<string> expected,
        IReadOnlyDictionary<string, bool> superSet,
        IDictionary<string, DotNetFileName> actual,
        bool expectFingerprintOnDotnetJs)
    {
        EnsureProjectDirIsSet();
        foreach (string expectedFilename in expected)
        {
            bool expectFingerprint = superSet[expectedFilename];

            Assert.True(actual.ContainsKey(expectedFilename), $"Could not find {expectedFilename} in {string.Join(", ", actual.Keys)}");

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
        }

        if (expected.Count < actual.Count)
        {
            StringBuilder sb = new();
            sb.AppendLine($"Expected: {string.Join(", ", expected)}");
            // FIXME: show the difference in a better way
            sb.AppendLine($"Actual: {string.Join(", ", actual.Values.Select(a => a.ActualPath).Order())}");
            throw new XunitException($"Expected and actual file sets do not match.{Environment.NewLine}{sb}");
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

    public static bool ShouldCheckFingerprint(string expectedFilename, bool expectFingerprintOnDotnetJs, bool expectFingerprintForThisFile) =>
        (expectedFilename == "dotnet.js" && expectFingerprintOnDotnetJs) || expectFingerprintForThisFile;

    public static void AssertRuntimePackPath(string buildOutput, string targetFramework)
    {
        var match = s_runtimePackPathRegex.Match(buildOutput);
        if (!match.Success || match.Groups.Count != 2)
            throw new XunitException($"Could not find the pattern in the build output: '{s_runtimePackPathPattern}'.{Environment.NewLine}Build output: {buildOutput}");

        string expectedRuntimePackDir = BuildTestBase.s_buildEnv.GetRuntimePackDir(targetFramework);
        string actualPath = match.Groups[1].Value;
        if (string.Compare(actualPath, expectedRuntimePackDir) != 0)
            throw new XunitException($"Runtime pack path doesn't match.{Environment.NewLine}Expected: '{expectedRuntimePackDir}'{Environment.NewLine}Actual:   '{actualPath}'");
    }

    public static void AssertDotNetJsSymbols(string bundleDir, bool fromRuntimePack, string targetFramework)
        => TestUtils.AssertFile(Path.Combine(BuildTestBase.s_buildEnv.GetRuntimeNativeDir(targetFramework), "dotnet.native.js.symbols"),
                        Path.Combine(bundleDir, "_framework/dotnet.native.js.symbols"),
                        same: fromRuntimePack);

    [MemberNotNull(nameof(ProjectDir))]
    protected void EnsureProjectDirIsSet()
    {
        if (string.IsNullOrEmpty(ProjectDir))
            throw new Exception($"{nameof(ProjectDir)} is not set");
    }
}
