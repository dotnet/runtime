// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_Exists : FileSystemTest
    {
        [Fact]
        public void InitializeExistsBeforeCreation()
        {
            string fileName = GetTestFilePath();
            FileInfo di = new FileInfo(fileName);
            // don't check it, data has not yet been init'ed
            File.Create(fileName).Dispose();
            // data will be init'ed at the time of calling exists
            Assert.True(di.Exists);
        }

        [Fact]
        public void InitializeExistsAfterCreation()
        {
            string fileName = GetTestFilePath();
            FileInfo di = new FileInfo(fileName);

            Assert.False(di.Exists);
            File.Create(fileName).Dispose();

            // data should be stale
            Assert.False(di.Exists);

            // force refresh
            di.Refresh();
            Assert.True(di.Exists);
        }

        [Fact]
        public void NonExistentFile()
        {
            Assert.False(new FileInfo("Da drar vi til fjells").Exists);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows | PlatformID.OSX)] // testing case-insensitivity
        public void CaseInsensitivity()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.True(new FileInfo(path.ToUpperInvariant()).Exists);
            Assert.True(new FileInfo(path.ToLowerInvariant()).Exists);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Linux | PlatformID.FreeBSD)] // testing case-Sensitivity
        public void CaseSensitivity()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.False(new FileInfo(path.ToUpperInvariant()).Exists);
            Assert.False(new FileInfo(path.ToLowerInvariant()).Exists);
        }

        [Fact]
        public void TrueForNewFileInfo()
        {
            string fileName = GetTestFilePath();
            File.Create(fileName).Dispose();

            FileInfo di = new FileInfo(fileName);
            Assert.True(di.Exists);
        }

        [Fact]
        public void FalseForDirectory()
        {
            string fileName = GetTestFilePath();
            Directory.CreateDirectory(fileName);
            FileInfo di = new FileInfo(fileName);
            Assert.False(di.Exists);
        }

        // In some cases (such as when running without elevated privileges,
        // the symbolic link may fail to create. Only run this test if it creates
        // links successfully.
        [ConditionalFact("CanCreateSymbolicLinks")]
        public void SymLinksExistIndependentlyOfTarget()
        {
            var path = GetTestFilePath();
            var linkPath = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.True(MountHelper.CreateSymbolicLink(linkPath, path));
            File.Delete(path);

            var info = new FileInfo(path);
            var linkInfo = new FileInfo(linkPath);
            Assert.True(linkInfo.Exists);
            Assert.False(info.Exists);
        }

        private static bool CanCreateSymbolicLinks
        {
            get
            {
                var path = Path.GetTempFileName();
                var linkPath = path + ".link";
                var ret = MountHelper.CreateSymbolicLink(linkPath, path);
                try { File.Delete(path); } catch { }
                try { File.Delete(linkPath); } catch { }
                return ret;
            }
        }
    }
}
