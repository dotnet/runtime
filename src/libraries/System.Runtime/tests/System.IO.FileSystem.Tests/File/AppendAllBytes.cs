// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Tests;
using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class File_AppendAllBytes : FileSystemTest
    {

        [Fact]
        public void NullParameters()
        {
            string path = GetTestFilePath();

            Assert.Throws<ArgumentNullException>(() => File.AppendAllBytes(null, new byte[0]));
            Assert.Throws<ArgumentNullException>(() => File.AppendAllBytes(path, null));
        }

        [Fact]
        public void NonExistentPath()
        {
            Assert.Throws<DirectoryNotFoundException>(() => File.AppendAllBytes(Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName()), new byte[0]));
        }

        [Fact]
        public void InvalidParameters()
        {
            Assert.Throws<ArgumentException>(() => File.AppendAllBytes(string.Empty, new byte[0]));
        }


        [Fact]
        public void AppendAllBytes_WithValidInput_AppendsBytes()
        {
            string path = GetTestFilePath();

            byte[] initialBytes = Encoding.UTF8.GetBytes("bytes");
            byte[] additionalBytes = Encoding.UTF8.GetBytes("additional bytes");

            File.WriteAllBytes(path, initialBytes);
            File.AppendAllBytes(path, additionalBytes);

            byte[] result = File.ReadAllBytes(path);

            byte[] expectedBytes = initialBytes.Concat(additionalBytes).ToArray();

            Assert.True(result.SequenceEqual(expectedBytes));
        }


        [Fact]
        public void EmptyContentCreatesFile()
        {
            string path = GetTestFilePath();
            Assert.False(File.Exists(path));
            File.AppendAllBytes(path, new byte[0]);
            Assert.True(File.Exists(path));
            Assert.Empty(File.ReadAllBytes(path));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsFileLockingEnabled))]
        public void OpenFile_ThrowsIOException()
        {
            string path = GetTestFilePath();
            byte[] bytes = Encoding.UTF8.GetBytes("bytes");

            using (File.Create(path))
            {
                Assert.Throws<IOException>(() => File.AppendAllBytes(path, bytes));
            }
        }

        /// <summary>
        /// On Unix, modifying a file that is ReadOnly will fail under normal permissions.
        /// If the test is being run under the superuser, however, modification of a ReadOnly
        /// file is allowed. On Windows, modifying a file that is ReadOnly will always fail.
        /// </summary>
        [Fact]
        public void AppendToReadOnlyFileAsync()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            File.SetAttributes(path, FileAttributes.ReadOnly);
            byte[] dataToAppend = Encoding.UTF8.GetBytes("bytes");

            try
            {
                if (PlatformDetection.IsNotWindows && PlatformDetection.IsPrivilegedProcess)
                {
                    File.AppendAllBytes(path, dataToAppend);
                    Assert.Equal(dataToAppend, File.ReadAllBytes(path));
                }
                else
                {
                    Assert.Throws<UnauthorizedAccessException>(() => File.AppendAllBytes(path, dataToAppend));
                }
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
        }
    }
}
