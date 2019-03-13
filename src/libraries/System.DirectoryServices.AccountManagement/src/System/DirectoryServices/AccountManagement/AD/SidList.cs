// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.DirectoryServices;
using System.Text;
using System.Net;

namespace System.DirectoryServices.AccountManagement
{
    internal class SidList
    {
        internal SidList(List<byte[]> sidListByteFormat) : this(sidListByteFormat, null, null)
        {
        }

        internal SidList(List<byte[]> sidListByteFormat, string target, NetCred credentials)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SidList", "SidList: processing {0} ByteFormat SIDs", sidListByteFormat.Count);
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SidList", "SidList: Targetting {0} ", (target != null) ? target : "local store");

            // Build the list of SIDs to resolve
            IntPtr hUser = IntPtr.Zero;

            int sidCount = sidListByteFormat.Count;
            IntPtr[] pSids = new IntPtr[sidCount];

            for (int i = 0; i < sidCount; i++)
            {
                pSids[i] = Utils.ConvertByteArrayToIntPtr(sidListByteFormat[i]);
            }

            try
            {
                if (credentials != null)
                {
                    Utils.BeginImpersonation(credentials, out hUser);
                }

                TranslateSids(target, pSids);
            }
            finally
            {
                if (hUser != IntPtr.Zero)
                    Utils.EndImpersonation(hUser);
            }
        }

        internal SidList(UnsafeNativeMethods.SID_AND_ATTR[] sidAndAttr)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SidList", "SidList: processing {0} Sid+Attr SIDs", sidAndAttr.Length);

            // Build the list of SIDs to resolve
            int sidCount = sidAndAttr.Length;
            IntPtr[] pSids = new IntPtr[sidCount];

            for (int i = 0; i < sidCount; i++)
            {
                pSids[i] = sidAndAttr[i].pSid;
            }

            TranslateSids(null, pSids);
        }

        protected void TranslateSids(string target, IntPtr[] pSids)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "SidList: processing {0} SIDs", pSids.Length);

            // if there are no SIDs to translate return
            if (pSids.Length == 0)
            {
                return;
            }

            // Build the list of SIDs to resolve
            int sidCount = pSids.Length;

            // Translate the SIDs in bulk
            IntPtr pOA = IntPtr.Zero;
            IntPtr pPolicyHandle = IntPtr.Zero;

            IntPtr pDomains = IntPtr.Zero;
            UnsafeNativeMethods.LSA_TRUST_INFORMATION[] domains;

            IntPtr pNames = IntPtr.Zero;
            UnsafeNativeMethods.LSA_TRANSLATED_NAME[] names;

            try
            {
                //
                // Get the policy handle
                //
                UnsafeNativeMethods.LSA_OBJECT_ATTRIBUTES oa = new UnsafeNativeMethods.LSA_OBJECT_ATTRIBUTES();

                pOA = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UnsafeNativeMethods.LSA_OBJECT_ATTRIBUTES)));
                Marshal.StructureToPtr(oa, pOA, false);

                int err = 0;
                if (target == null)
                {
                    err = UnsafeNativeMethods.LsaOpenPolicy(
                                    IntPtr.Zero,
                                    pOA,
                                    0x800,        // POLICY_LOOKUP_NAMES
                                    ref pPolicyHandle);
                }
                else
                {
                    // Build an entry.  Note that LSA_UNICODE_STRING.length is in bytes,
                    // while PtrToStringUni expects a length in characters.
                    UnsafeNativeMethods.LSA_UNICODE_STRING_Managed lsaTargetString = new UnsafeNativeMethods.LSA_UNICODE_STRING_Managed();
                    lsaTargetString.buffer = target;
                    lsaTargetString.length = (ushort)(target.Length * 2);
                    lsaTargetString.maximumLength = lsaTargetString.length;

                    IntPtr lsaTargetPr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UnsafeNativeMethods.LSA_UNICODE_STRING)));

                    try
                    {
                        Marshal.StructureToPtr(lsaTargetString, lsaTargetPr, false);

                        err = UnsafeNativeMethods.LsaOpenPolicy(
                                        lsaTargetPr,
                                        pOA,
                                        0x800,        // POLICY_LOOKUP_NAMES
                                        ref pPolicyHandle);
                    }
                    finally
                    {
                        if (lsaTargetPr != IntPtr.Zero)
                        {
                            UnsafeNativeMethods.LSA_UNICODE_STRING lsaTargetUnmanagedPtr =
                                (UnsafeNativeMethods.LSA_UNICODE_STRING)Marshal.PtrToStructure(lsaTargetPr, typeof(UnsafeNativeMethods.LSA_UNICODE_STRING));
                            if (lsaTargetUnmanagedPtr.buffer != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(lsaTargetUnmanagedPtr.buffer);
                                lsaTargetUnmanagedPtr.buffer = IntPtr.Zero;
                            }
                            Marshal.FreeHGlobal(lsaTargetPr);
                        }
                    }
                }

                if (err != 0)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "AuthZSet", "SidList: couldn't get policy handle, err={0}", err);

                    throw new PrincipalOperationException(SR.Format(
                                                               SR.AuthZErrorEnumeratingGroups,
                                                               SafeNativeMethods.LsaNtStatusToWinError(err)));
                }

                Debug.Assert(pPolicyHandle != IntPtr.Zero);

                //
                // Translate the SIDs
                //

                err = UnsafeNativeMethods.LsaLookupSids(
                                    pPolicyHandle,
                                    sidCount,
                                    pSids,
                                    out pDomains,
                                    out pNames);

                // ignore error STATUS_SOME_NOT_MAPPED = 0x00000107 and 
                // STATUS_NONE_MAPPED = 0xC0000073
                if (err != 0 &&
                     err != 263 &&
                     err != -1073741709)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "AuthZSet", "SidList: LsaLookupSids failed, err={0}", err);

                    throw new PrincipalOperationException(SR.Format(
                                                               SR.AuthZErrorEnumeratingGroups,
                                                               SafeNativeMethods.LsaNtStatusToWinError(err)));
                }

                //
                // Get the group names in managed form
                //
                names = new UnsafeNativeMethods.LSA_TRANSLATED_NAME[sidCount];
                IntPtr pCurrentName = pNames;

                for (int i = 0; i < sidCount; i++)
                {
                    names[i] = (UnsafeNativeMethods.LSA_TRANSLATED_NAME)
                                    Marshal.PtrToStructure(pCurrentName, typeof(UnsafeNativeMethods.LSA_TRANSLATED_NAME));

                    pCurrentName = new IntPtr(pCurrentName.ToInt64() + Marshal.SizeOf(typeof(UnsafeNativeMethods.LSA_TRANSLATED_NAME)));
                }

                //
                // Get the domain names in managed form
                //

                // Extract LSA_REFERENCED_DOMAIN_LIST.Entries            

                UnsafeNativeMethods.LSA_REFERENCED_DOMAIN_LIST referencedDomains = (UnsafeNativeMethods.LSA_REFERENCED_DOMAIN_LIST)Marshal.PtrToStructure(pDomains, typeof(UnsafeNativeMethods.LSA_REFERENCED_DOMAIN_LIST));

                int domainCount = referencedDomains.entries;

                // Extract LSA_REFERENCED_DOMAIN_LIST.Domains, by iterating over the array and marshalling
                // each native LSA_TRUST_INFORMATION into a managed LSA_TRUST_INFORMATION.

                domains = new UnsafeNativeMethods.LSA_TRUST_INFORMATION[domainCount];

                IntPtr pCurrentDomain = referencedDomains.domains;

                for (int i = 0; i < domainCount; i++)
                {
                    domains[i] = (UnsafeNativeMethods.LSA_TRUST_INFORMATION)Marshal.PtrToStructure(pCurrentDomain, typeof(UnsafeNativeMethods.LSA_TRUST_INFORMATION));
                    pCurrentDomain = new IntPtr(pCurrentDomain.ToInt64() + Marshal.SizeOf(typeof(UnsafeNativeMethods.LSA_TRUST_INFORMATION)));
                }

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "SidList: got {0} groups in {1} domains", sidCount, domainCount);

                //
                // Build the list of entries
                //
                Debug.Assert(names.Length == sidCount);

                for (int i = 0; i < names.Length; i++)
                {
                    UnsafeNativeMethods.LSA_TRANSLATED_NAME name = names[i];

                    // Build an entry.  Note that LSA_UNICODE_STRING.length is in bytes,
                    // while PtrToStringUni expects a length in characters.
                    SidListEntry entry = new SidListEntry();

                    Debug.Assert(name.name.length % 2 == 0);
                    entry.name = Marshal.PtrToStringUni(name.name.buffer, name.name.length / 2);

                    // Get the domain associated with this name
                    Debug.Assert(name.domainIndex < domains.Length);
                    if (name.domainIndex >= 0)
                    {
                        UnsafeNativeMethods.LSA_TRUST_INFORMATION domain = domains[name.domainIndex];
                        Debug.Assert(domain.name.length % 2 == 0);
                        entry.sidIssuerName = Marshal.PtrToStringUni(domain.name.buffer, domain.name.length / 2);
                    }

                    entry.pSid = pSids[i];

                    _entries.Add(entry);
                }

                // Sort the list so they are oriented by the issuer name.
                // this.entries.Sort( new SidListComparer());
            }
            finally
            {
                if (pDomains != IntPtr.Zero)
                    UnsafeNativeMethods.LsaFreeMemory(pDomains);

                if (pNames != IntPtr.Zero)
                    UnsafeNativeMethods.LsaFreeMemory(pNames);

                if (pPolicyHandle != IntPtr.Zero)
                    UnsafeNativeMethods.LsaClose(pPolicyHandle);

                if (pOA != IntPtr.Zero)
                    Marshal.FreeHGlobal(pOA);
            }
        }

        private List<SidListEntry> _entries = new List<SidListEntry>();

        public SidListEntry this[int index]
        {
            get { return _entries[index]; }
        }

        public int Length
        {
            get { return _entries.Count; }
        }

        public void RemoveAt(int index)
        {
            _entries[index].Dispose();
            _entries.RemoveAt(index);
        }

        public void Clear()
        {
            foreach (SidListEntry sl in _entries)
                sl.Dispose();

            _entries.Clear();
        }
    }

    /******
            class SidListComparer : IComparer<SidListEntry>
            {
              public int Compare(SidListEntry entry1, SidListEntry entry2)
              {
                 return ( string.Compare( entry1.sidIssuerName, entry2.sidIssuerName, true, CultureInfo.InvariantCulture));
              }

            }
    ********/
    internal class SidListEntry : IDisposable
    {
        public IntPtr pSid = IntPtr.Zero;
        public string name;
        public string sidIssuerName;
        //
        // IDisposable
        //
        public virtual void Dispose()
        {
            if (pSid != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pSid);
                pSid = IntPtr.Zero;
            }
        }
    }
}

