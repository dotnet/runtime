// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    public sealed partial class SecurityContext : System.IDisposable
    {
        internal SecurityContext() { }
        public static System.Security.SecurityContext Capture() { throw new System.NotSupportedException(); }
        public System.Security.SecurityContext CreateCopy() { throw new System.NotSupportedException(); }
        public void Dispose() { throw new System.NotSupportedException(); }
        public static bool IsFlowSuppressed() { throw new System.NotSupportedException(); }
        public static bool IsWindowsIdentityFlowSuppressed() { throw new System.NotSupportedException(); }
        public static void RestoreFlow() { throw new System.NotSupportedException(); }
        public static void Run(System.Security.SecurityContext securityContext, System.Threading.ContextCallback callback, object state) { throw new System.NotSupportedException(); }
    }
}
