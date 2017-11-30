// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Linq;
using Xunit;

namespace System.IO.IsolatedStorage
{
    [ActiveIssue(18940, TargetFrameworkMonikers.UapAot)]
    public class GetFileNamesTests : IsoStorageTest
    {
        [Fact]
        [ActiveIssue("dotnet/corefx #18268", TargetFrameworkMonikers.NetFramework)]
        public void GetFileNames_ThrowsArgumentNull()
        {
            using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                Assert.Throws<ArgumentNullException>(() => isf.GetFileNames(null));
            }
        }

        [Fact]
        public void GetFileNames_ThrowsObjectDisposed()
        {
            IsolatedStorageFile isf;
            using (isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
            }

            Assert.Throws<ObjectDisposedException>(() => isf.GetFileNames("foo"));
        }

        [Fact]
        public void GetFileNames_Deleted_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<InvalidOperationException>(() => isf.GetFileNames("foo"));
            }
        }

        [Fact]
        public void GetFileNames_Closed_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.GetFileNames("foo"));
            }
        }

        [Fact]
        [ActiveIssue(25428, TestPlatforms.AnyUnix)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void GetFileNames_RaisesInvalidPath_Core()
        {
            // We are no longer as agressive with filters for enumerating files
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.GetFileNames("\0bad");
            }
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void GetFileNames_RaisesInvalidPath_Desktop()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                AssertExtensions.Throws<ArgumentException>("path", null, () => isf.GetFileNames("\0bad"));
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        [ActiveIssue("dotnet/corefx #18265", TargetFrameworkMonikers.NetFramework)]
        public void GetFileNames_GetsFileNames(PresetScopes scope)
        {
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
                isf.CreateTestFile("A");
                isf.CreateTestFile("B");
                isf.CreateDirectory("C");
                isf.CreateTestFile(Path.Combine("C", "D"));
                isf.CreateTestFile(Path.Combine("C", "E"));
                Assert.Equal(new string[] { "A", "B" }, isf.GetFileNames().OrderBy(s => s));
                Assert.Equal(new string[] { "A", "B" }, isf.GetFileNames("*").OrderBy(s => s));
                Assert.Equal(new string[] { "A" }, isf.GetFileNames("A"));
                Assert.Equal(new string[] { "D", "E" }, isf.GetFileNames(Path.Combine("C", "*")).OrderBy(s => s));
                Assert.Equal(new string[] { "D" }, isf.GetFileNames(Path.Combine("C", "D")));
            }
        }
    }
}
