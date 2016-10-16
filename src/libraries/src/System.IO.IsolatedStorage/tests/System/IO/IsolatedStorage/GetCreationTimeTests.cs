// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.IsolatedStorage
{
    public class GetCreationTimeTests : IsoStorageTest
    {
        [Fact]
        public void GetCreationTime_ThrowsArgumentNull()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<ArgumentNullException>(() => isf.GetCreationTime(null));
            }
        }

        [Fact]
        public void GetCreationTime_ThrowsIsolatedStorageException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Remove();
                Assert.Throws<IsolatedStorageException>(() => isf.GetCreationTime("foo"));
            }
        }

        [Fact]
        public void GetCreationTime_ThrowsObjectDisposed()
        {
            IsolatedStorageFile isf;
            using (isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
            }

            Assert.Throws<ObjectDisposedException>(() => isf.GetCreationTime("foo"));
        }

        [Fact]
        public void GetCreationTime_ThrowsInvalidOperationException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                isf.Close();
                Assert.Throws<InvalidOperationException>(() => isf.GetCreationTime("foo"));
            }
        }

        [Fact]
        public void GetCreationTime_RaisesArgumentException()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                Assert.Throws<ArgumentException>(() => isf.GetCreationTime("\0bad"));
            }
        }

        [Fact]
        [ActiveIssue(12539, TestPlatforms.Linux)]
        [PlatformSpecific(TestPlatforms.FreeBSD | TestPlatforms.Linux | TestPlatforms.NetBSD)]
        public void GetCreationTime_GetsTime_Unix()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                string file = "GetCreationTime_GetsTime";
                isf.CreateTestFile(file);

                // Filesystem timestamps vary in granularity, we can't make a positive assertion that
                // the time will come before or after the current time.
                Assert.Equal(default(DateTimeOffset), isf.GetCreationTime(file));
            }
        }


        [Fact]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        public void GetCreationTime_GetsTime_Windows_OSX()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                string file = "GetCreationTime_GetsTime";
                isf.CreateTestFile(file);

                // Filesystem timestamps vary in granularity, we can't make a positive assertion that
                // the time will come before or after the current time.
                Assert.True(TestHelper.IsTimeCloseToNow(isf.GetCreationTime(file)));
            }
        }
    }
}
