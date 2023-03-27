// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Xunit;

namespace Common.Tests
{
    public class osReleaseTests
    {
        [Theory]
        // Double quotes:
        [InlineData("NAME=\"Fedora\"\nVERSION=\"37\"\nPRETTY_NAME=\"Fedora Linux 37\"", "Fedora Linux 37")]
        [InlineData("NAME=\"Fedora\"\nVERSION=\"37\"", "Fedora 37")]
        [InlineData("NAME=\"Fedora\"", "Fedora")]
        // Single quotes:
        [InlineData("NAME='Ubuntu'\nVERSION='22.04'\nPRETTY_NAME='Ubuntu Linux 22.04'", "Ubuntu Linux 22.04")]
        [InlineData("NAME='Ubuntu'\nVERSION='22.04'", "Ubuntu 22.04")]
        [InlineData("NAME='Ubuntu'", "Ubuntu")]
        // No quotes:
        [InlineData("NAME=Alpine\nVERSION=3.14\nPRETTY_NAME=Alpine_Linux_3.14", "Alpine_Linux_3.14")]
        [InlineData("NAME=Alpine\nVERSION=3.14", "Alpine 3.14")]
        [InlineData("NAME=Alpine", "Alpine")]
        // No pretty name fields:
        [InlineData("ID=fedora\nVERSION_ID=37", null)]
        [InlineData("", null)]
        public static void GetPrettyName_Success(
            string content,
            string expectedName)
        {
            string path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, content);

                string? name = Interop.OSReleaseFile.GetPrettyName(path);
                Assert.Equal(expectedName, name);
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public static void GetPrettyName_NoFile_ReturnsNull()
        {
            string path = Path.GetRandomFileName();
            Assert.False(File.Exists(path));

            string? name = Interop.OSReleaseFile.GetPrettyName(path);
            Assert.Null(name);
        }

        [Fact, PlatformSpecific(TestPlatforms.Linux)]
        public static void GetPrettyName_CannotRead_ReturnsNull()
        {
            string path = Path.GetTempFileName();
            try
            {
                File.SetUnixFileMode(path, UnixFileMode.None);
                Assert.ThrowsAny<Exception>(() => File.ReadAllText(path));

                string? name = Interop.OSReleaseFile.GetPrettyName(path);
                Assert.Null(name);
            }
            finally { File.Delete(path); }
        }
    }
}
