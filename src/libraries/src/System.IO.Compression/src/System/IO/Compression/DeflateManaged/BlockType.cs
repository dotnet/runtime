// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Compression
{
    internal enum BlockType
    {
        Uncompressed = 0,
        Static = 1,
        Dynamic = 2
    }
}
