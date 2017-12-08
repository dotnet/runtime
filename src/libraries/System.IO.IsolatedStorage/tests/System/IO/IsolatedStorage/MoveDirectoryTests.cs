// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.IsolatedStorage
{
    [ActiveIssue(18940, TargetFrameworkMonikers.UapAot)]
    public class MoveDirectoryTests : IsoStorageTest
    {
        [Fact]
        public void MoveDirectory_ThrowsArgumentNull()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                AssertExtensions.Throws<ArgumentNullException>("sourceDirectoryName", () => isf.MoveDirectory(null, "bar"));
                AssertExtensions.Throws<ArgumentNullException>("destinationDirectoryName", () => isf.MoveDirectory("foo", null));
            }
        }

        [Fact]
        public void MoveDirectory_ThrowsArgumentException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                AssertExtensions.Throws<ArgumentException>("sourceDirectoryName", () => isf.MoveDirectory(string.Empty, "bar"));
                AssertExtensions.Throws<ArgumentException>("destinationDirectoryName", () => isf.MoveDirectory("foo", string.Empty));
            }
        }

        [Fact]
        public void MoveDirectory_ThrowsObjectDisposed()
        {
            IsolatedStorageFile isf;
            using (isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
            }

            Assert.Throws<ObjectDisposedException>(() => isf.MoveDirectory("foo", "bar"));
        }

        [Fact]
        public void MoveDirectory_Removed_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<InvalidOperationException>(() => isf.MoveDirectory("foo", "bar"));
            }
        }

        [Fact]
        public void MoveDirectory_Closed_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.MoveDirectory("foo", "bar"));
            }
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void MoveDirectory_RaisesInvalidPath()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                AssertExtensions.Throws<ArgumentException>("path", null, () => isf.MoveDirectory("\0bad", "bar"));
                AssertExtensions.Throws<ArgumentException>("path", null, () => isf.MoveDirectory("foo", "\0bad"));
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void MoveDirectory_IsolatedStorageException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<IsolatedStorageException>(() => isf.MoveDirectory("\0bad", "bar"));
                Assert.Throws<IsolatedStorageException>(() => isf.MoveDirectory("foo", "\0bad"));
            }
        }

        [Fact]
        public void MoveDirectory_DoesNotExist()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<DirectoryNotFoundException>(() => isf.MoveDirectory("MoveDirectory_DoesNotExist", "MoveDirectory_DoesNotExist_Copy"));
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        [ActiveIssue("dotnet/corefx #18265", TargetFrameworkMonikers.NetFramework)]
        public void MoveDirectory_MoveOver(PresetScopes scope)
        {
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
                isf.CreateDirectory("foo");
                isf.CreateDirectory("bar");
                Assert.Throws<IsolatedStorageException>(() => isf.MoveDirectory("foo", "bar"));
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        [ActiveIssue("dotnet/corefx #18265", TargetFrameworkMonikers.NetFramework)]
        public void MoveDirectory_MovesDirectory(PresetScopes scope)
        {
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
                isf.CreateDirectory("foo");
                isf.CreateTestFile(Path.Combine("foo", "foofile"), "MoveDirectory_MovesDirectory");
                isf.MoveDirectory("foo", "bar");
                Assert.True(isf.DirectoryExists("bar"), "bar exists");
                Assert.False(isf.DirectoryExists("foo"), "foo doesn't exist");
                Assert.Equal("MoveDirectory_MovesDirectory", isf.ReadAllText(Path.Combine("bar", "foofile")));

                // Move into nested
                isf.CreateDirectory("foo");
                string nestedDirectory = Path.Combine("foo", "foobar");
                isf.MoveDirectory("bar", nestedDirectory);
                Assert.True(isf.DirectoryExists(nestedDirectory), "nested directory exists");
                Assert.Equal("MoveDirectory_MovesDirectory", isf.ReadAllText(Path.Combine(nestedDirectory, "foofile")));
            }
        }
    }
}
