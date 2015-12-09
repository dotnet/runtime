// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafePipeHandle : SafeHandle
    {
        private const int DefaultInvalidHandle = -1;

        /// <summary>Opens the specified file with the requested flags and mode.</summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="flags">The flags with which to open the file.</param>
        /// <param name="mode">The mode for opening the file.</param>
        /// <returns>A SafeFileHandle for the opened file.</returns>
        internal static SafePipeHandle Open(string path, Interop.Sys.OpenFlags flags, int mode)
        {
            // Ideally this would be a constrained execution region, but we don't have access to PrepareConstrainedRegions.
            SafePipeHandle handle = Interop.CheckIo(Interop.Sys.OpenPipe(path, flags, mode));

            Debug.Assert(!handle.IsInvalid);

            return handle;
        }

        protected override bool ReleaseHandle()
        {
            // Close the handle. Although close is documented to potentially fail with EINTR, we never want
            // to retry, as the descriptor could actually have been closed, been subsequently reassigned, and
            // be in use elsewhere in the process.  Instead, we simply check whether the call was successful.
            Debug.Assert(!this.IsInvalid);
            return Interop.Sys.Close(handle) == 0;
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return (long)handle < 0; }
        }
    }
}
