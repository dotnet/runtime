// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public abstract partial class FileStreamBase : Stream
    {
        //
        // FileStream methods (not inherited from Stream)
        //

        public abstract bool IsAsync { get; }
        public abstract string Name { get; }

        public abstract SafeFileHandle SafeFileHandle { get; }
        internal abstract bool IsClosed { get; }

        public abstract void Flush(bool flushToDisk);
        public abstract void Lock(long position, long length);
        public abstract void Unlock(long position, long length);
    }
}
