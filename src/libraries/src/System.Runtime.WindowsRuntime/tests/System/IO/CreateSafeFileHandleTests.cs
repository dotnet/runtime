// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using Windows.Storage;
using Xunit;

namespace System.IO
{   
    [ActiveIssue("https://github.com/dotnet/corefx/issues/18940", TargetFrameworkMonikers.UapAot)]
    public class CreateSafeFileHandleTests
    {
        [Fact]
        public void NullStorageFile_ThrowsArgumentNull()
        {
            IStorageFile file = null;
            AssertExtensions.Throws<ArgumentNullException>("windowsRuntimeFile", () => file.CreateSafeFileHandle());
        }

        [Fact]
        public void FromStorageFile_BadAccessThrowsOutOfRange()
        {
            IStorageFile file = new StorageFileMock();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => file.CreateSafeFileHandle((FileAccess)100));
        }

        [Fact]
        public void FromStorageFile_BadSharingThrowsOutOfRange()
        {
            IStorageFile file = new StorageFileMock();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("share", () => file.CreateSafeFileHandle(FileAccess.ReadWrite, (FileShare)100));
        }

        [Fact]
        public void FromStorageFile_BadOptionsThrowsOutOfRange()
        {
            IStorageFile file = new StorageFileMock();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => file.CreateSafeFileHandle(FileAccess.ReadWrite, FileShare.Read, (FileOptions)100));
        }

        [Fact]
        public void FromStorageFile_InheritableThrowsNotSupported()
        {
            IStorageFile file = new StorageFileMock();
            Assert.Throws<NotSupportedException>(() => file.CreateSafeFileHandle(FileAccess.ReadWrite, FileShare.Inheritable));
        }

        [Fact]
        public void FromStorageFile_EncryptedThrowsNotSupported()
        {
            IStorageFile file = new StorageFileMock();
            Assert.Throws<NotSupportedException>(() => file.CreateSafeFileHandle(FileAccess.ReadWrite, FileShare.Read, FileOptions.Encrypted));
        }

        [Fact]
        public void FromStorageFile_NoInterfaceReturnsNull()
        {
            // If the provided IStorageFile object can't be cast to the COM interface needed it should return null
            IStorageFile file = new StorageFileMock();
            Assert.Null(file.CreateSafeFileHandle());
        }

        [Fact]
        public void NullStorageFolder_ThrowsArgumentNull()
        {
            IStorageFolder folder = null;
            AssertExtensions.Throws<ArgumentNullException>("rootDirectory", () => folder.CreateSafeFileHandle("foo", FileMode.OpenOrCreate));
        }

        [Fact]
        public void NullStorageFolder_ThrowsArgumentNull2()
        {
            IStorageFolder folder = null;
            AssertExtensions.Throws<ArgumentNullException>("rootDirectory", () => folder.CreateSafeFileHandle("foo", FileMode.OpenOrCreate, FileAccess.Write));
        }

        [Fact]
        public void FromStorageFolder_BadModeThrowsOutOfRange()
        {
            IStorageFolder folder = new StorageFolderMock();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("mode", () => folder.CreateSafeFileHandle("Foo", (FileMode)100));
        }

        [Fact]
        public void FromStorageFolder_BadAccessThrowsOutOfRange()
        {
            IStorageFolder folder = new StorageFolderMock();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => folder.CreateSafeFileHandle("Foo", FileMode.OpenOrCreate, (FileAccess)100));
        }

        [Fact]
        public void FromStorageFolder_BadSharingThrowsOutOfRange()
        {
            IStorageFolder folder = new StorageFolderMock();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("share", () => folder.CreateSafeFileHandle("Foo", FileMode.OpenOrCreate, FileAccess.ReadWrite, (FileShare)100));
        }

        [Fact]
        public void FromStorageFolder_BadOptionsThrowsOutOfRange()
        {
            IStorageFolder folder = new StorageFolderMock();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => folder.CreateSafeFileHandle("Foo", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, (FileOptions)100));
        }

        [Fact]
        public void FromStorageFolder_InheritableThrowsNotSupported()
        {
            IStorageFolder folder = new StorageFolderMock();
            Assert.Throws<NotSupportedException>(() => folder.CreateSafeFileHandle("Foo", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Inheritable));
        }

        [Fact]
        public void FromStorageFolder_EncryptedThrowsNotSupported()
        {
            IStorageFolder folder = new StorageFolderMock();
            Assert.Throws<NotSupportedException>(() => folder.CreateSafeFileHandle("Foo", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, FileOptions.Encrypted));
        }

        [Fact]
        public void FromStorageFolder_Basic()
        {
            StorageFolder folder = ApplicationData.Current.TemporaryFolder;
            string filename = "FromStorageFolder_Basic_" + Path.GetRandomFileName();
            using (SafeFileHandle handle = folder.CreateSafeFileHandle(filename, FileMode.CreateNew))
            {
                Assert.NotNull(handle);
                Assert.False(handle.IsInvalid);
            }
            File.Delete(Path.Combine(folder.Path, filename));
        }

        [Fact]
        public void FromStorageFolder_SurfaceIOException()
        {
            StorageFolder folder = ApplicationData.Current.TemporaryFolder;
            string filename = "FromStorageFolder_SurfaceIOException_" + Path.GetRandomFileName();
            using (SafeFileHandle handle = folder.CreateSafeFileHandle(filename, FileMode.CreateNew))
            {
                Assert.NotNull(handle);
                Assert.False(handle.IsInvalid);
            }
            Assert.Contains(
                filename,
                Assert.Throws<IOException>(() => folder.CreateSafeFileHandle(filename, FileMode.CreateNew)).Message);
            File.Delete(Path.Combine(folder.Path, filename));
        }

        [Fact]
        public void FromStorageFolder_SurfaceNotFoundException()
        {
            StorageFolder folder = ApplicationData.Current.TemporaryFolder;
            string filename = "FromStorageFolder_SurfaceNotFoundException_" + Path.GetRandomFileName();
            Assert.Contains(
                filename,
                Assert.Throws<FileNotFoundException>(() => folder.CreateSafeFileHandle(filename, FileMode.Open)).Message);
        }

        [Fact]
        public void FromStorageFolder_FileStream()
        {
            StorageFolder folder = ApplicationData.Current.TemporaryFolder;
            string filename = "FromStorageFolder_FileStream_" + Path.GetRandomFileName();
            SafeFileHandle handle = folder.CreateSafeFileHandle(filename, FileMode.CreateNew, FileAccess.ReadWrite);
            Assert.NotNull(handle);
            Assert.False(handle.IsInvalid);
            using (FileStream fs = new FileStream(handle, FileAccess.ReadWrite))
            {
                byte[] data = { 0xDE, 0xAD, 0xBE, 0xEF };
                fs.Write(data, 0, data.Length);
                fs.Flush();
                fs.Position = 0;
                byte[] input = new byte[4];
                Assert.Equal(4, fs.Read(input, 0, 4));
                Assert.Equal(data, input);
            }

            File.Delete(Path.Combine(folder.Path, filename));
        }

        [Fact]
        public void FromStorageFile_Basic()
        {
            StorageFolder folder = ApplicationData.Current.TemporaryFolder;
            string filename = "FromStorageFile_Basic_" + Path.GetRandomFileName();
            StorageFile file = folder.CreateFileAsync(filename, CreationCollisionOption.FailIfExists).AsTask().Result;
            using (SafeFileHandle handle = file.CreateSafeFileHandle())
            {
                Assert.NotNull(handle);
                Assert.False(handle.IsInvalid);
            }
            file.DeleteAsync().AsTask().Wait();
        }

        [Fact]
        public void FromStorageFile_FileStream()
        {
            StorageFolder folder = ApplicationData.Current.TemporaryFolder;
            string filename = "FromStorageFile_FileStream_" + Path.GetRandomFileName();
            StorageFile file = folder.CreateFileAsync(filename, CreationCollisionOption.FailIfExists).AsTask().Result;
            SafeFileHandle handle = file.CreateSafeFileHandle();
            Assert.NotNull(handle);
            Assert.False(handle.IsInvalid);
            using (FileStream fs = new FileStream(handle, FileAccess.ReadWrite))
            {
                byte[] data = { 0xAB, 0xBA, 0xCA, 0xDA, 0xBA };
                fs.Write(data, 0, data.Length);
                fs.Flush();
                fs.Position = 0;
                byte[] input = new byte[5];
                Assert.Equal(5, fs.Read(input, 0, 5));
                Assert.Equal(data, input);
            }

            file.DeleteAsync().AsTask().Wait();
        }

        [Fact]
        public void FromStorageFile_SurfaceIOException()
        {
            StorageFolder folder = ApplicationData.Current.TemporaryFolder;
            string filename = "FromStorageFile_SurfaceIOException_" + Path.GetRandomFileName();
            StorageFile file = folder.CreateFileAsync(filename, CreationCollisionOption.FailIfExists).AsTask().Result;
            using (SafeFileHandle handle = file.CreateSafeFileHandle(FileAccess.ReadWrite, FileShare.None))
            {
                Assert.Contains(
                    filename,
                    Assert.Throws<IOException>(() => file.CreateSafeFileHandle(FileAccess.ReadWrite, FileShare.None)).Message);
            }

            file.DeleteAsync().AsTask().Wait();
        }
    }
}
