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

public abstract class ProjectProviderBase(string projectDir, ITestOutputHelper output)
{
    protected const string s_dotnetVersionHashRegex = @"\.(?<version>.+)\.(?<hash>[a-zA-Z0-9]+)\.";

    public ITestOutputHelper TestOutput { get; init; } = output;
    public string ProjectDir { get; init; } = projectDir;

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
