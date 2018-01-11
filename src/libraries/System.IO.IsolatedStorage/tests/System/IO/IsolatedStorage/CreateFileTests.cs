// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.IsolatedStorage
{
    [ActiveIssue(18940, TargetFrameworkMonikers.UapAot)]
    public class CreateFileTests : IsoStorageTest
    {
        [Fact]
        public void CreateFile_ThrowsArgumentNull()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<ArgumentNullException>(() => isf.CreateFile(null));
            }
        }

        [Fact]
        public void CreateRemovedFile_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<InvalidOperationException>(() => isf.CreateFile("foo"));
            }
        }

        [Fact]
        public void CreateFile_ThrowsObjectDisposed()
        {
            IsolatedStorageFile isf;
            using (isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
            }

            Assert.Throws<ObjectDisposedException>(() => isf.CreateFile("foo"));
        }

        [Fact]
        public void CreateClosedFile_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.CreateFile("foo"));
            }
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void CreateFile_RaisesArgumentException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                AssertExtensions.Throws<ArgumentException>("path", null, () => isf.CreateFile("\0bad"));
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void CreateFile_IsolatedStorageException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<IsolatedStorageException>(() => isf.CreateFile("\0bad"));
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        [ActiveIssue("dotnet/corefx #18268", TargetFrameworkMonikers.NetFramework)]
        public void CreateFile_Existence(PresetScopes scope)
        {
            using (var isf = GetPresetScope(scope))
            {
                string file = "CreateFile_Existence";
                string subdirectory = "CreateFile_Existence_Subdirectory";
                using (isf.CreateFile(file)) { }
                Assert.True(isf.FileExists(file), "file exists");
                isf.CreateDirectory(subdirectory);
                Assert.True(isf.DirectoryExists(subdirectory), "directory exists");

                string nestedFile = Path.Combine(subdirectory, file);
                using (isf.CreateFile(nestedFile)) { }

                Assert.True(isf.FileExists(nestedFile), "nested file exists");
            }
        }
    }
}
