// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Security.AccessControl;

namespace System.IO.Pipes
{
    public static class PipesAclExtensions
    {
        [System.Security.SecurityCritical]
        public static PipeSecurity GetAccessControl(this PipeStream stream)
        {
            // PipeState can not be Closed and the Handle can not be null or closed
            var handle = stream.SafePipeHandle; // A non-null, open handle implies a non-closed PipeState.
            return new PipeSecurity(handle, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        [System.Security.SecurityCritical]
        public static void SetAccessControl(this PipeStream stream, PipeSecurity pipeSecurity)
        {
            // PipeState can not be Closed and the Handle can not be null or closed
            if (pipeSecurity == null)
            {
                throw new ArgumentNullException(nameof(pipeSecurity));
            }

            var handle = stream.SafePipeHandle; // A non-null, open handle implies a non-closed PipeState.

            // NamedPipeClientStream: PipeState can not be WaitingToConnect or Broken. WaitingToConnect is covered by the SafePipeHandle check.
            if (stream is NamedPipeClientStream && !stream.IsConnected) // Pipe could also be WaitingToConnect, Broken, Disconnected, or Closed.
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeNotYetConnected);
            }

            pipeSecurity.Persist(handle);
        }
    }
}
