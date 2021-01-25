// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public partial class FileStream : Stream
    {
        private static void LockInternal(long position, long length)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OSXFileLocking);
        }

        private static void UnlockInternal(long position, long length)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OSXFileLocking);
        }
    }
}
