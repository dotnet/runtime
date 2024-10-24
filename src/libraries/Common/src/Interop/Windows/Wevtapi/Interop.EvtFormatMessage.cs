// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        [LibraryImport(Libraries.Wevtapi, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EvtFormatMessage(
                             EventLogHandle publisherMetadataHandle,
                             EventLogHandle eventHandle,
                             uint messageId,
                             int valueCount,
                             EvtStringVariant[] values,
                             EVT_FORMAT_MESSAGE_FLAGS flags,
                             int bufferSize,
                             Span<char> buffer,
                             out int bufferUsed);

        [LibraryImport(Libraries.Wevtapi, EntryPoint = "EvtFormatMessage", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EvtFormatMessageBuffer(
                             EventLogHandle publisherMetadataHandle,
                             EventLogHandle eventHandle,
                             uint messageId,
                             int valueCount,
                             IntPtr values,
                             Wevtapi.EVT_FORMAT_MESSAGE_FLAGS flags,
                             int bufferSize,
                             IntPtr buffer,
                             out int bufferUsed);
    }
}
