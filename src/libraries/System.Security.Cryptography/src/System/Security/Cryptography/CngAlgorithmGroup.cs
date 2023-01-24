// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Utility class to strongly type algorithm groups used with CNG. Since all CNG APIs which require or
    ///     return an algorithm group name take the name as a string, we use this string wrapper class to
    ///     specifically mark which parameters and return values are expected to be algorithm groups.  We also
    ///     provide a list of well known algorithm group names, which helps Intellisense users find a set of
    ///     good algorithm group names to use.
    /// </summary>
    public sealed class CngAlgorithmGroup : IEquatable<CngAlgorithmGroup>
    {
        public CngAlgorithmGroup(string algorithmGroup)
        {
            ArgumentException.ThrowIfNullOrEmpty(algorithmGroup);
            _algorithmGroup = algorithmGroup;
        }

        /// <summary>
        ///     Name of the algorithm group
        /// </summary>
        public string AlgorithmGroup
        {
            get
            {
                return _algorithmGroup;
            }
        }

        public static bool operator ==(CngAlgorithmGroup? left, CngAlgorithmGroup? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(CngAlgorithmGroup? left, CngAlgorithmGroup? right)
        {
            if (left is null)
            {
                return right is not null;
            }

            return !left.Equals(right);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            Debug.Assert(_algorithmGroup != null);

            return Equals(obj as CngAlgorithmGroup);
        }

        public bool Equals([NotNullWhen(true)] CngAlgorithmGroup? other)
        {
            if (other is null)
            {
                return false;
            }

            return _algorithmGroup.Equals(other.AlgorithmGroup);
        }

        public override int GetHashCode()
        {
            Debug.Assert(_algorithmGroup != null);
            return _algorithmGroup.GetHashCode();
        }

        public override string ToString()
        {
            Debug.Assert(_algorithmGroup != null);
            return _algorithmGroup;
        }

        //
        // Well known algorithm groups
        //

        public static CngAlgorithmGroup DiffieHellman
        {
            get
            {
                return s_dh ??= new CngAlgorithmGroup("DH"); // NCRYPT_DH_ALGORITHM_GROUP
            }
        }

        public static CngAlgorithmGroup Dsa
        {
            get
            {
                return s_dsa ??= new CngAlgorithmGroup("DSA"); // NCRYPT_DSA_ALGORITHM_GROUP
            }
        }

        public static CngAlgorithmGroup ECDiffieHellman
        {
            get
            {
                return s_ecdh ??= new CngAlgorithmGroup("ECDH"); // NCRYPT_ECDH_ALGORITHM_GROUP
            }
        }

        public static CngAlgorithmGroup ECDsa
        {
            get
            {
                return s_ecdsa ??= new CngAlgorithmGroup("ECDSA"); // NCRYPT_ECDSA_ALGORITHM_GROUP
            }
        }

        public static CngAlgorithmGroup Rsa
        {
            get
            {
                return s_rsa ??= new CngAlgorithmGroup("RSA"); // NCRYPT_RSA_ALGORITHM_GROUP
            }
        }

        private static CngAlgorithmGroup? s_dh;
        private static CngAlgorithmGroup? s_dsa;
        private static CngAlgorithmGroup? s_ecdh;
        private static CngAlgorithmGroup? s_ecdsa;
        private static CngAlgorithmGroup? s_rsa;

        private readonly string _algorithmGroup;
    }
}
