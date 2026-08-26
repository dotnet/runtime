// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Kerberos.NET.Crypto;
using Kerberos.NET.Entities;
using Kerberos.NET.Entities.Pac;
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

    // Deterministic domain SID for the fake realm. The value has the shape of an Active
    // Directory domain SID so that a PAC issued here matches what a real KDC produces.
    private static readonly SecurityIdentifier s_domainSid = new SecurityIdentifier(
        IdentifierAuthority.NTAuthority,
        new uint[] { 21, 2127521184, 1604012920, 1887927527 },
        SidAttributes.SE_GROUP_ENABLED);

    /// <summary>
    /// Relative identifier of this principal within <see cref="s_domainSid"/>, or null to issue
    /// no PAC at all. Issuing no PAC is the default because it matches a KDC that does not model
    /// Windows group membership, such as MIT Kerberos.
    /// </summary>
    public uint? UserId { get; set; }

    /// <summary>
    /// Relative identifiers of the domain groups the principal belongs to.
    /// </summary>
    public uint[] GroupIds { get; set; } = new uint[] { DomainUsersGroupId };

    /// <summary>
    /// Fully qualified SIDs of groups outside the logon domain, carried in the ExtraSids field.
    /// </summary>
    public SecurityIdentifier[] ExtraGroupSids { get; set; } = Array.Empty<SecurityIdentifier>();

    public const uint DomainUsersGroupId = 513;

    public SecurityIdentifier DomainSid => s_domainSid;

    public PrivilegedAttributeCertificate? GeneratePac()
    {
        if (UserId is not uint userId)
        {
            return null;
        }

        return new PrivilegedAttributeCertificate
        {
            LogonInfo = new PacLogonInfo
            {
                UserName = PrincipalName,
                UserDisplayName = PrincipalName,
                LogonScript = string.Empty,
                ProfilePath = string.Empty,
                HomeDirectory = string.Empty,
                HomeDrive = string.Empty,
                ServerName = "FAKEKDC",
                DomainName = Realm,
                DomainSid = s_domainSid,
                UserId = userId,
                GroupId = DomainUsersGroupId,
                GroupIds = GroupIds.Select(id => new GroupMembership
                {
                    RelativeId = id,
                    Attributes = SidAttributes.SE_GROUP_ENABLED,
                }).ToList(),
                ExtraIds = ExtraGroupSids.Select(sid => new RpcSidAttributes
                {
                    Sid = sid.ToRpcSid(),
                    Attributes = SidAttributes.SE_GROUP_ENABLED,
                }).ToList(),
            },
        };
    }

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
