// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_ctor_str_fm_fa_fs_buffer_fo : FileStream_ctor_str_fm_fa_fs_buffer
    {
        protected sealed override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
        {
            return CreateFileStream(path, mode, access, share, bufferSize, FileOptions.None);
        }

        protected virtual FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            return new FileStream(path, mode, access, share, bufferSize, options);
        }

        [Fact]
        public void InvalidFileOptionsThrow()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                GetExpectedParamName("options"),
                () => CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, c_DefaultBufferSize, ~FileOptions.None));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(FileOptions.None)]
        [InlineData(FileOptions.DeleteOnClose)]
        [InlineData(FileOptions.RandomAccess)]
        [InlineData(FileOptions.SequentialScan)]
        [InlineData(FileOptions.WriteThrough)]
        [InlineData((FileOptions)0x20000000)] // FILE_FLAG_NO_BUFFERING on Windows
        [InlineData((FileOptions)0x02000000)] // FILE_FLAG_BACKUP_SEMANTICS on Windows
        [InlineData(FileOptions.Asynchronous)]
        [InlineData(FileOptions.Asynchronous | FileOptions.DeleteOnClose)]
        [InlineData(FileOptions.Asynchronous | FileOptions.RandomAccess)]
        [InlineData(FileOptions.Asynchronous | FileOptions.SequentialScan)]
        [InlineData(FileOptions.Asynchronous | FileOptions.WriteThrough)]
        [InlineData(FileOptions.Asynchronous | (FileOptions)0x20000000)]
        [InlineData(FileOptions.Asynchronous | (FileOptions)0x02000000)]
        [InlineData(FileOptions.Asynchronous | FileOptions.DeleteOnClose | FileOptions.RandomAccess | FileOptions.SequentialScan | FileOptions.WriteThrough)]
        public void ValidFileOptions(FileOptions option)
        {
            byte[] data = new byte[c_DefaultBufferSize];
            new Random(1).NextBytes(data);

            using (FileStream fs = CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, c_DefaultBufferSize, option))
            {
                Assert.Equal((option & FileOptions.Asynchronous) != 0, fs.IsAsync);

                // make sure we can write, seek, and read data with this option set
                fs.Write(data, 0, data.Length);
                fs.Position = 0;

                byte[] tmp = new byte[data.Length];
                int totalRead = 0;
                while (true)
                {
                    int numRead = fs.Read(tmp, totalRead, tmp.Length - totalRead);
                    Assert.InRange(numRead, 0, tmp.Length);
                    if (numRead == 0)
                        break;
                    totalRead += numRead;
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(FileOptions.Encrypted)]
        [InlineData(FileOptions.Asynchronous | FileOptions.Encrypted)]
        [InlineData(FileOptions.Asynchronous | FileOptions.DeleteOnClose | FileOptions.Encrypted | FileOptions.RandomAccess | FileOptions.SequentialScan | FileOptions.WriteThrough)]
        public void ValidFileOptions_Encrypted(FileOptions option)
        {
            try { ValidFileOptions(option); }
            catch (UnauthorizedAccessException) { /* may not be allowed for some users */ }
        }

        [Theory]
        [InlineData(FileOptions.DeleteOnClose)]
        [InlineData(FileOptions.DeleteOnClose | FileOptions.Asynchronous)]
        public void DeleteOnClose_FileDeletedAfterClose(FileOptions options)
        {
            string path = GetTestFilePath();
            Assert.False(File.Exists(path));
            using (CreateFileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 0x1000, options))
            {
                Assert.True(File.Exists(path));
            }
            Assert.False(File.Exists(path));
        }

        [Theory]
        [InlineData(FileOptions.DeleteOnClose)]
        [InlineData(FileOptions.DeleteOnClose | FileOptions.Asynchronous)]
        public void DeleteOnClose_FileDeletedAfterSafeHandleRelease(FileOptions options)
        {
            string path = GetTestFilePath();
            Assert.False(File.Exists(path));

            using (FileStream fs = CreateFileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 0x1000, options))
            {
                Assert.True(File.Exists(path));

                bool added = false;
                try
                {
                    fs.SafeFileHandle.DangerousAddRef(ref added);

                    fs.Dispose();
                    Assert.True(File.Exists(path));
                }
                finally
                {
                    if (added)
                    {
                        fs.SafeFileHandle.DangerousRelease();
                    }

                    Assert.False(File.Exists(path));
                }
            }
        }
    }
}
