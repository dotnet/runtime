// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Security;

namespace System.IO.MemoryMappedFiles
{
    internal partial class MemoryMappedView
    {
        [SecurityCritical]
        public unsafe static MemoryMappedView CreateView(
            SafeMemoryMappedFileHandle memMappedFileHandle, MemoryMappedFileAccess access, Int64 offset, Int64 size)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        [SecurityCritical]
        public void Flush(UIntPtr capacity)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

    }
}
