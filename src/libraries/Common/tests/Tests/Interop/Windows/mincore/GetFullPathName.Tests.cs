// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using Xunit;

namespace Tests.Interop.Windows
{
    public class GetFullPathNameTests
    {
        [PlatformSpecific(PlatformID.Windows)]
        [Theory]
        [InlineData(@"C:\", @"C:\")]
        [InlineData(@"C:\.", @"C:\")]
        [InlineData(@"C:\..", @"C:\")]
        [InlineData(@"C:\..\..", @"C:\")]
        [InlineData(@"C:\A\..", @"C:\")]
        [InlineData(@"C:\..\..\A\..", @"C:\")]
        public static void GetFullPathName_Windows_RelativeRoot(string path, string expected)
        {
            StringBuilder sb = new StringBuilder(256);
            int result = global::Interop.mincore.GetFullPathName(path, sb.Capacity, sb);
            Assert.True(result > 0, "GetFullPathName should succeed");
            Assert.Equal(expected, sb.ToString());

            path = PathInternal.EnsureExtendedPrefix(path);
            result = global::Interop.mincore.GetFullPathName(path, sb.Capacity, sb);
            Assert.True(result > 0, "GetFullPathName with extended syntax should succeed");
            Assert.Equal(PathInternal.EnsureExtendedPrefix(expected), sb.ToString());
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public static void GetFullPathName_Windows_LongPath()
        {
            // FullPathName handles > MAX_PATH
            string path = Path.Combine(@"C:\", new string('a', 255), new string('b', 255));
            StringBuilder sb = new StringBuilder(1024);
            int result = global::Interop.mincore.GetFullPathName(path, sb.Capacity, sb);
            Assert.True(result > 0, "GetFullPathName should succeed");
            Assert.Equal(path, sb.ToString());
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public static void GetFullPathName_Windows_LongPathRoot()
        {
            // Long paths shouldn't recurse past the root
            string path = Path.Combine(@"C:\", new string('a', 255), new string('b', 255), "..", "..", "..");
            StringBuilder sb = new StringBuilder(1024);
            int result = global::Interop.mincore.GetFullPathName(path, sb.Capacity, sb);
            Assert.True(result > 0, "GetFullPathName should succeed");
            Assert.Equal(@"C:\", sb.ToString());

            path = PathInternal.EnsureExtendedPrefix(path);
            result = global::Interop.mincore.GetFullPathName(path, sb.Capacity, sb);
            Assert.True(result > 0, "GetFullPathName should succeed");
            Assert.Equal(@"\\?\C:\", sb.ToString());
        }
    }
}
