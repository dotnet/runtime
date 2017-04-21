// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    // Only static data; no need to serialize
    internal static class Error
    {
        internal static Exception GetEndOfFile()
        {
            return new EndOfStreamException(SR.IO_EOF_ReadBeyondEOF);
        }

        internal static Exception GetPipeNotOpen()
        {
            return new ObjectDisposedException(null, SR.ObjectDisposed_PipeClosed);
        }

        internal static Exception GetReadNotSupported()
        {
            return new NotSupportedException(SR.NotSupported_UnreadableStream);
        }

        internal static Exception GetSeekNotSupported()
        {
            return new NotSupportedException(SR.NotSupported_UnseekableStream);
        }

        internal static Exception GetWriteNotSupported()
        {
            return new NotSupportedException(SR.NotSupported_UnwritableStream);
        }

        internal static Exception GetOperationAborted()
        {
            return new IOException(SR.IO_OperationAborted);
        }
    }
}
