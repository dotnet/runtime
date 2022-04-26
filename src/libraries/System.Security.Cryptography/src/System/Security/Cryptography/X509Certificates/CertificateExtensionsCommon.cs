// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    internal static class CertificateExtensionsCommon
    {
        public static T? GetPublicKey<T>(
            this X509Certificate2 certificate,
            Predicate<X509Certificate2>? matchesConstraints = null)
            where T : AsymmetricAlgorithm
        {
            ArgumentNullException.ThrowIfNull(certificate);

            string oidValue = GetExpectedOidValue<T>();
            PublicKey publicKey = certificate.PublicKey;
            Oid algorithmOid = publicKey.Oid;

            if (oidValue != algorithmOid.Value)
                return null;

            if (matchesConstraints != null && !matchesConstraints(certificate))
                return null;

            if (typeof(T) == typeof(RSA) || typeof(T) == typeof(DSA))
            {
                byte[] rawEncodedKeyValue = publicKey.EncodedKeyValue.RawData;
                byte[] rawEncodedParameters = publicKey.EncodedParameters.RawData;
                return (T)(X509Pal.Instance.DecodePublicKey(algorithmOid, rawEncodedKeyValue, rawEncodedParameters, certificate.Pal));
            }
            else if (typeof(T) == typeof(ECDsa))
            {
                return (T)(object)(X509Pal.Instance.DecodeECDsaPublicKey(certificate.Pal));
            }
            else if (typeof(T) == typeof(ECDiffieHellman))
            {
                return (T)(object)(X509Pal.Instance.DecodeECDiffieHellmanPublicKey(certificate.Pal));
            }

            Debug.Fail("Expected GetExpectedOidValue() to have thrown before we got here.");
            throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
        }

        public static T? GetPrivateKey<T>(
            this X509Certificate2 certificate,
            Predicate<X509Certificate2>? matchesConstraints = null)
            where T : AsymmetricAlgorithm
        {
            ArgumentNullException.ThrowIfNull(certificate);

            string oidValue = GetExpectedOidValue<T>();
            if (!certificate.HasPrivateKey || oidValue != certificate.PublicKey.Oid.Value)
                return null;

            if (matchesConstraints != null && !matchesConstraints(certificate))
                return null;

            if (typeof(T) == typeof(RSA))
                return (T?)(object?)certificate.Pal.GetRSAPrivateKey();

            if (typeof(T) == typeof(ECDsa))
                return (T?)(object?)certificate.Pal.GetECDsaPrivateKey();

            if (typeof(T) == typeof(DSA))
                return (T?)(object?)certificate.Pal.GetDSAPrivateKey();

            if (typeof(T) == typeof(ECDiffieHellman))
                return (T?)(object?)certificate.Pal.GetECDiffieHellmanPrivateKey();

            Debug.Fail("Expected GetExpectedOidValue() to have thrown before we got here.");
            throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
        }

        private static string GetExpectedOidValue<T>() where T : AsymmetricAlgorithm
        {
            if (typeof(T) == typeof(RSA))
                return Oids.Rsa;
            if (typeof(T) == typeof(ECDsa) || typeof(T) == typeof(ECDiffieHellman))
                // Neither Windows nor OpenSSL permit id-ECDH as the SPKI public key algorithm.
                return Oids.EcPublicKey;
            if (typeof(T) == typeof(DSA))
                return Oids.Dsa;
            throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
        }
    }
}
