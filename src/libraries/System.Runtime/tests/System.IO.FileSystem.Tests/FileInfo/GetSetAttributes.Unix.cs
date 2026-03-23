// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public partial class FileInfo_GetSetAttributes
    {
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
