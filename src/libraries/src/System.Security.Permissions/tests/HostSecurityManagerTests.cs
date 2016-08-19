// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Permissions.Tests
{
    public class HostSecurityManagerTests
    {
        [Fact]
        public static void CallMethods()
        {
            HostSecurityManager hsm = new HostSecurityManager();
            Policy.ApplicationTrust at = hsm.DetermineApplicationTrust(new Policy.Evidence(), new Policy.Evidence(), new Policy.TrustManagerContext());
            Policy.Evidence e = hsm.ProvideAppDomainEvidence(new Policy.Evidence());
        }
    }
}
