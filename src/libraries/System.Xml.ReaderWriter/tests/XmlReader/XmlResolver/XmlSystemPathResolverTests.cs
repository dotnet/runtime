// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace XMLTests.ReaderWriter.XmlResolverTests
{
    public class XmlSystemPathResolverTests
    {
        private const int k_getUniqueFileNameAttempts = 10;

        [Fact]
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
            for (int i = 0; i < k_getUniqueFileNameAttempts; ++i)
            {
                string fileName = Path.GetRandomFileName();
                if (!File.Exists(fileName))
                    return fileName;
            }

            Assert.True(false, "Test infrastructure error: Failed to get non-existent file name.");
            return null;
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
            try
            {
                XmlReader.Create(path);
            }
            catch (ArgumentException)
            {
            }
            catch (AggregateException)
            {
            }
            catch (FileNotFoundException)
            {
            }
            catch (FormatException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (XmlException)
            {
            }
            catch (Exception e)
            {
                Assert.True(false, string.Format("Unexpected exception: {0}", e));
            }
        }

        [Fact]
        public static void TestResolveInvalidPaths()
        {
            AssertInvalidPath(null);
            AssertInvalidPath(string.Empty);
            AssertInvalidPath("ftp://www.bing.com");
            AssertInvalidPath("\\");
            AssertInvalidPath("  \r\n\t");
            AssertInvalidPath("??");
        }
    }
}