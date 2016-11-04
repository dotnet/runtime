// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Xml.Tests
{
    public class XmlSystemPathResolverTests
    {
        private const int k_getUniqueFileNameAttempts = 10;

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetcoreUwp)]  //[ActiveIssue(13121)]  // Access to path is denied in UWP
        public static void TestResolveRelativePaths()
        {
            string path = Path.GetRandomFileName();
            bool shouldDelete = !File.Exists(path);
            File.Open(path, FileMode.OpenOrCreate).Dispose();
            try
            {
                XmlReader.Create(path).Dispose();
                XmlReader.Create(Path.Combine(".", path)).Dispose();
            }
            finally
            {
                if (shouldDelete)
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public static void TestResolveAbsolutePath()
        {
            string path = Path.GetTempFileName();
            try
            {
                XmlReader.Create(path).Dispose();
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static string GetNonExistentFileName()
        {
            return Enumerable.Range(0, k_getUniqueFileNameAttempts).Select(x => Path.GetRandomFileName()).First(fileName => !File.Exists(fileName));
        }

        [Fact]
        public static void TestResolveNonExistentPath()
        {
            string relativePath = GetNonExistentFileName();
            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            Assert.Throws<FileNotFoundException>(() => XmlReader.Create(relativePath));
            Assert.Throws<FileNotFoundException>(() => XmlReader.Create(absolutePath));
        }

        private static void AssertInvalidPath(string path)
        {
            // Due to different shipping behavior in different products, different exceptions may be thrown
            // when an invalid path is specified on different platforms. We try to catch all the types
            // that can be thrown here to verify that more exception types are not introduced inadvertently.
            Exception e = Assert.ThrowsAny<Exception>(() => XmlReader.Create(path));
            Assert.True(e is ArgumentException
                || e is AggregateException
                || e is FileNotFoundException
                || e is FormatException
                || e is UnauthorizedAccessException
                || e is XmlException);
        }

        [Fact]
        public static void TestResolveInvalidPaths()
        {
            AssertInvalidPath(null);
            AssertInvalidPath(string.Empty);
            AssertInvalidPath("\\");
            AssertInvalidPath("  \r\n\t");
            AssertInvalidPath("??");
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetcoreUwp)]  //[ActiveIssue(13121)]  // Access to path is denied in UWP
        public static void TestResolveInvalidPath()
        {
            AssertInvalidPath("ftp://www.bing.com");
        }
    }
}
