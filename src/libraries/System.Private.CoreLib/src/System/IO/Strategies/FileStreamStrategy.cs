// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    internal abstract class FileStreamStrategy : Stream
    {
        internal abstract bool IsAsync { get; }

        internal abstract string Name { get; }

        internal abstract SafeFileHandle SafeFileHandle { get; }

        internal IntPtr Handle => SafeFileHandle.DangerousGetHandle();

        internal abstract bool IsClosed { get; }

        internal abstract void Lock(long position, long length);

        internal abstract void Unlock(long position, long length);

        internal abstract void Flush(bool flushToDisk);

        internal abstract void DisposeInternal(bool disposing);
    }
}
