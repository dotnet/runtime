// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.IsolatedStorage
{
    [ActiveIssue(18940, TargetFrameworkMonikers.UapAot)]
    public class DeleteFileTests : IsoStorageTest
    {
        [Fact]
        public void DeleteFile_ThrowsArgumentNull()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<ArgumentNullException>(() =>isf.DeleteFile(null));
            }
        }

        [Fact]
        public void DeleteFile_ThrowsObjectDisposed()
        {
            IsolatedStorageFile isf;
            using (isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
            }

            Assert.Throws<ObjectDisposedException>(() => isf.DeleteFile("foo"));
        }

        [Fact]
        public void DeleteRemovedFile_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<InvalidOperationException>(() => isf.DeleteFile("foo"));
            }
        }

        [Fact]
        public void DeleteClosedFile_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.DeleteFile("foo"));
            }
        }

        [Fact]
        public void DeleteFile_RaisesInvalidPath()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<IsolatedStorageException>(() => isf.DeleteFile("\0bad"));
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        [ActiveIssue("dotnet/corefx #18265", TargetFrameworkMonikers.NetFramework)]
        public void DeleteFile_DeletesFile(PresetScopes scope)
        {
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
                isf.CreateTestFile("DeleteFile_DeletesFile");

                Assert.True(isf.FileExists("DeleteFile_DeletesFile"));
                isf.DeleteFile("DeleteFile_DeletesFile");
                Assert.False(isf.FileExists("DeleteFile_DeletesFile"));
            }
        }
    }
}
