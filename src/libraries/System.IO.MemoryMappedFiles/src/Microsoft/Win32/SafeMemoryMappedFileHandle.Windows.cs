// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeMemoryMappedFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeMemoryMappedFileHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return Interop.Kernel32.CloseHandle(handle);
        }

        public override bool IsInvalid => base.IsInvalid;
    }
}
