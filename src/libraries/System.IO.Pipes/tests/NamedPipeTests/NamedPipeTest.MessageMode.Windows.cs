// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Tests;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    // Support for PipeAccessRights and setting ReadMode to Message is only supported on Windows
    public class NamedPipeTest_MessageMode_Windows
    {
        private const PipeAccessRights MinimumMessageAccessRights = PipeAccessRights.ReadData | PipeAccessRights.WriteAttributes;

        private static NamedPipeClientStream CreateClientStream(string pipeName, PipeOptions options) =>
            new NamedPipeClientStream(".", pipeName, MinimumMessageAccessRights, options, Security.Principal.TokenImpersonationLevel.None, HandleInheritability.None);

        [Theory]
        [InlineData(PipeDirection.Out, PipeOptions.None)]
        [InlineData(PipeDirection.InOut, PipeOptions.Asynchronous)]
        public async Task Client_DetectsMessageCompleted(PipeDirection serverDirection, PipeOptions options)
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();

            using NamedPipeServerStream server = new NamedPipeServerStream(pipeName, serverDirection, 1, PipeTransmissionMode.Message, options);
            using NamedPipeClientStream client = CreateClientStream(pipeName, options);

            Task.WaitAll(server.WaitForConnectionAsync(), client.ConnectAsync());
            client.ReadMode = PipeTransmissionMode.Message;

            ValueTask serverWrite = server.WriteAsync(new byte[] { 1, 2, 3, 4, 5 });

            byte[] buffer1 = new byte[2], buffer2 = new byte[2], buffer3 = new byte[2];
            bool[] messageCompleted = new bool[3];

            int bytesRead = client.Read(buffer1, 0, 2);
            messageCompleted[0] = client.IsMessageComplete;

            bytesRead += client.Read(buffer2, 0, 2);
            messageCompleted[1] = client.IsMessageComplete;

            bytesRead += client.Read(buffer3, 0, 2);
            messageCompleted[2] = client.IsMessageComplete;

            Assert.Equal(5, bytesRead);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 0 }, buffer1.Concat(buffer2).Concat(buffer3));
            Assert.Equal(new bool[] { false, false, true }, messageCompleted);

            await serverWrite;
        }

        [Theory]
        [InlineData(PipeTransmissionMode.Byte, PipeOptions.None)]
        [InlineData(PipeTransmissionMode.Message, PipeOptions.None)]
        [InlineData(PipeTransmissionMode.Byte, PipeOptions.Asynchronous)]
        [InlineData(PipeTransmissionMode.Message, PipeOptions.Asynchronous)]
        public void ServerIn_ClientConnect_Throws(PipeTransmissionMode serverMode, PipeOptions options)
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();

            using NamedPipeServerStream server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, serverMode, options);
            using NamedPipeClientStream client = CreateClientStream(pipeName, options);

            Assert.Throws<UnauthorizedAccessException>(() => client.Connect());
        }

        [Theory]
        [InlineData(PipeDirection.Out, PipeTransmissionMode.Byte, PipeOptions.None)]
        [InlineData(PipeDirection.Out, PipeTransmissionMode.Byte, PipeOptions.Asynchronous)]
        [InlineData(PipeDirection.InOut, PipeTransmissionMode.Byte, PipeOptions.None)]
        [InlineData(PipeDirection.InOut, PipeTransmissionMode.Byte, PipeOptions.Asynchronous)]
        public void ServerByteMode_ClientReadModeMessage_Throws(PipeDirection serverDirection, PipeTransmissionMode serverMode, PipeOptions options)
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();

            using NamedPipeServerStream server = new NamedPipeServerStream(pipeName, serverDirection, 1, serverMode, options);
            using NamedPipeClientStream client = CreateClientStream(pipeName, options);

            Task.WaitAll(server.WaitForConnectionAsync(), client.ConnectAsync());

            Assert.Throws<IOException>(() => client.ReadMode = PipeTransmissionMode.Message);
        }

        [Fact]
        public void PipeAccessRights_Without_WriteAttributes_ClientReadModeMessage_Throws()
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
            PipeAccessRights rights = MinimumMessageAccessRights & ~PipeAccessRights.WriteAttributes;

            using NamedPipeServerStream server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message);
            using NamedPipeClientStream client = new NamedPipeClientStream(".", pipeName, rights, PipeOptions.None, Security.Principal.TokenImpersonationLevel.None, HandleInheritability.None);

            Task.WaitAll(server.WaitForConnectionAsync(), client.ConnectAsync());

            Assert.Throws<UnauthorizedAccessException>(() => client.ReadMode = PipeTransmissionMode.Message);
        }
    }
}
