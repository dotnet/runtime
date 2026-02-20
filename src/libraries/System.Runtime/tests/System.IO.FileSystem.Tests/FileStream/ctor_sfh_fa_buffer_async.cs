// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UnmatchedAsyncIsAllowed(bool isAsync)
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, 4096, isAsync))
            {
                // isAsync parameter is now ignored, handle.IsAsync is used instead
                using (FileStream newFs = CreateFileStream(fs.SafeFileHandle, FileAccess.ReadWrite, 4096, !isAsync))
                {
                    // Verify that the new FileStream uses handle's IsAsync, not the parameter
                    Assert.Equal(isAsync, newFs.IsAsync);

                    // Perform async write, seek to beginning, and async read to verify functionality
                    byte[] writeBuffer = new byte[] { 1, 2, 3, 4, 5 };
                    await newFs.WriteAsync(writeBuffer, 0, writeBuffer.Length);
                    
                    newFs.Seek(0, SeekOrigin.Begin);
                    
                    byte[] readBuffer = new byte[writeBuffer.Length];
                    int bytesRead = await newFs.ReadAsync(readBuffer, 0, readBuffer.Length);
                    
                    Assert.Equal(writeBuffer.Length, bytesRead);
                    Assert.Equal(writeBuffer, readBuffer);
                }
            }
        }
    }
}
