// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_ctor_sfh_fa_buffer_async : FileStream_ctor_sfh_fa_buffer
    {
        protected sealed override FileStream CreateFileStream(SafeFileHandle handle, FileAccess access, int bufferSize)
        {
            return CreateFileStream(handle, access, bufferSize, false);
        }

        protected virtual FileStream CreateFileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
        {
            return new FileStream(handle, access, bufferSize, isAsync);
        }

        [Fact]
        public void MatchedAsync()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, 4096, true))
            {
                using (CreateFileStream(fs.SafeFileHandle, FileAccess.ReadWrite, 4096, true))
                { }
            }
        }

        [Fact]
        public void UnmatchedAsyncIsAllowed()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, 4096, true))
            {
                // isAsync parameter is now ignored, handle.IsAsync is used instead
                using (FileStream newFs = CreateFileStream(fs.SafeFileHandle, FileAccess.ReadWrite, 4096, false))
                {
                    // Verify that the new FileStream uses handle's IsAsync (true), not the parameter (false)
                    Assert.True(newFs.IsAsync);
                }
            }

            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, 4096, false))
            {
                // isAsync parameter is now ignored, handle.IsAsync is used instead
                using (FileStream newFs = CreateFileStream(fs.SafeFileHandle, FileAccess.ReadWrite, 4096, true))
                {
                    // Verify that the new FileStream uses handle's IsAsync (false), not the parameter (true)
                    Assert.False(newFs.IsAsync);
                }
            }
        }
    }
}
