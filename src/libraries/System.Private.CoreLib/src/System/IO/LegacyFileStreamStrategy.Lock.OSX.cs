// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    internal sealed partial class LegacyFileStreamStrategy : FileStreamStrategy
    {
        internal override void Lock(long position, long length)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OSXFileLocking);
        }

        internal override void Unlock(long position, long length)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OSXFileLocking);
        }
    }
}
