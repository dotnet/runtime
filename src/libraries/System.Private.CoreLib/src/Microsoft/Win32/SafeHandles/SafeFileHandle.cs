// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private string? _path;
        private volatile int _cachedFileType = -1;

        /// <summary>
        /// Creates a <see cref="T:Microsoft.Win32.SafeHandles.SafeFileHandle" /> around a file handle.
        /// </summary>
        /// <param name="preexistingHandle">Handle to wrap</param>
        /// <param name="ownsHandle">Whether to control the handle lifetime</param>
        public SafeFileHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        internal string? Path => _path;

        /// <summary>
        /// Gets the type of the file that this handle represents.
        /// </summary>
        /// <returns>The type of the file.</returns>
        /// <exception cref="ObjectDisposedException">The handle is closed.</exception>
        public System.IO.FileType GetFileType()
        {
            ObjectDisposedException.ThrowIf(IsClosed, this);
            return GetFileTypeCore();
        }
    }
}
