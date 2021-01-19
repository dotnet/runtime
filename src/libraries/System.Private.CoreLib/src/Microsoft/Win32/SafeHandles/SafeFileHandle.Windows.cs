// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    public sealed class SafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private bool? _isAsync;

        public SafeFileHandle() : base(true)
        {
            _isAsync = null;
        }

        public SafeFileHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);

            _isAsync = null;
        }

        internal bool? IsAsync
        {
            get => _isAsync;
            set => _isAsync = value;
        }

        internal ThreadPoolBoundHandle? ThreadPoolBinding { get; set; }

        protected override bool ReleaseHandle() =>
            Interop.Kernel32.CloseHandle(handle);
    }
}
