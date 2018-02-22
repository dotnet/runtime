// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.IO.Tests
{
    public static partial class PathTests
    {
        [Theory, MemberData(nameof(GetDirectoryName_NonControl_Test_Data))]
        public static void GetDirectoryName_NonControl_Span(string path)
        {
            Assert.Equal(string.Empty, new string(Path.GetDirectoryName(path.AsSpan())));
        }

        [Theory, MemberData(nameof(GetDirectoryName_NonControl_Test_Data))]
        public static void GetDirectoryName_NonControlWithSeparator_Span(string path)
        {
            Assert.Equal(path, new string(Path.GetDirectoryName(Path.Combine(path, path).AsSpan())));
        }

        [Theory, MemberData(nameof(GetDirectoryName_Test_Data))]
        public static void GetDirectoryName_Span(string path, string expected)
        {
            if (path != null)
                Assert.Equal(expected, new string(Path.GetDirectoryName(path.AsSpan())));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific paths
        [Theory, MemberData(nameof(GetDirectoryName_Windows_Test_Data))]
        public static void GetDirectoryName_Windows_Span(string path, string expected)
        {
            Assert.Equal(expected ?? string.Empty, new string(Path.GetDirectoryName(path.AsSpan())));
        }

        [Fact]
        public static void GetDirectoryName_CurrentDirectory_Span()
        {
            string curDir = Directory.GetCurrentDirectory();
            Assert.Equal(curDir, new string(Path.GetDirectoryName(Path.Combine(curDir, "baz").AsSpan())));
            Assert.Equal(string.Empty, new string(Path.GetDirectoryName(Path.GetPathRoot(curDir).AsSpan())));
        }




        [Theory, MemberData(nameof(GetExtension_Test_Data))]
        public static void GetExtension_Span(string path, string expected)
        {
            if (path != null)
            {
                path = path.Replace('/', Path.DirectorySeparatorChar);

                Assert.Equal(expected, new string(Path.GetExtension(path.AsSpan())));
                Assert.Equal(!string.IsNullOrEmpty(expected), Path.HasExtension(path.AsSpan()));
            }
        }

        public static IEnumerable<object[]> GetFileName_VolumeTestData()
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

        [ActiveIssue(27269)]
        [Theory]
        [MemberData(nameof(GetFileName_VolumeTestData))]
        public static void GetFileName_Volume(string path, string expected)
        {
            // We used to break on ':' on Windows. This is a valid file name character for alternate data streams.
            // Additionally the character can show up on unix volumes mounted to Windows.
            Assert.Equal(expected, Path.GetFileName(path));
            Assert.Equal(expected, new string(Path.GetFileName(path.AsSpan())));
        }

        [ActiveIssue(27269)]
        [Theory]
        [InlineData("B:", "")]
        [InlineData("A:.", ".")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void GetFileName_Volume_Windows(string path, string expected)
        {
            // With a valid drive letter followed by a colon, we have a root, but only on Windows.
            Assert.Equal(expected, Path.GetFileName(path));
            Assert.Equal(expected, new string(Path.GetFileName(path.AsSpan())));
        }

        [Theory]
        [InlineData("B:", "B:")]
        [InlineData("A:.", "A:.")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void GetFileName_Volume_Unix(string path, string expected)
        {
            // No such thing as a drive relative path on Unix.
            Assert.Equal(expected, Path.GetFileName(path));
            Assert.Equal(expected, new string(Path.GetFileName(path.AsSpan())));
        }

        [Theory]
        [MemberData(nameof(GetFileName_TestData))]
        public static void GetFileName_Span(string path, string expected)
        {
            if (path != null)
                Assert.Equal(expected, new string(Path.GetFileName(path.AsSpan())));
        }

        [Theory]
        [MemberData(nameof(GetFileNameWithoutExtension_TestData))]
        public static void GetFileNameWithoutExtension_Span(string path, string expected)
        {
            if(path != null)
                Assert.Equal(expected, new string(Path.GetFileNameWithoutExtension(path.AsSpan())));
        }

        [Fact]
        public static void GetPathRoot_Empty_Span()
        {
            Assert.Equal(string.Empty, new string(Path.GetPathRoot(ReadOnlySpan<char>.Empty)));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests UNC
        [Theory, MemberData(nameof(GetPathRoot_Windows_UncAndExtended_Test_Data))]
        public static void GetPathRoot_Windows_UncAndExtended_Span(string value, string expected)
        {
            Assert.True(Path.IsPathRooted(value.AsSpan()));
            Assert.Equal(expected, new string(Path.GetPathRoot(value.AsSpan())));
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Theory, MemberData(nameof(GetPathRoot_Windows_UncAndExtended_WithLegacySupport_Test_Data))]
        public static void GetPathRoot_Windows_UncAndExtended_WithLegacySupport_Span(string normalExpected, string legacyExpected, string value)
        {
            Assert.True(Path.IsPathRooted(value.AsSpan()));

            string expected = PathFeatures.IsUsingLegacyPathNormalization() ? legacyExpected : normalExpected;
            Assert.Equal(expected, new string(Path.GetPathRoot(value.AsSpan())));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific path convention
        [Theory, MemberData(nameof(GetPathRoot_Windows_Test_Data))]
        public static void GetPathRoot_Windows_Span(string value, string expected)
        {
            Assert.True(Path.IsPathRooted(value.AsSpan()));
            Assert.Equal(expected, new string(Path.GetPathRoot(value.AsSpan())));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public static void IsPathRooted_Span(string path)
        {
            Assert.False(Path.IsPathRooted(path.AsSpan()));
        }

        // Testing invalid drive letters !(a-zA-Z)
        [PlatformSpecific(TestPlatforms.Windows)]
        [Theory, MemberData(nameof(IsPathRooted_Windows_Invalid_Test_Data))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Bug fixed on Core where it would return true if the first char is not a drive letter followed by a VolumeSeparatorChar coreclr/10297")]
        public static void IsPathRooted_Windows_Invalid_Span(string value)
        {
            Assert.False(Path.IsPathRooted(value.AsSpan()));
        }

        [Fact]
        public static void GetInvalidPathChars_Span()
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

        [Theory, MemberData(nameof(GetDirectoryName_Unix_Test_Data))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests Unix-specific paths
        public static void GetDirectoryName_Unix_Span(string path, string expected)
        {
            Assert.Equal(expected ?? string.Empty, new string(Path.GetDirectoryName(path.AsSpan())));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks Unix-specific special characters in directory path
        [Theory, MemberData(nameof(GetDirectoryName_ControlCharacters_Unix_Test_Data))]
        public static void GetDirectoryName_ControlCharacters_Unix_Span(char ch, int count, string file)
        {
            Assert.Equal(new string(ch, count), new string(Path.GetDirectoryName(Path.Combine(new string(ch, count), file).AsSpan())));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks file extension behavior on Unix
        [Theory, MemberData(nameof(GetExtension_Unix_Test_Data))]
        public static void GetExtension_Unix_Span(string path, string expected)
        {
            Assert.Equal(expected, new string(Path.GetExtension(path.AsSpan())));
            Assert.Equal(!string.IsNullOrEmpty(expected), Path.HasExtension(path.AsSpan()));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests Unix-specific valid file names
        [Theory, MemberData(nameof(GetFileName_Unix_Test_Data))]
        public static void GetFileName_Unix_Span(string file)
        {
            Assert.Equal(file, new string(Path.GetFileName(file).AsSpan()));
        }

        [Fact]
        public static void GetFileNameWithSpaces_Unix_Span()
        {
            Assert.Equal("fi  le", new string(Path.GetFileName(Path.Combine("b \r\n ar", "fi  le").AsSpan())));
        }
    }
}
