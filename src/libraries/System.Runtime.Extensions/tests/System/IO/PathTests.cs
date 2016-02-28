// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public static class PathTests
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

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
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
        [PlatformSpecific(PlatformID.Windows)]
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
        [PlatformSpecific(PlatformID.AnyUnix)]
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

        [PlatformSpecific(PlatformID.AnyUnix)]
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

        [PlatformSpecific(PlatformID.AnyUnix)]
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

        [PlatformSpecific(PlatformID.AnyUnix)]
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
        public static void GetPathRoot()
        {
            Assert.Null(Path.GetPathRoot(null));
            Assert.Equal(string.Empty, Path.GetPathRoot(string.Empty));

            string cwd = Directory.GetCurrentDirectory();
            Assert.Equal(cwd.Substring(0, cwd.IndexOf(Path.DirectorySeparatorChar) + 1), Path.GetPathRoot(cwd));
            Assert.True(Path.IsPathRooted(cwd));

            Assert.Equal(string.Empty, Path.GetPathRoot(@"file.exe"));
            Assert.False(Path.IsPathRooted("file.exe"));
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Theory]
        [InlineData(@"\\test\unc\path\to\something", @"\\test\unc")]
        [InlineData(@"\\a\b\c\d\e", @"\\a\b")]
        [InlineData(@"\\a\b\", @"\\a\b")]
        [InlineData(@"\\a\b", @"\\a\b")]
        [InlineData(@"\\test\unc", @"\\test\unc")]
        [InlineData(@"\\?\UNC\test\unc\path\to\something", @"\\?\UNC\test\unc")]
        [InlineData(@"\\?\UNC\test\unc", @"\\?\UNC\test\unc")]
        [InlineData(@"\\?\UNC\a\b", @"\\?\UNC\a\b")]
        [InlineData(@"\\?\UNC\a\b\", @"\\?\UNC\a\b")]
        [InlineData(@"\\?\C:\foo\bar.txt", @"\\?\C:\")]
        public static void GetPathRoot_Windows_UncAndExtended(string value, string expected)
        {
            Assert.True(Path.IsPathRooted(value));
            Assert.Equal(expected, Path.GetPathRoot(value));
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Theory]
        [InlineData(@"C:", @"C:")]
        [InlineData(@"C:\", @"C:\")]
        [InlineData(@"C:\\", @"C:\")]
        [InlineData(@"C://", @"C:\")]
        [InlineData(@"C:\foo", @"C:\")]
        [InlineData(@"C:\\foo", @"C:\")]
        [InlineData(@"C://foo", @"C:\")]
        public static void GetPathRoot_Windows(string value, string expected)
        {
            Assert.True(Path.IsPathRooted(value));
            Assert.Equal(expected, Path.GetPathRoot(value));
        }

        [PlatformSpecific(PlatformID.AnyUnix)]
        [Fact]
        public static void GetPathRoot_Unix()
        {
            // slashes are normal filename characters
            string uncPath = @"\\test\unc\path\to\something";
            Assert.False(Path.IsPathRooted(uncPath));
            Assert.Equal(string.Empty, Path.GetPathRoot(uncPath));
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
        public static void GetInvalidPathChars()
        {
            Assert.NotNull(Path.GetInvalidPathChars());
            Assert.NotSame(Path.GetInvalidPathChars(), Path.GetInvalidPathChars());
            Assert.Equal((IEnumerable<char>)Path.GetInvalidPathChars(), Path.GetInvalidPathChars());
            Assert.True(Path.GetInvalidPathChars().Length > 0);
            Assert.All(Path.GetInvalidPathChars(), c =>
            {
                string bad = c.ToString();
                Assert.Throws<ArgumentException>(() => Path.ChangeExtension(bad, "ok"));
                Assert.Throws<ArgumentException>(() => Path.Combine(bad, "ok"));
                Assert.Throws<ArgumentException>(() => Path.Combine("ok", "ok", bad));
                Assert.Throws<ArgumentException>(() => Path.Combine("ok", "ok", bad, "ok"));
                Assert.Throws<ArgumentException>(() => Path.Combine(bad, bad, bad, bad, bad));
                Assert.Throws<ArgumentException>(() => Path.GetDirectoryName(bad));
                Assert.Throws<ArgumentException>(() => Path.GetExtension(bad));
                Assert.Throws<ArgumentException>(() => Path.GetFileName(bad));
                Assert.Throws<ArgumentException>(() => Path.GetFileNameWithoutExtension(bad));
                Assert.Throws<ArgumentException>(() => Path.GetFullPath(bad));
                Assert.Throws<ArgumentException>(() => Path.GetPathRoot(bad));
                Assert.Throws<ArgumentException>(() => Path.IsPathRooted(bad));
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

        [PlatformSpecific(PlatformID.Windows)]
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

        [PlatformSpecific(PlatformID.AnyUnix)]
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

        [Fact]

        public static void GetFullPath_InvalidArgs()
        {
            Assert.Throws<ArgumentNullException>(() => Path.GetFullPath(null));
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(string.Empty));
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

        [PlatformSpecific(PlatformID.AnyUnix)]
        [Fact]
        public static void GetFullPath_Unix_Whitespace()
        {
            string curDir = Directory.GetCurrentDirectory();
            Assert.Equal("/ / ", Path.GetFullPath("/ // "));
            Assert.Equal(Path.Combine(curDir, "    "), Path.GetFullPath("    "));
            Assert.Equal(Path.Combine(curDir, "\r\n"), Path.GetFullPath("\r\n"));
        }

        [PlatformSpecific(PlatformID.AnyUnix)]
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

        [PlatformSpecific(PlatformID.Windows)]
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

            // Now no longer throws unless over ~32K
            Assert.NotNull(Path.GetFullPath(longPath.ToString()));
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public static void GetFullPath_Windows_AlternateDataStreamsNotSupported()
        {
            // Throws via our invalid colon filtering
            Assert.Throws<NotSupportedException>(() => Path.GetFullPath(@"bad:path"));
            Assert.Throws<NotSupportedException>(() => Path.GetFullPath(@"C:\some\bad:path"));
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public static void GetFullPath_Windows_URIFormatNotSupported()
        {
            // Throws via our invalid colon filtering
            Assert.Throws<NotSupportedException>(() => Path.GetFullPath("http://www.microsoft.com"));
            Assert.Throws<NotSupportedException>(() => Path.GetFullPath("file://www.microsoft.com"));
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Theory]
        [InlineData(@"\\?\GLOBALROOT\")]
        [InlineData(@"\\?\")]
        [InlineData(@"\\?\.")]
        [InlineData(@"\\?\..")]
        [InlineData(@"\\?\\")]
        [InlineData(@"\\?\C:\\")]
        [InlineData(@"\\?\C:\|")]
        [InlineData(@"\\?\C:\.")]
        [InlineData(@"\\?\C:\..")]
        [InlineData(@"\\?\C:\Foo\.")]
        [InlineData(@"\\?\C:\Foo\..")]
        public static void GetFullPath_Windows_ArgumentExceptionPaths(string path)
        {
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(path));
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Theory]
        [InlineData(@"bad::$DATA")]
        [InlineData(@"C  :")]
        [InlineData(@"C  :\somedir")]
        public static void GetFullPath_Windows_NotSupportedExceptionPaths(string path)
        {
            // Many of these used to throw ArgumentException despite being documented as NotSupportedException
            Assert.Throws<NotSupportedException>(() => Path.GetFullPath(path));
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Theory]
        [InlineData(@"C:...")]
        [InlineData(@"C:...\somedir")]
        [InlineData(@"\.. .\")]
        [InlineData(@"\. .\")]
        [InlineData(@"\ .\")]
        public static void GetFullPath_Windows_LegacyArgumentExceptionPaths(string path)
        {
            // These paths are legitimate Windows paths that can be created without extended syntax.
            // We now allow them through.
            Path.GetFullPath(path);
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public static void GetFullPath_Windows_MaxPathNotTooLong()
        {
            // Shouldn't throw anymore
            Path.GetFullPath(@"C:\" + new string('a', 255) + @"\");
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public static void GetFullPath_Windows_PathTooLong()
        {
            Assert.Throws<PathTooLongException>(() => Path.GetFullPath(@"C:\" + new string('a', short.MaxValue) + @"\"));
        }

        [PlatformSpecific(PlatformID.Windows)]
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

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public static void GetFullPath_Windows_StrangeButLegalPaths()
        {
            // These are legal and creatable without using extended syntax if you use a trailing slash
            // (such as "md ...\"). We used to filter these out, but now allow them to prevent apps from
            // being blocked when they hit these paths.
            string curDir = Directory.GetCurrentDirectory();
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

        [PlatformSpecific(PlatformID.Windows)]
        [Theory]
        [InlineData(@"\\?\C:\ ")]
        [InlineData(@"\\?\C:\ \ ")]
        [InlineData(@"\\?\C:\ .")]
        [InlineData(@"\\?\C:\ ..")]
        [InlineData(@"\\?\C:\...")]
        public static void GetFullPath_Windows_ValidExtendedPaths(string path)
        {
            Assert.Equal(path, Path.GetFullPath(path));
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Theory]
        [InlineData(@"\\server\share", @"\\server\share")]
        [InlineData(@"\\server\share", @" \\server\share")]
        [InlineData(@"\\server\share\dir", @"\\server\share\dir")]
        [InlineData(@"\\server\share", @"\\server\share\.")]
        [InlineData(@"\\server\share", @"\\server\share\..")]
        [InlineData(@"\\server\share\", @"\\server\share\    ")]
        [InlineData(@"\\server\  share\", @"\\server\  share\")]
        [InlineData(@"\\?\UNC\server\share", @"\\?\UNC\server\share")]
        [InlineData(@"\\?\UNC\server\share\dir", @"\\?\UNC\server\share\dir")]
        [InlineData(@"\\?\UNC\server\share\. ", @"\\?\UNC\server\share\. ")]
        [InlineData(@"\\?\UNC\server\share\.. ", @"\\?\UNC\server\share\.. ")]
        [InlineData(@"\\?\UNC\server\share\    ", @"\\?\UNC\server\share\    ")]
        [InlineData(@"\\?\UNC\server\  share\", @"\\?\UNC\server\  share\")]
        public static void GetFullPath_Windows_UNC_Valid(string expected, string input)
        {
            Assert.Equal(expected, Path.GetFullPath(input));
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Theory]
        [InlineData(@"\\")]
        [InlineData(@"\\server")]
        [InlineData(@"\\server\")]
        [InlineData(@"\\server\\")]
        [InlineData(@"\\server\..")]
        [InlineData(@"\\?\UNC\")]
        [InlineData(@"\\?\UNC\server")]
        [InlineData(@"\\?\UNC\server\")]
        [InlineData(@"\\?\UNC\server\\")]
        [InlineData(@"\\?\UNC\server\..")]
        [InlineData(@"\\?\UNC\server\share\.")]
        [InlineData(@"\\?\UNC\server\share\..")]
        [InlineData(@"\\?\UNC\a\b\\")]
        public static void GetFullPath_Windows_UNC_Invalid(string invalidPath)
        {
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(invalidPath));
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
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
                    // Make sure the shortened name expands back to the original one
                    Assert.Equal(tempFilePath, Path.GetFullPath(sb.ToString()));

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

        [PlatformSpecific(PlatformID.Windows)]
        [Theory]
        [InlineData('*')]
        [InlineData('?')]
        public static void GetFullPath_Windows_Wildcards(char wildcard)
        {
            Assert.Throws<ArgumentException>("path", () => Path.GetFullPath("test" + wildcard + "ing"));
        }

        // Windows-only P/Invoke to create 8.3 short names from long names
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        private static extern uint GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, int cchBuffer);
    }
}
