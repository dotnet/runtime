// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
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
        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void SymLinksMayExistIndependentlyOfTarget()
        {
            var path = GetTestFilePath();
            var linkPath = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.True(MountHelper.CreateSymbolicLink(linkPath, path));
            File.Delete(path);

            // We've delete the target file, so it shouldn't exist.
            var info = new FileInfo(path);
            Assert.False(info.Exists);

            // On Windows we report about the existence of the symlink file itself, so
            // does still exist.  On Unix, we report about the target, where it doesn't.
            var linkInfo = new FileInfo(linkPath);
            Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), linkInfo.Exists);
        }
    }
}
