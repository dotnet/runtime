// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    public partial class FileStream : Stream
    {
        private void LockInternal(long position, long length)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OSXFileLocking);
        }

        private void UnlockInternal(long position, long length)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OSXFileLocking);
        }
    }
}
