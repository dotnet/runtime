// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System.Text;
using Xunit;

namespace System.IO.Compression.Tests;

public class ZipToolsTests : ZipFileTestBase
{
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is7zipAvailable))]
    public void DoNotMerge_CheckIf7zAvailable()
    {
        // Want to see if there's at least one machine that installs 7z
        Assert.Fail($"7zip is available in this machine: {PlatformDetection.Is7zipAvailable}");
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsZipAvailable))]
    public void DoNotMerge_CheckIfZipAvailable()
    {
        // Want to see if there's at least one machine that installs zip
        Assert.Fail($"zip is available in this machine: {PlatformDetection.IsZipAvailable}");
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsUnZipAvailable))]
    public void DoNotMerge_CheckIfUnzipAvailable()
    {
        // Want to see if there's at least one machine that installs unzip
        Assert.Fail($"unzip is available in this machine: {PlatformDetection.IsUnZipAvailable}");
    }

    // public static IEnumerable<object[]> Aapt2_Data()
    // {
    //     yield return new object[] { "packaged_resources", "AndroidManifest.xml", false }; // Originally created with aapt2
    //     yield return new object[] { "packaged_resources", "AndroidManifest.xml", true };
    //     yield return new object[] { "packaged_resources2", "assets/foo/bar.txt", false }; // Originally created with aapt2 and then updated with aapt2 link with extra files
    //     yield return new object[] { "packaged_resources2", "assets/foo/bar.txt", true };
    // }

    // [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is7zipAvailable))]
    // [MemberData(nameof(Aapt2_Data))]
    // public void ModifyAapt2ZipFileWithZipArchive_Then_TestAndExtractWith7zip(string testName, string entryName, bool addEntry)
    // {
    //     using TempDirectory root = new();

    //     string copiedAapt2ZipFile = ModifyAapt2ZipFileWithZipArchiveShared(root.Path, testName, entryNameToModify);

    //     CliZipTools cli = new();

    //     // This would fail if there's an issue verifying the zip with 7z
    //     cli.TestWith7Zip(copiedAapt2ZipFile);
    //     // This would fail if there's an issue extracting with 7z
    //     cli.ExtractWith7Zip(copiedAapt2ZipFile, Path.Combine(root.Path, "actualDirectory"));
    // }

    // [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is7zipAvailable))]
    // [MemberData(nameof(Aapt2_Data))]
    // public void ModifyAapt2ZipFileWithZipArchive_Then_TestAndExtractWithUnzip(string testName, string entryNameToModify, bool addEntry)
    // {
    //     using TempDirectory root = new();

    //     string copiedAapt2ZipFile = ModifyAapt2ZipFileWithZipArchiveShared(root.Path, testName, entryNameToModify);

    //     CliZipTools cli = new();

    //     // This would fail if there's an issue extracting with unzip
    //     cli.ExtractWithUnzip(copiedAapt2ZipFile, Path.Combine(root.Path, "actualDirectory"));
    // }

    // private string ModifyAapt2ZipFileWithZipArchiveShared(string tempDirectory, string testName, string entryNameToModify)
    // {
    //     string testZipFileName = $"{testName}.zip";
    //     string originalAapt2ZipFile = Path.Combine("ZipTestData", testZipFileName);
    //     string copiedAapt2ZipFile = Path.Combine(tempDirectory, testZipFileName);
    //     string expectedDirectory = Path.Combine("ZipTestData", testName);

    //     File.Copy(originalAapt2ZipFile, copiedAapt2ZipFile, true);

    //     // Use the logic for Update mode
    //     using (ZipArchive archive = ZipFile.Open(copiedAapt2ZipFile, ZipArchiveMode.Update))
    //     {
    //         // Adding a new entry should not cause a headers error when opening archive with 7zip
    //         if (addEntry)
    //         {
    //             archive.CreateEntry("newEntry.txt");
    //         }
    //         // Update an existing entry should not cause a CRC error when opening archive with 7zip
    //         else
    //         {
    //             ZipArchiveEntry entry = archive.GetEntry(entryNameToModify);
    //             entry.LastWriteTime = DateTimeOffset.UtcNow;
    //         }
    //     }

    //     return copiedAapt2ZipFile;
    // }
}