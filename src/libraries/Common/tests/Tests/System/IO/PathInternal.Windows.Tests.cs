// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
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
            InlineData(@"  C:", @"C:"),
            InlineData(@"   C:\", @"C:\"),
            InlineData(@"   C:/", @"C:\"),
            InlineData(@"  ", @"  "),
            InlineData(@"  \", @"\"),
            InlineData(@"  /", @"\"),
            InlineData(@"  8:", @"  8:"),
            InlineData(@"    \\", @"\\"),
            InlineData(@"    //", @"\\")
            ]
        [ActiveIssue(27401)]
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
    }
}
