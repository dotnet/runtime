// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    internal sealed class Locator
    {
        // To disable public/protected constructors for this class
        private Locator() { }

        internal static DomainControllerInfo GetDomainControllerInfo(string? computerName, string? domainName, string? siteName, long flags)
        {
            int errorCode = 0;
            DomainControllerInfo domainControllerInfo;

            errorCode = DsGetDcNameWrapper(computerName, domainName, siteName, flags, out domainControllerInfo);

            if (errorCode != 0)
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(errorCode, domainName);
            }

            return domainControllerInfo;
        }

        internal static int DsGetDcNameWrapper(string? computerName, string? domainName, string? siteName, long flags, out DomainControllerInfo domainControllerInfo)
        {
            IntPtr pDomainControllerInfo = IntPtr.Zero;
            int result = 0;

            // empty siteName/computerName should be treated as null
            if ((computerName != null) && (computerName.Length == 0))
            {
                computerName = null;
            }
            if ((siteName != null) && (siteName.Length == 0))
            {
                siteName = null;
            }

            result = NativeMethods.DsGetDcName(computerName, domainName, IntPtr.Zero, siteName, (int)(flags | (long)PrivateLocatorFlags.ReturnDNSName), out pDomainControllerInfo);
            if (result == 0)
            {
                try
                {
                    // success case
                    domainControllerInfo = new DomainControllerInfo();
                    Marshal.PtrToStructure(pDomainControllerInfo, domainControllerInfo);
                }
                finally
                {
                    // free the buffer
                    // what to do with error code??
                    if (pDomainControllerInfo != IntPtr.Zero)
                    {
                        result = NativeMethods.NetApiBufferFree(pDomainControllerInfo);
                    }
                }
            }
            else
            {
                domainControllerInfo = new DomainControllerInfo();
            }

            return result;
        }

        internal static ArrayList EnumerateDomainControllers(DirectoryContext context, string? domainName, string? siteName, long dcFlags)
        {
            Hashtable? allDCs = null;
            ArrayList dcs = new ArrayList();

            //
            // this api obtains the list of DCs/GCs based on dns records. The DCs/GCs that have registered
            // non site specific records for the domain/forest are returned. Additionally DCs/GCs that have registered site specific records
            // (site is either specified or defaulted to the site of the local machine) are also returned in this list.
            //

            if (siteName == null)
            {
                //
                // if the site name is not specified then we get the site specific records for the local machine's site (in the context of the domain/forest/application partition that is specified)
                // (sitename could still be null if the machine is not in any site for the specified domain/forest, in that case we don't look for any site specific records)
                //
                DomainControllerInfo domainControllerInfo;

                int errorCode = DsGetDcNameWrapper(null, domainName, null, dcFlags & (long)(PrivateLocatorFlags.GCRequired | PrivateLocatorFlags.DSWriteableRequired | PrivateLocatorFlags.OnlyLDAPNeeded), out domainControllerInfo);
                if (errorCode == 0)
                {
                    siteName = domainControllerInfo.ClientSiteName;
                }
                else if (errorCode == NativeMethods.ERROR_NO_SUCH_DOMAIN)
                {
                    // return an empty collection
                    return dcs;
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
                }
            }

            // this will get both the non site specific and the site specific records
            allDCs = DnsGetDcWrapper(domainName, siteName, dcFlags);

            foreach (string dcName in allDCs.Keys)
            {
                DirectoryContext dcContext = Utils.GetNewDirectoryContext(dcName, DirectoryContextType.DirectoryServer, context);

                if ((dcFlags & (long)PrivateLocatorFlags.GCRequired) != 0)
                {
                    // add a GlobalCatalog object
                    dcs.Add(new GlobalCatalog(dcContext, dcName));
                }
                else
                {
                    // add a domain controller object
                    dcs.Add(new DomainController(dcContext, dcName));
                }
            }

            return dcs;
        }

        private static Hashtable DnsGetDcWrapper(string? domainName, string? siteName, long dcFlags)
        {
            Hashtable domainControllers = new Hashtable();

            int optionFlags = 0;
            IntPtr retGetDcContext = IntPtr.Zero;
            IntPtr dcDnsHostNamePtr = IntPtr.Zero;
            int sockAddressCount = 0;
            IntPtr sockAddressCountPtr = new IntPtr(sockAddressCount);
            IntPtr sockAddressList = IntPtr.Zero;
            string? dcDnsHostName = null;
            int result = 0;

            result = NativeMethods.DsGetDcOpen(domainName, (int)optionFlags, siteName, IntPtr.Zero, null, (int)dcFlags, out retGetDcContext);
            if (result == 0)
            {
                try
                {
                    result = NativeMethods.DsGetDcNext(retGetDcContext, ref sockAddressCountPtr, out sockAddressList, out dcDnsHostNamePtr);

                    if (result != 0 && result != NativeMethods.ERROR_FILE_MARK_DETECTED && result != NativeMethods.DNS_ERROR_RCODE_NAME_ERROR && result != NativeMethods.ERROR_NO_MORE_ITEMS)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode(result);
                    }

                    while (result != NativeMethods.ERROR_NO_MORE_ITEMS)
                    {
                        if (result != NativeMethods.ERROR_FILE_MARK_DETECTED && result != NativeMethods.DNS_ERROR_RCODE_NAME_ERROR)
                        {
                            try
                            {
                                dcDnsHostName = Marshal.PtrToStringUni(dcDnsHostNamePtr)!;
                                string key = dcDnsHostName.ToLowerInvariant();

                                if (!domainControllers.Contains(key))
                                {
                                    domainControllers.Add(key, null);
                                }
                            }
                            finally
                            {
                                // what to do with the error?
                                if (dcDnsHostNamePtr != IntPtr.Zero)
                                {
                                    result = NativeMethods.NetApiBufferFree(dcDnsHostNamePtr);
                                }
                            }
                        }

                        result = NativeMethods.DsGetDcNext(retGetDcContext, ref sockAddressCountPtr, out sockAddressList, out dcDnsHostNamePtr);
                        if (result != 0 && result != NativeMethods.ERROR_FILE_MARK_DETECTED && result != NativeMethods.DNS_ERROR_RCODE_NAME_ERROR && result != NativeMethods.ERROR_NO_MORE_ITEMS)
                        {
                            throw ExceptionHelper.GetExceptionFromErrorCode(result);
                        }
                    }
                }
                finally
                {
                    NativeMethods.DsGetDcClose(retGetDcContext);
                }
            }
            else if (result != 0)
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(result);
            }

            return domainControllers;
        }

        private static Hashtable DnsQueryWrapper(string domainName, string siteName, long dcFlags)
        {
            Hashtable domainControllers = new Hashtable();
            string recordName = "_ldap._tcp.";
            int result = 0;
            int options = 0;
            IntPtr dnsResults = IntPtr.Zero;

            // construct the record name
            if ((siteName != null) && (!(siteName.Length == 0)))
            {
                // only looking for domain controllers / global catalogs within a
                // particular site
                recordName = recordName + siteName + "._sites.";
            }

            // check if gc or dc
            if (((long)dcFlags & (long)(PrivateLocatorFlags.GCRequired)) != 0)
            {
                // global catalog
                recordName += "gc._msdcs.";
            }
            else if (((long)dcFlags & (long)(PrivateLocatorFlags.DSWriteableRequired)) != 0)
            {
                // domain controller
                recordName += "dc._msdcs.";
            }

            // now add the domainName
            recordName += domainName;

            // set the BYPASS CACHE option is specified
            if (((long)dcFlags & (long)LocatorOptions.ForceRediscovery) != 0)
            {
                options |= NativeMethods.DnsQueryBypassCache;
            }

            // Call DnsQuery
            result = NativeMethods.DnsQuery(recordName, NativeMethods.DnsSrvData, options, IntPtr.Zero, out dnsResults, IntPtr.Zero);
            if (result == 0)
            {
                try
                {
                    IntPtr currentDnsRecord = dnsResults;

                    while (currentDnsRecord != IntPtr.Zero)
                    {
                        // partial marshalling of dns record data
                        PartialDnsRecord partialDnsRecord = new PartialDnsRecord();
                        Marshal.PtrToStructure(currentDnsRecord, partialDnsRecord);

                        //check if the record is of type DNS_SRV_DATA
                        if (partialDnsRecord.type == NativeMethods.DnsSrvData)
                        {
                            // remarshal to get the srv record data
                            DnsRecord dnsRecord = new DnsRecord();
                            Marshal.PtrToStructure(currentDnsRecord, dnsRecord);
                            string targetName = dnsRecord.data.targetName;
                            string key = targetName.ToLowerInvariant();

                            if (!domainControllers.Contains(key))
                            {
                                domainControllers.Add(key, null);
                            }
                        }
                        // move to next record
                        currentDnsRecord = partialDnsRecord.next;
                    }
                }
                finally
                {
                    // release the dns results buffer
                    if (dnsResults != IntPtr.Zero)
                    {
                        NativeMethods.DnsRecordListFree(dnsResults, true);
                    }
                }
            }
            else if (result != 0)
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(result);
            }

            return domainControllers;
        }
    }
}
