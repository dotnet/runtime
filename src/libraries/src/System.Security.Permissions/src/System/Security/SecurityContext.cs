// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Security
{
    public sealed partial class SecurityContext : System.IDisposable
    {
        internal SecurityContext() { }
        public static SecurityContext Capture() { throw new NotSupportedException(); }
        public SecurityContext CreateCopy() { throw new NotSupportedException(); }
        public void Dispose() { throw new NotSupportedException(); }
        public static bool IsFlowSuppressed() { throw new NotSupportedException(); }
        public static bool IsWindowsIdentityFlowSuppressed() { throw new NotSupportedException(); }
        public static void RestoreFlow() { throw new NotSupportedException(); }
        public static void Run(SecurityContext securityContext, ContextCallback callback, object state) { throw new NotSupportedException(); }
    }
}
