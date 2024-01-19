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

            result = Interop.Netapi32.DsGetDcName(computerName, domainName, IntPtr.Zero, siteName, (int)(flags | (long)PrivateLocatorFlags.ReturnDNSName), out pDomainControllerInfo);
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
                        result = Interop.Netapi32.NetApiBufferFree(pDomainControllerInfo);
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
                else if (errorCode == Interop.Errors.ERROR_NO_SUCH_DOMAIN)
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
            IntPtr sockAddressCountPtr = IntPtr.Zero;
            IntPtr sockAddressList = IntPtr.Zero;
            IntPtr dcDnsHostNamePtr = IntPtr.Zero;
            string? dcDnsHostName = null;
            int result = 0;

            result = Interop.Netapi32.DsGetDcOpen(domainName, (int)optionFlags, siteName, IntPtr.Zero, null, (int)dcFlags, out retGetDcContext);
            if (result == 0)
            {
                try
                {
                    result = Interop.Netapi32.DsGetDcNext(retGetDcContext, out sockAddressCountPtr, out sockAddressList, out dcDnsHostNamePtr);

                    if (result != 0 && result != Interop.Errors.ERROR_FILEMARK_DETECTED && result != Interop.Errors.DNS_ERROR_RCODE_NAME_ERROR && result != Interop.Errors.ERROR_NO_MORE_ITEMS)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode(result);
                    }

                    while (result != Interop.Errors.ERROR_NO_MORE_ITEMS)
                    {
                        if (result != Interop.Errors.ERROR_FILEMARK_DETECTED && result != Interop.Errors.DNS_ERROR_RCODE_NAME_ERROR)
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
                                if (sockAddressList != IntPtr.Zero)
                                {
                                    Marshal.FreeHGlobal(sockAddressList);
                                }

                                // what to do with the error?
                                if (dcDnsHostNamePtr != IntPtr.Zero)
                                {
                                    result = Interop.Netapi32.NetApiBufferFree(dcDnsHostNamePtr);
                                }
                            }
                        }

                        result = Interop.Netapi32.DsGetDcNext(retGetDcContext, out sockAddressCountPtr, out sockAddressList, out dcDnsHostNamePtr);
                        if (result != 0 && result != Interop.Errors.ERROR_FILEMARK_DETECTED && result != Interop.Errors.DNS_ERROR_RCODE_NAME_ERROR && result != Interop.Errors.ERROR_NO_MORE_ITEMS)
                        {
                            throw ExceptionHelper.GetExceptionFromErrorCode(result);
                        }
                    }
                }
                finally
                {
                    Interop.Netapi32.DsGetDcClose(retGetDcContext);
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
