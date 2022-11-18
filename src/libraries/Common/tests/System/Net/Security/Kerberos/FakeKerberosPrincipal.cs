// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Kerberos.NET.Crypto;
using Kerberos.NET.Entities;
using Kerberos.NET.Server;

namespace System.Net.Security.Kerberos;

class FakeKerberosPrincipal : IKerberosPrincipal
{
    private readonly byte[] _password;

    public FakeKerberosPrincipal(PrincipalType type, string principalName, string realm, byte[] password)
    {
        this.Type = type;
        this.PrincipalName = principalName;
        this.Realm = realm;
        this.Expires = DateTimeOffset.UtcNow.AddMonths(1);
        this._password = password;
    }

    public SupportedEncryptionTypes SupportedEncryptionTypes { get; set; }
            = SupportedEncryptionTypes.Aes128CtsHmacSha196 |
            SupportedEncryptionTypes.Aes256CtsHmacSha196 |
            SupportedEncryptionTypes.Aes128CtsHmacSha256 |
            SupportedEncryptionTypes.Aes256CtsHmacSha384 |
            SupportedEncryptionTypes.Rc4Hmac |
            SupportedEncryptionTypes.DesCbcCrc |
            SupportedEncryptionTypes.DesCbcMd5;

    public IEnumerable<PaDataType> SupportedPreAuthenticationTypes { get; set; } = new[]
    {
        PaDataType.PA_ENC_TIMESTAMP,
        PaDataType.PA_PK_AS_REQ
    };

    public PrincipalType Type { get; private set; }

    public string PrincipalName { get; private set; }

    public string Realm { get; private set; }

    public DateTimeOffset? Expires { get; set; }

    public PrivilegedAttributeCertificate? GeneratePac() => null;

    private static readonly ConcurrentDictionary<string, KerberosKey> KeyCache = new();

    public KerberosKey RetrieveLongTermCredential()
    {
        return this.RetrieveLongTermCredential(EncryptionType.AES256_CTS_HMAC_SHA1_96);
    }

    public KerberosKey RetrieveLongTermCredential(EncryptionType etype)
    {
        return KeyCache.GetOrAdd(etype + this.PrincipalName, pn =>
        {
            return new KerberosKey(
                password: this._password,
                principal: new PrincipalName(PrincipalNameType.NT_PRINCIPAL, Realm, new[] { this.PrincipalName }),
                etype: etype,
                saltType: SaltType.ActiveDirectoryUser);
        });
    }

    public void Validate(X509Certificate2Collection certificates)
    {
    }
}
