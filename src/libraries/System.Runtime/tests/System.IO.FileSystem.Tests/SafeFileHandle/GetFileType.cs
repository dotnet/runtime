// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace System.IO.Tests
{
    public class SafeFileHandle_GetFileType : FileSystemTest
    {
        [Fact]
        public void GetFileType_RegularFile()
        {
            string path = GetTestFilePath();
            File.WriteAllText(path, "test");

            using SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read);
            Assert.Equal(FileHandleType.RegularFile, handle.Type);
        }

        [Fact]
        public void GetFileType_NullDevice()
        {
            using SafeFileHandle handle = File.OpenNullHandle();
            Assert.Equal(FileHandleType.CharacterDevice, handle.Type);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "System.IO.Pipes aren't supported on browser")]
        public void GetFileType_AnonymousPipe()
        {
            using AnonymousPipeServerStream server = new(PipeDirection.Out);
            using SafeFileHandle serverHandle = new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), ownsHandle: false);

            Assert.Equal(FileHandleType.Pipe, serverHandle.Type);

            using SafeFileHandle clientHandle = new SafeFileHandle(server.ClientSafePipeHandle.DangerousGetHandle(), ownsHandle: false);
            Assert.Equal(FileHandleType.Pipe, clientHandle.Type);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets aren't supported on browser")]
        public void GetFileType_Socket()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(listener.LocalEndPoint);

            using Socket server = listener.Accept();

            using SafeFileHandle serverHandle = new SafeFileHandle(server.Handle, ownsHandle: false);
            using SafeFileHandle clientHandle = new SafeFileHandle(client.Handle, ownsHandle: false);

            Assert.Equal(FileHandleType.Socket, serverHandle.Type);
            Assert.Equal(FileHandleType.Socket, clientHandle.Type);
        }

        [Fact]
        public void GetFileType_ClosedHandle_ThrowsObjectDisposedException()
        {
            SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write);
            Assert.Equal(FileHandleType.RegularFile, handle.Type);

            handle.Dispose();

            Assert.Throws<ObjectDisposedException>(() => handle.Type);
        }

        [Fact]
        public void GetFileType_CachesResult()
        {
            string path = GetTestFilePath();
            File.WriteAllText(path, "test");

            using SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read);

            FileHandleType firstCall = handle.Type;
            FileHandleType secondCall = handle.Type;

            Assert.Equal(firstCall, secondCall);
            Assert.Equal(FileHandleType.RegularFile, firstCall);
        }
    }
}
