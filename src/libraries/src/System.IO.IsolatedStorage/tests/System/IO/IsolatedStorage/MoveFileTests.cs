// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.IO.IsolatedStorage
{
    public class MoveFileTests : IsoStorageTest
    {
        [Fact]
        public void MoveFile_ThrowsArgumentNull()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                AssertExtensions.Throws<ArgumentNullException>("sourceFileName", () => isf.MoveFile(null, "bar"));
                AssertExtensions.Throws<ArgumentNullException>("destinationFileName", () => isf.MoveFile("foo", null));
            }
        }

        [Fact]
        public void MoveFile_ThrowsArgumentException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                AssertExtensions.Throws<ArgumentException>("sourceFileName", () => isf.MoveFile(string.Empty, "bar"));
                AssertExtensions.Throws<ArgumentException>("destinationFileName", () => isf.MoveFile("foo", string.Empty));
            }
        }

        [Fact]
        public void MoveFile_ThrowsObjectDisposed()
        {
            IsolatedStorageFile isf;
            using (isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
            }

            Assert.Throws<ObjectDisposedException>(() => isf.MoveFile("foo", "bar"));
        }

        [Fact]
        public void MoveFile_Deleted_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<InvalidOperationException>(() => isf.MoveFile("foo", "bar"));
            }
        }

        [Fact]
        public void MoveFile_Closed_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.MoveFile("foo", "bar"));
            }
        }

        [Fact]
        public void MoveFile_RaisesIsolatedStorageException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<IsolatedStorageException>(() => isf.MoveFile("\0bad", "bar"));
                Assert.Throws<IsolatedStorageException>(() => isf.MoveFile("foo", "\0bad"));
            }
        }

        [Fact]
        public void MoveFile_DoesNotExist()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<FileNotFoundException>(() => isf.MoveFile("MoveFile_DoesNotExist", "MoveFile_DoesNotExist_Copy"));
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        public void MoveFile_MoveOver(PresetScopes scope)
        {
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
                isf.CreateTestFile("foo");
                isf.CreateTestFile("bar");
                Assert.Throws<IsolatedStorageException>(() => isf.MoveFile("foo", "bar"));
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        public void MoveFile_MovesFile(PresetScopes scope)
        {
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
                isf.CreateTestFile("foo", "MoveFile_MovesFile");
                isf.MoveFile("foo", "bar");
                Assert.True(isf.FileExists("bar"), "bar exists");
                Assert.Equal("MoveFile_MovesFile", isf.ReadAllText("bar"));
                Assert.False(isf.FileExists("foo"), "foo doesn't exist");

                string directory = "MoveFile_MovesFile";
                isf.CreateDirectory(directory);

                // Move into nested
                string nestedFile = Path.Combine(directory, "foobar");
                isf.MoveFile("bar", nestedFile);
                Assert.True(isf.FileExists(nestedFile), "nested file exists");
                Assert.Equal("MoveFile_MovesFile", isf.ReadAllText(nestedFile));
                Assert.False(isf.FileExists("bar"), "bar doesn't exist");

                // Move out of nested
                isf.MoveFile(nestedFile, "outbound");
                Assert.True(isf.FileExists("outbound"));
                Assert.Equal("MoveFile_MovesFile", isf.ReadAllText("outbound"));
                Assert.False(isf.FileExists(nestedFile), "nested file doesn't exist");
            }
        }
    }
}
