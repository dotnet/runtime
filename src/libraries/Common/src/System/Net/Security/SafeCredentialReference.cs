// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ConstrainedExecution;

namespace System.Net.Security
{
    //
    // This is a class holding a Credential handle reference, used for static handles cache
    //
    internal sealed class SafeCredentialReference : CriticalFinalizerObject, IDisposable
    {
        //
        // Static cache will return the target handle if found the reference in the table.
        //
        internal SafeFreeCredentials? Target { get; private set; }

        internal static SafeCredentialReference? CreateReference(SafeFreeCredentials target)
        {
            if (target.IsInvalid || target.IsClosed)
            {
                return null;
            }

            return new SafeCredentialReference(target);
        }

        private SafeCredentialReference(SafeFreeCredentials target)
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

        private void Dispose(bool disposing)
        {
            SafeFreeCredentials? target = Target;
            target?.DangerousRelease();
            Target = null;
        }

        ~SafeCredentialReference()
        {
            Dispose(false);
        }
    }

}
