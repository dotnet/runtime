// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

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
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (provider.Length == 0)
                throw new ArgumentException(SR.Format(SR.Cryptography_InvalidProviderName, provider), nameof(provider));

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

        public static bool operator ==(CngProvider left, CngProvider right)
        {
            if (object.ReferenceEquals(left, null))
                return object.ReferenceEquals(right, null);

            return left.Equals(right);
        }

        public static bool operator !=(CngProvider left, CngProvider right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return !object.ReferenceEquals(right, null);
            }

            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            Debug.Assert(_provider != null);

            return Equals(obj as CngProvider);
        }

        public bool Equals(CngProvider other)
        {
            if (object.ReferenceEquals(other, null))
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

        public static CngProvider MicrosoftSmartCardKeyStorageProvider
        {
            get
            {
                return s_msSmartCardKsp ?? (s_msSmartCardKsp = new CngProvider("Microsoft Smart Card Key Storage Provider")); // MS_SMART_CARD_KEY_STORAGE_PROVIDER
            }
        }

        public static CngProvider MicrosoftSoftwareKeyStorageProvider
        {
            get
            {
                return s_msSoftwareKsp ?? (s_msSoftwareKsp = new CngProvider("Microsoft Software Key Storage Provider")); // MS_KEY_STORAGE_PROVIDER
            }
        }

        private static CngProvider s_msSmartCardKsp;
        private static CngProvider s_msSoftwareKsp;

        private readonly string _provider;
    }
}

