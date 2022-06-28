// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace System.DirectoryServices.AccountManagement
{
    internal sealed class SidList
    {
        internal SidList(List<byte[]> sidListByteFormat) : this(sidListByteFormat, null, null)
        {
        }

        internal SidList(List<byte[]> sidListByteFormat, string target, NetCred credentials)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SidList", "SidList: processing {0} ByteFormat SIDs", sidListByteFormat.Count);
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SidList", "SidList: Targetting {0} ", target ?? "local store");

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

        internal SidList(Interop.SID_AND_ATTRIBUTES[] sidAndAttr)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SidList", "SidList: processing {0} Sid+Attr SIDs", sidAndAttr.Length);

            // Build the list of SIDs to resolve
            int sidCount = sidAndAttr.Length;
            IntPtr[] pSids = new IntPtr[sidCount];

            for (int i = 0; i < sidCount; i++)
            {
                pSids[i] = sidAndAttr[i].Sid;
            }

            TranslateSids(null, pSids);
        }

        private void TranslateSids(string target, IntPtr[] pSids)
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
            SafeLsaPolicyHandle policyHandle = null;
            SafeLsaMemoryHandle domainsHandle = null;
            SafeLsaMemoryHandle namesHandle = null;
            try
            {
                //
                // Get the policy handle
                //
                Interop.OBJECT_ATTRIBUTES oa = default;

                uint err = Interop.Advapi32.LsaOpenPolicy(
                                    target,
                                    ref oa,
                                    (int)Interop.Advapi32.PolicyRights.POLICY_LOOKUP_NAMES,
                                    out policyHandle);
                if (err != 0)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "AuthZSet", "SidList: couldn't get policy handle, err={0}", err);

                    throw new PrincipalOperationException(SR.Format(
                                                               SR.AuthZErrorEnumeratingGroups,
                                                               Interop.Advapi32.LsaNtStatusToWinError(err)));
                }

                Debug.Assert(!policyHandle.IsInvalid);

                //
                // Translate the SIDs
                //

                err = Interop.Advapi32.LsaLookupSids(
                                    policyHandle,
                                    sidCount,
                                    pSids,
                                    out domainsHandle,
                                    out namesHandle);

                // Ignore error STATUS_SOME_NOT_MAPPED and STATUS_NONE_MAPPED
                if (err != Interop.StatusOptions.STATUS_SUCCESS &&
                     err != Interop.StatusOptions.STATUS_SOME_NOT_MAPPED &&
                     err != Interop.StatusOptions.STATUS_NONE_MAPPED)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "AuthZSet", "SidList: LsaLookupSids failed, err={0}", err);

                    throw new PrincipalOperationException(SR.Format(
                                                               SR.AuthZErrorEnumeratingGroups,
                                                               Interop.Advapi32.LsaNtStatusToWinError(err)));
                }

                //
                // Get the group names in managed form
                //
                namesHandle.Initialize((uint)sidCount, (uint)Marshal.SizeOf<Interop.LSA_TRANSLATED_NAME>());

                Interop.LSA_TRANSLATED_NAME[] names = new Interop.LSA_TRANSLATED_NAME[sidCount];
                namesHandle.ReadArray(0, names, 0, names.Length);

                //
                // Get the domain names in managed form
                //
                domainsHandle.InitializeReferencedDomainsList();
                Interop.LSA_REFERENCED_DOMAIN_LIST domainList = domainsHandle.Read<Interop.LSA_REFERENCED_DOMAIN_LIST>(0);

                // Extract LSA_REFERENCED_DOMAIN_LIST.Entries

                int domainCount = domainList.Entries;

                // Extract LSA_REFERENCED_DOMAIN_LIST.Domains, by iterating over the array and marshalling
                // each native LSA_TRUST_INFORMATION into a managed LSA_TRUST_INFORMATION.

                Interop.LSA_TRUST_INFORMATION[] domains = new Interop.LSA_TRUST_INFORMATION[domainCount];

                IntPtr pCurrentDomain = domainList.Domains;

                for (int i = 0; i < domainCount; i++)
                {
                    domains[i] = (Interop.LSA_TRUST_INFORMATION)Marshal.PtrToStructure(pCurrentDomain, typeof(Interop.LSA_TRUST_INFORMATION));
                    pCurrentDomain = new IntPtr(pCurrentDomain.ToInt64() + Marshal.SizeOf(typeof(Interop.LSA_TRUST_INFORMATION)));
                }

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "SidList: got {0} groups in {1} domains", sidCount, domainCount);

                //
                // Build the list of entries
                //
                Debug.Assert(names.Length == sidCount);

                for (int i = 0; i < names.Length; i++)
                {
                    Interop.LSA_TRANSLATED_NAME name = names[i];

                    // Build an entry.  Note that LSA_UNICODE_STRING.length is in bytes,
                    // while PtrToStringUni expects a length in characters.
                    SidListEntry entry = new SidListEntry();

                    Debug.Assert(name.Name.Length % 2 == 0);
                    entry.name = Marshal.PtrToStringUni(name.Name.Buffer, name.Name.Length / 2);

                    // Get the domain associated with this name
                    Debug.Assert(name.DomainIndex < domains.Length);
                    if (name.DomainIndex >= 0)
                    {
                        Interop.LSA_TRUST_INFORMATION domain = domains[name.DomainIndex];
                        Debug.Assert(domain.Name.Length % 2 == 0);
                        entry.sidIssuerName = Marshal.PtrToStringUni(domain.Name.Buffer, domain.Name.Length / 2);
                    }

                    entry.pSid = pSids[i];

                    _entries.Add(entry);
                }

                // Sort the list so they are oriented by the issuer name.
                // this.entries.Sort( new SidListComparer());
            }
            finally
            {
                if (domainsHandle != null)
                    domainsHandle.Dispose();

                if (namesHandle != null)
                    namesHandle.Dispose();

                if (policyHandle != null)
                    policyHandle.Dispose();
            }
        }

        private readonly List<SidListEntry> _entries = new List<SidListEntry>();

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
    internal sealed class SidListEntry : IDisposable
    {
        public IntPtr pSid = IntPtr.Zero;
        public string name;
        public string sidIssuerName;
        //
        // IDisposable
        //
        public void Dispose()
        {
            if (pSid != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pSid);
                pSid = IntPtr.Zero;
            }
        }
    }
}
