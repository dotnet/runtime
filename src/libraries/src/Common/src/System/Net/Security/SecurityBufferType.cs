// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Security
{
    // sspi.h
    internal enum SecurityBufferType
    {
        SECBUFFER_EMPTY = 0,
        SECBUFFER_DATA = 1,
        SECBUFFER_TOKEN = 2,
        SECBUFFER_PKG_PARAMS = 3,
        SECBUFFER_MISSING = 4,
        SECBUFFER_EXTRA = 5,
        SECBUFFER_STREAM_TRAILER = 6,
        SECBUFFER_STREAM_HEADER = 7,
        SECBUFFER_PADDING = 9,    // non-data padding
        SECBUFFER_STREAM = 10,
        SECBUFFER_CHANNEL_BINDINGS = 14,
        SECBUFFER_TARGET_HOST = 16,
        SECBUFFER_ALERT = 17,
        SECBUFFER_APPLICATION_PROTOCOLS = 18,

        SECBUFFER_READONLY = unchecked((int)0x80000000),
        SECBUFFER_READONLY_WITH_CHECKSUM = 0x10000000
    }
}
