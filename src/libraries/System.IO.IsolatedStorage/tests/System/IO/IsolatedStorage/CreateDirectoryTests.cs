// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.IsolatedStorage
{
    [ActiveIssue(18940, TargetFrameworkMonikers.UapAot)]
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
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void CreateDirectory_RaisesArgumentException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                AssertExtensions.Throws<ArgumentException>("path", null, () => isf.CreateDirectory("\0bad"));
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void CreateDirectory_IsolatedStorageException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<IsolatedStorageException>(() => isf.CreateDirectory("\0bad"));
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        [ActiveIssue("dotnet/corefx #18268", TargetFrameworkMonikers.NetFramework)]
        public void CreateDirectory_Existance(PresetScopes scope)
        {
            using (var isf = GetPresetScope(scope))
            {
                string directory = "CreateDirectory_Existance";
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
