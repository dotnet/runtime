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
        private const string k_existingFileName = "Empty.xml";

        [DllImport("api-ms-win-core-file-l1-2-1.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetCompressedFileSizeW(string lpFileName, out uint lpFileSizeHigh);

        private static bool FileExists(string path)
        {
            const uint INVALID_FILE_SIZE = 0xFFFFFFFF;
            const uint NO_ERROR = 0;

            uint high;
            uint low = GetCompressedFileSizeW(path, out high);

            if (low == INVALID_FILE_SIZE && Marshal.GetLastWin32Error() != NO_ERROR)
                return false;

            return true;
        }

        private static string GetExistingFileName()
        {
            // Sanity check: ensure the file that we expect to exist does,
            // so that it is clear when the test fails due to infrastructure misconfiguration.
            if (!FileExists(k_existingFileName))
            {
                Assert.True(false, string.Format("Test infrastructure error: Failed to find test resource: {0}", k_existingFileName));
            }

            return k_existingFileName;
        }

        [Fact]
        public static void TestResolveRelativePaths()
        {
            string path = GetExistingFileName();
            XmlReader.Create(path).Dispose();
            XmlReader.Create(Path.Combine("./", path)).Dispose();
        }

        [DllImport("api-ms-win-core-processenvironment-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetCurrentDirectoryW(uint nBufferLength, [Out] StringBuilder lpBuffer);

        private static string GetCurrentDirectory()
        {
            // The generated interop code assumes that a StringBuilder
            // parameter for a PInvoke method cannot be null. So we cannot use GetCurrentDirectoryW(0, null)
            // to determine the actual size of the buffer required for the current directory. In addition,
            // the documentation for GetCurrentDirectory does not specify an upper bound on the size lpBuffer,
            // so we assume that MAX_PATH will work for the purposes of this test.
            // See http://msdn.microsoft.com/en-us/library/windows/desktop/aa364934(v=vs.85).aspx
            const int MAX_PATH = 260;
            StringBuilder builder = new StringBuilder(MAX_PATH);
            uint result = GetCurrentDirectoryW(MAX_PATH, builder);

            if (result == 0)
            {
                // GetCurrentDirectoryW was unable to write all the necessary characters into builder
                Assert.True(false, "Test infrastructure error: failed to get the current directory");
            }

            return builder.ToString();
        }

        [Fact]
        public static void TestResolveAbsolutePath()
        {
            string path = Path.Combine(GetCurrentDirectory(), GetExistingFileName());
            XmlReader.Create(path).Dispose();
        }

        private static string GetNonExistentFileName()
        {
            for (int i = 0; i < k_getUniqueFileNameAttempts; ++i)
            {
                string fileName = Path.GetRandomFileName();
                if (!FileExists(fileName))
                    return fileName;
            }

            Assert.True(false, "Test infrastructure error: Failed to get non-existent file name.");
            return null;
        }

        [Fact]
        public static void TestResolveNonExistentPath()
        {
            string relativePath = GetNonExistentFileName();
            string absolutePath = Path.Combine(GetCurrentDirectory(), relativePath);
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