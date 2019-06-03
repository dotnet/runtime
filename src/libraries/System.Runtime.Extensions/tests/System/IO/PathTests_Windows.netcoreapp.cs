// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public partial class PathTests_Windows : PathTestsBase
    {
        [Theory,
            MemberData(nameof(TestData_GetPathRoot_Windows)),
            MemberData(nameof(TestData_GetPathRoot_Unc)),
            MemberData(nameof(TestData_GetPathRoot_DevicePaths))]
        public void GetPathRoot_Span(string value, string expected)
        {
            Assert.Equal(expected, new string(Path.GetPathRoot(value.AsSpan())));
            Assert.True(Path.IsPathRooted(value.AsSpan()));
        }

        [Theory, MemberData(nameof(TestData_UnicodeWhiteSpace))]
        public void GetFullPath_UnicodeWhiteSpaceStays(string component)
        {
            // When not NetFX full path should not cut off component
            string path = "C:\\Test" + component;
            Assert.Equal(path, Path.GetFullPath(path));
        }

        [Theory, MemberData(nameof(TestData_Periods))]
        public void GetFullPath_TrailingPeriodsCut(string component)
        {
            // Windows cuts off any simple white space added to a path
            string path = "C:\\Test" + component;
            Assert.Equal("C:\\Test", Path.GetFullPath(path));
        }

        public static TheoryData<string, string, string> GetFullPath_Windows_FullyQualified => new TheoryData<string, string, string>
        {
            { @"C:\git\corefx", @"C:\git\corefx", @"C:\git\corefx" },
            { @"C:\git\corefx.\.\.\.\.\.", @"C:\git\corefx", @"C:\git\corefx" },
            { @"C:\git\corefx\\\.", @"C:\git\corefx", @"C:\git\corefx" },
            { @"C:\git\corefx\..\corefx\.\..\corefx", @"C:\git\corefx", @"C:\git\corefx" },
            { @"C:\somedir\..", @"C:\git\corefx", @"C:\" },
            { @"C:\", @"C:\git\corefx", @"C:\" },
            { @"..\..\..\..", @"C:\git\corefx", @"C:\" },
            { @"C:\\\", @"C:\git\corefx", @"C:\" },
            { @"C:\..\..\", @"C:\git\corefx", @"C:\" },
            { @"C:\..\git\..\.\", @"C:\git\corefx", @"C:\" },
            { @"C:\git\corefx\..\..\..\", @"C:\git\corefx", @"C:\" },
            { @"C:\.\corefx\", @"C:\git\corefx", @"C:\corefx\" },
        };

        [Theory,
            MemberData(nameof(GetFullPath_Windows_FullyQualified))]
        public void GetFullPath_BasicExpansions_Windows(string path, string basePath, string expected)
        {
            Assert.Equal(expected, Path.GetFullPath(path, basePath));
        }

        public static TheoryData<string, string, string> GetFullPath_Windows_PathIsDevicePath => new TheoryData<string, string, string>
        {
            // Device Paths with \\?\ wont get normalized i.e. relative segments wont get removed.
            { @"\\?\C:\git\corefx.\.\.\.\.\.", @"C:\git\corefx", @"\\?\C:\git\corefx.\.\.\.\.\." },
            { @"\\?\C:\git\corefx\\\.", @"C:\git\corefx", @"\\?\C:\git\corefx\\\." },
            { @"\\?\C:\git\corefx\..\corefx\.\..\corefx", @"C:\git\corefx", @"\\?\C:\git\corefx\..\corefx\.\..\corefx" },
            { @"\\?\\somedir\..", @"C:\git\corefx", @"\\?\\somedir\.." },
            { @"\\?\", @"C:\git\corefx", @"\\?\" },
            { @"\\?\..\..\..\..", @"C:\git\corefx", @"\\?\..\..\..\.." },
            { @"\\?\\\\" , @"C:\git\corefx", @"\\?\\\\" },
            { @"\\?\C:\Foo." , @"C:\git\corefx", @"\\?\C:\Foo." },
            { @"\\?\C:\Foo " , @"C:\git\corefx", @"\\?\C:\Foo " },

            { @"\\.\C:\git\corefx.\.\.\.\.\.", @"C:\git\corefx", @"\\.\C:\git\corefx" },
            { @"\\.\C:\git\corefx\\\.", @"C:\git\corefx", @"\\.\C:\git\corefx" },
            { @"\\.\C:\git\corefx\..\corefx\.\..\corefx", @"C:\git\corefx", @"\\.\C:\git\corefx" },
            { @"\\.\\somedir\..", @"C:\git\corefx", @"\\.\" },
            { @"\\.\", @"C:\git\corefx", @"\\.\" },
            { @"\\.\..\..\..\..", @"C:\git\corefx", @"\\.\" },
            { @"\\.\", @"C:\git\corefx", @"\\.\" },
            { @"\\.\C:\Foo." , @"C:\git\corefx", @"\\.\C:\Foo" },
            { @"\\.\C:\Foo " , @"C:\git\corefx", @"\\.\C:\Foo" },
        };

        [Theory,
            MemberData(nameof(GetFullPath_Windows_PathIsDevicePath))]
        public void GetFullPath_BasicExpansions_Windows_PathIsDevicePath(string path, string basePath, string expected)
        {
            Assert.Equal(expected, Path.GetFullPath(path, basePath));
            Assert.Equal(expected, Path.GetFullPath(path, @"\\.\" + basePath));
            Assert.Equal(expected, Path.GetFullPath(path, @"\\?\" + basePath));
        }

        public static TheoryData<string, string, string> GetFullPath_Windows_UNC => new TheoryData<string, string, string>
        {
            { @"foo", @"", @"foo" },
            { @"foo", @"server1", @"server1\foo" },
            { @"\foo", @"server2", @"server2\foo" },
            { @"foo", @"server3\", @"server3\foo" },
            { @"..\foo", @"server4", @"server4\..\foo" },
            { @".\foo", @"server5\share", @"server5\share\foo" },
            { @"..\foo", @"server6\share", @"server6\share\foo" },
            { @"\foo", @"a\b\\", @"a\b\foo" },
            { @"foo", @"LOCALHOST\share8\test.txt.~SS", @"LOCALHOST\share8\test.txt.~SS\foo" },
            { @"foo", @"LOCALHOST\share9", @"LOCALHOST\share9\foo" },
            { @"foo", @"LOCALHOST\shareA\dir", @"LOCALHOST\shareA\dir\foo" },
            { @". \foo", @"LOCALHOST\shareB\", @"LOCALHOST\shareB\. \foo" },
            { @".. \foo", @"LOCALHOST\shareC\", @"LOCALHOST\shareC\.. \foo" },
            { @"    \foo", @"LOCALHOST\shareD\", @"LOCALHOST\shareD\    \foo" },

            { "foo", @"LOCALHOST\  shareE\", @"LOCALHOST\  shareE\foo" },
            { "foo", @"LOCALHOST\shareF\test.txt.~SS", @"LOCALHOST\shareF\test.txt.~SS\foo" },
            { "foo", @"LOCALHOST\shareG", @"LOCALHOST\shareG\foo" },
            { "foo", @"LOCALHOST\shareH\dir", @"LOCALHOST\shareH\dir\foo" },
            { "foo", @"LOCALHOST\shareK\", @"LOCALHOST\shareK\foo" },
            { "foo", @"LOCALHOST\  shareL\", @"LOCALHOST\  shareL\foo" },

            // Relative segments eating into the root
            { @".\..\foo\..\", @"server\share", @"server\share\" },
            { @"..\foo\tmp\..\..\", @"server\share", @"server\share\" },
            { @"..\..\..\foo", @"server\share", @"server\share\foo" },
            { @"..\foo\..\..\tmp", @"server\share", @"server\share\tmp" },
            { @"..\foo", @"server\share", @"server\share\foo" },
            { @"...\\foo", @"server\share", @"server\share\...\foo" },
            { @"...\..\.\foo", @"server\share", @"server\share\foo" },
            { @"..\foo\tmp\..\..\..\..\..\", @"server\share", @"server\share\" },
            { @"..\..\..\..\foo", @"server\share", @"server\share\foo" },
        };

        [Theory,
           MemberData(nameof(GetFullPath_Windows_UNC))]
        public void GetFullPath_CommonUnc_Windows(string path, string basePath, string expected)
        {
            Assert.Equal(@"\\" + expected, Path.GetFullPath(path, @"\\" + basePath));
            Assert.Equal(@"\\.\UNC\" + expected, Path.GetFullPath(path, @"\\.\UNC\" + basePath));
            Assert.Equal(@"\\?\UNC\" + expected, Path.GetFullPath(path, @"\\?\UNC\" + basePath));
        }

        public static TheoryData<string, string, string> GetFullPath_Windows_CommonDevicePaths => new TheoryData<string, string, string>
        {
            // Device paths
            { "foo", @"C:\ ", @"C:\ \foo" },
            { @" \ \foo", @"C:\", @"C:\ \ \foo" },
            { @" .\foo", @"C:\", @"C:\ .\foo" },
            { @" ..\foo", @"C:\", @"C:\ ..\foo" },
            { @"...\foo", @"C:\", @"C:\...\foo" },

            { @"foo", @"C:\\", @"C:\foo" },
            { @"foo.", @"C:\\", @"C:\foo." },
            { @"foo \git", @"C:\\", @"C:\foo \git" },
            { @"foo. \git", @"C:\\", @"C:\foo. \git" },
            { @" foo \git", @"C:\\", @"C:\ foo \git" },
            { @"foo ", @"C:\\", @"C:\foo " },
            { @"|\foo", @"C:\", @"C:\|\foo" },
            { @".\foo", @"C:\", @"C:\foo" },
            { @"..\foo", @"C:\", @"C:\foo" },

            { @"\Foo1\.\foo", @"C:\", @"C:\Foo1\foo" },
            { @"\Foo2\..\foo", @"C:\", @"C:\foo" },

            { @"foo", @"GLOBALROOT\", @"GLOBALROOT\foo" },
            { @"foo", @"", @"foo" },
            { @".\foo", @"", @".\foo" },
            { @"..\foo", @"", @"..\foo" },
            { @"C:", @"", @"C:\"},

            // Relative segments eating into the root
            { @"foo", @"GLOBALROOT\", @"GLOBALROOT\foo" },
            { @"..\..\foo\..\..\", @"", @"..\" },
            { @".\..\..\..\..\foo", @"", @".\foo" },
            { @"..\foo\..\..\..\", @"", @"..\" },
            { @"\.\.\..\", @"C:\", @"C:\"},
            { @"..\..\..\foo", @"GLOBALROOT\", @"GLOBALROOT\foo" },
            { @"foo\..\..\", @"", @"foo\" },
            { @".\.\foo\..\", @"", @".\" },
        };

        [Theory,
           MemberData(nameof(GetFullPath_Windows_CommonDevicePaths))]
        public void GetFullPath_CommonDevice_Windows(string path, string basePath, string expected)
        {
            Assert.Equal(@"\\.\" + expected, Path.GetFullPath(path, @"\\.\" + basePath));
            Assert.Equal(@"\\?\" + expected, Path.GetFullPath(path, @"\\?\" + basePath));            
        }

        public static TheoryData<string, string, string> GetFullPath_CommonRootedWindowsData => new TheoryData<string, string, string>
        {
            { "", @"C:\git\corefx", @"C:\git\corefx" },
            { "..", @"C:\git\corefx", @"C:\git" },

            // Current drive rooted
            { @"\tmp\bar", @"C:\git\corefx", @"C:\tmp\bar" },
            { @"\.\bar", @"C:\git\corefx", @"C:\bar" },
            { @"\tmp\..", @"C:\git\corefx", @"C:\" },
            { @"\tmp\bar\..", @"C:\git\corefx", @"C:\tmp" },
            { @"\tmp\bar\..", @"C:\git\corefx", @"C:\tmp" },
            { @"\", @"C:\git\corefx", @"C:\" },

            { @"..\..\tmp\bar", @"C:\git\corefx", @"C:\tmp\bar" },
            { @"..\..\.\bar", @"C:\git\corefx", @"C:\bar" },
            { @"..\..\..\..\tmp\..", @"C:\git\corefx", @"C:\" },
            { @"\tmp\..\bar..\..\..", @"C:\git\corefx", @"C:\" },
            { @"\tmp\..\bar\..", @"C:\git\corefx", @"C:\" },
            { @"\.\.\..\..\", @"C:\git\corefx", @"C:\" },

            // Specific drive rooted
            { @"C:tmp\foo\..", @"C:\git\corefx", @"C:\git\corefx\tmp" },
            { @"C:tmp\foo\.", @"C:\git\corefx", @"C:\git\corefx\tmp\foo" },
            { @"C:tmp\foo\..", @"C:\git\corefx", @"C:\git\corefx\tmp" },
            { @"C:tmp", @"C:\git\corefx", @"C:\git\corefx\tmp" },
            { @"C:", @"C:\git\corefx", @"C:\git\corefx" },
            { @"C", @"C:\git\corefx", @"C:\git\corefx\C" },

            { @"Z:tmp\foo\..", @"C:\git\corefx", @"Z:\tmp" },
            { @"Z:tmp\foo\.", @"C:\git\corefx", @"Z:\tmp\foo" },
            { @"Z:tmp\foo\..", @"C:\git\corefx", @"Z:\tmp" },
            { @"Z:tmp", @"C:\git\corefx", @"Z:\tmp" },
            { @"Z:", @"C:\git\corefx", @"Z:\" },
            { @"Z", @"C:\git\corefx", @"C:\git\corefx\Z" },

            // Relative segments eating into the root
            { @"C:..\..\..\tmp\foo\..", @"C:\git\corefx", @"C:\tmp" },
            { @"C:tmp\..\..\foo\.", @"C:\git\corefx", @"C:\git\foo" },
            { @"C:..\..\tmp\foo\..", @"C:\git\corefx", @"C:\tmp" },
            { @"C:tmp\..\", @"C:\git\corefx", @"C:\git\corefx\" },
            { @"C:", @"C:\git\corefx", @"C:\git\corefx" },
            { @"C", @"C:\git\corefx", @"C:\git\corefx\C" },

            { @"C:tmp\..\..\..\..\foo\..", @"C:\git\corefx", @"C:\" },
            { @"C:tmp\..\..\foo\.", @"C:\", @"C:\foo" },
            { @"C:..\..\tmp\..\foo\..", @"C:\", @"C:\" },
            { @"C:tmp\..\", @"C:\", @"C:\" },

            { @"Z:tmp\foo\..", @"C:\git\corefx", @"Z:\tmp" },
            { @"Z:tmp\foo\.", @"C:\git\corefx", @"Z:\tmp\foo" },
            { @"Z:tmp\foo\..", @"C:\git\corefx", @"Z:\tmp" },
            { @"Z:tmp", @"C:\git\corefx", @"Z:\tmp" },
            { @"Z:", @"C:\git\corefx", @"Z:\" },
            { @"Z", @"C:\git\corefx", @"C:\git\corefx\Z" },

            { @"Z:..\..\..\tmp\foo\..", @"C:\git\corefx", @"Z:\tmp" },
            { @"Z:tmp\..\..\foo\.", @"C:\git\corefx", @"Z:\foo" },
            { @"Z:..\..\tmp\foo\..", @"C:\git\corefx", @"Z:\tmp" },
            { @"Z:tmp\..\", @"C:\git\corefx", @"Z:\" },
            { @"Z:", @"C:\git\corefx", @"Z:\" },
            { @"Z", @"C:\git\corefx", @"C:\git\corefx\Z" },

            { @"Z:tmp\..\..\..\..\foo\..", @"C:\git\corefx", @"Z:\" },
            { @"Z:tmp\..\..\foo\.", @"C:\", @"Z:\foo" },
            { @"Z:..\..\tmp\..\foo\..", @"C:\", @"Z:\" },
            { @"Z:tmp\..\", @"C:\", @"Z:\" },
        };

        [Theory,
            MemberData(nameof(GetFullPath_CommonRootedWindowsData))]
        public void GetFullPath_CommonUnRooted_Windows(string path, string basePath, string expected)
        {
            Assert.Equal(expected, Path.GetFullPath(path, basePath));
            Assert.Equal(@"\\.\" + expected, Path.GetFullPath(path, @"\\.\" + basePath));
            Assert.Equal(@"\\?\" + expected, Path.GetFullPath(path, @"\\?\" + basePath));
        }

        [Fact]
        public void GetFullPath_ThrowsOnEmbeddedNulls()
        {
            Assert.Throws<ArgumentException>(null, () => Path.GetFullPath("/gi\0t", @"C:\foo\bar"));
        }

        public static TheoryData<string, string> TestData_TrimEndingDirectorySeparator => new TheoryData<string, string>
        {
            { @"C:\folder\", @"C:\folder" },
            { @"C:/folder/", @"C:/folder" },
            { @"/folder/", @"/folder" },
            { @"\folder\", @"\folder" },
            { @"folder\", @"folder" },
            { @"folder/", @"folder" },
            { @"C:\", @"C:\" },
            { @"C:/", @"C:/" },
            { @"", @"" },
            { @"/", @"/" },
            { @"\", @"\" },
            { @"\\server\share\", @"\\server\share" },
            { @"\\server\share\folder\", @"\\server\share\folder" },
            { @"\\?\C:\", @"\\?\C:\" },
            { @"\\?\C:\folder\", @"\\?\C:\folder" },
            { @"\\?\UNC\", @"\\?\UNC\" },
            { @"\\?\UNC\a\", @"\\?\UNC\a\" },
            { @"\\?\UNC\a\folder\", @"\\?\UNC\a\folder" },
            { null, null }
        };

        public static TheoryData<string, bool> TestData_EndsInDirectorySeparator => new TheoryData<string, bool>
        {
            { @"\", true },
            { @"/", true },
            { @"C:\folder\", true },
            { @"C:/folder/", true },
            { @"C:\", true },
            { @"C:/", true },
            { @"\\", true },
            { @"//", true },
            { @"\\server\share\", true },
            { @"\\?\UNC\a\", true },
            { @"\\?\C:\", true },
            { @"\\?\UNC\", true },
            { @"folder\", true },
            { @"folder", false },
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
