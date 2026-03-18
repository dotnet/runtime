// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_GetSetAttributes : InfoGetSetAttributes<FileInfo>
    {
        protected override bool CanBeReadOnly => true;
        protected override FileAttributes GetAttributes(string path) => new FileInfo(path).Attributes;
        protected override void SetAttributes(string path, FileAttributes attributes) => new FileInfo(path).Attributes = attributes;
        protected override FileInfo CreateInfo(string path) => new FileInfo(path);

        [Fact]
        public void IsReadOnly_SetAndGet()
        {
            FileInfo test = new FileInfo(GetTestFilePath());
            test.Create().Dispose();

            // Set to True
            test.IsReadOnly = true;
            test.Refresh();
            Assert.True(test.IsReadOnly);

            // Set To False
            test.IsReadOnly = false;
            test.Refresh();
            Assert.False(test.IsReadOnly);
        }

        [Theory]
        [InlineData(".", true)]
        [InlineData("", false)]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void HiddenAttributeSetCorrectly_OSX(string filePrefix, bool hidden)
        {
            string testFilePath = Path.Combine(TestDirectory, $"{filePrefix}{GetTestFileName()}");
            FileInfo fileInfo = new FileInfo(testFilePath);
            fileInfo.Create().Dispose();

            Assert.Equal(hidden, (fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void TogglingHiddenAttribute_PreservesOtherUserFlags()
        {
            // UF_NODUMP (0x01) is a harmless BSD user flag that any file owner can set/clear.
            const uint UF_NODUMP = 0x01;
            const uint UF_HIDDEN = (uint)Interop.Sys.UserFlags.UF_HIDDEN;

            string path = GetTestFilePath();
            File.Create(path).Dispose();

            // Set UF_NODUMP on the file directly via lchflags.
            Assert.Equal(0, Interop.Sys.LChflags(path, UF_NODUMP));
            Assert.Equal(0, Interop.Sys.Stat(path, out Interop.Sys.FileStatus before));
            Assert.NotEqual(0u, before.UserFlags & UF_NODUMP);

            // Toggle Hidden ON via the public API — this must preserve UF_NODUMP.
            var fi = new FileInfo(path);
            fi.Attributes |= FileAttributes.Hidden;

            Assert.Equal(0, Interop.Sys.Stat(path, out Interop.Sys.FileStatus afterSet));
            Assert.NotEqual(0u, afterSet.UserFlags & UF_HIDDEN);
            Assert.NotEqual(0u, afterSet.UserFlags & UF_NODUMP);

            // Toggle Hidden OFF — UF_NODUMP must still survive.
            fi.Refresh();
            fi.Attributes &= ~FileAttributes.Hidden;

            Assert.Equal(0, Interop.Sys.Stat(path, out Interop.Sys.FileStatus afterClear));
            Assert.Equal(0u, afterClear.UserFlags & UF_HIDDEN);
            Assert.NotEqual(0u, afterClear.UserFlags & UF_NODUMP);
        }
    }
}
