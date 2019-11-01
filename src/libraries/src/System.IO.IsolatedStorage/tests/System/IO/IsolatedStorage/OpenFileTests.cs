// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.IsolatedStorage
{
    public class OpenFileTests : IsoStorageTest
    {
        [Fact]
        public void OpenFile_ThrowsArgumentNull()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                AssertExtensions.Throws<ArgumentNullException>("path", () => isf.OpenFile(null, FileMode.Create));
                AssertExtensions.Throws<ArgumentNullException>("path", () => isf.OpenFile(null, FileMode.Create, FileAccess.ReadWrite));
                AssertExtensions.Throws<ArgumentNullException>("path", () => isf.OpenFile(null, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
            }
        }

        [Fact]
        public void OpenFile_Deleted_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<InvalidOperationException>(() => isf.OpenFile("foo", FileMode.Create));
                Assert.Throws<InvalidOperationException>(() => isf.OpenFile("foo", FileMode.Create, FileAccess.ReadWrite));
                Assert.Throws<InvalidOperationException>(() => isf.OpenFile("foo", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
            }
        }

        [Fact]
        public void OpenFile_ThrowsObjectDisposed()
        {
            IsolatedStorageFile isf;
            using (isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
            }

            Assert.Throws<ObjectDisposedException>(() => isf.OpenFile("foo", FileMode.Create));
            Assert.Throws<ObjectDisposedException>(() => isf.OpenFile("foo", FileMode.Create, FileAccess.ReadWrite));
            Assert.Throws<ObjectDisposedException>(() => isf.OpenFile("foo", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
        }

        [Fact]
        public void OpenFile_Closed_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.OpenFile("foo", FileMode.Append));
                Assert.Throws<InvalidOperationException>(() => isf.OpenFile("foo", FileMode.Append, FileAccess.ReadWrite));
                Assert.Throws<InvalidOperationException>(() => isf.OpenFile("foo", FileMode.Append, FileAccess.ReadWrite, FileShare.ReadWrite));
            }
        }

        [Fact]
        public void OpenFile_RaisesIsolatedStorageException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<IsolatedStorageException>(() => isf.OpenFile("\0bad", FileMode.Create));
                Assert.Throws<IsolatedStorageException>(() => isf.OpenFile("\0bad", FileMode.Create, FileAccess.ReadWrite));
                Assert.Throws<IsolatedStorageException>(() => isf.OpenFile("\0bad", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
            }
        }

        [Fact]
        public void OpenFile_PassesFileShare()
        {
            TestHelper.WipeStores();

            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                string file = "OpenFile_PassesFileShare";
                using (var stream = isf.OpenFile(file, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    Assert.True(isf.FileExists(file), "file exists");
                    using (isf.OpenFile(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)) { }
                }

                using (var stream = isf.OpenFile(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    Assert.True(isf.FileExists(file), "file exists");
                    Assert.Throws<IsolatedStorageException>(() => isf.OpenFile(file, FileMode.Open, FileAccess.ReadWrite));
                }
            }
        }

        [Fact]
        public void OpenFile_PassesFileAccess()
        {
            TestHelper.WipeStores();

            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                string file = "OpenFile_PassesFileAccess";
                using (var stream = isf.OpenFile(file, FileMode.CreateNew, FileAccess.Write))
                {
                    Assert.True(isf.FileExists(file), "file exists");
                    stream.WriteByte(0xAB);
                }

                using (var stream = isf.OpenFile(file, FileMode.Open, FileAccess.Read))
                {
                    Assert.Equal(0xAB, stream.ReadByte());
                    Assert.Throws<NotSupportedException>(() => stream.WriteByte(0xCD));
                }
            }
        }

        [Fact]
        public void OpenFile_PassesFileMode()
        {
            TestHelper.WipeStores();

            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                string file = "OpenFile_PassesFileMode";
                using (isf.OpenFile(file, FileMode.CreateNew)) { }
                Assert.True(isf.FileExists(file), "file exists");

                Assert.Throws<IsolatedStorageException>(() => isf.OpenFile(file, FileMode.CreateNew));
                using (isf.OpenFile(file, FileMode.Create)) { }
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        public void OpenFile_Existence(PresetScopes scope)
        {
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
                string file = "OpenFile_Existence";
                string subdirectory = "OpenFile_Existence_Subdirectory";
                using (isf.OpenFile(file, FileMode.CreateNew)) { }
                Assert.True(isf.FileExists(file), "file exists");
                isf.CreateDirectory(subdirectory);
                Assert.True(isf.DirectoryExists(subdirectory), "directory exists");

                string nestedFile = Path.Combine(subdirectory, file);
                using (isf.OpenFile(nestedFile, FileMode.CreateNew)) { }

                Assert.True(isf.FileExists(nestedFile), "nested file exists");
            }
        }
    }
}
