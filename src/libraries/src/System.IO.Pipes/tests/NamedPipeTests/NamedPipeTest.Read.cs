// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class NamedPipeTest_Read_ServerOut_ClientIn : PipeTest_Read
    {
        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            string pipeName = GetUniquePipeName();
            var writeablePipe = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var readablePipe = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);

            Task clientConnect = readablePipe.ConnectAsync();
            writeablePipe.WaitForConnection();
            clientConnect.Wait();

            ret.readablePipe = readablePipe;
            ret.writeablePipe = writeablePipe;
            return ret;
        }
    }

    public class NamedPipeTest_Read_ServerIn_ClientOut : PipeTest_Read
    {
        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            string pipeName = GetUniquePipeName();
            var readablePipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var writeablePipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);

            Task clientConnect = writeablePipe.ConnectAsync();
            readablePipe.WaitForConnection();
            clientConnect.Wait();

            ret.readablePipe = readablePipe;
            ret.writeablePipe = writeablePipe;
            return ret;
        }
    }

    public class NamedPipeTest_Read_ServerInOut_ClientInOut : PipeTest_Read
    {
        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            string pipeName = GetUniquePipeName();
            var readablePipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var writeablePipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            Task clientConnect = writeablePipe.ConnectAsync();
            readablePipe.WaitForConnection();
            clientConnect.Wait();

            ret.readablePipe = readablePipe;
            ret.writeablePipe = writeablePipe;
            return ret;
        }

        // InOut pipes can be written/read from either direction
        public override void WriteToReadOnlyPipe_Throws_NotSupportedException() { }
        public override async Task ReadFromPipeWithClosedPartner_ReadNoBytes()
        {
            // On Unix a read from an InOut pipe with a closed partner will wait indefinitely
            // for bytes to be sent.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                await base.ReadFromPipeWithClosedPartner_ReadNoBytes();
        }
    }
}
