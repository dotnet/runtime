// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kerberos.NET.Entities;
using Kerberos.NET.Server;

namespace System.Net.Security.Kerberos;

class FakeRealmReferral : IRealmReferral
{
    private readonly KrbKdcReqBody _body;

    public FakeRealmReferral(KrbKdcReqBody body)
    {
        _body = body;
    }

    public IKerberosPrincipal Refer()
    {
        var fqn = _body.SName.FullyQualifiedName;
        var predictedRealm = fqn[(fqn.IndexOf('.') + 1)..];

        var krbName = KrbPrincipalName.FromString($"krbtgt/{predictedRealm}");

        return new FakeKerberosPrincipal(PrincipalType.Service, krbName.FullyQualifiedName, predictedRealm, new byte[16]);
    }
}
