// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Security;

namespace System.IO
{
    // Only static data; no need to serialize
    internal static class __Error
    {
        internal static Exception GetEndOfFile()
        {
            return new EndOfStreamException(SR.IO_EOF_ReadBeyondEOF);
        }

        internal static Exception GetFileNotOpen()
        {
            return new ObjectDisposedException(null, SR.ObjectDisposed_FileClosed);
        }

        internal static Exception GetPipeNotOpen()
        {
            return new ObjectDisposedException(null, SR.ObjectDisposed_PipeClosed);
        }

        internal static Exception GetStreamIsClosed()
        {
            return new ObjectDisposedException(null, SR.ObjectDisposed_StreamIsClosed);
        }

        internal static Exception GetReadNotSupported()
        {
            return new NotSupportedException(SR.NotSupported_UnreadableStream);
        }

        internal static Exception GetSeekNotSupported()
        {
            return new NotSupportedException(SR.NotSupported_UnseekableStream);
        }

        internal static Exception GetWrongAsyncResult()
        {
            return new ArgumentException(SR.Argument_WrongAsyncResult);
        }

        internal static Exception GetEndReadCalledTwice()
        {
            // Should ideally be InvalidOperationExc but we can't maintain parity with Stream and FileStream without some work
            return new ArgumentException(SR.InvalidOperation_EndReadCalledMultiple);
        }

        internal static Exception GetEndWriteCalledTwice()
        {
            // Should ideally be InvalidOperationExc but we can't maintain parity with Stream and FileStream without some work
            return new ArgumentException(SR.InvalidOperation_EndWriteCalledMultiple);
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
