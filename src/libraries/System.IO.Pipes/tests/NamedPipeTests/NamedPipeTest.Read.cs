// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    [ActiveIssue(22271, TargetFrameworkMonikers.UapNotUapAot)]
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
    
    [ActiveIssue(22271, TargetFrameworkMonikers.UapNotUapAot)]
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
    
    [ActiveIssue(22271, TargetFrameworkMonikers.UapNotUapAot)]
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

        public override bool SupportsBidirectionalReadingWriting => true;
    }
    
    [ActiveIssue(22271, TargetFrameworkMonikers.UapNotUapAot)]
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

        public override bool SupportsBidirectionalReadingWriting => true;
    }
}
