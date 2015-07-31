// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    [Flags]
    public enum DecompressionMethods
    {
        None = 0,
        GZip = 1,
        Deflate = 2
    }
}
