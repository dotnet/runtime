// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Utility class to strongly type providers used with CNG. Since all CNG APIs which require a
    ///     provider name take the name as a string, we use this string wrapper class to specifically mark
    ///     which parameters are expected to be providers.  We also provide a list of well known provider
    ///     names, which helps Intellisense users find a set of good provider names to use.
    /// </summary>
    public sealed class CngProvider : IEquatable<CngProvider>
    {
        public CngProvider(string provider)
        {
            ArgumentException.ThrowIfNullOrEmpty(provider);
            _provider = provider;
        }

        /// <summary>
        ///     Name of the CNG provider
        /// </summary>
        public string Provider
        {
            get
            {
                return _provider;
            }
        }

        public static bool operator ==(CngProvider? left, CngProvider? right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(CngProvider? left, CngProvider? right)
        {
            if (left is null)
            {
                return right is not null;
            }

            return !left.Equals(right);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            Debug.Assert(_provider != null);

            return Equals(obj as CngProvider);
        }

        public bool Equals([NotNullWhen(true)] CngProvider? other)
        {
            if (other is null)
            {
                return false;
            }

            return _provider.Equals(other.Provider);
        }

        public override int GetHashCode()
        {
            Debug.Assert(_provider != null);
            return _provider.GetHashCode();
        }

        public override string ToString()
        {
            Debug.Assert(_provider != null);
            return _provider.ToString();
        }

        //
        // Well known NCrypt KSPs
        //

        /// <summary>
        /// Gets a <see cref="CngProvider" /> object that specifies the Microsoft Platform Crypto Storage Provider.
        /// </summary>
        /// <value>An object that specifies the Microsoft Platform Crypto Storage Provider.</value>
        public static CngProvider MicrosoftPlatformCryptoProvider
        {
            get
            {
                return s_msPlatformKsp ??= new CngProvider("Microsoft Platform Crypto Provider"); // MS_PLATFORM_CRYPTO_PROVIDER
            }
        }

        public static CngProvider MicrosoftSmartCardKeyStorageProvider
        {
            get
            {
                return s_msSmartCardKsp ??= new CngProvider("Microsoft Smart Card Key Storage Provider"); // MS_SMART_CARD_KEY_STORAGE_PROVIDER
            }
        }

        public static CngProvider MicrosoftSoftwareKeyStorageProvider
        {
            get
            {
                return s_msSoftwareKsp ??= new CngProvider("Microsoft Software Key Storage Provider"); // MS_KEY_STORAGE_PROVIDER
            }
        }

        private static CngProvider? s_msPlatformKsp;
        private static CngProvider? s_msSmartCardKsp;
        private static CngProvider? s_msSoftwareKsp;

        private readonly string _provider;
    }
}
