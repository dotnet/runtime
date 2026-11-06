// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography;

internal static class IncrementalHashPolyfills
{
    extension(IncrementalHash hash)
    {
        public void AppendData(ArraySegment<byte> data) =>
            hash.AppendData(data.Array!, data.Offset, data.Count);
    }
}
