// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Contains helper methods used in AnonymousPipeTests
    /// </summary>
    public class AnonymousPipeTestBase : PipeTestBase
    {
        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            ret.readablePipe = new AnonymousPipeServerStream(PipeDirection.In);
            ret.writeablePipe = new AnonymousPipeClientStream(PipeDirection.Out, ((AnonymousPipeServerStream)ret.readablePipe).ClientSafePipeHandle);
            return ret;
        }

        protected static void StartClient(PipeDirection direction, SafePipeHandle clientPipeHandle)
        {
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(direction, clientPipeHandle))
            {
                DoStreamOperations(client);
            }
        }
    }
}
