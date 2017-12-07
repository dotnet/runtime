// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security
{
    // A public contract for a base abstract authenticated stream.
    public abstract class AuthenticatedStream : Stream
    {
        protected AuthenticatedStream(Stream innerStream, bool leaveInnerStreamOpen)
        {
        }

        protected Stream InnerStream
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override void Dispose(bool disposing)
        {
        }

        public abstract bool IsAuthenticated { get; }
        public abstract bool IsMutuallyAuthenticated { get; }
        public abstract bool IsEncrypted { get; }
        public abstract bool IsSigned { get; }
        public abstract bool IsServer { get; }

        public new abstract Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token);
        public new abstract ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    }
}



