// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace Tests.System.IO
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public class PathInternalTests_Windows
    {
        [Theory,
            InlineData(@"\\?\", @"\\?\"),
            InlineData(@"Foo", @"Foo"),
            InlineData(@"C:\Foo", @"\\?\C:\Foo"),
            InlineData(@"\\.\Foo", @"\\.\Foo"),
            InlineData(@"\\?\Foo", @"\\?\Foo"),
            InlineData(@"\??\Foo", @"\??\Foo"),
            InlineData(@"//?/Foo", @"//?/Foo"),
            InlineData(@"\\Server\Share", PathInternal.UncExtendedPathPrefix + @"Server\Share")
            ]
        public void EnsureExtendedPrefixTest(string path, string expected)
        {
            Assert.Equal(expected, PathInternal.EnsureExtendedPrefix(path));
        }

        [Theory,
            InlineData(@"", false),
            InlineData(@"\\?\", true),
            InlineData(@"\??\", true),
            InlineData(@"\\.\", false),
            InlineData(@"\\?", false),
            InlineData(@"\??", false),
            InlineData(@"//?/", false),
            InlineData(@"/??/", false)
            ]
        public void IsExtendedTest(string path, bool expected)
        {
            Assert.Equal(expected, PathInternal.IsExtended(path));
        }

        [Theory,
            InlineData(@"", false),
            InlineData(@"\\?\", true),
            InlineData(@"\??\", true),
            InlineData(@"\\.\", true),
            InlineData(@"\\?", false),
            InlineData(@"\??", false),
            InlineData(@"//?/", true),
            InlineData(@"/??/", false)
            ]
        public void IsDeviceTest(string path, bool expected)
        {
            Assert.Equal(expected, PathInternal.IsDevice(path));
        }

        [Theory,
            InlineData("", true),
            InlineData("C:", true),
            InlineData("**", true),
            InlineData(@"\\.\path", false),
            InlineData(@"\\?\path", false),
            InlineData(@"\\.", false),
            InlineData(@"\\?", false),
            InlineData(@"\?", false),
            InlineData(@"/?", false),
            InlineData(@"\\", false),
            InlineData(@"//", false),
            InlineData(@"\a", true),
            InlineData(@"/a", true),
            InlineData(@"\", true),
            InlineData(@"/", true),
            InlineData(@"C:Path", true),
            InlineData(@"C:\Path", false),
            InlineData(@"\\?\C:\Path", false),
            InlineData(@"Path", true),
            InlineData(@"X", true)
            ]
        public void IsPartiallyQualifiedTest(string path, bool expected)
        {
            Assert.Equal(expected, PathInternal.IsPartiallyQualified(path));
        }

        [Theory,
            InlineData(@"", @""),
            InlineData(null, null),
            InlineData(@"\", @"\"),
            InlineData(@"/", @"\"),
            InlineData(@"\\", @"\\"),
            InlineData(@"\\\", @"\\"),
            InlineData(@"//", @"\\"),
            InlineData(@"///", @"\\"),
            InlineData(@"\/", @"\\"),
            InlineData(@"\/\", @"\\"),

            InlineData(@"a\a", @"a\a"),
            InlineData(@"a\\a", @"a\a"),
            InlineData(@"a/a", @"a\a"),
            InlineData(@"a//a", @"a\a"),
            InlineData(@"a\", @"a\"),
            InlineData(@"a\\", @"a\"),
            InlineData(@"a/", @"a\"),
            InlineData(@"a//", @"a\"),
            InlineData(@"\a", @"\a"),
            InlineData(@"\\a", @"\\a"),
            InlineData(@"/a", @"\a"),
            InlineData(@"//a", @"\\a"),

            // Skip tests
            InlineData(@"  :", @"  :"),
            InlineData(@"  C:", @"  C:"),
            InlineData(@"C:\", @"C:\"),
            InlineData(@"C:/", @"C:\"),
            InlineData(@"  ", @"  "),
            InlineData(@"  \", @"  \"),
            InlineData(@"  /", @"  \"),
            InlineData(@"  8:", @"  8:"),
            InlineData(@"    \\", @"    \"),
            InlineData(@"    //", @"    \")
            ]
        public void NormalizeDirectorySeparatorTests(string path, string expected)
        {
            string result = PathInternal.NormalizeDirectorySeparators(path);
            Assert.Equal(expected, result);
            if (string.Equals(path, expected, StringComparison.Ordinal))
                Assert.Same(path, result);
        }

        [Theory,
            InlineData(@"", @"", StringComparison.OrdinalIgnoreCase, true),
            InlineData(@"", @"", StringComparison.Ordinal, true),
            InlineData(@"A", @"a", StringComparison.OrdinalIgnoreCase, true),
            InlineData(@"A", @"a", StringComparison.Ordinal, true),
            InlineData(@"C:\", @"c:\", StringComparison.OrdinalIgnoreCase, true),
            InlineData(@"C:\", @"c:\", StringComparison.Ordinal, false)
            ]
        public void AreRootsEqual(string first, string second, StringComparison comparisonType, bool expected)
        {
            Assert.Equal(expected, PathInternal.AreRootsEqual(first, second, comparisonType));
        }

        public static TheoryData<string, int, int> GetRootLength_Data => new TheoryData<string, int, int>
        {
            { @"C:\git\runtime", 3, 7 },
            { @"C:\git\.\", 3, 7 },
            { @"C:\git\..\", 3, 7 },
            { @"C:\git\..\..\", 3, 7 },
            { @"C:\..\", 3, 7 },
            { @"C:\", 3, 7 },
            { @"C:\\", 3, 7 },

            // With drive relative paths, the length changes with device syntax. There is no
            // concept of non-resolved "\\?\" paths. "\\?\C:git\" is not rooted at "\\?\C:",
            // it is rooted at "\\?\C:git\". While "\\.\" paths are resolved by Win32 (via
            // GetFullPathName), they also are not recognized as drive relative. Rather than
            // muddy the waters we'll consider the full segment as the root volume there
            // as well, even though GetFullPathName would eat to the prefix. We don't want
            // a conceptual model where device paths can resolve themselves out of the "volume".
            { @"C:", 2, 6},
            { @"C:git\", 2, 10},
            { @"C:git\\", 2, 10},
            { @"C:git", 2, 9},
            { @"C:git\runtime", 2, 10},
            { @"C:git\.\", 2, 10},
            { @"C:git\\.\", 2, 10},
            { @"C:git\..\", 2, 10},
            { @"C:..\", 2, 9},
        };

        public static TheoryData<string, int> GetRootLengthRooted => new TheoryData<string, int>
        {
            { @"\tmp", 1},
            { @"\.", 1},
            { @"\..", 1},
            { @"\tmp\..\..", 1},
            { @"\\", 2},
        };

        [Theory,
            MemberData(nameof(GetRootLength_Data))]
        public void GetRootLength(string path, int length, int deviceLength)
        {
            Assert.Equal(length, PathInternal.GetRootLength(path));
            Assert.Equal(deviceLength, PathInternal.GetRootLength(@"\\?\" + path));
            Assert.Equal(deviceLength, PathInternal.GetRootLength(@"\\.\" + path));
        }

        [Theory,
            MemberData(nameof(GetRootLengthRooted))]
        public void GetRootLength_Rooted(string path, int length)
        {
            Assert.Equal(length, PathInternal.GetRootLength(path));
        }

        public static TheoryData<string, int> GetRootLength_UNCData => new TheoryData<string, int>
        {
            // Historically we've never included the separator after a UNC with GetPathRoot()
            // We'll continue to do so.
            { @"a\b\git\runtime", 3},
            { @"Server\Share\git\runtime", 12},
            { @"Server\Share\git\.\", 12},
            { @"Server\Share\git\..\", 12},
            { @"Server\Share\git\..\..\", 12},
            { @"Server\Share\..\", 12},
            { @"Server\Share\", 12},
            { @"Server\Share\\", 12},
            { @"Server\Share", 12},
            { @"Server\Share\", 12},
            { @"Server\Share\\git", 12},

            // Degenerate paths.
            //
            // We expect paths to be well formed up to the root. If you have "\\Server\\Share"
            // instead of "\\Server\Share", all bets are off. We'll test them here to make
            // sure we don't choke.
            { @"\a\b\git\runtime", 2},
            { @"a\\b\git\runtime", 2},
        };

        [Theory,
            MemberData(nameof(GetRootLength_UNCData))]
        public void GetRootLengthUnc(string path, int length)
        {
            Assert.Equal(length + 2, PathInternal.GetRootLength(@"\\" + path));
            Assert.Equal(length + PathInternal.UncExtendedPrefixLength, PathInternal.GetRootLength(@"\\?\UNC\" + path));
            Assert.Equal(length + PathInternal.UncExtendedPrefixLength, PathInternal.GetRootLength(@"\\.\UNC\" + path));
        }

        public static TheoryData<string, int> GetRootLength_DeviceData => new TheoryData<string, int>
        {
            { @"..\", 3},
            { @"..\..\", 3},
            { @"..\.\", 3},
            { @"...\", 4},
            { @".\", 2},
            { @".\..\", 2},
            { @"..\", 3},
            { @"\.\", 0},
            { @"\..\", 0},
            { @"\\..", 0},
            { @"foo\..", 4},
            { @".", 1},
            { @"..", 2},
            { @"foo", 3},
            { @"foo\", 4},
            { @"foo\\", 4},
            { @"..\foo", 3},
            { @"\\\.\", 0},
            { @"\\.\", 0},
        };

        [Theory,
            MemberData(nameof(GetRootLength_DeviceData))]
        public void GetRootLengthDevice(string path, int length)
        {
            Assert.Equal(length + PathInternal.ExtendedPathPrefix.Length, PathInternal.GetRootLength(@"\\?\" + path));
            Assert.Equal(length + PathInternal.ExtendedPathPrefix.Length, PathInternal.GetRootLength(@"\\.\" + path));
        }

        public static TheoryData<string, string> DosToNtPathTest_Data => new TheoryData<string, string>
        {
            { @"C:\tests\file.cs", @"\??\C:\tests\file.cs" }, // typical path
            { @"\\?\C:\tests\file.cs", @"\??\C:\tests\file.cs" }, // NtPath with \\?\ prefix
            { @"\\.\device\file.cs", @"\??\device\file.cs" }, // device path with \\.\ prefix
            { @"\\server\file.cs", @"\??\UNC\server\file.cs" }, // UNC path with \\ prefix
            { @"\\?\UNC\server\file", @"\??\UNC\server\file" }, // extended UNC prefix
            { @"\??\C:\tests\file.cs", @"\??\C:\tests\file.cs" }, // NtPath with \??\ prefix (no changes required)
            { @"C:\", @"\??\C:\" }, // a short path
            { @"\\s", @"\??\UNC\s" }, // short UNC path
            { @"\\s\", @"\??\UNC\s\" }, // short UNC path with trailing
            { $@"C:\{string.Join("\\", Enumerable.Repeat("a", PathInternal.MaxShortPath + 1))}",  $@"\??\C:\{string.Join("\\", Enumerable.Repeat("a", PathInternal.MaxShortPath + 1))}"}, // long path
        };

        [Theory, MemberData(nameof(DosToNtPathTest_Data))]
        public void DosToNtPathTest(string path, string expected)
        {
            // first of all, we use an internal Windows API to ensure that expected value is valid
            if (path.Length < PathInternal.MaxShortPath) // RtlDosPathNameToRelativeNtPathName_U_WithStatus does not support long paths
            {
                RtlDosPathNameToRelativeNtPathName_U_WithStatus(path, out Interop.UNICODE_STRING ntFileName, out IntPtr _, IntPtr.Zero);
                try
                {
                    Assert.Equal(expected, Marshal.PtrToStringUni(ntFileName.Buffer));
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ntFileName.Buffer);
                }
            }

            // after that, we test our implementation
            var vsb = new ValueStringBuilder(stackalloc char[PathInternal.MaxShortPath]);
            PathInternal.DosToNtPath(path, ref vsb);
            Assert.Equal(expected, vsb.ToString());

            [DllImport(Interop.Libraries.NtDll, CharSet = CharSet.Unicode)]
            static extern int RtlDosPathNameToRelativeNtPathName_U_WithStatus(string DosFileName, out Interop.UNICODE_STRING NtFileName, out IntPtr FilePart, IntPtr RelativeName);
        }
    }
}
