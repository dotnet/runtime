// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeChainEngineHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeChainEngineHandle()
            : base(true)
        {
        }

        private SafeChainEngineHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
        }

        public static readonly SafeChainEngineHandle MachineChainEngine =
            new SafeChainEngineHandle((IntPtr)Interop.Crypt32.ChainEngine.HCCE_LOCAL_MACHINE);

        public static readonly SafeChainEngineHandle UserChainEngine =
            new SafeChainEngineHandle((IntPtr)Interop.Crypt32.ChainEngine.HCCE_CURRENT_USER);

        protected sealed override bool ReleaseHandle()
        {
            Interop.Crypt32.CertFreeCertificateChainEngine(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (this != UserChainEngine && this != MachineChainEngine)
            {
                base.Dispose(disposing);
            }
        }
    }
}
