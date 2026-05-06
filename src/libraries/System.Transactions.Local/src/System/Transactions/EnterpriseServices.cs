// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Transactions
{
    internal static class EnterpriseServices
    {
        internal static bool EnterpriseServicesOk => false;

        internal static void VerifyEnterpriseServicesOk()
        {
            if (!EnterpriseServicesOk)
            {
                ThrowNotSupported();
            }
        }

        internal static Transaction? GetContextTransaction()
        {
            if (EnterpriseServicesOk)
            {
                ThrowNotSupported();
            }

            return null;
        }

        internal static bool CreatedServiceDomain { get; set; }

        internal static bool UseServiceDomainForCurrent() => false;

        internal static void PushServiceDomain()
        {
            ThrowNotSupported();
        }

        internal static void LeaveServiceDomain()
        {
            ThrowNotSupported();
        }

        private static void ThrowNotSupported()
        {
            throw new PlatformNotSupportedException(SR.EsNotSupported);
        }
    }
}
