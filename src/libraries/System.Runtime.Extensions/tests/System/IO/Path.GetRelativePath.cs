// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.IO.Tests
{
    public static class GetRelativePathTests
    {
        [Theory]
        [InlineData(@"C:\", @"C:\", @".")]
        [InlineData(@"C:\a", @"C:\a\", @".")]
        [InlineData(@"C:\A", @"C:\a\", @".")]
        [InlineData(@"C:\a\", @"C:\a", @".")]
        [InlineData(@"C:\", @"C:\b", @"b")]
        [InlineData(@"C:\a", @"C:\b", @"..\b")]
        [InlineData(@"C:\a", @"C:\b\", @"..\b\")]
        [InlineData(@"C:\a\", @"C:\b", @"..\b")]
        [InlineData(@"C:\a", @"C:\a\b", @"b")]
        [InlineData(@"C:\a", @"C:\A\b", @"b")]
        [InlineData(@"C:\a", @"C:\b\c", @"..\b\c")]
        [InlineData(@"C:\a\", @"C:\a\b", @"b")]
        [InlineData(@"C:\", @"D:\b", @"D:\b")]
        [InlineData(@"C:\a", @"D:\b", @"D:\b")]
        [InlineData(@"C:\a\", @"D:\b", @"D:\b")]
        [InlineData(@"C:\ab", @"C:\a", @"..\a")]
        [InlineData(@"C:\a", @"C:\ab", @"..\ab")]
        [InlineData(@"C:\", @"\\LOCALHOST\Share\b", @"\\LOCALHOST\Share\b")]
        [InlineData(@"\\LOCALHOST\Share\a", @"\\LOCALHOST\Share\b", @"..\b")]
        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific paths
        public static void GetRelativePath_Windows(string relativeTo, string path, string expected)
        {
            string result = Path.GetRelativePath(relativeTo, path);
            Assert.Equal(expected, result);

            // Check that we get the equivalent path when the result is combined with the sources
            Assert.Equal(
                Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(Path.Combine(Path.GetFullPath(relativeTo), result)).TrimEnd(Path.DirectorySeparatorChar),
                ignoreCase: true,
                ignoreLineEndingDifferences: false,
                ignoreWhiteSpaceDifferences: false);
        }

        [Theory]
        [InlineData(@"/", @"/", @".")]
        [InlineData(@"/a", @"/a/", @".")]
        [InlineData(@"/a/", @"/a", @".")]
        [InlineData(@"/", @"/b", @"b")]
        [InlineData(@"/a", @"/b", @"../b")]
        [InlineData(@"/a/", @"/b", @"../b")]
        [InlineData(@"/a", @"/a/b", @"b")]
        [InlineData(@"/a", @"/b/c", @"../b/c")]
        [InlineData(@"/a/", @"/a/b", @"b")]
        [InlineData(@"/ab", @"/a", @"../a")]
        [InlineData(@"/a", @"/ab", @"../ab")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests Unix-specific paths
        public static void GetRelativePath_AnyUnix(string relativeTo, string path, string expected)
        {
            string result = Path.GetRelativePath(relativeTo, path);
            Assert.Equal(expected, result);

            // Check that we get the equivalent path when the result is combined with the sources
            Assert.Equal(
                Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(Path.Combine(Path.GetFullPath(relativeTo), result)).TrimEnd(Path.DirectorySeparatorChar));
        }

        [Theory]
        [InlineData(@"/a", @"/A/", @"../A/")]
        [InlineData(@"/a/", @"/A", @"../A")]
        [InlineData(@"/a/", @"/A/b", @"../A/b")]
        [PlatformSpecific(TestPlatforms.Linux)]  // Tests Linux relative path behavior
        public static void GetRelativePath_Linux(string relativeTo, string path, string expected)
        {
            string result = Path.GetRelativePath(relativeTo, path);
            Assert.Equal(expected, result);

            // Check that we get the equivalent path when the result is combined with the sources
            Assert.Equal(
                Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(Path.Combine(Path.GetFullPath(relativeTo), result)).TrimEnd(Path.DirectorySeparatorChar));
        }

        [Theory]
        [InlineData(@"/a", @"/A/", @"../A/")]
        [InlineData(@"/a/", @"/A", @"../A")]
        [InlineData(@"/a/", @"/A/b", @"../A/b")]
        [PlatformSpecific(TestPlatforms.FreeBSD)]  // Tests FreeBSD relative path behavior
        public static void GetRelativePath_FreeBSD(string relativeTo, string path, string expected)
        {
            string result = Path.GetRelativePath(relativeTo, path);
            Assert.Equal(expected, result);

            // Check that we get the equivalent path when the result is combined with the sources
            Assert.Equal(
                Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(Path.Combine(Path.GetFullPath(relativeTo), result)).TrimEnd(Path.DirectorySeparatorChar));
        }

        [Theory]
        [InlineData(@"/a", @"/A/", @"../A/")]
        [InlineData(@"/a/", @"/A", @"../A")]
        [InlineData(@"/a/", @"/A/b", @"../A/b")]
        [PlatformSpecific(TestPlatforms.NetBSD)]  // Tests NetBSD relative path behavior
        public static void GetRelativePath_NetBSD(string relativeTo, string path, string expected)
        {
            string result = Path.GetRelativePath(relativeTo, path);
            Assert.Equal(expected, result);

            // Check that we get the equivalent path when the result is combined with the sources
            Assert.Equal(
                Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(Path.Combine(Path.GetFullPath(relativeTo), result)).TrimEnd(Path.DirectorySeparatorChar));
        }

        [Theory]
        [InlineData(@"/a", @"/A/", @".")]
        [InlineData(@"/a/", @"/A", @".")]
        [InlineData(@"/a/", @"/A/b", @"b")]
        [PlatformSpecific(TestPlatforms.OSX)]  // Tests OSX relative path behavior
        public static void GetRelativePath_Mac(string relativeTo, string path, string expected)
        {
            string result = Path.GetRelativePath(relativeTo, path);
            Assert.Equal(expected, result);

            // Check that we get the equivalent path when the result is combined with the sources
            Assert.Equal(
                Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(Path.Combine(Path.GetFullPath(relativeTo), result)).TrimEnd(Path.DirectorySeparatorChar),
                ignoreCase: true,
                ignoreLineEndingDifferences: false,
                ignoreWhiteSpaceDifferences: false);
        }
    }
}
