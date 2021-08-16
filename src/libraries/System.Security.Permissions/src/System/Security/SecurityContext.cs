// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Security
{
#if NET6_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed partial class SecurityContext : System.IDisposable
    {
        internal SecurityContext() { }
        public static SecurityContext Capture() { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
        public SecurityContext CreateCopy() { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
        public void Dispose() { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
        public static bool IsFlowSuppressed() { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
        public static bool IsWindowsIdentityFlowSuppressed() { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
        public static void RestoreFlow() { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
        public static void Run(SecurityContext securityContext, ContextCallback callback, object state) { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
        public static AsyncFlowControl SuppressFlow() { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
        public static AsyncFlowControl SuppressFlowWindowsIdentity() { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
    }
}
