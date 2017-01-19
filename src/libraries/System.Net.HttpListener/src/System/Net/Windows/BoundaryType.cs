// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    internal enum BoundaryType
    {
        ContentLength = 0, // Content-Length: XXX
        Chunked = 1, // Transfer-Encoding: chunked
        Multipart = 3,
        None = 4,
        Invalid = 5,
    }
}

