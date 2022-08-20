// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.IsolatedStorage
{
    public class CreateDirectoryTests : IsoStorageTest
    {
        [Fact]
        public void CreateDirectory_ThrowsArgumentNull()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<ArgumentNullException>(() => isf.CreateDirectory(null));
            }
        }

        [Fact]
        public void CreateRemovedDirectory_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<InvalidOperationException>(() => isf.CreateDirectory("foo"));
            }
        }

        [Fact]
        public void CreateDirectory_ThrowsObjectDisposed()
        {
            IsolatedStorageFile isf;
            using (isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
            }

            Assert.Throws<ObjectDisposedException>(() => isf.CreateDirectory("foo"));
        }

        [Fact]
        public void CreateClosedDirectory_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.CreateDirectory("foo"));
            }
        }

        [Fact]
        public void CreateDirectory_IsolatedStorageException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<IsolatedStorageException>(() => isf.CreateDirectory("\0bad"));
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        public void CreateDirectory_Existence(PresetScopes scope)
        {
            using (var isf = GetPresetScope(scope))
            {
                string directory = "CreateDirectory_Existence";
                string subdirectory = Path.Combine(directory, "Subdirectory");
                isf.CreateDirectory(directory);
                Assert.True(isf.DirectoryExists(directory), "directory exists");
                isf.CreateDirectory(subdirectory);
                Assert.True(isf.DirectoryExists(subdirectory), "nested directory exists");
                isf.DeleteDirectory(subdirectory);
                Assert.False(isf.DirectoryExists(subdirectory), "directory removed");
                isf.DeleteDirectory(directory);
                Assert.False(isf.DirectoryExists(directory), "nested directory removed");
            }
        }
    }
}
