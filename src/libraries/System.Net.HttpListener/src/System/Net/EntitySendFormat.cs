// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    internal enum EntitySendFormat
    {
        ContentLength = 0, // Content-Length: XXX
        Chunked = 1, // Transfer-Encoding: chunked
    }
}
