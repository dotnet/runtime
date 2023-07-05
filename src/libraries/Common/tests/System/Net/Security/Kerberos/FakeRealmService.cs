// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kerberos.NET.Configuration;
using Kerberos.NET.Server;

namespace System.Net.Security.Kerberos;

class FakeRealmService : IRealmService
{
    private readonly IPrincipalService _principalService;
    private readonly KerberosCompatibilityFlags _compatibilityFlags;

    public FakeRealmService(string realm, Krb5Config config, IPrincipalService principalService, KerberosCompatibilityFlags compatibilityFlags = KerberosCompatibilityFlags.None)
    {
        Name = realm;
        Configuration = config;
        _principalService = principalService;
        _compatibilityFlags = compatibilityFlags;
    }

    public IRealmSettings Settings => new FakeRealmSettings(_compatibilityFlags);

    public IPrincipalService Principals => _principalService;

    public string Name { get; private set; }

    public DateTimeOffset Now() => DateTimeOffset.UtcNow;

    public ITrustedRealmService TrustedRealms => new FakeTrustedRealms(this.Name);

    public Krb5Config Configuration { get; private set; }
}