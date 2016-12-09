//------------------------------------------------------------------------------
// <copyright file="ForestTrustDomainInformation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

  namespace System.DirectoryServices.ActiveDirectory {
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Globalization;
    using System.ComponentModel;

    public enum ForestTrustDomainStatus {
        Enabled = 0,
        SidAdminDisabled = 1,
        SidConflictDisabled = 2,
        NetBiosNameAdminDisabled = 4,
        NetBiosNameConflictDisabled = 8 
    }

    public class ForestTrustDomainInformation {
        private string dnsName = null;
        private string nbName = null;
        private string sid = null;
        private ForestTrustDomainStatus status;
        internal LARGE_INTEGER time;        

        internal ForestTrustDomainInformation(int flag, LSA_FOREST_TRUST_DOMAIN_INFO domainInfo, LARGE_INTEGER time) 
        {
            status = (ForestTrustDomainStatus) flag;
            dnsName = Marshal.PtrToStringUni(domainInfo.DNSNameBuffer, domainInfo.DNSNameLength/2);
            nbName = Marshal.PtrToStringUni(domainInfo.NetBIOSNameBuffer, domainInfo.NetBIOSNameLength/2);
            IntPtr ptr = (IntPtr)0;
            int result = UnsafeNativeMethods.ConvertSidToStringSidW(domainInfo.sid, ref ptr);
            if(result == 0)
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastWin32Error());
            }

            try
            {
                sid = Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                UnsafeNativeMethods.LocalFree(ptr);
            }

            this.time = time;
            
        }

        public string DnsName {
            get {
                return dnsName;
            }
        }

        public string NetBiosName {
            get {
                return nbName;
            }
        }

        public string DomainSid {
            get {
                return sid;
            }
        }

        public ForestTrustDomainStatus Status {
            get {
                return status;
            }
            set {
                if (value != ForestTrustDomainStatus.Enabled && 
                    value != ForestTrustDomainStatus.SidAdminDisabled &&
                    value != ForestTrustDomainStatus.SidConflictDisabled &&
                    value != ForestTrustDomainStatus.NetBiosNameAdminDisabled &&
                    value != ForestTrustDomainStatus.NetBiosNameConflictDisabled) 
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(ForestTrustDomainStatus));

                status = value;
            }
        }
    }    
}
