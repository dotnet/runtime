// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace System.Net.Security
{
    //
    // Implementation of handles dependable on FreeCredentialsHandle
    //
#if DEBUG
    internal abstract class SafeFreeCredentials : DebugSafeHandle
    {
#else
    internal abstract class SafeFreeCredentials : SafeHandle
    {
#endif
        protected SafeFreeCredentials(IntPtr handle, bool ownsHandle) : base(handle, ownsHandle)
        {
        }
    }

    //
    // This is a class holding a Credential handle reference, used for static handles cache
    //
    internal sealed class SafeCredentialReference : CriticalFinalizerObject, IDisposable
    {
        //
        // Static cache will return the target handle if found the reference in the table.
        //
        internal SafeFreeCredentials Target { get; private set; }

        internal static SafeCredentialReference? CreateReference(SafeFreeCredentials target)
        {
            if (target.IsInvalid || target.IsClosed)
            {
                return null;
            }

            SafeCredentialReference result = new SafeCredentialReference(target);
            return result;
        }
        private SafeCredentialReference(SafeFreeCredentials target) : base()
        {
            // Bumps up the refcount on Target to signify that target handle is statically cached so
            // its dispose should be postponed
            bool ignore = false;
            target.DangerousAddRef(ref ignore);
            Target = target;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            SafeFreeCredentials target = Target;
            target?.DangerousRelease();
            Target = null!;
        }

        ~SafeCredentialReference()
        {
            Dispose(false);

#if DEBUG
            Debug.Fail("Finalizer should never be called. The SafeCredentialReference should be always disposed manually.");
#endif
        }

    }
}
