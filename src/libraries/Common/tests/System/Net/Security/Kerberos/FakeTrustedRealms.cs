// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kerberos.NET.Entities;
using Kerberos.NET.Server;

namespace System.Net.Security.Kerberos;

class FakeTrustedRealms : ITrustedRealmService
{
    private readonly string _currentRealm;

    public FakeTrustedRealms(string name)
    {
        _currentRealm = name;
    }

    public IRealmReferral? ProposeTransit(KrbTgsReq tgsReq, PreAuthenticationContext context)
    {
        if (!tgsReq.Body.SName.FullyQualifiedName.EndsWith(_currentRealm, StringComparison.InvariantCultureIgnoreCase) &&
            !tgsReq.Body.SName.FullyQualifiedName.Contains("not.found"))
        {
            return new FakeRealmReferral(tgsReq.Body);
        }

        return null;
    }
}
