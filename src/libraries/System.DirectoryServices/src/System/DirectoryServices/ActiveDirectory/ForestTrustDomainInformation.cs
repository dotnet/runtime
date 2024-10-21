// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
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
        private ForestTrustDomainStatus _status;
        internal LARGE_INTEGER time;

        internal ForestTrustDomainInformation(int flag, LSA_FOREST_TRUST_DOMAIN_INFO domainInfo, LARGE_INTEGER time)
        {
            _status = (ForestTrustDomainStatus)flag;
            DnsName = Marshal.PtrToStringUni(domainInfo.DNSNameBuffer, domainInfo.DNSNameLength / 2);
            NetBiosName = Marshal.PtrToStringUni(domainInfo.NetBIOSNameBuffer, domainInfo.NetBIOSNameLength / 2);

            string sidLocal;
            global::Interop.BOOL result = global::Interop.Advapi32.ConvertSidToStringSid(domainInfo.sid, out sidLocal);
            if (result == global::Interop.BOOL.FALSE)
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastPInvokeError());
            }

            DomainSid = sidLocal;
            this.time = time;
        }

        public string DnsName { get; }

        public string NetBiosName { get; }

        public string DomainSid { get; }

        public ForestTrustDomainStatus Status
        {
            get => _status;
            set
            {
                if (value != ForestTrustDomainStatus.Enabled &&
                    value != ForestTrustDomainStatus.SidAdminDisabled &&
                    value != ForestTrustDomainStatus.SidConflictDisabled &&
                    value != ForestTrustDomainStatus.NetBiosNameAdminDisabled &&
                    value != ForestTrustDomainStatus.NetBiosNameConflictDisabled)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ForestTrustDomainStatus));

                _status = value;
            }
        }
    }
}
