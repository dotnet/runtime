// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;

namespace System.Net.Security
{
    // A public contract for a base abstract authenticated stream.
    public abstract class AuthenticatedStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly bool _leaveStreamOpen;

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
        }

        public override ValueTask DisposeAsync()
        {
            try
            {
                ValueTask vt = _leaveStreamOpen ?
                    new ValueTask(_innerStream.FlushAsync()) :
                    _innerStream.DisposeAsync();
                GC.SuppressFinalize(this);
                return vt;
            }
            catch (Exception exc)
            {
                return ValueTask.FromException(exc);
            }
        }

        public abstract bool IsAuthenticated { get; }
        public abstract bool IsMutuallyAuthenticated { get; }
        public abstract bool IsEncrypted { get; }
        public abstract bool IsSigned { get; }
        public abstract bool IsServer { get; }
    }
}
