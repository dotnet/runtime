// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class Oids
    {
        internal static readonly Oid RsaOid = new Oid(Rsa, "RSA");
        internal static readonly Oid EcPublicKeyOid = new Oid(EcPublicKey, "ECC");
        internal static readonly Oid secp256r1Oid = new Oid(secp256r1, nameof(ECCurve.NamedCurves.nistP256));
        internal static readonly Oid secp384r1Oid = new Oid(secp384r1, nameof(ECCurve.NamedCurves.nistP384));
        internal static readonly Oid secp521r1Oid = new Oid(secp521r1, nameof(ECCurve.NamedCurves.nistP521));
    }
}
