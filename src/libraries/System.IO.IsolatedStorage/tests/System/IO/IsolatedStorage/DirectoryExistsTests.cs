// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.IsolatedStorage
{
    public class DirectoryExistsTests : IsoStorageTest
    {
        [Fact]
        public void DirectoryExists_ThrowsArugmentNull()
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
        public void DirectoryExists_ThrowsIsolatedStorageException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<IsolatedStorageException>(() => isf.DirectoryExists("foo"));
            }
        }

        [Fact]
        public void DirectoryExists_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.DirectoryExists("foo"));
            }
        }

        [Fact]
        public void DirectoryExists_RaisesArgumentException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<ArgumentException>(() => isf.DirectoryExists("\0bad"));
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
    }
}
