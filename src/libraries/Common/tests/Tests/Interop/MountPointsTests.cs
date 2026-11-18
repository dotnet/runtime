// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Common.Tests;

public class MountPointsTests : FileCleanupTestBase
{
    public static IEnumerable<object?[]> GetAllMountPointsData()
    {
        const string MountInfo = "36 35 0:42 / /mnt/USB\\040Disk rw - vfat /dev/block/vold/public:8,1 rw\n";

        foreach ((bool isLinux, bool isAndroid) in new[] { (true, false), (false, true) })
        {
            foreach ((string encoded, string decoded) in new[]
            {
                ("/data", "/data"),
                ("/mnt/USB\\040Disk", "/mnt/USB Disk"),
                ("/mnt/USB\\011Disk", "/mnt/USB\tDisk"),
                ("/mnt/USB\\012Disk", "/mnt/USB\nDisk"),
                ("/mnt/USB\\134Disk", "/mnt/USB\\Disk"),
                ("/mnt/USB\\134040Disk", "/mnt/USB\\040Disk"),
                ("/mnt/" + new string('a', 9000) + "\\040Disk", "/mnt/" + new string('a', 9000) + " Disk")
            })
            {
                yield return
                [
                    isLinux,
                    isAndroid,
                    $"36 35 0:42 /root\\040directory {encoded} rw - vfat /dev/block/vold/public:8,1 rw\n",
                    new[] { decoded }
                ];
            }

            yield return
            [
                isLinux,
                isAndroid,
                "36 35 0:42 /root\\040one /mnt/USB\\040Disk rw - vfat /dev/one rw\n" +
                "37 35 0:42 /root\\040two /mnt/USB\\040Disk rw - vfat /dev/two rw\n" +
                "38 35 0:43 / /storage/3439-3532 rw - vfat /dev/three rw\n",
                new[] { "/mnt/USB Disk", "/mnt/USB Disk", "/storage/3439-3532" }
            ];
            yield return [isLinux, isAndroid, "", Array.Empty<string>()];
            yield return [isLinux, isAndroid, null, null];
        }

        yield return [false, false, MountInfo, null];
        yield return [false, false, null, null];
    }

    [Theory]
    [MemberData(nameof(GetAllMountPointsData))]
    public void GetAllMountPoints_UsesExpectedSource(
        bool isLinux, bool isAndroid, string? mountInfo, string[]? expectedMountPoints)
    {
        string path = GetTestFilePath();
        if (mountInfo is not null)
        {
            File.WriteAllText(path, mountInfo);
        }

        if (expectedMountPoints is null)
        {
            Assert.Throws<MountPointsTestHelper.NativeFallbackException>(
                () => MountPointsTestHelper.GetAllMountPoints(isLinux, isAndroid, path));
        }
        else
        {
            Assert.Equal(expectedMountPoints, MountPointsTestHelper.GetAllMountPoints(isLinux, isAndroid, path));
        }
    }
}
