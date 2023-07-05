// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.IsolatedStorage
{
    public class FileExistsTests : IsoStorageTest
    {
        [Fact]
        public void FileExists_ThrowsArgumentNull()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<ArgumentNullException>(() => isf.FileExists(null));
            }
        }

        [Fact]
        public void FileExists_ThrowsObjectDisposed()
        {
            IsolatedStorageFile isf;
            using (isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
            }

            Assert.Throws<ObjectDisposedException>(() => isf.FileExists("foo"));
        }

        [Fact]
        public void FileExists_Removed_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<InvalidOperationException>(() => isf.FileExists("foo"));
            }
        }

        [Fact]
        public void FileExists_Closed_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.FileExists("foo"));
            }
        }

        [Fact]
        public void FileExists_False()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.False(isf.FileExists("\0bad"));
            }
        }

        [Theory]
        [MemberData(nameof(ValidStores))]
        public void FileExists_Existence(PresetScopes scope)
        {
            using (var isf = GetPresetScope(scope))
            {
                string root = isf.GetUserRootDirectory();
                string file = scope.ToString() + "FileExists_Existence";
                isf.CreateTestFile(file);

                Assert.True(File.Exists(Path.Combine(root, file)), "exists per file.io where expected");
                Assert.True(isf.FileExists(file), "exists per iso");
                isf.DeleteFile(file);
                Assert.False(File.Exists(Path.Combine(root, file)), "doesn't exist per file.io where expected");
                Assert.False(isf.FileExists(file), "doesn't exist per iso");

                // Now nested
                isf.CreateDirectory(file);
                file = Path.Combine(file, file);
                isf.CreateTestFile(file);

                Assert.True(File.Exists(Path.Combine(root, file)), "exists nested per file.io where expected");
                Assert.True(isf.FileExists(file), "exists nested per iso");
                isf.DeleteFile(file);
                Assert.False(File.Exists(Path.Combine(root, file)), "doesn't exist nested per file.io where expected");
                Assert.False(isf.FileExists(file), "doesn't exist nested per iso");
            }
        }
    }
}
