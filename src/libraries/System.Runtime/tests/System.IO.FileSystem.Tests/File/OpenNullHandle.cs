// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class File_OpenNullHandle : FileSystemTest
    {
        [Fact]
        public void OpenNullHandle_ReturnsValidHandle()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            Assert.NotNull(handle);
            Assert.False(handle.IsInvalid);
            Assert.False(handle.IsClosed);
        }

        [Fact]
        public void OpenNullHandle_CanBeUsedWithFileStream()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            using FileStream stream = new FileStream(handle, FileAccess.ReadWrite);
            Assert.True(stream.CanRead);
            Assert.True(stream.CanWrite);
        }

        [Fact]
        public void OpenNullHandle_SyncRead_ReturnsZero()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            using FileStream stream = new FileStream(handle, FileAccess.Read);
            
            byte[] buffer = new byte[100];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            Assert.Equal(0, bytesRead);
        }

        [Fact]
        public void OpenNullHandle_SyncReadSpan_ReturnsZero()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            using FileStream stream = new FileStream(handle, FileAccess.Read);
            
            byte[] buffer = new byte[100];
            int bytesRead = stream.Read(buffer.AsSpan());
            Assert.Equal(0, bytesRead);
        }

        [Fact]
        public async Task OpenNullHandle_AsyncRead_ReturnsZero()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            using FileStream stream = new FileStream(handle, FileAccess.Read, bufferSize: 4096, useAsync: true);
            
            byte[] buffer = new byte[100];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            Assert.Equal(0, bytesRead);
        }

        [Fact]
        public async Task OpenNullHandle_AsyncReadMemory_ReturnsZero()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            using FileStream stream = new FileStream(handle, FileAccess.Read, bufferSize: 4096, useAsync: true);
            
            byte[] buffer = new byte[100];
            int bytesRead = await stream.ReadAsync(buffer.AsMemory());
            Assert.Equal(0, bytesRead);
        }

        [Fact]
        public void OpenNullHandle_SyncWrite_Succeeds()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            using FileStream stream = new FileStream(handle, FileAccess.Write);
            
            byte[] buffer = new byte[100];
            // Should not throw
            stream.Write(buffer, 0, buffer.Length);
        }

        [Fact]
        public void OpenNullHandle_SyncWriteSpan_Succeeds()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            using FileStream stream = new FileStream(handle, FileAccess.Write);
            
            byte[] buffer = new byte[100];
            // Should not throw
            stream.Write(buffer.AsSpan());
        }

        [Fact]
        public async Task OpenNullHandle_AsyncWrite_Succeeds()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            using FileStream stream = new FileStream(handle, FileAccess.Write, bufferSize: 4096, useAsync: true);
            
            byte[] buffer = new byte[100];
            // Should not throw
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        [Fact]
        public async Task OpenNullHandle_AsyncWriteMemory_Succeeds()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            using FileStream stream = new FileStream(handle, FileAccess.Write, bufferSize: 4096, useAsync: true);
            
            byte[] buffer = new byte[100];
            // Should not throw
            await stream.WriteAsync(buffer.AsMemory());
        }

        [Fact]
        public void OpenNullHandle_MultipleReads_AlwaysReturnZero()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            using FileStream stream = new FileStream(handle, FileAccess.Read);
            
            byte[] buffer = new byte[100];
            for (int i = 0; i < 10; i++)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                Assert.Equal(0, bytesRead);
            }
        }

        [Fact]
        public void OpenNullHandle_MultipleWrites_AllSucceed()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            using FileStream stream = new FileStream(handle, FileAccess.Write);
            
            byte[] buffer = new byte[100];
            for (int i = 0; i < 10; i++)
            {
                // Should not throw
                stream.Write(buffer, 0, buffer.Length);
            }
        }

        [Fact]
        public void OpenNullHandle_ReadWriteAccess_BothWork()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            using FileStream stream = new FileStream(handle, FileAccess.ReadWrite);
            
            byte[] writeBuffer = new byte[100];
            byte[] readBuffer = new byte[100];
            
            // Write should succeed
            stream.Write(writeBuffer, 0, writeBuffer.Length);
            
            // Read should return 0
            int bytesRead = stream.Read(readBuffer, 0, readBuffer.Length);
            Assert.Equal(0, bytesRead);
        }

        [Fact]
        public async Task OpenNullHandle_UseAsync_True_AsyncOperationsWork()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            using FileStream stream = new FileStream(handle, FileAccess.ReadWrite, bufferSize: 4096, useAsync: true);
            
            byte[] writeBuffer = new byte[100];
            byte[] readBuffer = new byte[100];
            
            // Async write should succeed
            await stream.WriteAsync(writeBuffer, 0, writeBuffer.Length);
            
            // Async read should return 0
            int bytesRead = await stream.ReadAsync(readBuffer, 0, readBuffer.Length);
            Assert.Equal(0, bytesRead);
        }

        [Fact]
        public void OpenNullHandle_MultipleHandles_AllWork()
        {
            using SafeFileHandle handle1 = File.OpenNullHandle();
            using SafeFileHandle handle2 = File.OpenNullHandle();
            using FileStream stream1 = new FileStream(handle1, FileAccess.ReadWrite);
            using FileStream stream2 = new FileStream(handle2, FileAccess.ReadWrite);
            
            byte[] buffer = new byte[100];
            
            // Both handles should work independently
            stream1.Write(buffer, 0, buffer.Length);
            stream2.Write(buffer, 0, buffer.Length);
            
            Assert.Equal(0, stream1.Read(buffer, 0, buffer.Length));
            Assert.Equal(0, stream2.Read(buffer, 0, buffer.Length));
        }
    }
}
