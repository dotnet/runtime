// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.DotNet.RemoteExecutor;
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

        public static IEnumerable<string[]> GetTempPath_SetEnvVar_Data()
        {
            yield return new string[] { "/tmp/", "/tmp" };
            yield return new string[] { "/tmp/", "/tmp/" };
            yield return new string[] { "/", "/" };
            yield return new string[] { "/var/tmp/", "/var/tmp" };
            yield return new string[] { "/var/tmp/", "/var/tmp/" };
            yield return new string[] { "~/", "~" };
            yield return new string[] { "~/", "~/" };
            yield return new string[] { ".tmp/", ".tmp" };
            yield return new string[] { "./tmp/", "./tmp" };
            yield return new string[] { "/home/someuser/sometempdir/", "/home/someuser/sometempdir/" };
            yield return new string[] { "/home/someuser/some tempdir/", "/home/someuser/some tempdir/" };
            yield return new string[] { "/tmp/", null };
        }

        [Fact]
        public void GetTempPath_SetEnvVar_Unix()
        {
            RemoteExecutor.Invoke(() =>
            {
                foreach (string[] tempPath in GetTempPath_SetEnvVar_Data())
                {
                    GetTempPath_SetEnvVar("TMPDIR", tempPath[0], tempPath[1]);
                }
            }).Dispose();
        }

        [Fact]
        public void GetFullPath_Unix_Whitespace()
        {
            string curDir = Directory.GetCurrentDirectory();
            Assert.Equal("/ / ", Path.GetFullPath("/ // "));
            Assert.Equal(Path.Combine(curDir, "    "), Path.GetFullPath("    "));
            Assert.Equal(Path.Combine(curDir, "\r\n"), Path.GetFullPath("\r\n"));
        }

        public static TheoryData<string, string, string> GetFullPath_BasePath_BasicExpansions_TestData_Unix => new TheoryData<string, string, string>
        {
            { @"/home/git", @"/home/git", @"/home/git" },
            { "", @"/home/git", @"/home/git" },
            { "..", @"/home/git", @"/home" },
            { @"/home/git/././././././", @"/home/git", @"/home/git/" },
            { @"/home/git///.", @"/home/git", @"/home/git" },
            { @"/home/git/../git/./../git", @"/home/git", @"/home/git" },
            { @"/home/git/somedir/..", @"/home/git", @"/home/git" },
            { @"/home/git/./", @"/home/git", @"/home/git/" },
            { @"/home/../../../../..", @"/home/git", @"/" },
            { @"/home///", @"/home/git", @"/home/" },
            { "tmp", @"/home/git", @"/home/git/tmp" },
            { "tmp/bar/..", @"/home/git", @"/home/git/tmp" },
            { "tmp/..", @"/home/git", @"/home/git" },
            { "tmp/./bar/../", @"/home/git", @"/home/git/tmp/" },
            { "tmp/bar/../../", @"/home/git", @"/home/git/" },
            { "tmp/bar/../next/../", @"/home/git", @"/home/git/tmp/" },
            { "tmp/bar/next", @"/home/git", @"/home/git/tmp/bar/next" },

            // Rooted
            { @"/tmp/bar", @"/home/git", @"/tmp/bar" },
            { @"/bar", @"/home/git", @"/bar" },
            { @"/tmp/..", @"/home/git", @"/" },
            { @"/tmp/bar/..", @"/home/git", @"/tmp" },
            { @"/tmp/..", @"/home/git", @"/" },
            { @"/", @"/home/git", @"/" },

            { @"/tmp/../../../bar", @"/home/git", @"/bar" },
            { @"/bar/././././../../..", @"/home/git", @"/" },
            { @"/../../tmp/../../", @"/home/git", @"/" },
            { @"/../../tmp/bar/..", @"/home/git", @"/tmp" },
            { @"/tmp/..", @"/home/git", @"/" },
            { @"/././../../../../", @"/home/git", @"/" },

            { @"/tmp/../../../../../bar", @"/home/git", @"/bar" },
            { @"/./././bar/../../../", @"/home/git", @"/" },
            { @"/tmp/..", @"/home/git", @"/" },
            { @"/../../tmp/bar/..", @"/home/git", @"/tmp" },
            { @"/tmp/..", @"/home/git", @"/" },
            { @"../../../", @"/home/git", @"/" },

            { @"../.././././bar/../../../", @"/home/git", @"/" },
            { @"../../.././tmp/..", @"/home/git", @"/" },
            { @"../../../tmp/bar/..", @"/home/git", @"/tmp" },
            { @"../../././tmp/..", @"/home/git", @"/" },
            { @"././../../../", @"/home/git", @"/" },
        };

        [Theory,
           MemberData(nameof(GetFullPath_BasePath_BasicExpansions_TestData_Unix))]
        public static void GetFullPath_BasicExpansions_Unix(string path, string basePath, string expected)
        {
            Assert.Equal(expected, Path.GetFullPath(path, basePath));
        }

        [Fact]
        public void GetFullPath_ThrowsOnEmbeddedNulls()
        {
            Assert.Throws<ArgumentException>(null, () => Path.GetFullPath("/gi\0t", "/foo/bar"));
        }

        public static TheoryData<string, string> TestData_TrimEndingDirectorySeparator => new TheoryData<string, string>
        {
            { @"/folder/", @"/folder" },
            { @"folder/", @"folder" },
            { @"", @"" },
            { @"/", @"/" },
            { null, null }
        };

        public static TheoryData<string, bool> TestData_EndsInDirectorySeparator => new TheoryData<string, bool>
        {
            { @"/", true },
            { @"/folder/", true },
            { @"//", true },
            { @"folder", false },
            { @"folder/", true },
            { @"", false },
            { null, false }
        };

        [Theory,
            MemberData(nameof(TestData_TrimEndingDirectorySeparator))]
        public void TrimEndingDirectorySeparator_String(string path, string expected)
        {
            string trimmed = Path.TrimEndingDirectorySeparator(path);
            Assert.Equal(expected, trimmed);
            Assert.Same(trimmed, Path.TrimEndingDirectorySeparator(trimmed));
        }

        [Theory,
            MemberData(nameof(TestData_TrimEndingDirectorySeparator))]
        public void TrimEndingDirectorySeparator_ReadOnlySpan(string path, string expected)
        {
            ReadOnlySpan<char> trimmed = Path.TrimEndingDirectorySeparator(path.AsSpan());
            PathAssert.Equal(expected, trimmed);
            PathAssert.Equal(trimmed, Path.TrimEndingDirectorySeparator(trimmed));
        }

        [Theory,
            MemberData(nameof(TestData_EndsInDirectorySeparator))]
        public void EndsInDirectorySeparator_String(string path, bool expected)
        {
            Assert.Equal(expected, Path.EndsInDirectorySeparator(path));
        }

        [Theory,
            MemberData(nameof(TestData_EndsInDirectorySeparator))]
        public void EndsInDirectorySeparator_ReadOnlySpan(string path, bool expected)
        {
            Assert.Equal(expected, Path.EndsInDirectorySeparator(path.AsSpan()));
        }
    }
}
