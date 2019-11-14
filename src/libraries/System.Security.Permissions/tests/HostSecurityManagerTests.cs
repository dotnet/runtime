// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Policy;
using Xunit;

namespace System.Security.Permissions.Tests
{
    public class HostSecurityManagerTests
    {
        [Fact]
        public static void CallMethods()
        {
            HostSecurityManager hsm = new HostSecurityManager();
            ApplicationTrust at = hsm.DetermineApplicationTrust(new Evidence(), new Evidence(), new TrustManagerContext());
            Evidence e = hsm.ProvideAppDomainEvidence(new Evidence());
        }
    }
}
