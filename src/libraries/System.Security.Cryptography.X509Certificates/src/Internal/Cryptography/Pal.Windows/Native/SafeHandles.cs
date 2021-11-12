// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

#pragma warning disable CA1419 // TODO https://github.com/dotnet/roslyn-analyzers/issues/5232: not intended for use with P/Invoke

namespace Internal.Cryptography.Pal.Native
{
    /// <summary>
    /// SafeHandle for LocalAlloc'd memory.
    /// </summary>
    internal sealed class SafeLocalAllocHandle : SafeCrypt32Handle<SafeLocalAllocHandle>
    {
        public static SafeLocalAllocHandle Create(int cb)
        {
            var h = new SafeLocalAllocHandle();
            h.SetHandle(Marshal.AllocHGlobal(cb));
            return h;
        }

        protected sealed override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }

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
            new SafeChainEngineHandle((IntPtr)ChainEngine.HCCE_LOCAL_MACHINE);

        public static readonly SafeChainEngineHandle UserChainEngine =
            new SafeChainEngineHandle((IntPtr)ChainEngine.HCCE_CURRENT_USER);

        protected sealed override bool ReleaseHandle()
        {
            Interop.crypt32.CertFreeCertificateChainEngine(handle);
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
