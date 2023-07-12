// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Wasm.Build.Tests;

public abstract class ProjectProviderBase(string projectDir, ITestOutputHelper _testOutput)
{
    protected const string s_dotnetVersionHashRegex = @"\.(?<version>.+)\.(?<hash>[a-zA-Z0-9]+)\.";
    private static string[] s_dotnetExtensionsToIgnore = new[]
    {
        ".gz",
        ".br",
        ".symbols"
    };

    public string ProjectDir { get; } = projectDir;

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
}
