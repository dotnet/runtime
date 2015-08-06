// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class File_Copy_str_str : FileSystemTest
    {
        #region Utilities

        public static string[] WindowsInvalidUnixValid = new string[] { "         ", " ", "\n", ">", "<", "\t" };
        public virtual void Copy(string source, string dest)
        {
            File.Copy(source, dest);
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void NullFileName()
        {
            Assert.Throws<ArgumentNullException>(() => Copy(null, "."));
            Assert.Throws<ArgumentNullException>(() => Copy(".", null));
        }

        [Fact]
        public void EmptyFileName()
        {
            Assert.Throws<ArgumentException>(() => Copy(string.Empty, "."));
            Assert.Throws<ArgumentException>(() => Copy(".", string.Empty));
        }

        [Fact]
        public void CopyOntoDirectory()
        {
            string testFile = GetTestFilePath();
            File.Create(testFile).Dispose();
            Assert.Throws<IOException>(() => Copy(testFile, TestDirectory));
        }

        [Fact]
        public void CopyOntoSelf()
        {
            string testFile = GetTestFilePath();
            File.Create(testFile).Dispose();
            Assert.Throws<IOException>(() => Copy(testFile, testFile));
        }

        [Fact]
        public void NonExistentPath()
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Throws<FileNotFoundException>(() => Copy(GetTestFilePath(), testFile.FullName));
                Assert.Throws<DirectoryNotFoundException>(() => Copy(testFile.FullName, Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName())));
                Assert.Throws<DirectoryNotFoundException>(() => Copy(Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName()), testFile.FullName));
            }
            else
            {
                Assert.Throws<FileNotFoundException>(() => Copy(GetTestFilePath(), testFile.FullName));
                Assert.Throws<DirectoryNotFoundException>(() => Copy(testFile.FullName, Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName())));
                Assert.Throws<FileNotFoundException>(() => Copy(Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName()), testFile.FullName));
            }
}

        [Fact]
        public void CopyValid()
        {
            string testFileSource = GetTestFilePath();
            string testFileDest = GetTestFilePath();
            File.Create(testFileSource).Dispose();
            Copy(testFileSource, testFileDest);
            Assert.True(File.Exists(testFileDest));
            Assert.True(File.Exists(testFileSource));
        }

        [Fact]
        public void ShortenLongPath()
        {
            string testFileSource = GetTestFilePath();
            string testFileDest = Path.GetDirectoryName(testFileSource) + string.Concat(Enumerable.Repeat(Path.DirectorySeparatorChar + ".", 90).ToArray()) + Path.DirectorySeparatorChar + Path.GetFileName(testFileSource);
            File.Create(testFileSource).Dispose();
            Assert.Throws<IOException>(() => Copy(testFileSource, testFileDest));
        }

        [Fact]
        public void InvalidFileNames()
        {
            string testFile = GetTestFilePath();
            File.Create(testFile).Dispose();
            Assert.Throws<ArgumentException>(() => Copy(testFile, "\0"));
            Assert.Throws<ArgumentException>(() => Copy(testFile, "*\0*"));
            Assert.Throws<ArgumentException>(() => Copy("*\0*", testFile));
            Assert.Throws<ArgumentException>(() => Copy("\0", testFile));
        }

        [Fact]
        public void CopyFileWithData()
        {
            string testFileSource = GetTestFilePath();
            string testFileDest = GetTestFilePath();
            char[] data = { 'a', 'A', 'b' };

            // Write and copy file
            using (StreamWriter stream = new StreamWriter(File.Create(testFileSource)))
            {
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
            Copy(testFileSource, testFileDest);

            // Ensure copy transferred written data
            using (StreamReader stream = new StreamReader(File.OpenRead(testFileDest)))
            {
                char[] readData = new char[data.Length];
                stream.Read(readData, 0, data.Length);
                Assert.Equal(data, readData);
            }
        }

        #endregion

        #region PlatformSpecific

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void WindowsWhitespacePath()
        {
            string testFile = GetTestFilePath();
            File.Create(testFile).Dispose();
            foreach (string invalid in WindowsInvalidUnixValid)
            {
                Assert.Throws<ArgumentException>(() => Copy(testFile, invalid));
                Assert.Throws<ArgumentException>(() => Copy(invalid, testFile));
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void UnixWhitespacePath()
        {
            string testFile = GetTestFilePath();
            File.Create(testFile).Dispose();
            foreach (string valid in WindowsInvalidUnixValid)
            {
                Copy(testFile, Path.Combine(TestDirectory, valid));
                Assert.True(File.Exists(testFile));
                Assert.True(File.Exists(Path.Combine(TestDirectory, valid)));
            }
        }

        #endregion
    }

    public class File_Copy_str_str_b : File_Copy_str_str
    {
        #region Utilities

        public override void Copy(string source, string dest)
        {
            File.Copy(source, dest, false);
        }

        public virtual void Copy(string source, string dest, bool overwrite)
        {
            File.Copy(source, dest, overwrite);
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void OverwriteTrue()
        {
            string testFileSource = GetTestFilePath();
            string testFileDest = GetTestFilePath();
            char[] sourceData = { 'a', 'A', 'b' };
            char[] destData = { 'x', 'X', 'y' };

            // Write and copy file
            using (StreamWriter sourceStream = new StreamWriter(File.Create(testFileSource)))
            using (StreamWriter destStream = new StreamWriter(File.Create(testFileDest)))
            {
                sourceStream.Write(sourceData, 0, sourceData.Length);
                destStream.Write(destData, 0, destData.Length);
            }
            Copy(testFileSource, testFileDest, true);

            // Ensure copy transferred written data
            using (StreamReader stream = new StreamReader(File.OpenRead(testFileDest)))
            {
                char[] readData = new char[sourceData.Length];
                stream.Read(readData, 0, sourceData.Length);
                Assert.Equal(sourceData, readData);
            }
        }

        [Fact]
        public void OverwriteFalse()
        {
            string testFileSource = GetTestFilePath();
            string testFileDest = GetTestFilePath();
            char[] sourceData = { 'a', 'A', 'b' };
            char[] destData = { 'x', 'X', 'y' };

            // Write and copy file
            using (StreamWriter sourceStream = new StreamWriter(File.Create(testFileSource)))
            using (StreamWriter destStream = new StreamWriter(File.Create(testFileDest)))
            {
                sourceStream.Write(sourceData, 0, sourceData.Length);
                destStream.Write(destData, 0, destData.Length);
            }
            Assert.Throws<IOException>(() => Copy(testFileSource, testFileDest, false));

            // Ensure copy didn't overwrite existing data
            using (StreamReader stream = new StreamReader(File.OpenRead(testFileDest)))
            {
                char[] readData = new char[sourceData.Length];
                stream.Read(readData, 0, sourceData.Length);
                Assert.Equal(destData, readData);
            }
        }

        #endregion
    }
}
