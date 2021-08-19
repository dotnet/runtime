// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

#pragma warning disable CA1419 // TODO https://github.com/dotnet/roslyn-analyzers/issues/5232: not intended for use with P/Invoke

namespace System.Net.Security
{
#if DEBUG
    internal sealed class SafeFreeCertContext : DebugSafeHandle
    {
#else
    internal sealed class SafeFreeCertContext : SafeHandle
    {
#endif
        private readonly SafeX509Handle? _certificate;

        public SafeFreeCertContext(SafeX509Handle certificate) : base(IntPtr.Zero, true)
        {
            // In certain scenarios (e.g. server querying for a client cert), the
            // input certificate may be invalid and this is OK
            if ((null != certificate) && !certificate.IsInvalid)
            {
                bool gotRef = false;
                certificate.DangerousAddRef(ref gotRef);
                Debug.Assert(gotRef, "Unexpected failure in AddRef of certificate");
                _certificate = certificate;
                handle = _certificate.DangerousGetHandle();
            }
        }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }

        protected override bool ReleaseHandle()
        {
            _certificate!.DangerousRelease();
            _certificate.Dispose();
            return true;
        }
    }

}
