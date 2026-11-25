// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Common.Tests
{
    public class procfsTests : FileCleanupTestBase
    {
        [Theory]
        [InlineData("1 (systemd) S 0 1 1 0 -1 4194560 11536 2160404 55 593 70 169 4213 1622 20 0 1 0 4 189767680 1491 18446744073709551615 1 1 0 0 0 0 671173123 4096 1260 0 0 0 17 4 0 0 25 0 0 0 0 0 0 0 0 0 0", 1, "systemd", 'S', 1, 70, 169, 0, 4, 189767680, 1491, 18446744073709551615)]
        [InlineData("5955 (dotnet with space) S 1806 5955 5955 34823 5955 4194304 1426 5872 0 3 16 3 16 4 20 0 1 0 674762 32677888 1447 18446744073709551615 4194304 5192652 140725672538992 140725672534152 140236068968880 0 0 3670020 1266777851 1 0 0 17 4 0 0 0 0 0 7290352 7326856 21204992 140725672540419 140725672540424 140725672540424 140725672542190 0", 5955, "dotnet with space", 'S', 5955, 16, 3, 0, 674762, 32677888, 1447, 18446744073709551615)]
        [InlineData("5955 (bash) S 1806 5955 5955 34823 5955 4194304 1426 5872 0 3 16 3 16 4 20 0 1 0 674762 32677888 1447 18446744073709551615 4194304 5192652 140725672538992 140725672534152 140236068968880 0 0 3670020 1266777851 1 0 0 17 4 0 0 0 0 0 7290352 7326856 21204992 140725672540419 140725672540424 140725672540424 140725672542190 0", 5955, "bash", 'S', 5955, 16, 3, 0, 674762, 32677888, 1447, 18446744073709551615)]
        [InlineData("22831 (dotnet) S 22708 22613 1811 34822 22613 4194304 215237 0 0 0 6491 4634 0 0 20 0 18 0 6054850 3672928256 23593 18446744073709551615 4194304 4294944 140729667338512 140729667328848 139709719439277 0 4 4096 17630 0 0 0 17 4 0 0 0 0 0 6392096 6393592 15552512 140729667342465 140729667342553 140729667342553 140729667346408 0", 22831, "dotnet", 'S', 1811, 6491, 4634, 0, 6054850, 3672928256, 23593, 18446744073709551615)]
        [InlineData("22719 (fsnotifier64) S 22671 22613 1811 34822 22613 4194304 135 0 0 0 28 541 0 0 20 0 1 0 6052788 4882432 498 18446744073709551615 4194304 4212092 140731745592928 140731745592312 139627376925939 0 4 4096 0 1 0 0 17 3 0 0 0 0 0 6311936 6312884 25800704 140731745595967 140731745596016 140731745596016 140731745599431 0", 22719, "fsnotifier64", 'S', 1811, 28, 541, 0, 6052788, 4882432, 498, 18446744073709551615)]
        [InlineData("1512 (at-spi-bus-laun) S 1163 1295 1295 0 -1 4194304 377 0 1 0 0 0 0 0 20 0 4 0 967 345894912 1453 18446744073709551615 4194304 4210132 140732353904128 140732353903600 139935649062541 0 0 4096 81920 0 0 0 17 5 0 0 0 0 0 6310768 6311952 8675328 140732353906996 140732353907038 140732353907038 140732353908686 0", 1512, "at-spi-bus-laun", 'S', 1295, 0, 0, 0, 967, 345894912, 1453, 18446744073709551615)]
        [InlineData("15 (kworker/1:0H) S 2 0 0 0 -1 69238880 0 0 0 0 0 0 0 0 0 -20 1 0 4 0 0 18446744073709551615 0 0 0 0 0 0 0 2147483647 0 0 0 0 17 1 0 0 0 0 0 0 0 0 0 0 0 0 0", 15, "kworker/1:0H", 'S', 0, 0, 0, -20, 4, 0, 0, 18446744073709551615)]
        [InlineData("1369 (sh) S 1163 1369 1369 0 -1 4194304 240 1291 0 0 0 0 0 0 20 0 1 0 902 4616192 419 18446744073709551615 93922724966400 93922725110300 140735239002352 140735239000952 140586773169914 0 0 0 65538 1 0 0 17 4 0 0 0 0 0 93922727210856 93922727215648 93922759745536 140735239006661 140735239006720 140735239006720 140735239008240 0", 1369, "sh", 'S', 1369, 0, 0, 0, 902, 4616192, 419, 18446744073709551615)]
        [InlineData("1295 (dbus-daemon) S 1163 1295 1295 0 -1 4194368 1909 460 0 0 21 35 0 0 20 0 1 0 794 44421120 987 18446744073709551615 94692716101632 94692716315676 140720853604656 140720853602536 140124915441971 0 0 0 16385 1 0 0 17 5 0 0 0 0 0 94692718415176 94692718420816 94692735217664 140720853613214 140720853613288 140720853613288 140720853614563 0", 1295, "dbus-daemon", 'S', 1295, 21, 35, 0, 794, 44421120, 987, 18446744073709551615)]
        [InlineData("1291 (upstart-udev-br) S 1163 1290 1290 0 -1 4194368 59 0 0 0 0 0 0 0 20 0 1 0 792 41316352 71 18446744073709551615 94876928040960 94876928122852 140734109368976 140734109368152 140299085954291 0 0 1 81920 1 0 0 17 4 0 0 0 0 0 94876930220584 94876930224400 94876938022912 140734109371165 140734109371201 140734109371201 140734109372382 0", 1291, "upstart-udev-br", 'S', 1290, 0, 0, 0, 792, 41316352, 71, 18446744073709551615)]
        [InlineData("123 (deferwq) S 2 0 0 0 -1 69238880 0 0 0 0 0 0 0 0 0 -20 1 0 90 0 0 18446744073709551615 0 0 0 0 0 0 0 2147483647 0 0 0 0 17 5 0 0 0 0 0 0 0 0 0 0 0 0 0", 123, "deferwq", 'S', 0, 0, 0, -20, 90, 0, 0, 18446744073709551615)]
        [InlineData("1160 ((sd-pam)) S 1158 1158 1158 0 -1 1077936448 28 0 0 0 0 0 0 0 20 0 1 0 758 64921600 510 18446744073709551615 1 1 0 0 0 0 0 4096 0 0 0 0 17 1 0 0 0 0 0 0 0 0 0 0 0 0 0", 1160, "(sd-pam)", 'S', 1158, 0, 0, 0, 758, 64921600, 510, 18446744073709551615)]
        [InlineData("11 (watchdog/1) S 2 0 0 0 -1 69239104 0 0 0 0 0 324 0 0 -100 0 1 0 4 0 0 18446744073709551615 0 0 0 0 0 0 0 2147483647 0 0 0 0 17 1 99 1 0 0 0 0 0 0 0 0 0 0 0", 11, "watchdog/1", 'S', 0, 0, 324, 0, 4, 0, 0, 18446744073709551615)]
        [InlineData("109 (ipv6_addrconf) S 2 0 0 0 -1 69238880 0 0 0 0 0 0 0 0 0 -20 1 0 89 0 0 18446744073709551615 0 0 0 0 0 0 0 2147483647 0 0 0 0 17 1 0 0 0 0 0 0 0 0 0 0 0 0 0", 109, "ipv6_addrconf", 'S', 0, 0, 0, -20, 89, 0, 0, 18446744073709551615)]
        [InlineData("102 (scsi_tmf_0) S 2 0 0 0 -1 69238880 0 0 0 0 0 0 0 0 0 -20 1 0 83 0 0 18446744073709551615 0 0 0 0 0 0 0 2147483647 0 0 0 0 17 1 0 0 0 0 0 0 0 0 0 0 0 0 0", 102, "scsi_tmf_0", 'S', 0, 0, 0, -20, 83, 0, 0, 18446744073709551615)]
        [InlineData("10 (watchdog/0) S 2 0 0 0 -1 69239104 0 0 0 0 0 243 0 0 -100 0 1 0 4 0 0 18446744073709551615 0 0 0 0 0 0 0 2147483647 0 0 0 0 17 0 99 1 0 0 0 0 0 0 0 0 0 0 0", 10, "watchdog/0", 'S', 0, 0, 243, 0, 4, 0, 0, 18446744073709551615)]
        [InlineData("5955 (dotnet (nested) space) S 1806 5955 5955 34823 5955 4194304 1426 5872 0 3 16 3 16 4 20 0 1 0 674762 32677888 1447 18446744073709551615 4194304 5192652 140725672538992 140725672534152 140236068968880 0 0 3670020 1266777851 1 0 0 17 4 0 0 0 0 0 7290352 7326856 21204992 140725672540419 140725672540424 140725672540424 140725672542190 0", 5955, "dotnet (nested) space", 'S', 5955, 16, 3, 0, 674762, 32677888, 1447, 18446744073709551615)]
        [InlineData("5955 (dotnet (with (parens)) sp(a)ce) S 1806 5955 5955 34823 5955 4194304 1426 5872 0 3 16 3 16 4 20 0 1 0 674762 32677888 1447 18446744073709551615 4194304 5192652 140725672538992 140725672534152 140236068968880 0 0 3670020 1266777851 1 0 0 17 4 0 0 0 0 0 7290352 7326856 21204992 140725672540419 140725672540424 140725672540424 140725672542190 0", 5955, "dotnet (with (parens)) sp(a)ce", 'S', 5955, 16, 3, 0, 674762, 32677888, 1447, 18446744073709551615)]
        [InlineData("5955 (executable-(name)) S 1806 5955 5955 34823 5955 4194304 1426 5872 0 3 16 3 16 4 20 0 1 0 674762 32677888 1447 18446744073709551615 4194304 5192652 140725672538992 140725672534152 140236068968880 0 0 3670020 1266777851 1 0 0 17 4 0 0 0 0 0 7290352 7326856 21204992 140725672540419 140725672540424 140725672540424 140725672542190 0", 5955, "executable-(name)", 'S', 5955, 16, 3, 0, 674762, 32677888, 1447, 18446744073709551615)]
        [InlineData("5955 (a)))b) S 1806 5955 5955 34823 5955 4194304 1426 5872 0 3 16 3 16 4 20 0 1 0 674762 32677888 1447 18446744073709551615 4194304 5192652 140725672538992 140725672534152 140236068968880 0 0 3670020 1266777851 1 0 0 17 4 0 0 0 0 0 7290352 7326856 21204992 140725672540419 140725672540424 140725672540424 140725672542190 0", 5955, "a)))b", 'S', 5955, 16, 3, 0, 674762, 32677888, 1447, 18446744073709551615)]
        [InlineData("5955 (a(((b) S 1806 5955 5955 34823 5955 4194304 1426 5872 0 3 16 3 16 4 20 0 1 0 674762 32677888 1447 18446744073709551615 4194304 5192652 140725672538992 140725672534152 140236068968880 0 0 3670020 1266777851 1 0 0 17 4 0 0 0 0 0 7290352 7326856 21204992 140725672540419 140725672540424 140725672540424 140725672542190 0", 5955, "a(((b", 'S', 5955, 16, 3, 0, 674762, 32677888, 1447, 18446744073709551615)]
        [InlineData("5955 (a)( ) b (() () S 1806 5955 5955 34823 5955 4194304 1426 5872 0 3 16 3 16 4 20 0 1 0 674762 32677888 1447 18446744073709551615 4194304 5192652 140725672538992 140725672534152 140236068968880 0 0 3670020 1266777851 1 0 0 17 4 0 0 0 0 0 7290352 7326856 21204992 140725672540419 140725672540424 140725672540424 140725672542190 0", 5955, "a)( ) b (() (", 'S', 5955, 16, 3, 0, 674762, 32677888, 1447, 18446744073709551615)]
        [InlineData("5955 (has\\backslash) S 1806 5955 5955 34823 5955 4194304 1426 5872 0 3 16 3 16 4 20 0 1 0 674762 32677888 1447 18446744073709551615 4194304 5192652 140725672538992 140725672534152 140236068968880 0 0 3670020 1266777851 1 0 0 17 4 0 0 0 0 0 7290352 7326856 21204992 140725672540419 140725672540424 140725672540424 140725672542190 0", 5955, "has\\backslash", 'S', 5955, 16, 3, 0, 674762, 32677888, 1447, 18446744073709551615)]
        public void ParseValidStatFiles_Success(
            string statFileText,
            int expectedPid, string expectedComm, char expectedState, int expectedSession,
            ulong expectedUtime, ulong expectedStime, long expectedNice, ulong expectedStarttime,
            ulong expectedVsize, long expectedRss, ulong expectedRsslim)
        {
            string path = GetTestFilePath();
            File.WriteAllText(path, statFileText);

            Interop.procfs.ParsedStat result;
            Assert.True(Interop.procfs.TryParseStatFile(path, out result));

            Assert.Equal(expectedPid, result.pid);
            Assert.Equal(expectedComm, result.comm);
            Assert.Equal(expectedState, result.state);
            Assert.Equal(expectedSession, result.session);
            Assert.Equal(expectedUtime, result.utime);
            Assert.Equal(expectedStime, result.stime);
            Assert.Equal(expectedNice, result.nice);
            Assert.Equal(expectedStarttime, result.starttime);
            Assert.Equal(expectedVsize, result.vsize);
            Assert.Equal(expectedRss, result.rss);
            Assert.Equal(expectedRsslim, result.rsslim);
        }

        [Theory]
        [InlineData("37 79 0:6 / /dev rw,nosuid shared:2 - devtmpfs devtmpfs rw,seclabel,size=4096k,nr_inodes=4070495,mode=755,inode64", "/", "/dev", "devtmpfs", "rw,seclabel,size=4096k,nr_inodes=4070495,mode=755,inode64")]
        [InlineData("42 40 0:28 / /sys/fs/cgroup rw,nosuid,nodev,noexec,relatime shared:7 - cgroup2 cgroup2 rw,seclabel,nsdelegate,memory_recursiveprot", "/", "/sys/fs/cgroup", "cgroup2", "rw,seclabel,nsdelegate,memory_recursiveprot")]
        [InlineData("34 28 0:28 / /sys/fs/cgroup/cpu,cpuacct rw,nosuid,nodev,noexec,relatime shared:16 - cgroup cgroup rw,cpu,cpuacct", "/", "/sys/fs/cgroup/cpu,cpuacct", "cgroup", "rw,cpu,cpuacct")]
        [InlineData("396 394 0:21 /kubepods/besteffort/pod9a18ffb8-8513-11e7-b26e-7e29fbe2a5a3/d28e0087cf8f3f0429f755d60b0de415b20fcf76736ded7bab6e30e7b739ee36 /sys/fs/cgroup/cpu ro,nosuid,nodev,noexec,relatime - cgroup cgroup rw,cpu", "/kubepods/besteffort/pod9a18ffb8-8513-11e7-b26e-7e29fbe2a5a3/d28e0087cf8f3f0429f755d60b0de415b20fcf76736ded7bab6e30e7b739ee36", "/sys/fs/cgroup/cpu", "cgroup", "rw,cpu")]
        [InlineData("55 79 259:2 / /boot rw,relatime shared:111 - ext4 /dev/nvme0n1p2 rw,seclabel", "/", "/boot", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 /root\\040path /mnt rw,relatime - ext4 /dev/root rw,seclabel", "/root path", "/mnt", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 / /run/media/user/DISC\\0403 ro,relatime - iso9660 /dev/sr0 ro", "/", "/run/media/user/DISC 3", "iso9660", "ro")]
        [InlineData("36 35 98:0 /root\\011path /mnt rw,relatime - ext4 /dev/root rw,seclabel", "/root\tpath", "/mnt", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 / /mnt\\011path rw,relatime - ext4 /dev/root rw,seclabel", "/", "/mnt\tpath", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 /root\\012path /mnt rw,relatime - ext4 /dev/root rw,seclabel", "/root\npath", "/mnt", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 / /mnt\\012path rw,relatime - ext4 /dev/root rw,seclabel", "/", "/mnt\npath", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 /root\\134path /mnt rw,relatime - ext4 /dev/root rw,seclabel", "/root\\path", "/mnt", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 / /mnt\\134path rw,relatime - ext4 /dev/root rw,seclabel", "/", "/mnt\\path", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 /root\\040\\011\\012\\134path /mnt\\040\\011\\012\\134path rw,relatime - ext4 /dev/root rw,label=\\040\\134", "/root \t\n\\path", "/mnt \t\n\\path", "ext4", "rw,label=\\040\\134")]
        [InlineData("36 35 98:0 /root\\040with\\040spaces /mnt\\040with\\040spaces rw,relatime shared:1 master:2 - ext4 /dev/root rw,seclabel", "/root with spaces", "/mnt with spaces", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 /root\\134040 /mnt\\134040 rw,relatime - ext4 /dev/root rw,seclabel", "/root\\040", "/mnt\\040", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 /root\\134011\\134012\\134134 /mnt\\134011\\134012\\134134 rw,relatime - ext4 /dev/root rw,seclabel", "/root\\011\\012\\134", "/mnt\\011\\012\\134", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 /root\\040-\\040path /mnt\\040-\\040path rw,relatime - ext4 /dev/root rw,seclabel", "/root - path", "/mnt - path", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 /root\\134\\040 /mnt\\040\\134 rw,relatime - ext4 /dev/root rw,seclabel", "/root\\ ", "/mnt \\", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 /root\\04 /mnt\\041 rw,relatime - ext4 /dev/root rw,seclabel", "/root\\04", "/mnt\\041", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 /root\\134\\134 /mnt\\134\\134 rw,relatime - ext4 /dev/root rw,seclabel", "/root\\\\", "/mnt\\\\", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 /root\\041\\040path /mnt\\x\\011path rw,relatime - ext4 /dev/root rw,seclabel", "/root\\041 path", "/mnt\\x\tpath", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 /root\\040path\\0 /mnt\\011path\\ rw,relatime - ext4 /dev/root rw,seclabel", "/root path\\0", "/mnt\tpath\\", "ext4", "rw,seclabel")]
        [InlineData("36 35 98:0 /r\u00E9sum\u00E9\\040path /mnt\uD83D\uDE00\\011path rw,relatime - ext4 /dev/root rw,seclabel", "/r\u00E9sum\u00E9 path", "/mnt\uD83D\uDE00\tpath", "ext4", "rw,seclabel")]
        public void ParseMountInfoLine_Success(
            string line,
            string expectedRoot,
            string expectedMountPoint,
            string expectedFileSystemType,
            string expectedSuperOptions)
        {
            Interop.procfs.ParsedMount result;
            Assert.True(Interop.procfs.TryParseMountInfoLine(line, out result));

            Assert.Equal(expectedRoot, result.Root);
            Assert.Equal(expectedMountPoint, result.MountPoint);
            Assert.Equal(expectedFileSystemType, result.FileSystemType);
            Assert.Equal(expectedSuperOptions, result.SuperOptions);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(248)]
        [InlineData(249)]
        [InlineData(250)]
        [InlineData(251)]
        [InlineData(4096)]
        public void ParseMountInfoLine_EscapedPaths_VaryingLengths(int length)
        {
            string prefix = "/" + new string('a', length);
            string line = $"36 35 98:0 {prefix}\\040x {prefix}\\134040 rw,relatime - ext4 /dev/root rw,seclabel";

            Assert.True(Interop.procfs.TryParseMountInfoLine(line, out Interop.procfs.ParsedMount result));

            Assert.Equal(prefix + " x", result.Root);
            Assert.Equal(prefix + "\\040", result.MountPoint);
        }

        [Fact]
        public void ParseMountInfoLine_UnescapedPathsReferenceInput()
        {
            string line = "55 79 259:2 /root /boot rw,relatime shared:111 - ext4 /dev/nvme0n1p2 rw,seclabel";

            Assert.True(Interop.procfs.TryParseMountInfoLine(line, out Interop.procfs.ParsedMount result));

            Assert.True(line.AsSpan().Overlaps(result.MountId));
            Assert.True(line.AsSpan().Overlaps(result.ParentId));
            Assert.True(line.AsSpan().Overlaps(result.Root));
            Assert.True(line.AsSpan().Overlaps(result.MountPoint));
        }

        [Theory]
        [InlineData("36", "35")]
        [InlineData("4294967295", "0")]
        [InlineData("-1", "35")]
        [InlineData("36", "-1")]
        [InlineData("x", "35")]
        [InlineData("36", "x")]
        [InlineData("4294967296", "35")]
        [InlineData("36", "4294967296")]
        [InlineData("18446744073709551615", "18446744073709551614")]
        [InlineData("18446744073709551616", "0")]
        [InlineData("", "35")]
        [InlineData("36", "")]
        public void ParseMountInfoLine_PreservesMountIdentifiers(string mountId, string parentId)
        {
            string line = $"{mountId} {parentId} 8:1 / / rw - ext4 /dev/root rw";

            Assert.True(Interop.procfs.TryParseMountInfoLine(line, out Interop.procfs.ParsedMount result));
            Assert.Equal(mountId, result.MountId);
            Assert.Equal(parentId, result.ParentId);
        }

        public static IEnumerable<object?[]> FileSystemTypeForPath_TestData()
        {
            const string RootMount = "1 1 8:1 / / rw - ext4 /dev/root rw";

            yield return ["/", Array.Empty<string>(), null];
            yield return ["/", new[] { RootMount }, "ext4"];
            yield return ["/home/file", new[] { RootMount }, "ext4"];

            string[] mounts =
            [
                RootMount,
                "20 1 0:20 / /mnt rw - tmpfs tmpfs rw",
                "3 20 11:0 / /mnt/child ro - iso9660 /dev/sr0 ro"
            ];
            yield return ["/mnt", mounts, "tmpfs"];
            yield return ["/mnt/child", mounts, "iso9660"];
            yield return ["/mnt/child/file", mounts, "iso9660"];
            yield return ["/mnt/childish", mounts, "tmpfs"];
            yield return ["/mnt2/file", mounts, "ext4"];

            mounts =
            [
                RootMount,
                "200 1 0:20 / /media/disc rw - autofs systemd-1 rw",
                "3 200 11:0 / /media/disc ro - iso9660 /dev/sr0 ro"
            ];
            yield return ["/media/disc", mounts, "iso9660"];
            yield return ["/media/disc/file", mounts, "iso9660"];
            string[] stackedMounts = [.. mounts, "150 3 0:21 / /media/disc rw - nfs server:/share rw"];
            yield return ["/media/disc", stackedMounts, "nfs"];
            yield return ["/media/disc/file", stackedMounts, "nfs"];

            mounts =
            [
                RootMount,
                "25 1 0:4 / /proc rw - proc proc rw",
                "39 25 0:20 / /proc/sys/fs/binfmt_misc rw - autofs systemd-1 rw",
                "102 39 0:21 / /proc/sys/fs/binfmt_misc rw - binfmt_misc binfmt_misc rw"
            ];
            yield return ["/proc/sys/fs/binfmt_misc", mounts, "binfmt_misc"];
            yield return ["/proc/sys/fs/binfmt_misc/status", mounts, "binfmt_misc"];

            mounts =
            [
                RootMount,
                "200 1 0:20 / /sys/fs/cgroup rw - tmpfs tmpfs rw",
                "3 200 0:21 / /sys/fs/cgroup rw - cgroup2 cgroup2 rw"
            ];
            yield return ["/sys/fs/cgroup", mounts, "cgroup2"];
            yield return ["/sys/fs/cgroup/memory.max", mounts, "cgroup2"];

            mounts =
            [
                RootMount,
                "200 1 8:2 / /mnt rw - ext4 /dev/lower rw",
                "30 200 0:21 / /mnt/child rw - nfs server:/share rw",
                "3 200 0:22 / /mnt rw - tmpfs tmpfs rw"
            ];
            yield return ["/mnt", mounts, "tmpfs"];
            yield return ["/mnt/child", mounts, "tmpfs"];
            yield return ["/mnt/child/file", mounts, "tmpfs"];
            string[] visibleChildMounts = [.. mounts, "4 3 11:0 / /mnt/child ro - iso9660 /dev/sr0 ro"];
            yield return ["/mnt/child/file", visibleChildMounts, "iso9660"];

            mounts =
            [
                "200 999 0:1 / / rw - rootfs rootfs rw",
                "30 200 8:1 / /hidden rw - ext4 /dev/lower rw",
                "3 200 0:2 / / rw - overlay overlay rw",
                "4 3 0:3 / /data rw - tmpfs tmpfs rw"
            ];
            yield return ["/", mounts, "overlay"];
            yield return ["/hidden/file", mounts, "overlay"];
            yield return ["/data/file", mounts, "tmpfs"];
            yield return ["/", new[] { RootMount, "3 1 0:2 / / rw - overlay overlay rw" }, "overlay"];

            mounts =
            [
                "200 999 8:2 / /mnt rw - exfat /dev/lower rw",
                "3 200 0:21 / /mnt rw - cifs //server/share rw"
            ];
            yield return ["/mnt", mounts, "cifs"];
            yield return ["/mnt/file", mounts, "cifs"];
            yield return ["/mnt2", mounts, null];

            mounts =
            [
                RootMount,
                "200 1 0:20 / /mnt/disc\\040space rw - autofs systemd-1 rw",
                "3 200 11:0 / /mnt/disc\\040space ro - iso9660 /dev/sr0 ro"
            ];
            yield return ["/mnt/disc space", mounts, "iso9660"];
            yield return ["/mnt/disc space/file", mounts, "iso9660"];
            yield return ["/mnt/disc", mounts, "ext4"];

            mounts =
            [
                RootMount,
                "3 1 0:2 / /mnt/a\\134b rw - tmpfs tmpfs rw",
                "4 1 0:3 / /mnt/r\u00E9sum\u00E9 rw - exfat /dev/data rw",
                "5 1 0:4 / /etc/hosts rw - tmpfs tmpfs rw"
            ];
            yield return ["/mnt/a\\b/file", mounts, "tmpfs"];
            yield return ["/mnt/r\u00E9sum\u00E9/file", mounts, "exfat"];
            yield return ["/etc/hosts", mounts, "tmpfs"];
            yield return ["/MNT/a\\b/file", mounts, "ext4"];

            yield return ["/mnt/file", new[] { RootMount, "invalid", "x 1 0:2 / /mnt rw - tmpfs tmpfs rw" }, "ext4"];
            yield return ["/mnt/file", new[] { "2 3 0:2 / /mnt rw - ext4 /dev/lower rw", "3 2 0:3 / /mnt rw - tmpfs tmpfs rw" }, null];

            mounts =
            [
                RootMount,
                "4294967296 1 0:20 / /mnt rw - autofs systemd-1 rw",
                "18446744073709551615 4294967296 11:0 / /mnt ro - iso9660 /dev/sr0 ro"
            ];
            yield return ["/mnt", mounts, "iso9660"];
            yield return ["/mnt/file", mounts, "iso9660"];

            mounts =
            [
                "1 1 0:1 / / rw - rootfs root rw",
                "2 1 0:2 / /mnt rw - ext4 root rw",
                "3 2 0:3 / /mnt/sub rw - tmpfs tmpfs rw",
                "4 1 0:4 / /other rw - nfs server rw"
            ];
            yield return ["/", mounts, "rootfs"];
            yield return ["/file", mounts, "rootfs"];
            yield return ["/mnt", mounts, "ext4"];
            yield return ["/mnt/file", mounts, "ext4"];
            yield return ["/mnt2", mounts, "rootfs"];
            yield return ["/mnt/sub", mounts, "tmpfs"];
            yield return ["/mnt/sub/file", mounts, "tmpfs"];
            yield return ["/mnt/subway", mounts, "ext4"];
            yield return ["/Mnt/sub", mounts, "rootfs"];

            mounts =
            [
                "1 99 0:1 / / rw - rootfs root rw",
                "5 1 0:2 / / rw - overlay overlay rw",
                "6 5 0:3 / /mnt\\040path rw - autofs systemd rw",
                "2 6 0:4 / /mnt\\040path rw - ext4 root rw",
                "3 6 0:5 / /mnt\\040path/nested rw - tmpfs tmpfs rw",
                "4 2 0:6 / /mnt\\040path/visible rw - nfs server rw",
                "7 1 0:7 / /mnt\\040path/root-hidden rw - xfs root rw",
                "8 5 0:8 / /mnt\\040path/visible/hidden rw - btrfs root rw"
            ];
            yield return ["/", mounts, "overlay"];
            yield return ["/file", mounts, "overlay"];
            yield return ["/mnt path", mounts, "ext4"];
            yield return ["/mnt path/file", mounts, "ext4"];
            yield return ["/mnt path/nested", mounts, "ext4"];
            yield return ["/mnt path/nested/file", mounts, "ext4"];
            yield return ["/mnt path/visible", mounts, "nfs"];
            yield return ["/mnt path/visible/file", mounts, "nfs"];
            yield return ["/mnt path/visible/hidden", mounts, "nfs"];
            yield return ["/mnt path/root-hidden", mounts, "ext4"];
            yield return ["/mnt pathway", mounts, "overlay"];

            mounts =
            [
                "3 99 0:3 / /mnt/sub rw - tmpfs tmpfs rw",
                "2 99 0:2 / /mnt rw - ext4 root rw",
                "4 2 0:4 / /mnt/sub/visible rw - nfs server rw"
            ];
            yield return ["/mnt/sub/file", mounts, "ext4"];
            yield return ["/mnt/sub/visible", mounts, "nfs"];
            yield return ["/mnt/sub/visible/file", mounts, "nfs"];

            yield return ["/mnt", new[] { "" }, null];
            yield return ["/mnt", new[] { "invalid" }, null];
            yield return ["/mnt", new[] { "1 99 0:1 / /different rw - ext4 root rw" }, null];
        }

        [Theory]
        [MemberData(nameof(FileSystemTypeForPath_TestData))]
        public void GetFileSystemTypeForPath_SelectsVisibleMount(string path, string[] mounts, string? expectedFormat)
        {
            for (int first = 0; first < Math.Max(1, mounts.Length); first++)
            {
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    string[] orderedMounts = new string[mounts.Length];
                    for (int i = 0; i < mounts.Length; i++)
                    {
                        orderedMounts[i] = mounts[(first + mounts.Length + i * direction) % mounts.Length];
                    }

                    Assert.Equal(expectedFormat, Interop.procfs.GetFileSystemTypeForPath(path, orderedMounts));
                }
            }
        }
    }
}
