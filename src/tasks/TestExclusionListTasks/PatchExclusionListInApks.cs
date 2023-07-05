// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
namespace TestExclusionListTasks;

public class PatchExclusionListInApks : Task
{
    [Required]
    public ITaskItem[]? ApkPaths { get; set; }

    [Required]
    public ITaskItem[]? ExcludedTests { get; set; }

    public string? AndroidSdk { get; set; }

    public string? MinApiLevel { get; set; }

    public string? BuildToolsVersion { get; set; }

    public string? KeyStorePath { get; set; }

    public override bool Execute()
    {
        var apkBuilder = new ApkBuilder(Log);
        apkBuilder.AndroidSdk = AndroidSdk;
        apkBuilder.MinApiLevel = MinApiLevel;
        apkBuilder.BuildToolsVersion = BuildToolsVersion;
        apkBuilder.KeyStorePath = KeyStorePath;

        string testExclusionList = string.Join(
            '\n',
            (ExcludedTests ?? Enumerable.Empty<ITaskItem>()).Select(t => t.ItemSpec));
        foreach (ITaskItem apk in ApkPaths ?? Enumerable.Empty<ITaskItem>())
        {
            string apkPath = apk.GetMetadata("FullPath")!;
            apkBuilder.OutputDir = Path.GetDirectoryName(apkPath)!;
            using (ZipArchive apkArchive = ZipFile.Open(apkPath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry assetsZipEntry = apkArchive.GetEntry("assets/assets.zip")!;
                using ZipArchive assetsArchive = new ZipArchive(assetsZipEntry.Open(), ZipArchiveMode.Update);
                ZipArchiveEntry testExclusionListEntry = assetsArchive.GetEntry("TestExclusionList.txt")!;
                using StreamWriter textExclusionListWriter = new StreamWriter(testExclusionListEntry.Open());
                textExclusionListWriter.WriteLine(testExclusionList);
            }
            apkBuilder.ZipAndSignApk(apkPath);
        }
        return true;
    }
}
