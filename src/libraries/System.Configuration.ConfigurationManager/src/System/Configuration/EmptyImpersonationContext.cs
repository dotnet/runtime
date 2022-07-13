// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    // Used in cases where the Host does not require impersonation.
    internal sealed class EmptyImpersonationContext : IDisposable
    {
        private static volatile IDisposable s_emptyImpersonationContext;

        public void Dispose() { }

        internal static IDisposable GetStaticInstance()
        {
            return s_emptyImpersonationContext ??= new EmptyImpersonationContext();
        }
    }
}
