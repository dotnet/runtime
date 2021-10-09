// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if MS_IO_REDIST
namespace Microsoft.IO
#else
namespace System.IO
#endif
{
    internal enum SearchTarget
    {
        Files = 0x1,
        Directories = 0x2,
        Both = 0x3
    }
}
