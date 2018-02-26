// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
    public class PathTests_Windows_NetFX : PathTestsBase
    {
        [Theory,
            MemberData(nameof(TestData_EmbeddedNull)),
            MemberData(nameof(TestData_EmptyString)),
            MemberData(nameof(TestData_ControlChars))]
        public void GetDirectoryName_ArgumentExceptions(string path)
        {
            // In NetFX we normalize and check invalid path chars
            AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetDirectoryName(path));
        }

        [Theory,
            MemberData(nameof(TestData_ControlChars)),
            MemberData(nameof(TestData_EmbeddedNull))]
        public void IsPathRooted_ArgumentExceptions(string path)
        {
            // In NetFX we check invalid path chars
            AssertExtensions.Throws<ArgumentException>("path", null, () => Path.IsPathRooted(path));
        }

        [Theory,
            MemberData(nameof(TestData_Spaces)),
            MemberData(nameof(TestData_UnicodeWhiteSpace)),
            MemberData(nameof(TestData_EmptyString))]
        public void IsPathRooted_NegativeCases(string path)
        {
            Assert.False(Path.IsPathRooted(path));
        }

        [Theory,
            MemberData(nameof(TestData_InvalidDriveLetters)),
            MemberData(nameof(TestData_ValidDriveLetters))]
        public void IsPathRooted(string path)
        {
            Assert.True(Path.IsPathRooted(path));
        }

        [Theory,
            InlineData(@" C:\dir\baz", @"C:\dir"),
            InlineData(@" C:\dir\baz", @"C:\dir")]
        public void GetDirectoryName_SkipSpaces(string path, string expected)
        {
            // In very specific cases we would trim leading spaces
            Assert.Equal(expected, Path.GetDirectoryName(path));
        }

        [Fact]
        public void GetPathRoot_EmptyThrows_Desktop()
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetPathRoot(string.Empty));
        }

        [Fact]
        public void GetInvalidPathChars()
        {
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

        [Theory, MemberData(nameof(TestData_NonDriveColonPaths))]
        public void GetFullPath_NotSupportedColons(string path)
        {
            // Throws via our invalid colon filtering (as part of FileIOPermissions)
            AssertExtensions.ThrowsAny<NotSupportedException, ArgumentException>(() => Path.GetFullPath(path));
        }

        [Theory,
            MemberData(nameof(TestData_Wildcards)),
            MemberData(nameof(TestData_ExtendedWildcards))]
        public void GetFullPath_Wildcards(char wildcard)
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetFullPath("test" + wildcard + "ing"));
        }

        [Theory, MemberData(nameof(TestData_InvalidUnc))]
        public void GetFullPath_UNC_Invalid(string invalidPath)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Path.GetFullPath(invalidPath));
        }
    }
}
