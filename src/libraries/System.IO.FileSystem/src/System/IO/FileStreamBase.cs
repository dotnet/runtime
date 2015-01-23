// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    internal abstract class FileStreamBase : Stream
    {
        protected readonly FileStream _parent;

        protected FileStreamBase(FileStream parent)
        {
            _parent = parent;
        }

        // Expose the protected dispose so that FileStream can call it
        internal void DisposeInternal(bool disposing)
        {
            this.Dispose(disposing);
        }

        public abstract bool IsAsync { get; }
        public abstract string Name { get; }
        public abstract SafeFileHandle SafeFileHandle { get; }

        public abstract void Flush(bool flushToDisk);

    }
}
