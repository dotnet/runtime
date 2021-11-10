// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    // Unix max paths are typically 1K or 4K UTF-8 bytes, 256 should handle the majority of paths
    // without putting too much pressure on the stack.
    internal const int DefaultPathBufferSize = 256;
}
