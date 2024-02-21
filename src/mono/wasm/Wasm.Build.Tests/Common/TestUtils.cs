// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests;

public static class TestUtils
{
    public static void AssertFilesDontExist(string dir, string[] filenames, string? label = null)
        => AssertFilesExist(dir, filenames, label, expectToExist: false);

    public static void AssertFilesExist(string dir, IEnumerable<string> filenames, string? label = null, bool expectToExist = true)
    {
        string prefix = label != null ? $"{label}: " : string.Empty;
        if (!Directory.Exists(dir))
            throw new XunitException($"[{label}] {dir} directory not found");
        foreach (string filename in filenames)
        {
            string path = Path.IsPathRooted(filename) ? filename : Path.Combine(dir, filename);
            if (expectToExist && !File.Exists(path))
                throw new XunitException($"{prefix}Expected the file to exist: {path}");

            if (!expectToExist && File.Exists(path))
                throw new XunitException($"{prefix}Expected the file to *not* exist: {path}");
        }
    }

    public static void AssertSameFile(string file0, string file1, string? label = null) => AssertFile(file0, file1, label, same: true);
    public static void AssertNotSameFile(string file0, string file1, string? label = null) => AssertFile(file0, file1, label, same: false);

    public static void AssertFile(string file0, string file1, string? label = null, bool same = true)
    {
        string prefix = label != null ? $"{label}: " : string.Empty;
        Assert.True(File.Exists(file0), $"{prefix}Expected to find {file0}");
        Assert.True(File.Exists(file1), $"{prefix}Expected to find {file1}");

        FileInfo finfo0 = new(file0);
        FileInfo finfo1 = new(file1);

        bool areEqual = FilesAreEqual(finfo0, finfo1);

        if (same && !areEqual)
            throw new XunitException($"{label}:{Environment.NewLine}  File metadata don't match for {GetFileMetadata(finfo0)}), and {GetFileMetadata(finfo1)})");

        if (!same && areEqual)
            throw new XunitException($"{label}:{Environment.NewLine}  File metadata should not match for {GetFileMetadata(finfo0)}), and {GetFileMetadata(finfo1)})");
    }

    static bool FilesAreEqual(FileInfo fileInfo1, FileInfo fileInfo2) =>
        (fileInfo1.Length == fileInfo2.Length &&
        fileInfo1.CreationTime == fileInfo2.CreationTime);

    static string GetFileMetadata(FileInfo fileInfo) =>
        $"File Name: {fileInfo.Name}\n" +
        $"Size: {fileInfo.Length} bytes\n" +
        $"Last Write Time: {fileInfo.LastWriteTime}\n" +
        $"Creation Time: {fileInfo.CreationTime}\n";

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

    public static void AssertSubstring(string substring, string full, bool contains)
    {
        if (contains)
            Assert.Contains(substring, full);
        else
            Assert.DoesNotContain(substring, full);
    }

    public static void AssertMatches(string regex, string full, bool contains)
    {
        if (contains)
            Assert.Matches(regex, full);
        else
            Assert.DoesNotMatch(regex, full);
    }

    public static void AssertEqual(object expected, object actual, string label)
    {
        if (expected?.Equals(actual) == true)
            return;

        throw EqualException.ForMismatchedValues(
            expected, actual,
            $"[{label}]\n");
    }

    private static readonly char[] s_charsToReplace = new[] { '.', '-', '+' };
    public static string FixupSymbolName(string name)
    {
        UTF8Encoding utf8 = new();
        byte[] bytes = utf8.GetBytes(name);
        StringBuilder sb = new();

        foreach (byte b in bytes)
        {
            if ((b >= (byte)'0' && b <= (byte)'9') ||
                (b >= (byte)'a' && b <= (byte)'z') ||
                (b >= (byte)'A' && b <= (byte)'Z') ||
                (b == (byte)'_'))
            {
                sb.Append((char)b);
            }
            else if (s_charsToReplace.Contains((char)b))
            {
                sb.Append('_');
            }
            else
            {
                sb.Append($"_{b:X}_");
            }
        }

        return sb.ToString();
    }
}
