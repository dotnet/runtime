// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.DotNet.RemoteExecutor;
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

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void MissingDirectory(char trailingChar)
        {
            string path = GetTestFilePath();
            FileInfo info = new FileInfo(Path.Combine(path, "file" + trailingChar));
            Assert.False(info.Exists);
        }

        [Fact]
        [PlatformSpecific(CaseInsensitivePlatforms)]
        public void CaseInsensitivity()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.True(new FileInfo(path.ToUpperInvariant()).Exists);
            Assert.True(new FileInfo(path.ToLowerInvariant()).Exists);
        }

        [Fact]
        [PlatformSpecific(CaseSensitivePlatforms)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52857", TestPlatforms.MacCatalyst)]
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

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser)]  // Uses P/Invokes
        public void TrueForNonRegularFile()
        {
            string fileName = GetTestFilePath();
            Assert.Equal(0, mkfifo(fileName, 0));
            FileInfo fi = new FileInfo(fileName);
            Assert.True(fi.Exists);
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void SymLinksMayExistIndependentlyOfTarget()
        {
            var path = GetTestFilePath();
            var linkPath = GetTestFilePath();

            var pathFI = new FileInfo(path);
            var linkPathFI = new FileInfo(linkPath);

            pathFI.Create().Dispose();
            Assert.True(MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: false));

            // Both the symlink and the target exist
            pathFI.Refresh();
            linkPathFI.Refresh();
            Assert.True(pathFI.Exists, "path should exist");
            Assert.True(linkPathFI.Exists, "linkPath should exist");

            // Delete the target.  The symlink should still exist
            pathFI.Delete();
            pathFI.Refresh();
            linkPathFI.Refresh();
            Assert.False(pathFI.Exists, "path should now not exist");
            Assert.True(linkPathFI.Exists, "linkPath should still exist");

            // Now delete the symlink.
            linkPathFI.Delete();
            linkPathFI.Refresh();
            Assert.False(linkPathFI.Exists, "linkPath should no longer exist");
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void UnsharedFileExists()
        {
            string path = GetTestFilePath();
            using (FileStream stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            {
                RemoteExecutor.Invoke((p) =>
                {
                    FileInfo info = new FileInfo(p);
                    Assert.True(info.Exists);
                }, path).Dispose();
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnPlatform(TestPlatforms.OSX, "Not supported on OSX.")]
        public void LockedFileExists()
        {
            string path = GetTestFilePath();
            File.WriteAllBytes(path, new byte[10]);

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                stream.Lock(0, 10);

                RemoteExecutor.Invoke((p) =>
                {
                    FileInfo info = new FileInfo(p);
                    Assert.True(info.Exists);
                }, path).Dispose();

                stream.Unlock(0, 10);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInAppContainer))] // Can't read root in appcontainer
        [PlatformSpecific(TestPlatforms.Windows)]
        public void PageFileExists()
        {
            // Typically there is a page file on the C: drive, if not, don't bother trying to track it down.
            string pageFilePath = Directory.EnumerateFiles(@"C:\", "pagefile.sys").FirstOrDefault();
            if (pageFilePath != null)
            {
                FileInfo info = new FileInfo(pageFilePath);
                Assert.True(info.Exists);
            }
        }
    }
}
