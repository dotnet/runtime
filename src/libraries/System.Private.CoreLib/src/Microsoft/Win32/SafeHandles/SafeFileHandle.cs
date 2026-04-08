// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private string? _path;
        private int _cachedFileType = -1;

        /// <summary>
        /// Creates an anonymous pipe.
        /// </summary>
        /// <param name="readHandle">When this method returns, contains the read end of the pipe.</param>
        /// <param name="writeHandle">When this method returns, contains the write end of the pipe.</param>
        /// <param name="asyncRead"><see langword="true"/> to enable asynchronous IO for the read end of the pipe; otherwise, <see langword="false"/>.</param>
        /// <param name="asyncWrite"><see langword="true"/> to enable asynchronous IO for the write end of the pipe; otherwise, <see langword="false"/>.</param>
        /// <remarks>
        /// <para>
        /// The created handles are not inheritable by design to avoid accidental handle leaks to child processes.
        /// </para>
        /// <para>
        /// On Windows, async handles are created with the FILE_FLAG_OVERLAPPED flag.
        /// On Unix, async handles are created with the O_NONBLOCK flag.
        /// </para>
        /// </remarks>
        public static partial void CreateAnonymousPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle, bool asyncRead = false, bool asyncWrite = false);

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
        /// Gets the path of the file that this handle represents.
        /// </summary>
        /// <value>
        /// A string that represents the path of the file,
        /// or <c>[Unknown]</c> if the path cannot be determined.
        /// </value>
        /// <exception cref="ObjectDisposedException">The handle is closed.</exception>
        /// <remarks>
        /// <para>
        /// If the <see cref="SafeFileHandle"/> was created by opening a file via
        /// <see cref="System.IO.File.OpenHandle"/> or <see cref="System.IO.FileStream"/>,
        /// this property returns the path that was provided to those APIs.
        /// </para>
        /// <para>
        /// If the handle was created from a raw OS handle (for example, via
        /// <see cref="SafeFileHandle(IntPtr, bool)"/>), this property attempts to
        /// retrieve the path from the operating system.
        /// On Windows, <c>GetFinalPathNameByHandle</c> is used.
        /// On Linux, the <c>/proc/self/fd</c> symlink is read.
        /// On macOS and FreeBSD, <c>fcntl(F_GETPATH)</c> is used.
        /// On other platforms, <c>[Unknown]</c> is returned.
        /// </para>
        /// </remarks>
        public string Name
        {
            get
            {
                ObjectDisposedException.ThrowIf(IsClosed, this);
                return GetName();
            }
        }

        /// <summary>
        /// Gets the type of the file that this handle represents.
        /// </summary>
        /// <value>The type of the file.</value>
        /// <exception cref="ObjectDisposedException">The handle is closed.</exception>
        public System.IO.FileHandleType Type
        {
            get
            {
                ObjectDisposedException.ThrowIf(IsClosed, this);

                int cachedType = _cachedFileType;
                if (cachedType == -1)
                {
                    _cachedFileType = cachedType = (int)GetFileTypeCore();
                }

                return (System.IO.FileHandleType)cachedType;
            }
        }
    }
}
