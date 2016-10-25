// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.IO.Pipes.Tests
{
    public class NamedPipeTest_Read_ServerInOut_ClientInOut_APMWaitForConnection : PipeTest_Read
    {
        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            string pipeName = GetUniquePipeName();
            var readablePipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var writeablePipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            Task serverConnect = Task.Factory.FromAsync(readablePipe.BeginWaitForConnection, readablePipe.EndWaitForConnection, null);
            writeablePipe.Connect();
            serverConnect.Wait();

            ret.readablePipe = readablePipe;
            ret.writeablePipe = writeablePipe;
            return ret;
        }

        // InOut pipes can be written/read from either direction
        public override void WriteToReadOnlyPipe_Throws_NotSupportedException() { }
    }
}
