// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.IsolatedStorage
{
    public class DeleteDirectoryTests : IsoStorageTest
    {
        [Fact]
        public void DeleteDirectory_ThrowsArgumentNull()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<ArgumentNullException>(() => isf.DeleteDirectory(null));
            }
        }

        [Fact]
        public void DeleteDirectory_ThrowsObjectDisposed()
        {
            IsolatedStorageFile isf;
            using (isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
            }

            Assert.Throws<ObjectDisposedException>(() => isf.DeleteDirectory("foo"));
        }

        [Fact]
        public void DeleteRemovedDirectory_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<InvalidOperationException>(() => isf.DeleteDirectory("foo"));
            }
        }

        [Fact]
        public void DeleteClosedDirectory_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.DeleteDirectory("foo"));
            }
        }

        [Fact]
        public void DeleteDirectory_RaisesInvalidPath()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<IsolatedStorageException>(() => isf.DeleteDirectory("\0bad"));
            }
        }

        [Fact]
        public void DeleteDirectory_DeleteNested()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                string directory = "DeleteDirectory_DeleteNested";
                string subdirectory = Path.Combine(directory, "Subdirectory");
                isf.CreateDirectory(subdirectory);
                Assert.True(isf.DirectoryExists(subdirectory));

                // Shouldn't be recursive
                Assert.Throws<IsolatedStorageException>(() => isf.DeleteDirectory(directory));

                isf.DeleteDirectory(subdirectory);
                isf.DeleteDirectory(directory);
                Assert.False(isf.DirectoryExists(directory));
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        public void DeleteDirectory_DeletesDirectory(PresetScopes scope)
        {
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
                string directory = "DeleteDirectory_DeletesDirectory";
                string subdirectory = Path.Combine(directory, directory);

                isf.CreateDirectory(directory);
                Assert.True(isf.DirectoryExists(directory), "directory exists");

                isf.CreateDirectory(subdirectory);
                Assert.True(isf.DirectoryExists(subdirectory), "subdirectory exists");

                // Can't delete a directory with content
                Assert.Throws<IsolatedStorageException>(() => isf.DeleteDirectory(directory));
                Assert.True(isf.DirectoryExists(directory));

                isf.DeleteDirectory(subdirectory);
                Assert.False(isf.DirectoryExists(subdirectory));
                isf.DeleteDirectory(directory);
                Assert.False(isf.DirectoryExists(directory));
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        public void DeleteDirectory_CannotDeleteWithContent(PresetScopes scope)
        {
            TestHelper.WipeStores();

            // Validating that we aren't passing recursive:true
            using (var isf = GetPresetScope(scope))
            {
                string directory = "DeleteDirectory_CannotDeleteWithContent";
                isf.CreateDirectory(directory);
                Assert.True(isf.DirectoryExists(directory), "directory exists");
                string testFile = Path.Combine(directory, "content.file");
                isf.CreateTestFile(testFile);
                Assert.Throws<IsolatedStorageException>(() => isf.DeleteDirectory(directory));
                isf.DeleteFile(testFile);
                isf.DeleteDirectory(directory);
                Assert.False(isf.DirectoryExists(directory));
            }
        }
    }
}
