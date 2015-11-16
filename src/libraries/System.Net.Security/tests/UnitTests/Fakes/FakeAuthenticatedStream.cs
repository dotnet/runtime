// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

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
    }
}



