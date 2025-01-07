// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Tests
{
    public partial class FileStream_ctor_options
    {
        private static long GetAllocatedSize(FileStream fileStream)
        {
            return 0;
        }

        private static bool SupportsPreallocation => false;

        private static bool IsGetAllocatedSizeImplemented => false;
    }
}
