// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.IsolatedStorage
{
    public class CopyFileTests : IsoStorageTest
    {
        [Fact]
        public void CopyFile_ThrowsArgumentNull()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                AssertExtensions.Throws<ArgumentNullException>("sourceFileName", () => isf.CopyFile(null, "bar"));
                AssertExtensions.Throws<ArgumentNullException>("sourceFileName", () => isf.CopyFile(null, "bar", true));
                AssertExtensions.Throws<ArgumentNullException>("destinationFileName", () => isf.CopyFile("foo", null));
                AssertExtensions.Throws<ArgumentNullException>("destinationFileName", () => isf.CopyFile("foo", null, true));
            }
        }

        [Fact]
        public void CopyFile_ThrowsArgumentException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                AssertExtensions.Throws<ArgumentException>("sourceFileName", () => isf.CopyFile(string.Empty, "bar"));
                AssertExtensions.Throws<ArgumentException>("sourceFileName", () => isf.CopyFile(string.Empty, "bar", true));
                AssertExtensions.Throws<ArgumentException>("destinationFileName", () => isf.CopyFile("foo", string.Empty));
                AssertExtensions.Throws<ArgumentException>("destinationFileName", () => isf.CopyFile("foo", string.Empty, true));
            }
        }

        [Fact]
        public void CopyFile_ThrowsObjectDisposed()
        {
            IsolatedStorageFile isf;
            using (isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
            }

            Assert.Throws<ObjectDisposedException>(() => isf.CopyFile("foo", "bar"));
        }

        [Fact]
        public void CopyDeletedFile_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<InvalidOperationException>(() => isf.CopyFile("foo", "bar"));
            }
        }

        [Fact]
        public void CopyClosedFile_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.CopyFile("foo", "bar"));
            }
        }

        [Fact]
        public void CopyFile_RaisesIsolatedStorageException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<IsolatedStorageException>(() => isf.CopyFile("\0bad", "bar"));
                Assert.Throws<IsolatedStorageException>(() => isf.CopyFile("foo", "\0bad"));
            }
        }

        [Fact]
        public void CopyFile_DoesNotExist()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<FileNotFoundException>(() => isf.CopyFile("CopyFile_DoesNotExist", "CopyFile_DoesNotExist_Copy"));
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        public void CopyFile_CopyOver(PresetScopes scope)
        {
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
                isf.CreateTestFile("foo", "CopyFile_CopyOver_Foo");
                isf.CreateTestFile("bar", "CopyFile_CopyOver_Bar");
                Assert.Throws<IsolatedStorageException>(() => isf.CopyFile("foo", "bar"));
                Assert.Throws<IsolatedStorageException>(() => isf.CopyFile("foo", "bar", overwrite: false));
                isf.CopyFile("foo", "bar", overwrite: true);
                Assert.Equal("CopyFile_CopyOver_Foo", isf.ReadAllText("bar"));
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        public void CopyFile_CopiesFile(PresetScopes scope)
        {
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
                isf.CreateTestFile("foo", "CopyFile_CopiesFile");
                isf.CopyFile("foo", "bar");
                Assert.True(isf.FileExists("bar"), "bar exists");
                Assert.Equal("CopyFile_CopiesFile", isf.ReadAllText("bar"));
                string directory = "CopyFile_CopiesFile";
                isf.CreateDirectory(directory);

                // Copy into nested
                string nestedFile = Path.Combine(directory, "foobar");
                isf.CopyFile("foo", nestedFile);
                Assert.True(isf.FileExists(nestedFile), "nested file exists");
                Assert.Equal("CopyFile_CopiesFile", isf.ReadAllText(nestedFile));

                // Copy out of nested
                isf.CopyFile(nestedFile, "outbound");
                Assert.True(isf.FileExists("outbound"), "outbound file exists");
                Assert.Equal("CopyFile_CopiesFile", isf.ReadAllText("outbound"));
            }
        }
    }
}
