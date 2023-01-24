// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class PathTests : PathTestsBase
    {
        [Theory,
            InlineData(null, null, null),
            InlineData(null, "exe", null),
            InlineData("", "", ""),
            InlineData("file.exe", null, "file"),
            InlineData("file.exe", "", "file."),
            InlineData("file", "exe", "file.exe"),
            InlineData("file", ".exe", "file.exe"),
            InlineData("file.txt", "exe", "file.exe"),
            InlineData("file.txt", ".exe", "file.exe"),
            InlineData("file.txt.bin", "exe", "file.txt.exe"),
            InlineData("dir/file.t", "exe", "dir/file.exe"),
            InlineData("dir/file.exe", "t", "dir/file.t"),
            InlineData("dir/file", "exe", "dir/file.exe")]
        public void ChangeExtension(string path, string newExtension, string expected)
        {
            if (expected != null)
                expected = expected.Replace('/', Path.DirectorySeparatorChar);
            if (path != null)
                path = path.Replace('/', Path.DirectorySeparatorChar);
            Assert.Equal(expected, Path.ChangeExtension(path, newExtension));
        }

        [Fact]
        public void GetDirectoryName_NullReturnsNull()
        {
            Assert.Null(Path.GetDirectoryName(null));
        }

        [Theory, MemberData(nameof(TestData_GetDirectoryName))]
        public void GetDirectoryName(string path, string expected)
        {
            Assert.Equal(expected, Path.GetDirectoryName(path));
        }

        [Fact]
        public void GetDirectoryName_CurrentDirectory()
        {
            string curDir = Directory.GetCurrentDirectory();
            Assert.Equal(curDir, Path.GetDirectoryName(Path.Combine(curDir, "baz")));

            Assert.Null(Path.GetDirectoryName(Path.GetPathRoot(curDir)));
            Assert.True(Path.GetDirectoryName(Path.GetPathRoot(curDir.AsSpan())).IsEmpty);
        }

        [Fact]
        public void GetExtension_Null()
        {
            Assert.Null(Path.GetExtension(null));
        }

        [Theory, MemberData(nameof(TestData_GetExtension))]
        public void GetExtension(string path, string expected)
        {
            Assert.Equal(expected, Path.GetExtension(path));
            Assert.Equal(!string.IsNullOrEmpty(expected), Path.HasExtension(path));
        }

        [Fact]
        public void GetFileName_Null()
        {
            Assert.Null(Path.GetFileName(null));
        }

        [Fact]
        public void GetFileName_Empty()
        {
            Assert.Empty(Path.GetFileName(string.Empty));
        }

        [Theory, MemberData(nameof(TestData_GetFileName))]
        public void GetFileName(string path, string expected)
        {
            Assert.Equal(expected, Path.GetFileName(path));
            Assert.Equal(expected, Path.GetFileName(Path.Combine("whizzle", path)));
        }

        [Fact]
        public void GetFileNameWithoutExtension_Null()
        {
            Assert.Null(Path.GetFileNameWithoutExtension(null));
        }

        [Theory, MemberData(nameof(TestData_GetFileNameWithoutExtension))]
        public void GetFileNameWithoutExtension(string path, string expected)
        {
            Assert.Equal(expected, Path.GetFileNameWithoutExtension(path));
        }

        [Fact]
        public void GetPathRoot_Null()
        {
            Assert.Null(Path.GetPathRoot(null));
        }

        [Fact]
        public void GetPathRoot_Basic()
        {
            string cwd = Directory.GetCurrentDirectory();
            string substring = cwd.Substring(0, cwd.IndexOf(Path.DirectorySeparatorChar) + 1);

            Assert.Equal(substring, Path.GetPathRoot(cwd));
            PathAssert.Equal(substring.AsSpan(), Path.GetPathRoot(cwd.AsSpan()));

            Assert.True(Path.IsPathRooted(cwd));

            Assert.Equal(string.Empty, Path.GetPathRoot(@"file.exe"));
            Assert.True(Path.GetPathRoot(@"file.exe".AsSpan()).IsEmpty);

            Assert.False(Path.IsPathRooted("file.exe"));
        }

        [Fact]
        public void GetInvalidPathChars_Invariants()
        {
            Assert.NotNull(Path.GetInvalidPathChars());
            Assert.NotSame(Path.GetInvalidPathChars(), Path.GetInvalidPathChars());
            Assert.Equal((IEnumerable<char>)Path.GetInvalidPathChars(), Path.GetInvalidPathChars());
            Assert.True(Path.GetInvalidPathChars().Length > 0);
        }

        [Fact]
        public void InvalidPathChars_MatchesGetInvalidPathChars()
        {
#pragma warning disable 0618
            Assert.NotNull(Path.InvalidPathChars);
            Assert.Equal(Path.GetInvalidPathChars(), Path.InvalidPathChars);
            Assert.Same(Path.InvalidPathChars, Path.InvalidPathChars);
#pragma warning restore 0618
        }

        [Fact]
        public void GetInvalidFileNameChars_Invariants()
        {
            Assert.NotNull(Path.GetInvalidFileNameChars());
            Assert.NotSame(Path.GetInvalidFileNameChars(), Path.GetInvalidFileNameChars());
            Assert.Equal((IEnumerable<char>)Path.GetInvalidFileNameChars(), Path.GetInvalidFileNameChars());
            Assert.True(Path.GetInvalidFileNameChars().Length > 0);
        }

        [Fact]
        public void GetRandomFileName()
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var fileNames = new HashSet<string>();
            for (int i = 0; i < 100; i++)
            {
                string s = Path.GetRandomFileName();
                Assert.Equal(8 + 1 + 3, s.Length);
                Assert.Equal('.', s[8]);
                Assert.Equal(-1, s.IndexOfAny(invalidChars));
                Assert.True(fileNames.Add(s));
            }
        }

        [Fact]
        public void GetTempPath_Default()
        {
            string tmpPath = Path.GetTempPath();
            Assert.False(string.IsNullOrEmpty(tmpPath));
            Assert.Equal(tmpPath, Path.GetTempPath());
            Assert.Equal(Path.DirectorySeparatorChar, tmpPath[tmpPath.Length - 1]);
            Assert.True(Directory.Exists(tmpPath));
        }

        [Fact]
        public void GetTempFileName()
        {
            string tmpFile = Path.GetTempFileName();
            try
            {
                Assert.True(File.Exists(tmpFile));
                string fileName = Path.GetFileName(tmpFile);
                Assert.StartsWith("tmp", fileName);
                Assert.Equal("tmpXXXXXX.tmp".Length, fileName.Length);
                Assert.EndsWith(".tmp", fileName);

                const string ValidChars = "abcdefghijklmnopqrstuvwxyz0123456789";
                for (int i = 3; i < 9; i++)
                {
                    // Unix allows upper and lower case
                    Assert.True(ValidChars.Contains(char.ToLowerInvariant(fileName[i])));
                }

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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetTempFileNameTempUnicode()
        {
            RemoteExecutor.Invoke(() =>
            {
                DirectoryInfo tempPathWithUnicode = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + "\u00F6"));
                tempPathWithUnicode.Create();

                string tempEnvVar = OperatingSystem.IsWindows() ? "TMP" : "TMPDIR";
                Environment.SetEnvironmentVariable(tempEnvVar, tempPathWithUnicode.FullName);
                
                try
                {
                    string tmpFile = Path.GetTempFileName();
                    Assert.True(File.Exists(tmpFile));
                    Assert.Equal(tempPathWithUnicode.FullName, Path.GetDirectoryName(tmpFile));

                    Environment.SetEnvironmentVariable(tempEnvVar, tempPathWithUnicode.Parent.FullName);
                }
                finally
                {
                    tempPathWithUnicode.Delete(recursive: true);
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetTempFileNameTempInvalidTemp()
        {
            // GetTempFileName has retries in it. It shouldn't hang in the face of a
            // bad temp path.
            RemoteExecutor.Invoke(() =>
            {
                string goodTemp = Path.GetTempPath();
                string tempEnvVar = OperatingSystem.IsWindows() ? "TMP" : "TMPDIR";

                try
                {
                    string badTemp = Path.GetTempFileName();
                    Assert.True(File.Exists(badTemp));

                    Environment.SetEnvironmentVariable(tempEnvVar, badTemp);

                    Assert.StartsWith(badTemp, Path.GetTempPath());

                    Assert.Throws<DirectoryNotFoundException>(() => Path.GetTempFileName()); // file not directory
                    File.Delete(badTemp);
                    Assert.Throws<DirectoryNotFoundException>(() => Path.GetTempFileName()); // non existent

                    if (OperatingSystem.IsWindows())
                    {
                        Environment.SetEnvironmentVariable(tempEnvVar, "|||");
                        Assert.Throws<IOException>(() => Path.GetTempFileName()); // invalid path
                    }
                }
                finally
                {
                    Environment.SetEnvironmentVariable(tempEnvVar, goodTemp);
                }
            }).Dispose();
        }

        [Fact]
        public void GetFullPath_InvalidArgs()
        {
            Assert.Throws<ArgumentNullException>(() => Path.GetFullPath(null));
            AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetFullPath(string.Empty));
        }

        public static TheoryData<string, string> GetFullPath_BasicExpansions
        {
            get
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                string root = Path.GetPathRoot(currentDirectory);
                string fileName = Path.GetFileName(currentDirectory);

                TheoryData<string, string> data = new TheoryData<string, string>
                {
                    // Current directory => current directory
                    { currentDirectory, currentDirectory },
                    // "." => current directory
                    { ".", currentDirectory },
                    // "dir/./././." => "dir"
                    { Path.Combine(currentDirectory, ".", ".", ".", ".", "."), currentDirectory },
                    // "dir///." => "dir"
                    { currentDirectory + new string(Path.DirectorySeparatorChar, 3) + ".", currentDirectory },
                    // "dir/../dir/./../dir" => "dir"
                    { Path.Combine(currentDirectory, "..", fileName, ".", "..", fileName), currentDirectory },
                    // "C:\somedir\.." => "C:\"
                    { Path.Combine(root, "somedir", ".."), root },
                    // "C:\." => "C:\"
                    { Path.Combine(root, "."), root },
                    // "C:\..\..\..\.." => "C:\"
                    { Path.Combine(root, "..", "..", "..", ".."), root },
                    // "C:\\\" => "C:\"
                    { root + new string(Path.DirectorySeparatorChar, 3), root },
                };

                if (currentDirectory != Path.GetPathRoot(currentDirectory))
                {
                    // ".." => up a directory
                    data.Add("..", Path.GetDirectoryName(currentDirectory));
                }

                // Path longer than MaxPath that normalizes down to less than MaxPath
                const int Iters = 10000;
                var longPath = new StringBuilder(currentDirectory, currentDirectory.Length + (Iters * 2));
                for (int i = 0; i < 10000; i++)
                {
                    longPath.Append(Path.DirectorySeparatorChar).Append('.');
                }
                data.Add(longPath.ToString(), currentDirectory);

                return data;
            }
        }

        public static TheoryData<string, string> GetFullPath_TildePaths
        {
            get
            {
                // Paths with tildes '~' are processed for 8.3 expansion on Windows
                string currentDirectory = Directory.GetCurrentDirectory();
                string root = Path.GetPathRoot(currentDirectory);

                TheoryData<string, string> data = new TheoryData<string, string>
                {
                    { "~", Path.Combine(currentDirectory, "~") },
                    { Path.Combine(root, "~"), Path.Combine(root, "~") }
                };

                return data;
            }
        }

        [Theory,
            MemberData(nameof(GetFullPath_BasicExpansions)),
            MemberData(nameof(GetFullPath_TildePaths))]
        public void GetFullPath_CoreTests(string path, string expected)
        {
            Assert.Equal(expected, Path.GetFullPath(path));
        }

        [Fact]
        public void GetDirectoryName_EmptyReturnsNull()
        {
            // In .NET Framework this throws argument exception
            Assert.Null(Path.GetDirectoryName(string.Empty));
        }

        [Theory, MemberData(nameof(TestData_Spaces))]
        public void GetDirectoryName_Spaces(string path)
        {
            if (PlatformDetection.IsWindows)
            {
                // In Windows spaces are eaten by Win32, making them effectively empty
                Assert.Null(Path.GetDirectoryName(path));
            }
            else
            {
                Assert.Empty(Path.GetDirectoryName(path));
            }
        }

        [Theory, MemberData(nameof(TestData_Spaces))]
        public void GetDirectoryName_Span_Spaces(string path)
        {
            PathAssert.Empty(Path.GetDirectoryName(path.AsSpan()));
        }

        [Theory,
            MemberData(nameof(TestData_EmbeddedNull)),
            MemberData(nameof(TestData_ControlChars)),
            MemberData(nameof(TestData_UnicodeWhiteSpace))]
        public void GetDirectoryName_NetFxInvalid(string path)
        {
            Assert.Empty(Path.GetDirectoryName(path));
            Assert.Equal(path, Path.GetDirectoryName(Path.Combine(path, path)));
            PathAssert.Empty(Path.GetDirectoryName(path.AsSpan()));
            PathAssert.Equal(path, new string(Path.GetDirectoryName(Path.Combine(path, path).AsSpan())));
        }

        [Theory, MemberData(nameof(TestData_GetDirectoryName))]
        public void GetDirectoryName_Span(string path, string expected)
        {
            PathAssert.Equal(expected ?? ReadOnlySpan<char>.Empty, Path.GetDirectoryName(path.AsSpan()));
        }

        [Fact]
        public void GetDirectoryName_Span_CurrentDirectory()
        {
            string curDir = Directory.GetCurrentDirectory();
            PathAssert.Equal(curDir, Path.GetDirectoryName(Path.Combine(curDir, "baz").AsSpan()));
            PathAssert.Empty(Path.GetDirectoryName(Path.GetPathRoot(curDir).AsSpan()));
        }

        [Theory,
            InlineData(@" C:\dir/baz", @" C:\dir")]
        public void GetDirectoryName_SkipSpaces(string path, string expected)
        {
            // We no longer trim leading spaces for any path
            Assert.Equal(expected, Path.GetDirectoryName(path));
        }

        [Theory, MemberData(nameof(TestData_GetExtension))]
        public void GetExtension_Span(string path, string expected)
        {
            PathAssert.Equal(expected, Path.GetExtension(path.AsSpan()));
            Assert.Equal(!string.IsNullOrEmpty(expected), Path.HasExtension(path.AsSpan()));
        }

        [Theory, MemberData(nameof(TestData_GetFileName))]
        public void GetFileName_Span(string path, string expected)
        {
            PathAssert.Equal(expected, Path.GetFileName(path.AsSpan()));
        }

        public static IEnumerable<object[]> TestData_GetFileName_Volume()
        {
            yield return new object[] { ":", ":" };
            yield return new object[] { ".:", ".:" };
            yield return new object[] { ".:.", ".:." };     // Not a valid drive letter
            yield return new object[] { "file:", "file:" };
            yield return new object[] { ":file", ":file" };
            yield return new object[] { "file:exe", "file:exe" };
            yield return new object[] { Path.Combine("baz", "file:exe"), "file:exe" };
            yield return new object[] { Path.Combine("bar", "baz", "file:exe"), "file:exe" };
        }

        [Theory, MemberData(nameof(TestData_GetFileName_Volume))]
        public void GetFileName_Volume(string path, string expected)
        {
            // We used to break on ':' on Windows. This is a valid file name character for alternate data streams.
            // Additionally the character can show up on unix volumes mounted to Windows.
            Assert.Equal(expected, Path.GetFileName(path));
            PathAssert.Equal(expected, Path.GetFileName(path.AsSpan()));
        }

        [Theory, MemberData(nameof(TestData_GetFileNameWithoutExtension))]
        public void GetFileNameWithoutExtension_Span(string path, string expected)
        {
            PathAssert.Equal(expected, Path.GetFileNameWithoutExtension(path.AsSpan()));
        }

        [Fact]
        public void GetPathRoot_Empty()
        {
            Assert.Null(Path.GetPathRoot(string.Empty));
        }

        [Fact]
        public void GetPathRoot_Empty_Span()
        {
            PathAssert.Empty(Path.GetPathRoot(ReadOnlySpan<char>.Empty));
        }

        [Theory,
            InlineData(nameof(TestData_Spaces)),
            InlineData(nameof(TestData_ControlChars)),
            InlineData(nameof(TestData_EmbeddedNull)),
            InlineData(nameof(TestData_InvalidDriveLetters)),
            InlineData(nameof(TestData_UnicodeWhiteSpace)),
            InlineData(nameof(TestData_EmptyString))]
        public void IsPathRooted_NegativeCases(string path)
        {
            Assert.False(Path.IsPathRooted(path));
            Assert.False(Path.IsPathRooted(path.AsSpan()));
        }

        [Fact]
        public void GetInvalidPathChars()
        {
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
                if (bad[0] == '\0')
                {
                    Assert.Throws<ArgumentException>("path", () => Path.GetFullPath(bad));
                }
                else
                {
                    Assert.EndsWith(bad, Path.GetFullPath(bad));
                }
                Assert.Equal(string.Empty, Path.GetPathRoot(bad));
                Assert.True(Path.GetPathRoot(bad.AsSpan()).IsEmpty);
                Assert.False(Path.IsPathRooted(bad));
            });
        }

        [Fact]
        public void GetInvalidPathChars_Span()
        {
            Assert.All(Path.GetInvalidPathChars(), c =>
            {
                string bad = c.ToString();
                Assert.Equal(string.Empty, new string(Path.GetDirectoryName(bad.AsSpan())));
                Assert.Equal(string.Empty, new string(Path.GetExtension(bad.AsSpan())));
                Assert.Equal(bad, new string(Path.GetFileName(bad.AsSpan())));
                Assert.Equal(bad, new string(Path.GetFileNameWithoutExtension(bad.AsSpan())));
                Assert.True(Path.GetPathRoot(bad.AsSpan()).IsEmpty);
                Assert.False(Path.IsPathRooted(bad.AsSpan()));
            });
        }

        [Theory,
            InlineData("http://www.microsoft.com"),
            InlineData("file://somefile")]
        public void GetFullPath_URIsAsFileNames(string uriAsFileName)
        {
            // URIs are valid filenames, though the multiple slashes will be consolidated in GetFullPath
            Assert.Equal(
                Path.Combine(Directory.GetCurrentDirectory(), uriAsFileName.Replace("//", Path.DirectorySeparatorChar.ToString())),
                Path.GetFullPath(uriAsFileName));
        }

        [Theory, MemberData(nameof(TestData_NonDriveColonPaths))]
        public void GetFullPath_NowSupportedColons(string path)
        {
            // Used to throw on Windows, now should never throw
            Path.GetFullPath(path);
        }

        [Theory, MemberData(nameof(TestData_InvalidUnc))]
        public static void GetFullPath_UNC_Invalid(string path)
        {
            // These UNCs used to throw on Windows
            Path.GetFullPath(path);
        }

        [Theory,
            MemberData(nameof(TestData_Wildcards)),
            MemberData(nameof(TestData_ExtendedWildcards))]
        public void GetFullPath_Wildcards(string wildcard)
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + wildcard + "ing");
            Assert.Equal(path, Path.GetFullPath(path));
        }

        public static TheoryData<string, string, string> GetFullPathBasePath_ArgumentNullException => new TheoryData<string, string, string>
        {
            { @"", null, "basePath" },
            { @"tmp",null, "basePath" },
            { @"\home", null, "basePath"},
            { null, @"foo\bar", "path"},
            { null, @"foo\bar", "path"},
        };

        [Theory,
            MemberData(nameof(GetFullPathBasePath_ArgumentNullException))]
        public static void GetFullPath_BasePath_NullInput(string path, string basePath, string paramName)
        {
            Assert.Throws<ArgumentNullException>(paramName, () => Path.GetFullPath(path, basePath));
        }

        public static TheoryData<string, string, string> GetFullPathBasePath_ArgumentException => new TheoryData<string, string, string>
        {
            { @"", @"foo\bar", "basePath"},
            { @"tmp", @"foo\bar", "basePath"},
            { @"\home", @"foo\bar", "basePath"},
        };

        [Theory,
            MemberData(nameof(GetFullPathBasePath_ArgumentException))]
        public static void GetFullPath_BasePath_Input(string path, string basePath, string paramName)
        {
            Assert.Throws<ArgumentException>(paramName, () => Path.GetFullPath(path, basePath));
        }
    }
}
