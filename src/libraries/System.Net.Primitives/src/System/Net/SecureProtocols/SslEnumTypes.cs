// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Runtime.InteropServices;

namespace System.Security.Authentication
{
    [Flags]
    public enum SslProtocols
    {
        None = 0,
        Ssl2 = Interop.SChannel.SP_PROT_SSL2,
        Ssl3 = Interop.SChannel.SP_PROT_SSL3,
        Tls = Interop.SChannel.SP_PROT_TLS1_0,
        Tls11 = Interop.SChannel.SP_PROT_TLS1_1,
        Tls12 = Interop.SChannel.SP_PROT_TLS1_2,
        Default = Ssl3 | Tls
    }

    public enum ExchangeAlgorithmType
    {
        None = 0,
        RsaSign = (Interop.Crypt32.ALG_CLASS_SIGNATURE | Interop.Crypt32.ALG_TYPE_RSA | Interop.Crypt32.ALG_CLASS_ANY),
        RsaKeyX = (Interop.Crypt32.ALG_CLASS_KEY_EXCHANGE | Interop.Crypt32.ALG_TYPE_RSA | Interop.Crypt32.ALG_CLASS_ANY),
        DiffieHellman = (Interop.Crypt32.ALG_CLASS_KEY_EXCHANGE | Interop.Crypt32.ALG_TYPE_DH | Interop.Crypt32.ALG_SID_DH_EPHEM),
    }

    public enum CipherAlgorithmType
    {
        None = 0,  // No encryption
        Rc2 = (Interop.Crypt32.ALG_CLASS_ENCRYPT | Interop.Crypt32.ALG_TYPE_BLOCK | Interop.Crypt32.ALG_SID_RC2),
        Rc4 = (Interop.Crypt32.ALG_CLASS_ENCRYPT | Interop.Crypt32.ALG_TYPE_STREAM | Interop.Crypt32.ALG_SID_RC4),
        Des = (Interop.Crypt32.ALG_CLASS_ENCRYPT | Interop.Crypt32.ALG_TYPE_BLOCK | Interop.Crypt32.ALG_SID_DES),
        TripleDes = (Interop.Crypt32.ALG_CLASS_ENCRYPT | Interop.Crypt32.ALG_TYPE_BLOCK | Interop.Crypt32.ALG_SID_3DES),
        Aes = (Interop.Crypt32.ALG_CLASS_ENCRYPT | Interop.Crypt32.ALG_TYPE_BLOCK | Interop.Crypt32.ALG_SID_AES),
        Aes128 = (Interop.Crypt32.ALG_CLASS_ENCRYPT | Interop.Crypt32.ALG_TYPE_BLOCK | Interop.Crypt32.ALG_SID_AES_128),
        Aes192 = (Interop.Crypt32.ALG_CLASS_ENCRYPT | Interop.Crypt32.ALG_TYPE_BLOCK | Interop.Crypt32.ALG_SID_AES_192),
        Aes256 = (Interop.Crypt32.ALG_CLASS_ENCRYPT | Interop.Crypt32.ALG_TYPE_BLOCK | Interop.Crypt32.ALG_SID_AES_256),
        Null = (Interop.Crypt32.ALG_CLASS_ENCRYPT),  // 0-bit NULL cipher algorithm
    }

    public enum HashAlgorithmType
    {
        None = 0,
        Md5 = (Interop.Crypt32.ALG_CLASS_HASH | Interop.Crypt32.ALG_CLASS_ANY | Interop.Crypt32.ALG_SID_MD5),
        Sha1 = (Interop.Crypt32.ALG_CLASS_HASH | Interop.Crypt32.ALG_CLASS_ANY | Interop.Crypt32.ALG_SID_SHA),
        Sha256 = (Interop.Crypt32.ALG_CLASS_HASH | Interop.Crypt32.ALG_CLASS_ANY | Interop.Crypt32.ALG_SID_SHA_256),
        Sha384 = (Interop.Crypt32.ALG_CLASS_HASH | Interop.Crypt32.ALG_CLASS_ANY | Interop.Crypt32.ALG_SID_SHA_384),
        Sha512 = (Interop.Crypt32.ALG_CLASS_HASH | Interop.Crypt32.ALG_CLASS_ANY | Interop.Crypt32.ALG_SID_SHA_512),
    }
}
