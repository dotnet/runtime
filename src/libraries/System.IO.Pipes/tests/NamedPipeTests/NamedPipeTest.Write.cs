// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class NamedPipeTest_Write_ServerOut_ClientIn : PipeTest_Write
    {
        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            string pipeName = GetUniquePipeName();
            var writeablePipe = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var readablePipe = new NamedPipeClientStream(".", pipeName, PipeDirection.In);

            Task clientConnect = readablePipe.ConnectAsync();
            writeablePipe.WaitForConnection();
            clientConnect.Wait();

            ret.readablePipe = readablePipe;
            ret.writeablePipe = writeablePipe;
            return ret;
        }
    }

    public class NamedPipeTest_Write_ServerIn_ClientOut : PipeTest_Write
    {
        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            string pipeName = GetUniquePipeName();
            var readablePipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var writeablePipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);

            Task clientConnect = writeablePipe.ConnectAsync();
            readablePipe.WaitForConnection();
            clientConnect.Wait();

            ret.readablePipe = readablePipe;
            ret.writeablePipe = writeablePipe;
            return ret;
        }
    }

    public class NamedPipeTest_Write_ServerInOut_ClientInOut : PipeTest_Write
    {
        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            string pipeName = GetUniquePipeName();
            var readablePipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var writeablePipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);

            Task clientConnect = writeablePipe.ConnectAsync();
            readablePipe.WaitForConnection();
            clientConnect.Wait();

            ret.readablePipe = readablePipe;
            ret.writeablePipe = writeablePipe;
            return ret;
        }

        // InOut pipes can be written/read from either direction
        public override void ReadOnWriteOnlyPipe_Throws_NotSupportedException() { }
        public override void WriteToPipeWithClosedPartner_Throws_IOException()
        {
            // timeout on Unix: 
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                base.WriteToPipeWithClosedPartner_Throws_IOException();
        }
    }
}
