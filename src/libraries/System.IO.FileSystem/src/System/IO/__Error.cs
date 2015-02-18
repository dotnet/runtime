// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace System.IO
{
    /// <summary>
    ///     Provides centralized methods for creating exceptions for System.IO.FileSystem.
    /// </summary>
    [Pure]
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
            return new ArgumentException(SR.Arg_WrongAsyncResult);
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
    }
}
