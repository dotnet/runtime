// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.IsolatedStorage
{
    [ActiveIssue(18940, TargetFrameworkMonikers.UapAot)]
    public class GetLastAccessTimeTests : IsoStorageTest
    {
        [Fact]
        public void GetLastAccessTime_ThrowsArgumentNull()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<ArgumentNullException>(() => isf.GetLastAccessTime(null));
            }
        }

        [Fact]
        public void GetLastAccessTime_Removed_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<InvalidOperationException>(() => isf.GetLastAccessTime("foo"));
            }
        }

        [Fact]
        public void GetLastAccessTime_ThrowsObjectDisposed()
        {
            IsolatedStorageFile isf;
            using (isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
            }

            Assert.Throws<ObjectDisposedException>(() => isf.GetLastAccessTime("foo"));
        }

        [Fact]
        public void GetLastAccessTime_Closed_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.GetLastAccessTime("foo"));
            }
        }

        [Fact]
        public void GetLastAccessTime_RaisesArgumentException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                AssertExtensions.Throws<ArgumentException>("path", null, () => isf.GetLastAccessTime("\0bad"));
            }
        }

        [Fact]
        [ActiveIssue("dotnet/corefx #18265", TargetFrameworkMonikers.NetFramework)]
        public void GetLastAccessTime_GetsTime()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                string file = "GetLastAccessTime_GetsTime";
                isf.CreateTestFile(file);

                // Filesystem timestamps vary in granularity, we can't make a positive assertion that
                // the time will come before or after the current time.
                Assert.True(TestHelper.IsTimeCloseToNow(isf.GetLastAccessTime(file)));
            }
        }
    }
}
