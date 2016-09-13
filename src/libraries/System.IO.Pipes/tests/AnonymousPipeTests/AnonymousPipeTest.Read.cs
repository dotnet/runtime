// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class AnonymousPipeTest_Read_ServerIn_ClientOut : PipeTest_Read
    {
        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            ret.readablePipe = new AnonymousPipeServerStream(PipeDirection.In);
            ret.writeablePipe = new AnonymousPipeClientStream(PipeDirection.Out, ((AnonymousPipeServerStream)ret.readablePipe).ClientSafePipeHandle);
            return ret;
        }
    }

    public class AnonymousPipeTest_Read_ServerOut_ClientIn : PipeTest_Read
    {
        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            ret.writeablePipe = new AnonymousPipeServerStream(PipeDirection.Out);
            ret.readablePipe = new AnonymousPipeClientStream(PipeDirection.In, ((AnonymousPipeServerStream)ret.writeablePipe).ClientSafePipeHandle);
            return ret;
        }
    }
}

