// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        internal static Transaction GetContextTransaction(ContextData contextData)
        {
            if (EnterpriseServicesOk)
            {
                ThrowNotSupported();
            }

            return null;
        }

        internal static bool CreatedServiceDomain { get; set; } = false;

        internal static bool UseServiceDomainForCurrent() => false;

        internal static void PushServiceDomain(Transaction newCurrent)
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
