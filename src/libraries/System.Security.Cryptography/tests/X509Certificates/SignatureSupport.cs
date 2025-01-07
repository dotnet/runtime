// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public class SignatureSupport
    {
        // The RHEL9/CentOS9/Fedora39 change to disable SHA-1 signature support only affects OpenSSL's
        // equivalent of RSA.SignHash/VerifyHash, but affects all asymmetric algorithms' versions of
        // SignData/VerifyData. The OpenSSL library uses the VerifyData-esque path as an implementation
        // detail when checking certificate signatures, and that means that in the context of X509Chain
        // it's all SHA-1-based signatures.
        //
        // If there's ever a platform that blocks RSASSA+SHA-1 but doesn't block ECDSA or DSA with SHA-1,
        // the logic here will need to get more complicated.
        public static bool SupportsX509Sha1Signatures { get; } = GetSupportsX509Sha1Signatures();


        private static bool GetSupportsX509Sha1Signatures()
        {
            RSA rsa;

            try
            {
                rsa = RSA.Create();
            }
            catch (PlatformNotSupportedException)
            {
                return false;
            }

            return System.Security.Cryptography.Tests.SignatureSupport.CanProduceSha1Signature(rsa);
        }
    }
}
