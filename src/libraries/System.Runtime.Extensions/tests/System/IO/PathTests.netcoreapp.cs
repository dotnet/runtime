// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.IO.Tests
{
    public partial class PathTests : PathTestsBase
    {
        [Fact]
        public void GetDirectoryName_EmptyReturnsNull()
        {
            // In NetFX this throws argument exception
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
            InlineData(@" C:\dir/baz", @" C:\dir"),
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
                    Assert.True(Path.GetFullPath(bad).EndsWith(bad));
                }
                Assert.Equal(string.Empty, Path.GetPathRoot(bad));
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
                Assert.Equal(string.Empty, new string(Path.GetPathRoot(bad.AsSpan())));
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
