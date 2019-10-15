// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.IsolatedStorage
{
    public class GetLastWriteTimeTests : IsoStorageTest
    {
        [Fact]
        public void GetLastWriteTime_ThrowsArgumentNull()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<ArgumentNullException>(() => isf.GetLastWriteTime(null));
            }
        }

        [Fact]
        public void GetLastWriteTime_Deleted_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<InvalidOperationException>(() => isf.GetLastWriteTime("foo"));
            }
        }

        [Fact]
        public void GetLastWriteTime_ThrowsObjectDisposed()
        {
            IsolatedStorageFile isf;
            using (isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
            }

            Assert.Throws<ObjectDisposedException>(() => isf.GetLastWriteTime("foo"));
        }

        [Fact]
        public void GetLastWriteTime_Closed_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.GetLastWriteTime("foo"));
            }
        }

        [Fact]
        public void GetLastWriteTime_RaisesArgumentException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                AssertExtensions.Throws<ArgumentException>("path", null, () => isf.GetLastWriteTime("\0bad"));
            }
        }

        [Fact]
        public void GetLastWriteTime_GetsTime()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                string file = "GetLastWriteTime_GetsTime";
                isf.CreateTestFile(file);

                // Filesystem timestamps vary in granularity, we can't make a positive assertion that
                // the time will come before or after the current time.
                Assert.True(TestHelper.IsTimeCloseToNow(isf.GetLastWriteTime(file)));
            }
        }
    }
}
