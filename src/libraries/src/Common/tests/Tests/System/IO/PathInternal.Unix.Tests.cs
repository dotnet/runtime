// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace Tests.System.IO
{
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public class PathInternalTests_Unix
    {
        [Theory,
           InlineData(@"", @""),
           InlineData(null, null),
           InlineData(@"/", @"/"),
           InlineData(@"//", @"/"),
           InlineData(@"///", @"/"),
           InlineData(@"\", @"\"),
           InlineData(@"\\", @"\\"),
           InlineData(@"\\\", @"\\\"),
           InlineData(@"\/", @"\/"),
           InlineData(@"\/\", @"\/\"),

           InlineData(@"a/a", @"a/a"),
           InlineData(@"a//a", @"a/a"),
           InlineData(@"a\\a", @"a\\a"),
           InlineData(@"/a", @"/a"),
           InlineData(@"//a", @"/a"),
           InlineData(@"\\a", @"\\a"),
           InlineData(@"a/", @"a/"),
           InlineData(@"a//", @"a/"),
           InlineData(@"a\\", @"a\\"),
           ]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void NormalizeDirectorySeparatorTests(string path, string expected)
        {
            string result = PathInternal.NormalizeDirectorySeparators(path);
            Assert.Equal(expected, result);
            if (string.Equals(path, expected, StringComparison.Ordinal))
                Assert.Same(path, result);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void IsCaseInsensitive_OSX()
        {
            // There have been reports of casing handling not being appropriate on MacOS
            // https://github.com/dotnet/corefx/issues/26797
            Assert.False(PathInternal.IsCaseSensitive);
        }
    }
}
