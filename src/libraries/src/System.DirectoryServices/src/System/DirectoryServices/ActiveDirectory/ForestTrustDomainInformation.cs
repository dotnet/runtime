// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Globalization;
    using System.ComponentModel;

    public enum ForestTrustDomainStatus
    {
        Enabled = 0,
        SidAdminDisabled = 1,
        SidConflictDisabled = 2,
        NetBiosNameAdminDisabled = 4,
        NetBiosNameConflictDisabled = 8
    }

    public class ForestTrustDomainInformation
    {
        private string _dnsName = null;
        private string _nbName = null;
        private string _sid = null;
        private ForestTrustDomainStatus _status;
        internal LARGE_INTEGER time;

        internal ForestTrustDomainInformation(int flag, LSA_FOREST_TRUST_DOMAIN_INFO domainInfo, LARGE_INTEGER time)
        {
            _status = (ForestTrustDomainStatus)flag;
            _dnsName = Marshal.PtrToStringUni(domainInfo.DNSNameBuffer, domainInfo.DNSNameLength / 2);
            _nbName = Marshal.PtrToStringUni(domainInfo.NetBIOSNameBuffer, domainInfo.NetBIOSNameLength / 2);
            IntPtr ptr = (IntPtr)0;
            int result = UnsafeNativeMethods.ConvertSidToStringSidW(domainInfo.sid, ref ptr);
            if (result == 0)
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastWin32Error());
            }

            try
            {
                _sid = Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                UnsafeNativeMethods.LocalFree(ptr);
            }

            this.time = time;
        }

        public string DnsName
        {
            get
            {
                return _dnsName;
            }
        }

        public string NetBiosName
        {
            get
            {
                return _nbName;
            }
        }

        public string DomainSid
        {
            get
            {
                return _sid;
            }
        }

        public ForestTrustDomainStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (value != ForestTrustDomainStatus.Enabled &&
                    value != ForestTrustDomainStatus.SidAdminDisabled &&
                    value != ForestTrustDomainStatus.SidConflictDisabled &&
                    value != ForestTrustDomainStatus.NetBiosNameAdminDisabled &&
                    value != ForestTrustDomainStatus.NetBiosNameConflictDisabled)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(ForestTrustDomainStatus));

                _status = value;
            }
        }
    }
}
