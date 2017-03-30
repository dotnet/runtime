// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Net.Security
{
    // A public contract for a base abstract authenticated stream.
    public abstract class AuthenticatedStream : Stream
    {
        private Stream _innerStream;
        private bool _leaveStreamOpen;

        protected AuthenticatedStream(Stream innerStream, bool leaveInnerStreamOpen)
        {
            if (innerStream == null || innerStream == Stream.Null)
            {
                throw new ArgumentNullException(nameof(innerStream));
            }

            if (!innerStream.CanRead || !innerStream.CanWrite)
            {
                throw new ArgumentException(SR.net_io_must_be_rw_stream, nameof(innerStream));
            }

            _innerStream = innerStream;
            _leaveStreamOpen = leaveInnerStreamOpen;
        }

        public bool LeaveInnerStreamOpen
        {
            get
            {
                return _leaveStreamOpen;
            }
        }

        protected Stream InnerStream
        {
            get
            {
                return _innerStream;
            }
        }

        protected override void Dispose(bool disposing)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User))
            {
#endif
                try
                {
                    if (disposing)
                    {
                        if (_leaveStreamOpen)
                        {
                            _innerStream.Flush();
                        }
                        else
                        {
                            _innerStream.Dispose();
                        }
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
#if DEBUG
            }
#endif
        }

        public abstract bool IsAuthenticated { get; }
        public abstract bool IsMutuallyAuthenticated { get; }
        public abstract bool IsEncrypted { get; }
        public abstract bool IsSigned { get; }
        public abstract bool IsServer { get; }
    }
}



