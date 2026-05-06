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
    private readonly string _realm;
    private readonly Dictionary<string, IKerberosPrincipal> _principals;

    public FakePrincipalService(string realm)
    {
        _realm = realm;
        _principals = new Dictionary<string, IKerberosPrincipal>(StringComparer.InvariantCultureIgnoreCase);
    }

    public void Add(string name, IKerberosPrincipal principal)
    {
        _principals.Add(name, principal);
    }

    public Task<IKerberosPrincipal?> FindAsync(KrbPrincipalName principalName, string? realm = null)
    {
        return Task.FromResult(Find(principalName, realm));
    }

    public IKerberosPrincipal? Find(KrbPrincipalName principalName, string? realm = null)
    {
        if (_principals.TryGetValue(principalName.FullyQualifiedName, out var principal))
        {
            return principal;
        }

        return null;
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
