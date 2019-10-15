// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.IsolatedStorage
{
    public class DirectoryExistsTests : IsoStorageTest
    {
        [Fact]
        public void DirectoryExists_ThrowsArgumentNull()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<ArgumentNullException>(() => isf.DirectoryExists(null));
            }
        }

        [Fact]
        public void DirectoryExists_ThrowsObjectDisposed()
        {
            IsolatedStorageFile isf;
            using (isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
            }

            Assert.Throws<ObjectDisposedException>(() => isf.DirectoryExists("foo"));
        }

        [Fact]
        public void DirectoryExists_Removed_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<InvalidOperationException>(() => isf.DirectoryExists("foo"));
            }
        }

        [Fact]
        public void DirectoryExists_Closed_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.DirectoryExists("foo"));
            }
        }

        [Fact]
        public void DirectoryExists_False()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.False(isf.DirectoryExists("\0bad"));
            }
        }

        [Fact]
        public void DirectoryExists_Existance()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.CreateDirectory("DirectoryExists_Existance");

                Assert.True(isf.DirectoryExists("DirectoryExists_Existance"));
                isf.DeleteDirectory("DirectoryExists_Existance");
                Assert.False(isf.DirectoryExists("DirectoryExists_Existance"));
            }
        }

        [Theory]
        [MemberData(nameof(ValidStores))]
        public void DirectoryExists_Existance(PresetScopes scope)
        {
            using (var isf = GetPresetScope(scope))
            {
                string root = isf.GetUserRootDirectory();
                string directory = "DirectoryExists_Existance";
                isf.CreateDirectory(directory);

                Assert.True(Directory.Exists(Path.Combine(root, directory)), "exists per file.io where expected");
                Assert.True(isf.DirectoryExists(directory), "exists per iso");
                isf.DeleteDirectory(directory);
                Assert.False(Directory.Exists(Path.Combine(root, directory)), "doesn't exist per file.io where expected");
                Assert.False(isf.DirectoryExists(directory), "doesn't exist per iso");

                // Now nested
                directory = Path.Combine(directory, directory);
                isf.CreateDirectory(directory);

                Assert.True(Directory.Exists(Path.Combine(root, directory)), "exists nested per file.io where expected");
                Assert.True(isf.DirectoryExists(directory), "exists nested per iso");
                isf.DeleteDirectory(directory);
                Assert.False(Directory.Exists(Path.Combine(root, directory)), "doesn't exist nested per file.io where expected");
                Assert.False(isf.DirectoryExists(directory), "doesn't exist nested per iso");
            }
        }
    }
}
