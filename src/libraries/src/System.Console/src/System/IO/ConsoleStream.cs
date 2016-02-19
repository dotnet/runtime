// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    // Provides the platform-agnostic functionality for streams used as console input and output.
    // Platform-specific implementations derive from ConsoleStream to implement Read and Write
    // (and optionally Flush), as well as any additional ctor/Dispose logic necessary.
    internal abstract class ConsoleStream : Stream
    {
        private bool _canRead, _canWrite;

        internal ConsoleStream(FileAccess access)
        {
            Debug.Assert(access == FileAccess.Read || access == FileAccess.Write);
            _canRead = ((access & FileAccess.Read) == FileAccess.Read);
            _canWrite = ((access & FileAccess.Write) == FileAccess.Write);
        }

        protected override void Dispose(bool disposing)
        {
            _canRead = false;
            _canWrite = false;
            base.Dispose(disposing);
        }

        public sealed override bool CanRead
        {
            [Pure]
            get { return _canRead; }
        }

        public sealed override bool CanWrite
        {
            [Pure]
            get { return _canWrite; }
        }

        public sealed override bool CanSeek
        {
            [Pure]
            get { return false; }
        }

        public sealed override long Length
        {
            get { throw Error.GetSeekNotSupported(); }
        }

        public sealed override long Position
        {
            get { throw Error.GetSeekNotSupported(); }
            set { throw Error.GetSeekNotSupported(); }
        }

        public override void Flush()
        {
            if (!CanWrite) throw Error.GetWriteNotSupported();
        }

        public sealed override void SetLength(long value)
        {
            throw Error.GetSeekNotSupported();
        }

        public sealed override long Seek(long offset, SeekOrigin origin)
        {
            throw Error.GetSeekNotSupported();
        }

        protected void ValidateRead(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException((offset < 0 ? "offset" : "count"), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();
            if (!_canRead) throw Error.GetReadNotSupported();
        }

        protected void ValidateWrite(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException((offset < 0 ? "offset" : "count"), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();
            if (!_canWrite) throw Error.GetWriteNotSupported();
        }
    }
}
