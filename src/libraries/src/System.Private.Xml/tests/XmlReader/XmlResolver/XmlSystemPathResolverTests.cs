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
        public static void TestResolveRelativePaths()
        {
            string curDir = Directory.GetCurrentDirectory();
            string path = Path.GetRandomFileName();
            bool shouldDelete = !File.Exists(path);
            try
            {
                Directory.SetCurrentDirectory(Path.GetTempPath()); // Workaround for System.UnauthorizedAccessException on relative path in File.Open on UWP F5
                File.Open(path, FileMode.OpenOrCreate).Dispose();

                XmlReader.Create(path).Dispose();
                XmlReader.Create(Path.Combine(".", path)).Dispose();
            }
            finally
            {
                if (shouldDelete)
                {
                    File.Delete(path);
                }

                Directory.SetCurrentDirectory(curDir);
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
        public static void TestResolveInvalidPath()
        {
            Assert.Throws<System.Net.WebException>(() => XmlReader.Create("ftp://host.invalid"));
        }

        [Fact]
        public static void TestResolveDTD_Default()
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            XmlReader reader = XmlReader.Create("TestFiles/ResolveDTD_1.xml", settings);
            Assert.Throws<XmlException>(() => reader.ReadToDescendant("baz")); // For security reasons DTD is prohibited in this XML document.
        }

        [Fact]
        public static void TestResolveDTD_AllowDTDProcessing()
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;
            XmlReader reader = XmlReader.Create("TestFiles/ResolveDTD_1.xml", settings);
            reader.ReadToDescendant("baz");
        }
    }
}
