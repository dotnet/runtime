// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeMemoryMappedFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Creates a <see cref="T:Microsoft.Win32.SafeHandles.SafeMemoryMappedFileHandle" />.
        /// </summary>
        public SafeMemoryMappedFileHandle()
            : base(true)
        {
        }
    }
}
