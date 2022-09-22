// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;

namespace System.Security.Cryptography.Rsa.Tests
{
    public interface IRSAProvider
    {
        RSA Create();
        RSA Create(int keySize);
        bool Supports384PrivateKey { get; }
        bool SupportsLargeExponent { get; }
        bool SupportsSha2Oaep { get; }
        bool SupportsPss { get; }
        bool SupportsSha1Signatures { get; }
    }

    public static partial class RSAFactory
    {
        public static RSA Create()
        {
            return s_provider.Create();
        }

        public static RSA Create(int keySize)
        {
            return s_provider.Create(keySize);
        }

        public static RSA Create(RSAParameters rsaParameters)
        {
            RSA rsa = Create();
            rsa.ImportParameters(rsaParameters);
            return rsa;
        }

        internal static RSALease CreateIdempotent()
        {
            RSALease? lease = null;
            CreateIdempotent(ref lease);

            if (lease != null)
            {
                return lease.Value;
            }

            RSA key = Create();
            return new RSALease(key, key);
        }

        internal static RSALease CreateIdempotent(int keySize)
        {
            RSALease? lease = null;
            CreateIdempotent(keySize, ref lease);

            if (lease != null)
            {
                return lease.Value;
            }

            RSA key = Create(keySize);
            return new RSALease(key, key);
        }

        static partial void CreateIdempotent(ref RSALease? lease);
        static partial void CreateIdempotent(int keySize, ref RSALease? lease);

        public static bool Supports384PrivateKey => s_provider.Supports384PrivateKey;

        public static bool SupportsLargeExponent => s_provider.SupportsLargeExponent;

        public static bool SupportsSha2Oaep => s_provider.SupportsSha2Oaep;

        public static bool SupportsPss => s_provider.SupportsPss;

        public static bool SupportsSha1Signatures => s_provider.SupportsSha1Signatures;
    }
}
