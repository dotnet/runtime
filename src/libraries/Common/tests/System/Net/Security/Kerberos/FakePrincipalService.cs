// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Kerberos.NET.Crypto;
using Kerberos.NET.Entities;
using Kerberos.NET.Server;

namespace System.Net.Security.Kerberos;

class FakePrincipalService : IPrincipalService
{
    private readonly string realm;

    public FakePrincipalService(string realm)
    {
        this.realm = realm;
    }

    public Task<IKerberosPrincipal?> FindAsync(KrbPrincipalName principalName, string? realm = null)
    {
        return Task.FromResult(Find(principalName, realm));
    }

    public IKerberosPrincipal? Find(KrbPrincipalName principalName, string? realm = null)
    {
        IKerberosPrincipal? principal = null;

        bool fallback = false;

        if (principalName.FullyQualifiedName.Contains("-fallback", StringComparison.OrdinalIgnoreCase) &&
            principalName.Type == PrincipalNameType.NT_ENTERPRISE)
        {
            principal = null;
            fallback = true;
        }

        if ((principalName.FullyQualifiedName.EndsWith(this.realm, StringComparison.InvariantCultureIgnoreCase) ||
            principalName.FullyQualifiedName.StartsWith("krbtgt", StringComparison.InvariantCultureIgnoreCase) ||
            principalName.Type == PrincipalNameType.NT_PRINCIPAL)
            && !fallback)
        {
            principal = new FakeKerberosPrincipal(principalName.FullyQualifiedName, this.realm);
        }

        return principal;
    }

    public X509Certificate2 RetrieveKdcCertificate()
    {
        throw new NotImplementedException();
    }

    private static readonly Dictionary<KeyAgreementAlgorithm, IExchangeKey> KeyCache = new();

    public IExchangeKey? RetrieveKeyCache(KeyAgreementAlgorithm algorithm)
    {
        if (KeyCache.TryGetValue(algorithm, out IExchangeKey? key))
        {
            if (key.CacheExpiry < DateTimeOffset.UtcNow)
            {
                KeyCache.Remove(algorithm);
            }
            else
            {
                return key;
            }
        }

        return null;
    }

    public IExchangeKey CacheKey(IExchangeKey key)
    {
        key.CacheExpiry = DateTimeOffset.UtcNow.AddMinutes(60);

        KeyCache[key.Algorithm] = key;

        return key;
    }
}
