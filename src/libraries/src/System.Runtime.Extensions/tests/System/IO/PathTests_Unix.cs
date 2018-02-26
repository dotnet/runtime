// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public class PathTests_Unix : PathTestsBase
    {
        [Theory,
            MemberData(nameof(TestData_GetPathRoot_Unc)),
            MemberData(nameof(TestData_GetPathRoot_DevicePaths))]
        public static void GetPathRoot(string value, string expected)
        {
            // UNCs and device paths have no special meaning in Unix
            Assert.Empty(Path.GetPathRoot(value));
        }

        [Theory,
            InlineData("B:", "B:"),
            InlineData("A:.", "A:.")]
        public void GetFileName_Volume(string path, string expected)
        {
            // No such thing as a drive relative path on Unix.
            Assert.Equal(expected, Path.GetFileName(path));
            Assert.Equal(expected, new string(Path.GetFileName(path.AsSpan())));
        }

        [Theory,
            InlineData("/tmp/", "/tmp"),
            InlineData("/tmp/", "/tmp/"),
            InlineData("/", "/"),
            InlineData("/var/tmp/", "/var/tmp"),
            InlineData("/var/tmp/", "/var/tmp/"),
            InlineData("~/", "~"),
            InlineData("~/", "~/"),
            InlineData(".tmp/", ".tmp"),
            InlineData("./tmp/", "./tmp"),
            InlineData("/home/someuser/sometempdir/", "/home/someuser/sometempdir/"),
            InlineData("/home/someuser/some tempdir/", "/home/someuser/some tempdir/"),
            InlineData("/tmp/", null)]
        public void GetTempPath_SetEnvVar_Unix(string expected, string newTempPath)
        {
            GetTempPath_SetEnvVar("TMPDIR", expected, newTempPath);
        }

        [Fact]
        public void GetFullPath_Unix_Whitespace()
        {
            string curDir = Directory.GetCurrentDirectory();
            Assert.Equal("/ / ", Path.GetFullPath("/ // "));
            Assert.Equal(Path.Combine(curDir, "    "), Path.GetFullPath("    "));
            Assert.Equal(Path.Combine(curDir, "\r\n"), Path.GetFullPath("\r\n"));
        }
    }
}
