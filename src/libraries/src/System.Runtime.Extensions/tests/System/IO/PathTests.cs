// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace System.IO.Tests
{
    public static partial class PathTests
    {
        [Theory]
        [InlineData(null, null, null)]
        [InlineData(null, "exe", null)]
        [InlineData("", "", "")]
        [InlineData("file.exe", null, "file")]
        [InlineData("file.exe", "", "file.")]
        [InlineData("file", "exe", "file.exe")]
        [InlineData("file", ".exe", "file.exe")]
        [InlineData("file.txt", "exe", "file.exe")]
        [InlineData("file.txt", ".exe", "file.exe")]
        [InlineData("file.txt.bin", "exe", "file.txt.exe")]
        [InlineData("dir/file.t", "exe", "dir/file.exe")]
        [InlineData("dir/file.exe", "t", "dir/file.t")]
        [InlineData("dir/file", "exe", "dir/file.exe")]
        public static void ChangeExtension(string path, string newExtension, string expected)
        {
            if (expected != null)
                expected = expected.Replace('/', Path.DirectorySeparatorChar);
            if (path != null)
                path = path.Replace('/', Path.DirectorySeparatorChar);
            Assert.Equal(expected, Path.ChangeExtension(path, newExtension));
        }

        [Fact]
        public static void GetDirectoryName_EmptyThrows()
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetDirectoryName(string.Empty));
        }

        [Theory,
            InlineData(" "),
            InlineData("\r\n")]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void GetDirectoryName_SpaceOrControlCharsThrowOnWindows(string path)
        {
            Action action = () => Path.GetDirectoryName(path);
            if (PlatformDetection.IsWindows)
            {
                AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetDirectoryName(path));
            }
            else
            {
                // These are valid paths on Unix
                action();
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void GetDirectoryName_SpaceThrowOnWindows_Core()
        {
            string path = " ";
            Action action = () => Path.GetDirectoryName(path);
            if (PlatformDetection.IsWindows)
            {
                AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetDirectoryName(path));
            }
            else
            {
                // This is a valid path on Unix
                action();
            }
        }

        [Theory]
        [InlineData("\u00A0")] // Non-breaking Space
        [InlineData("\u2028")] // Line separator
        [InlineData("\u2029")] // Paragraph separator
        public static void GetDirectoryName_NonControl(string path)
        {
            Assert.Equal(string.Empty, Path.GetDirectoryName(path));
        }

        [Theory]
        [InlineData("\u00A0")] // Non-breaking Space
        [InlineData("\u2028")] // Line separator
        [InlineData("\u2029")] // Paragraph separator
        public static void GetDirectoryName_NonControlWithSeparator(string path)
        {
            Assert.Equal(path, Path.GetDirectoryName(Path.Combine(path, path)));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(".", "")]
        [InlineData("..", "")]
        [InlineData("baz", "")]
        public static void GetDirectoryName(string path, string expected)
        {
            Assert.Equal(expected, Path.GetDirectoryName(path));
        }

        [Theory]
        [InlineData(@"dir/baz", "dir")]
        [InlineData(@"dir\baz", "dir")]
        [InlineData(@"dir\baz\bar", @"dir\baz")]
        [InlineData(@"dir\\baz", "dir")]
        [InlineData(@" dir\baz", " dir")]
        [InlineData(@" C:\dir\baz", @"C:\dir")]
        [InlineData(@"..\..\files.txt", @"..\..")]
        [InlineData(@"C:\", null)]
        [InlineData(@"C:", null)]
        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific paths
        public static void GetDirectoryName_Windows(string path, string expected)
        {
            Assert.Equal(expected, Path.GetDirectoryName(path));
        }

        [Theory]
        [InlineData(@"dir/baz", @"dir")]
        [InlineData(@"dir//baz", @"dir")]
        [InlineData(@"dir\baz", @"")]
        [InlineData(@"dir/baz/bar", @"dir/baz")]
        [InlineData(@"../../files.txt", @"../..")]
        [InlineData(@"/", null)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests Unix-specific paths
        public static void GetDirectoryName_Unix(string path, string expected)
        {
            Assert.Equal(expected, Path.GetDirectoryName(path));
        }

        [Fact]
        public static void GetDirectoryName_CurrentDirectory()
        {
            string curDir = Directory.GetCurrentDirectory();
            Assert.Equal(curDir, Path.GetDirectoryName(Path.Combine(curDir, "baz")));
            Assert.Equal(null, Path.GetDirectoryName(Path.GetPathRoot(curDir)));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks Unix-specific special characters in directory path
        [Fact]
        public static void GetDirectoryName_ControlCharacters_Unix()
        {
            Assert.Equal(new string('\t', 1), Path.GetDirectoryName(Path.Combine(new string('\t', 1), "file")));
            Assert.Equal(new string('\b', 2), Path.GetDirectoryName(Path.Combine(new string('\b', 2), "fi le")));
            Assert.Equal(new string('\v', 3), Path.GetDirectoryName(Path.Combine(new string('\v', 3), "fi\nle")));
            Assert.Equal(new string('\n', 4), Path.GetDirectoryName(Path.Combine(new string('\n', 4), "fi\rle")));
        }

        [Theory]
        [InlineData("file.exe", ".exe")]
        [InlineData("file", "")]
        [InlineData(null, null)]
        [InlineData("file.", "")]
        [InlineData("file.s", ".s")]
        [InlineData("test/file", "")]
        [InlineData("test/file.extension", ".extension")]
        public static void GetExtension(string path, string expected)
        {
            if (path != null)
            {
                path = path.Replace('/', Path.DirectorySeparatorChar);
            }
            Assert.Equal(expected, Path.GetExtension(path));
            Assert.Equal(!string.IsNullOrEmpty(expected), Path.HasExtension(path));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks file extension behavior on Unix
        [Theory]
        [InlineData("file.e xe", ".e xe")]
        [InlineData("file. ", ". ")]
        [InlineData(" file. ", ". ")]
        [InlineData(" file.extension", ".extension")]
        [InlineData("file.exten\tsion", ".exten\tsion")]
        public static void GetExtension_Unix(string path, string expected)
        {
            Assert.Equal(expected, Path.GetExtension(path));
            Assert.Equal(!string.IsNullOrEmpty(expected), Path.HasExtension(path));
        }

        public static IEnumerable<object[]> GetFileName_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { ".", "." };
            yield return new object[] { "..", ".." };
            yield return new object[] { "file", "file" };
            yield return new object[] { "file.", "file." };
            yield return new object[] { "file.exe", "file.exe" };
            yield return new object[] { Path.Combine("baz", "file.exe"), "file.exe" };
            yield return new object[] { Path.Combine("bar", "baz", "file.exe"), "file.exe" };
            yield return new object[] { Path.Combine("bar", "baz", "file.exe") + Path.DirectorySeparatorChar, "" };
        }

        [Theory]
        [MemberData(nameof(GetFileName_TestData))]
        public static void GetFileName(string path, string expected)
        {
            Assert.Equal(expected, Path.GetFileName(path));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests Unix-specific valid file names
        [Fact]
        public static void GetFileName_Unix()
        {
            Assert.Equal(" . ", Path.GetFileName(" . "));
            Assert.Equal(" .. ", Path.GetFileName(" .. "));
            Assert.Equal("fi le", Path.GetFileName("fi le"));
            Assert.Equal("fi  le", Path.GetFileName("fi  le"));
            Assert.Equal("fi  le", Path.GetFileName(Path.Combine("b \r\n ar", "fi  le")));
        }

        public static IEnumerable<object[]> GetFileNameWithoutExtension_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { "", "" };
            yield return new object[] { "file", "file" };
            yield return new object[] { "file.exe", "file" };
            yield return new object[] { Path.Combine("bar", "baz", "file.exe"), "file" };
            yield return new object[] { Path.Combine("bar", "baz") + Path.DirectorySeparatorChar, "" };
        }

        [Theory]
        [MemberData(nameof(GetFileNameWithoutExtension_TestData))]
        public static void GetFileNameWithoutExtension(string path, string expected)
        {
            Assert.Equal(expected, Path.GetFileNameWithoutExtension(path));
        }

        [Fact]
        public static void GetPathRoot_NullReturnsNull()
        {
            Assert.Null(Path.GetPathRoot(null));
        }

        [Fact]
        public static void GetPathRoot_EmptyThrows()
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetPathRoot(string.Empty));
        }

        [Fact]
        public static void GetPathRoot_Basic()
        {
            string cwd = Directory.GetCurrentDirectory();
            Assert.Equal(cwd.Substring(0, cwd.IndexOf(Path.DirectorySeparatorChar) + 1), Path.GetPathRoot(cwd));
            Assert.True(Path.IsPathRooted(cwd));

            Assert.Equal(string.Empty, Path.GetPathRoot(@"file.exe"));
            Assert.False(Path.IsPathRooted("file.exe"));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests UNC
        [Theory]
        [InlineData(@"\\test\unc\path\to\something", @"\\test\unc")]
        [InlineData(@"\\a\b\c\d\e", @"\\a\b")]
        [InlineData(@"\\a\b\", @"\\a\b")]
        [InlineData(@"\\a\b", @"\\a\b")]
        [InlineData(@"\\test\unc", @"\\test\unc")]
        public static void GetPathRoot_Windows_UncAndExtended(string value, string expected)
        {
            Assert.True(Path.IsPathRooted(value));
            Assert.Equal(expected, Path.GetPathRoot(value));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests UNC
        [Theory]
        [InlineData(@"\\?\UNC\test\unc", @"\\?\UNC", @"\\?\UNC\test\unc\path\to\something")]
        [InlineData(@"\\?\UNC\test\unc", @"\\?\UNC", @"\\?\UNC\test\unc")]
        [InlineData(@"\\?\UNC\a\b1", @"\\?\UNC", @"\\?\UNC\a\b1")]
        [InlineData(@"\\?\UNC\a\b2", @"\\?\UNC", @"\\?\UNC\a\b2\")]
        [InlineData(@"\\?\C:\", @"\\?\C:", @"\\?\C:\foo\bar.txt")]
        public static void GetPathRoot_Windows_UncAndExtended_WithLegacySupport(string normalExpected, string legacyExpected, string value)
        {
            Assert.True(Path.IsPathRooted(value));

            string expected = PathFeatures.IsUsingLegacyPathNormalization() ? legacyExpected : normalExpected;
            Assert.Equal(expected, Path.GetPathRoot(value));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific path convention
        [Theory]
        [InlineData(@"C:", @"C:")]
        [InlineData(@"C:\", @"C:\")]
        [InlineData(@"C:\\", @"C:\")]
        [InlineData(@"C://", @"C:\")]
        [InlineData(@"C:\foo1", @"C:\")]
        [InlineData(@"C:\\foo2", @"C:\")]
        [InlineData(@"C://foo3", @"C:\")]
        public static void GetPathRoot_Windows(string value, string expected)
        {
            Assert.True(Path.IsPathRooted(value));
            Assert.Equal(expected, Path.GetPathRoot(value));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests Unix-specific path convention
        [Fact]
        public static void GetPathRoot_Unix()
        {
            // slashes are normal filename characters
            string uncPath = @"\\test\unc\path\to\something";
            Assert.False(Path.IsPathRooted(uncPath));
            Assert.Equal(string.Empty, Path.GetPathRoot(uncPath));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public static void IsPathRooted(string path)
        {
            Assert.False(Path.IsPathRooted(path));
        }

        // Testing invalid drive letters !(a-zA-Z)
        [PlatformSpecific(TestPlatforms.Windows)]
        [Theory]
        [InlineData(@"@:\foo")]    // 064 = @     065 = A
        [InlineData(@"[:\\")]       // 091 = [     090 = Z
        [InlineData(@"`:\foo")]    // 096 = `     097 = a
        [InlineData(@"{:\\")]       // 123 = {     122 = z
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Bug fixed on Core where it would return true if the first char is not a drive letter followed by a VolumeSeparatorChar coreclr/10297")]
        public static void IsPathRooted_Windows_Invalid(string value)
        {
            Assert.False(Path.IsPathRooted(value));
        }

        [Fact]
        public static void GetRandomFileName()
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var fileNames = new HashSet<string>();
            for (int i = 0; i < 100; i++)
            {
                string s = Path.GetRandomFileName();
                Assert.Equal(s.Length, 8 + 1 + 3);
                Assert.Equal(s[8], '.');
                Assert.Equal(-1, s.IndexOfAny(invalidChars));
                Assert.True(fileNames.Add(s));
            }
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void GetInvalidPathChars()
        {
            Assert.NotNull(Path.GetInvalidPathChars());
            Assert.NotSame(Path.GetInvalidPathChars(), Path.GetInvalidPathChars());
            Assert.Equal((IEnumerable<char>)Path.GetInvalidPathChars(), Path.GetInvalidPathChars());
            Assert.True(Path.GetInvalidPathChars().Length > 0);
            Assert.All(Path.GetInvalidPathChars(), c =>
            {
                string bad = c.ToString();
                AssertExtensions.Throws<ArgumentException>("path", null, () => Path.ChangeExtension(bad, "ok"));
                AssertExtensions.Throws<ArgumentException>("path", null, () => Path.Combine(bad, "ok"));
                AssertExtensions.Throws<ArgumentException>("path", null, () => Path.Combine("ok", "ok", bad));
                AssertExtensions.Throws<ArgumentException>("path", null, () => Path.Combine("ok", "ok", bad, "ok"));
                AssertExtensions.Throws<ArgumentException>("path", null, () => Path.Combine(bad, bad, bad, bad, bad));
                AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetDirectoryName(bad));
                AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetExtension(bad));
                AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetFileName(bad));
                AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetFileNameWithoutExtension(bad));
                AssertExtensions.Throws<ArgumentException>(c == 124 ? null : "path", null, () => Path.GetFullPath(bad));
                AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetPathRoot(bad));
                AssertExtensions.Throws<ArgumentException>("path", null, () => Path.IsPathRooted(bad));
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void GetInvalidPathChars_Core()
        {
            Assert.NotNull(Path.GetInvalidPathChars());
            Assert.NotSame(Path.GetInvalidPathChars(), Path.GetInvalidPathChars());
            Assert.Equal((IEnumerable<char>)Path.GetInvalidPathChars(), Path.GetInvalidPathChars());
            Assert.True(Path.GetInvalidPathChars().Length > 0);
            Assert.All(Path.GetInvalidPathChars(), c =>
            {
                string bad = c.ToString();
                Assert.Equal(bad + ".ok", Path.ChangeExtension(bad, "ok"));
                Assert.Equal(bad + Path.DirectorySeparatorChar + "ok", Path.Combine(bad, "ok"));
                Assert.Equal("ok" + Path.DirectorySeparatorChar + "ok" + Path.DirectorySeparatorChar + bad, Path.Combine("ok", "ok", bad));
                Assert.Equal("ok" + Path.DirectorySeparatorChar + "ok" + Path.DirectorySeparatorChar + bad + Path.DirectorySeparatorChar + "ok", Path.Combine("ok", "ok", bad, "ok"));
                Assert.Equal(bad + Path.DirectorySeparatorChar + bad + Path.DirectorySeparatorChar + bad + Path.DirectorySeparatorChar + bad + Path.DirectorySeparatorChar + bad, Path.Combine(bad, bad, bad, bad, bad));
                Assert.Equal("", Path.GetDirectoryName(bad));
                Assert.Equal(string.Empty, Path.GetExtension(bad));
                Assert.Equal(bad, Path.GetFileName(bad));
                Assert.Equal(bad, Path.GetFileNameWithoutExtension(bad));
                AssertExtensions.Throws<ArgumentException>(c == 124 ? null : "path", null, () => Path.GetFullPath(bad));
                Assert.Equal(string.Empty, Path.GetPathRoot(bad));
                Assert.False(Path.IsPathRooted(bad));
            });
        }

        [Fact]
        public static void GetInvalidFileNameChars()
        {
            Assert.NotNull(Path.GetInvalidFileNameChars());
            Assert.NotSame(Path.GetInvalidFileNameChars(), Path.GetInvalidFileNameChars());
            Assert.Equal((IEnumerable<char>)Path.GetInvalidFileNameChars(), Path.GetInvalidFileNameChars());
            Assert.True(Path.GetInvalidFileNameChars().Length > 0);
        }

        [Fact]
        [OuterLoop]
        public static void GetInvalidFileNameChars_OtherCharsValid()
        {
            string curDir = Directory.GetCurrentDirectory();
            var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
            for (int i = 0; i < char.MaxValue; i++)
            {
                char c = (char)i;
                if (!invalidChars.Contains(c))
                {
                    string name = "file" + c + ".txt";
                    Assert.Equal(Path.Combine(curDir, name), Path.GetFullPath(name));
                }
            }
        }

        [Fact]
        public static void GetTempPath_Default()
        {
            string tmpPath = Path.GetTempPath();
            Assert.False(string.IsNullOrEmpty(tmpPath));
            Assert.Equal(tmpPath, Path.GetTempPath());
            Assert.Equal(Path.DirectorySeparatorChar, tmpPath[tmpPath.Length - 1]);
            Assert.True(Directory.Exists(tmpPath));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Sets environment vars with Windows-specific paths
        [Theory]
        [InlineData(@"C:\Users\someuser\AppData\Local\Temp\", @"C:\Users\someuser\AppData\Local\Temp")]
        [InlineData(@"C:\Users\someuser\AppData\Local\Temp\", @"C:\Users\someuser\AppData\Local\Temp\")]
        [InlineData(@"C:\", @"C:\")]
        [InlineData(@"C:\tmp\", @"C:\tmp")]
        [InlineData(@"C:\tmp\", @"C:\tmp\")]
        public static void GetTempPath_SetEnvVar_Windows(string expected, string newTempPath)
        {
            GetTempPath_SetEnvVar("TMP", expected, newTempPath);
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Sets environment vars with Unix-specific paths
        [Theory]
        [InlineData("/tmp/", "/tmp")]
        [InlineData("/tmp/", "/tmp/")]
        [InlineData("/", "/")]
        [InlineData("/var/tmp/", "/var/tmp")]
        [InlineData("/var/tmp/", "/var/tmp/")]
        [InlineData("~/", "~")]
        [InlineData("~/", "~/")]
        [InlineData(".tmp/", ".tmp")]
        [InlineData("./tmp/", "./tmp")]
        [InlineData("/home/someuser/sometempdir/", "/home/someuser/sometempdir/")]
        [InlineData("/home/someuser/some tempdir/", "/home/someuser/some tempdir/")]
        [InlineData("/tmp/", null)]
        public static void GetTempPath_SetEnvVar_Unix(string expected, string newTempPath)
        {
            GetTempPath_SetEnvVar("TMPDIR", expected, newTempPath);
        }

        private static void GetTempPath_SetEnvVar(string envVar, string expected, string newTempPath)
        {
            string original = Path.GetTempPath();
            Assert.NotNull(original);
            try
            {
                Environment.SetEnvironmentVariable(envVar, newTempPath);
                Assert.Equal(
                    Path.GetFullPath(expected),
                    Path.GetFullPath(Path.GetTempPath()));
            }
            finally
            {
                Environment.SetEnvironmentVariable(envVar, original);
                Assert.Equal(original, Path.GetTempPath());
            }
        }

        [Fact]
        public static void GetTempFileName()
        {
            string tmpFile = Path.GetTempFileName();
            try
            {
                Assert.True(File.Exists(tmpFile));
                Assert.Equal(".tmp", Path.GetExtension(tmpFile), ignoreCase: true, ignoreLineEndingDifferences: false, ignoreWhiteSpaceDifferences: false);
                Assert.Equal(-1, tmpFile.IndexOfAny(Path.GetInvalidPathChars()));
                using (FileStream fs = File.OpenRead(tmpFile))
                {
                    Assert.Equal(0, fs.Length);
                }
                Assert.Equal(Path.Combine(Path.GetTempPath(), Path.GetFileName(tmpFile)), tmpFile);
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }

        [Theory]
        [InlineData("\u0085")] // Next line
        [InlineData("\u00A0")] // Non breaking space
        [InlineData("\u2028")] // Line separator
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // not NetFX
        public static void GetFullPath_NonControlWhiteSpaceStays(string component)
        {
            // When not NetFX full path should not cut off component
            string path = "C:\\Test" + component;
            Assert.Equal(path, Path.GetFullPath(path));
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData("   ")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void GetFullPath_TrailingSpaceCut(string component)
        {
            // Windows cuts off any simple white space added to a path
            string path = "C:\\Test" + component;
            Assert.Equal("C:\\Test", Path.GetFullPath(path));
        }

        [Fact]
        public static void GetFullPath_InvalidArgs()
        {
            Assert.Throws<ArgumentNullException>(() => Path.GetFullPath(null));
            AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetFullPath(string.Empty));
        }

        public static IEnumerable<object[]> GetFullPath_BasicExpansions_TestData()
        {
            string curDir = Directory.GetCurrentDirectory();
            yield return new object[] { curDir, curDir }; // Current directory => current directory
            yield return new object[] { ".", curDir }; // "." => current directory
            yield return new object[] { "..", Path.GetDirectoryName(curDir) }; // "." => up a directory
            yield return new object[] { Path.Combine(curDir, ".", ".", ".", ".", "."), curDir }; // "dir/./././." => "dir"
            yield return new object[] { curDir + new string(Path.DirectorySeparatorChar, 3) + ".", curDir }; // "dir///." => "dir"
            yield return new object[] { Path.Combine(curDir, "..", Path.GetFileName(curDir), ".", "..", Path.GetFileName(curDir)), curDir }; // "dir/../dir/./../dir" => "dir"
            yield return new object[] { Path.Combine(Path.GetPathRoot(curDir), "somedir", ".."), Path.GetPathRoot(curDir) }; // "C:\somedir\.." => "C:\"
            yield return new object[] { Path.Combine(Path.GetPathRoot(curDir), "."), Path.GetPathRoot(curDir) }; // "C:\." => "C:\"
            yield return new object[] { Path.Combine(Path.GetPathRoot(curDir), "..", "..", "..", ".."), Path.GetPathRoot(curDir) }; // "C:\..\..\..\.." => "C:\"
            yield return new object[] { Path.GetPathRoot(curDir) + new string(Path.DirectorySeparatorChar, 3), Path.GetPathRoot(curDir) }; // "C:\\\" => "C:\"

            // Path longer than MaxPath that normalizes down to less than MaxPath
            const int Iters = 10000;
            var longPath = new StringBuilder(curDir, curDir.Length + (Iters * 2));
            for (int i = 0; i < 10000; i++)
            {
                longPath.Append(Path.DirectorySeparatorChar).Append('.');
            }
            yield return new object[] { longPath.ToString(), curDir };
        }

        [Theory]
        [MemberData(nameof(GetFullPath_BasicExpansions_TestData))]
        public static void GetFullPath_BasicExpansions(string path, string expected)
        {
            Assert.Equal(expected, Path.GetFullPath(path));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests whitespace in paths on Unix
        [Fact]
        public static void GetFullPath_Unix_Whitespace()
        {
            string curDir = Directory.GetCurrentDirectory();
            Assert.Equal("/ / ", Path.GetFullPath("/ // "));
            Assert.Equal(Path.Combine(curDir, "    "), Path.GetFullPath("    "));
            Assert.Equal(Path.Combine(curDir, "\r\n"), Path.GetFullPath("\r\n"));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests URIs as valid file names
        [Theory]
        [InlineData("http://www.microsoft.com")]
        [InlineData("file://somefile")]
        public static void GetFullPath_Unix_URIsAsFileNames(string uriAsFileName)
        {
            // URIs are valid filenames, though the multiple slashes will be consolidated in GetFullPath
            Assert.Equal(
                Path.Combine(Directory.GetCurrentDirectory(), uriAsFileName.Replace("//", "/")),
                Path.GetFullPath(uriAsFileName));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Checks normalized long path (> MaxPath) on Windows
        [Fact]
        public static void GetFullPath_Windows_NormalizedLongPathTooLong()
        {
            // Try out a long path that normalizes down to more than MaxPath
            string curDir = Directory.GetCurrentDirectory();
            const int Iters = 260;
            var longPath = new StringBuilder(curDir, curDir.Length + (Iters * 4));
            for (int i = 0; i < Iters; i++)
            {
                longPath.Append(Path.DirectorySeparatorChar).Append('a').Append(Path.DirectorySeparatorChar).Append('.');
            }

            if (PathFeatures.AreAllLongPathsAvailable())
            {
                // Now no longer throws unless over ~32K
                Assert.NotNull(Path.GetFullPath(longPath.ToString()));
            }
            else
            {
                Assert.Throws<PathTooLongException>(() => Path.GetFullPath(longPath.ToString()));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific invalid paths
        [Fact]
        public static void GetFullPath_Windows_AlternateDataStreamsNotSupported()
        {
            // Throws via our invalid colon filtering
            Assert.Throws<NotSupportedException>(() => Path.GetFullPath(@"bad:path"));
            Assert.Throws<NotSupportedException>(() => Path.GetFullPath(@"C:\some\bad:path"));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific invalid paths
        [Theory]
        [InlineData("http://www.microsoft.com")]
        [InlineData("file://www.microsoft.com")]
        public static void GetFullPath_Windows_URIFormatNotSupported(string path)
        {
            // Throws via our invalid colon filtering
            if (!PathFeatures.IsUsingLegacyPathNormalization())
            {
                Assert.Throws<NotSupportedException>(() => Path.GetFullPath(path));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific invalid paths
        [Theory]
        [InlineData(@"bad::$DATA")]
        [InlineData(@"C  :")]
        [InlineData(@"C  :\somedir")]
        public static void GetFullPath_Windows_NotSupportedExceptionPaths(string path)
        {
            // Old path normalization throws ArgumentException, new one throws NotSupportedException
            if (!PathFeatures.IsUsingLegacyPathNormalization())
            {
                Assert.Throws<NotSupportedException>(() => Path.GetFullPath(path));
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>(null, () => Path.GetFullPath(path));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests legitimate Windows paths that are now allowed
        [Theory]
        [InlineData(@"C:...")]
        [InlineData(@"C:...\somedir")]
        [InlineData(@"\.. .\")]
        [InlineData(@"\. .\")]
        [InlineData(@"\ .\")]
        public static void GetFullPath_Windows_LegacyArgumentExceptionPaths(string path)
        {
            if (PathFeatures.IsUsingLegacyPathNormalization())
            {
                // We didn't allow these paths on < 4.6.2
                AssertExtensions.Throws<ArgumentException>(null, () => Path.GetFullPath(path));
            }
            else
            {
                // These paths are legitimate Windows paths that can be created without extended syntax.
                // We now allow them through.
                Path.GetFullPath(path);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests MaxPathNotTooLong on Windows
        [Fact]
        public static void GetFullPath_Windows_MaxPathNotTooLong()
        {
            string value = @"C:\" + new string('a', 255) + @"\";
            if (PathFeatures.AreAllLongPathsAvailable())
            {
                // Shouldn't throw anymore
                Path.GetFullPath(value);
            }
            else
            {
                Assert.Throws<PathTooLongException>(() => Path.GetFullPath(value));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests PathTooLong on Windows
        [Fact]
        public static void GetFullPath_Windows_PathTooLong()
        {
            Assert.Throws<PathTooLongException>(() => Path.GetFullPath(@"C:\" + new string('a', short.MaxValue) + @"\"));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific paths
        [Theory]
        [InlineData(@"C:\", @"C:\")]
        [InlineData(@"C:\.", @"C:\")]
        [InlineData(@"C:\..", @"C:\")]
        [InlineData(@"C:\..\..", @"C:\")]
        [InlineData(@"C:\A\..", @"C:\")]
        [InlineData(@"C:\..\..\A\..", @"C:\")]
        public static void GetFullPath_Windows_RelativeRoot(string path, string expected)
        {
            Assert.Equal(Path.GetFullPath(path), expected);
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests legitimate strage windows paths that are now allowed
        [Fact]
        public static void GetFullPath_Windows_StrangeButLegalPaths()
        {
            // These are legal and creatable without using extended syntax if you use a trailing slash
            // (such as "md ...\"). We used to filter these out, but now allow them to prevent apps from
            // being blocked when they hit these paths.
            string curDir = Directory.GetCurrentDirectory();
            if (PathFeatures.IsUsingLegacyPathNormalization())
            {
                // Legacy path Path.GetFullePath() ignores . when there is less or more that two, when there is .. in the path it returns one directory up.
                Assert.Equal(
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar),
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar + ". " + Path.DirectorySeparatorChar));
                Assert.Equal(
                    Path.GetFullPath(Path.GetDirectoryName(curDir) + Path.DirectorySeparatorChar),
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar + "..." + Path.DirectorySeparatorChar));
                Assert.Equal(
                    Path.GetFullPath(Path.GetDirectoryName(curDir) + Path.DirectorySeparatorChar),
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar + ".. " + Path.DirectorySeparatorChar));
            }
            else
            {
                Assert.NotEqual(
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar),
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar + ". " + Path.DirectorySeparatorChar));
                Assert.NotEqual(
                    Path.GetFullPath(Path.GetDirectoryName(curDir) + Path.DirectorySeparatorChar),
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar + "..." + Path.DirectorySeparatorChar));
                Assert.NotEqual(
                    Path.GetFullPath(Path.GetDirectoryName(curDir) + Path.DirectorySeparatorChar),
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar + ".. " + Path.DirectorySeparatorChar));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific paths
        [Theory]
        [InlineData(@"\\?\C:\ ")]
        [InlineData(@"\\?\C:\ \ ")]
        [InlineData(@"\\?\C:\ .")]
        [InlineData(@"\\?\C:\ ..")]
        [InlineData(@"\\?\C:\...")]
        [InlineData(@"\\?\GLOBALROOT\")]
        [InlineData(@"\\?\")]
        [InlineData(@"\\?\.")]
        [InlineData(@"\\?\..")]
        [InlineData(@"\\?\\")]
        [InlineData(@"\\?\C:\\")]
        [InlineData(@"\\?\C:\|")]
        [InlineData(@"\\?\C:\.")]
        [InlineData(@"\\?\C:\..")]
        [InlineData(@"\\?\C:\Foo1\.")]
        [InlineData(@"\\?\C:\Foo2\..")]
        [InlineData(@"\\?\UNC\")]
        [InlineData(@"\\?\UNC\server1")]
        [InlineData(@"\\?\UNC\server2\")]
        [InlineData(@"\\?\UNC\server3\\")]
        [InlineData(@"\\?\UNC\server4\..")]
        [InlineData(@"\\?\UNC\server5\share\.")]
        [InlineData(@"\\?\UNC\server6\share\..")]
        [InlineData(@"\\?\UNC\a\b\\")]
        [InlineData(@"\\.\")]
        [InlineData(@"\\.\.")]
        [InlineData(@"\\.\..")]
        [InlineData(@"\\.\\")]
        [InlineData(@"\\.\C:\\")]
        [InlineData(@"\\.\C:\|")]
        [InlineData(@"\\.\C:\.")]
        [InlineData(@"\\.\C:\..")]
        [InlineData(@"\\.\C:\Foo1\.")]
        [InlineData(@"\\.\C:\Foo2\..")]
        public static void GetFullPath_Windows_ValidExtendedPaths(string path)
        {
            if (PathFeatures.IsUsingLegacyPathNormalization())
            {
                // Legacy Path doesn't support any of these paths.
                AssertExtensions.ThrowsAny<ArgumentException, NotSupportedException>(() => Path.GetFullPath(path));
                return;
            }

            // None of these should throw
            if (path.StartsWith(@"\\?\"))
            {
                Assert.Equal(path, Path.GetFullPath(path));
            }
            else
            {
                Path.GetFullPath(path);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific paths
        [Theory]
        [InlineData(@"\\.\UNC\")]
        [InlineData(@"\\.\UNC\LOCALHOST")]
        [InlineData(@"\\.\UNC\localHOST\")]
        [InlineData(@"\\.\UNC\LOcaLHOST\\")]
        [InlineData(@"\\.\UNC\lOCALHOST\..")]
        [InlineData(@"\\.\UNC\LOCALhost\share\.")]
        [InlineData(@"\\.\UNC\loCALHOST\share\..")]
        [InlineData(@"\\.\UNC\a\b\\")]
        public static void GetFullPath_Windows_ValidLegacy_ValidExtendedPaths(string path)
        {
            // should not throw
            Path.GetFullPath(path);
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests valid paths based on UNC
        [Theory]
        // https://github.com/dotnet/corefx/issues/11965
        [InlineData(@"\\LOCALHOST\share\test.txt.~SS", @"\\LOCALHOST\share\test.txt.~SS")]
        [InlineData(@"\\LOCALHOST\share1", @"\\LOCALHOST\share1")]
        [InlineData(@"\\LOCALHOST\share2", @" \\LOCALHOST\share2")]
        [InlineData(@"\\LOCALHOST\share3\dir", @"\\LOCALHOST\share3\dir")]
        [InlineData(@"\\LOCALHOST\share4", @"\\LOCALHOST\share4\.")]
        [InlineData(@"\\LOCALHOST\share5", @"\\LOCALHOST\share5\..")]
        [InlineData(@"\\LOCALHOST\share6\", @"\\LOCALHOST\share6\    ")]
        [InlineData(@"\\LOCALHOST\  share7\", @"\\LOCALHOST\  share7\")]
        [InlineData(@"\\?\UNC\LOCALHOST\share8\test.txt.~SS", @"\\?\UNC\LOCALHOST\share8\test.txt.~SS")]
        [InlineData(@"\\?\UNC\LOCALHOST\share9", @"\\?\UNC\LOCALHOST\share9")]
        [InlineData(@"\\?\UNC\LOCALHOST\shareA\dir", @"\\?\UNC\LOCALHOST\shareA\dir")]
        [InlineData(@"\\?\UNC\LOCALHOST\shareB\. ", @"\\?\UNC\LOCALHOST\shareB\. ")]
        [InlineData(@"\\?\UNC\LOCALHOST\shareC\.. ", @"\\?\UNC\LOCALHOST\shareC\.. ")]
        [InlineData(@"\\?\UNC\LOCALHOST\shareD\    ", @"\\?\UNC\LOCALHOST\shareD\    ")]
        [InlineData(@"\\.\UNC\LOCALHOST\  shareE\", @"\\.\UNC\LOCALHOST\  shareE\")]
        [InlineData(@"\\.\UNC\LOCALHOST\shareF\test.txt.~SS", @"\\.\UNC\LOCALHOST\shareF\test.txt.~SS")]
        [InlineData(@"\\.\UNC\LOCALHOST\shareG", @"\\.\UNC\LOCALHOST\shareG")]
        [InlineData(@"\\.\UNC\LOCALHOST\shareH\dir", @"\\.\UNC\LOCALHOST\shareH\dir")]
        [InlineData(@"\\.\UNC\LOCALHOST\shareK\", @"\\.\UNC\LOCALHOST\shareK\    ")]
        [InlineData(@"\\.\UNC\LOCALHOST\  shareL\", @"\\.\UNC\LOCALHOST\  shareL\")]
        public static void GetFullPath_Windows_UNC_Valid(string expected, string input)
        {
            if (input.StartsWith(@"\\?\") && PathFeatures.IsUsingLegacyPathNormalization())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => Path.GetFullPath(input));
            }
            else
            {
                Assert.Equal(expected, Path.GetFullPath(input));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests valid paths based on UNC
        [Theory]
        [InlineData(@"\\.\UNC\LOCALHOST\shareI\", @"\\.\UNC\LOCALHOST\shareI", @"\\.\UNC\LOCALHOST\shareI\. ")]
        [InlineData(@"\\.\UNC\LOCALHOST\shareJ\", @"\\.\UNC\LOCALHOST", @"\\.\UNC\LOCALHOST\shareJ\.. ")]
        public static void GetFullPath_Windows_UNC_Valid_LegacyPathSupport(string normalExpected, string legacyExpected, string input)
        {
            string expected = PathFeatures.IsUsingLegacyPathNormalization() ? legacyExpected : normalExpected;
            Assert.Equal(expected, Path.GetFullPath(input));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests invalid paths based on UNC
        [Theory]
        [InlineData(@"\\")]
        [InlineData(@"\\LOCALHOST")]
        [InlineData(@"\\LOCALHOST\")]
        [InlineData(@"\\LOCALHOST\\")]
        [InlineData(@"\\LOCALHOST\..")]
        public static void GetFullPath_Windows_UNC_Invalid(string invalidPath)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Path.GetFullPath(invalidPath));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes to get short path name
        public static void GetFullPath_Windows_83Paths()
        {
            // Create a temporary file name with a name longer than 8.3 such that it'll need to be shortened.
            string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
            File.Create(tempFilePath).Dispose();
            try
            {
                // Get its short name
                var sb = new StringBuilder(260);
                if (GetShortPathName(tempFilePath, sb, sb.Capacity) > 0) // only proceed if we could successfully create the short name
                {
                    string shortName = sb.ToString();

                    // Make sure the shortened name expands back to the original one
                    // Sometimes shortening or GetFullPath is changing the casing of "temp" on some test machines: normalize both sides
                    tempFilePath = Regex.Replace(tempFilePath, @"\\temp\\", @"\TEMP\",  RegexOptions.IgnoreCase);
                    shortName = Regex.Replace(Path.GetFullPath(shortName), @"\\temp\\", @"\TEMP\",  RegexOptions.IgnoreCase);
                    Assert.Equal(tempFilePath, shortName);

                    // Should work with device paths that aren't well-formed extended syntax
                    if (!PathFeatures.IsUsingLegacyPathNormalization())
                    {
                        Assert.Equal(@"\\.\" + tempFilePath, Path.GetFullPath(@"\\.\" + shortName));
                        Assert.Equal(@"\\?\" + tempFilePath, Path.GetFullPath(@"//?/" + shortName));

                        // Shouldn't mess with well-formed extended syntax
                        Assert.Equal(@"\\?\" + shortName, Path.GetFullPath(@"\\?\" + shortName));
                    }

                    // Validate case where short name doesn't expand to a real file
                    string invalidShortName = @"S:\DOESNT~1\USERNA~1.RED\LOCALS~1\Temp\bg3ylpzp";
                    Assert.Equal(invalidShortName, Path.GetFullPath(invalidShortName));

                    // Same thing, but with a long path that normalizes down to a short enough one
                    const int Iters = 1000;
                    var shortLongName = new StringBuilder(invalidShortName, invalidShortName.Length + (Iters * 2));
                    for (int i = 0; i < Iters; i++)
                    {
                        shortLongName.Append(Path.DirectorySeparatorChar).Append('.');
                    }
                    Assert.Equal(invalidShortName, Path.GetFullPath(shortLongName.ToString()));
                }
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific invalid paths
        [Theory]
        [InlineData('*')]
        [InlineData('?')]
        public static void GetFullPath_Windows_Wildcards(char wildcard)
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetFullPath("test" + wildcard + "ing"));
        }

        // Windows-only P/Invoke to create 8.3 short names from long names
        [DllImport("kernel32.dll", EntryPoint = "GetShortPathNameW" ,CharSet = CharSet.Unicode)]
        private static extern uint GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, int cchBuffer);

        [Fact]
        public static void InvalidPathChars_MatchesGetInvalidPathChars()
        {
#pragma warning disable 0618
            Assert.NotNull(Path.InvalidPathChars);
            Assert.Equal(Path.GetInvalidPathChars(), Path.InvalidPathChars);
            Assert.Same(Path.InvalidPathChars, Path.InvalidPathChars);
#pragma warning restore 0618
        }
    }
}
