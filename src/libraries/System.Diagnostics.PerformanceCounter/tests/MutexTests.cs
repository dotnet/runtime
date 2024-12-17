// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using Xunit;

namespace System.Diagnostics.Tests
{
    public static class MutexTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInAppContainer))] // Can't create global objects in appcontainer
        public static void VerifySecurityIdentifier()
        {
            string mutexName = $"{Guid.NewGuid():N}";

            Mutex mutex = null;

            // We can't test with the same global mutex used by performance counters, since the mutex was likely already
            // initialized elsewhere and perhaps with with different ACLs, so we use a Guid to create a new mutex and
            // then simulate the behavior by calling into EnterMutex() like the performance monitor code does.
#pragma warning disable CS0436 // Type conflicts with imported type
            NetFrameworkUtils.EnterMutex(mutexName, ref mutex);
#pragma warning restore CS0436

            try
            {
                // This is the SID that is added by EnterMutex().
                SecurityIdentifier authenticatedUserSid = new(WellKnownSidType.AuthenticatedUserSid, null);

                MutexSecurity security = mutex.GetAccessControl();
                AuthorizationRuleCollection rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
                Assert.Equal(1, rules.Count);
                MutexAccessRule accessRule = (MutexAccessRule)rules[0];
                SecurityIdentifier sid = (SecurityIdentifier)accessRule.IdentityReference;
                Assert.Equal(authenticatedUserSid, sid);
            }
            finally
            {
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Close();
                }
            }
        }
    }
}
