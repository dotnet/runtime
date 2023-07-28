// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Interop.Libraries.Advapi32, EntryPoint = "LsaLookupSids", SetLastError = true)]
        internal static partial uint LsaLookupSids(
            SafeLsaPolicyHandle handle,
            int count,
            IntPtr[] sids,
            out SafeLsaMemoryHandle referencedDomains,
            out SafeLsaMemoryHandle names
        );
    }
}

internal static partial class SafeLsaMemoryHandleExtensions
{
    public static unsafe void InitializeReferencedDomainsList(this SafeLsaMemoryHandle referencedDomains)
    {
        // We don't know the real size of the referenced domains yet, so we need to set an initial
        // size based on the LSA_REFERENCED_DOMAIN_LIST structure, then resize it to include all of
        // the domains.
        referencedDomains.Initialize((uint)Marshal.SizeOf<Interop.LSA_REFERENCED_DOMAIN_LIST>());
        Interop.LSA_REFERENCED_DOMAIN_LIST domainList = referencedDomains.Read<Interop.LSA_REFERENCED_DOMAIN_LIST>(0);

        byte* pRdl = null;
        try
        {
            referencedDomains.AcquirePointer(ref pRdl);

            // If there is a trust information list, then the buffer size is the end of that list minus
            // the beginning of the domain list. Otherwise, then the buffer is just the size of the
            // referenced domain list structure, which is what we defaulted to.
            if (domainList.Domains != IntPtr.Zero)
            {
                Interop.LSA_TRUST_INFORMATION* pTrustInformation = (Interop.LSA_TRUST_INFORMATION*)domainList.Domains;
                pTrustInformation += domainList.Entries;

                long bufferSize = (byte*)pTrustInformation - pRdl;
                System.Diagnostics.Debug.Assert(bufferSize > 0, "bufferSize > 0");
                referencedDomains.Initialize((ulong)bufferSize);
            }
        }
        finally
        {
            if (pRdl != null)
                referencedDomains.ReleasePointer();
        }
    }
}
