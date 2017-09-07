// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Centralized error methods for the IO package.  
** Mostly useful for translating Win32 HRESULTs into meaningful
** error strings & exceptions.
**
**
===========================================================*/

using System;
using System.Runtime.InteropServices;
using Win32Native = Microsoft.Win32.Win32Native;
using System.Text;
using System.Globalization;
using System.Security;
using System.Diagnostics.Contracts;

namespace System.IO
{
    [Pure]
    internal static class __Error
    {
        internal static void EndOfFile()
        {
            throw new EndOfStreamException(SR.IO_EOF_ReadBeyondEOF);
        }

        internal static void FileNotOpen()
        {
            throw new ObjectDisposedException(null, SR.ObjectDisposed_FileClosed);
        }

        internal static void StreamIsClosed()
        {
            throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
        }

        internal static void MemoryStreamNotExpandable()
        {
            throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);
        }

        internal static void ReaderClosed()
        {
            throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
        }

        internal static void ReadNotSupported()
        {
            throw new NotSupportedException(SR.NotSupported_UnreadableStream);
        }

        internal static void WrongAsyncResult()
        {
            throw new ArgumentException(SR.Arg_WrongAsyncResult);
        }

        internal static void EndReadCalledTwice()
        {
            // Should ideally be InvalidOperationExc but we can't maitain parity with Stream and FileStream without some work
            throw new ArgumentException(SR.InvalidOperation_EndReadCalledMultiple);
        }

        internal static void EndWriteCalledTwice()
        {
            // Should ideally be InvalidOperationExc but we can't maintain parity with Stream and FileStream without some work
            throw new ArgumentException(SR.InvalidOperation_EndWriteCalledMultiple);
        }

        internal static void WriteNotSupported()
        {
            throw new NotSupportedException(SR.NotSupported_UnwritableStream);
        }
    }
}
