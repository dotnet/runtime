// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Security.Cryptography
{
    public sealed partial class SafeEvpPKeyHandle : SafeHandle
    {
        internal static readonly SafeEvpPKeyHandle InvalidHandle = new SafeEvpPKeyHandle();
        private static readonly Lock s_contextCacheLock = new();
        // We don't use a ConcurrentDictionary here because ConcurrentDictionary documents that its valueFactory
        // can execute multiple times for the same key, but only one will win. That would result in a leak in this case,
        // so use an ordinary dictionary that we will take a full lock on.
        private static readonly Dictionary<string, IntPtr> s_contextCache = new(StringComparer.Ordinal);

        /// <summary>
        /// In some cases like when a key is loaded from a provider, the key may have an associated
        /// process-lifetime context that is needed for operations using the key.
        /// </summary>
        internal IntPtr ExtraHandle { get; private set; }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public SafeEvpPKeyHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public SafeEvpPKeyHandle(IntPtr handle, bool ownsHandle)
            : base(handle, ownsHandle)
        {
        }

        internal SafeEvpPKeyHandle(IntPtr handle, IntPtr extraHandle)
            : base(handle, ownsHandle: true)
        {
            ExtraHandle = extraHandle;
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.EvpPkeyDestroy(handle);
            ExtraHandle = IntPtr.Zero;

            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        /// <summary>
        /// Create another instance of SafeEvpPKeyHandle which has an independent lifetime
        /// from this instance, but tracks the same resource.
        /// </summary>
        /// <returns>An equivalent SafeEvpPKeyHandle with a different lifetime</returns>
        public SafeEvpPKeyHandle DuplicateHandle()
        {
            if (IsInvalid)
                throw new InvalidOperationException(SR.Cryptography_OpenInvalidHandle);

            // Keep the source handle alive so that a concurrent Dispose on another
            // thread does not zero the handle field between UpRef and the copy below.
            bool addedRef = false;

            try
            {
                DangerousAddRef(ref addedRef);

                // Reliability: Allocate the SafeHandle before calling UpRefEvpPkey so
                // that we don't lose a tracked reference in low-memory situations.
                SafeEvpPKeyHandle safeHandle = new SafeEvpPKeyHandle();

                int success = Interop.Crypto.UpRefEvpPkey(this);

                if (success != 1)
                {
                    Debug.Fail("Called UpRefEvpPkey on a key which was already marked for destruction");
                    Exception e = Interop.Crypto.CreateOpenSslCryptographicException();
                    safeHandle.Dispose();
                    throw e;
                }

                // Since we didn't actually create a new handle, copy the handle
                // to the new SafeHandle. DangerousAddRef prevents ReleaseHandle
                // from being called, so handle and ExtraHandle are stable here.
                safeHandle.SetHandle(handle);
                // ExtraHandle points to process-lifetime state.
                safeHandle.ExtraHandle = ExtraHandle;
                return safeHandle;
            }
            finally
            {
                if (addedRef)
                {
                    DangerousRelease();
                }
            }
        }

        /// <summary>
        ///   Open a named private key using a named OpenSSL <code>ENGINE</code>.
        /// </summary>
        /// <param name="engineName">
        ///   The name of the <code>ENGINE</code> to process the private key open request.
        /// </param>
        /// <param name="keyId">
        ///   The name of the key to open.
        /// </param>
        /// <returns>
        ///   The opened key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="engineName"/> or <paramref name="keyId"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="engineName"/> or <paramref name="keyId"/> is the empty string.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the key could not be opened via the specified ENGINE.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     This operation will fail if OpenSSL cannot successfully load the named <code>ENGINE</code>,
        ///     or if the named <code>ENGINE</code> cannot load the named key.
        ///   </para>
        ///   <para>
        ///     Not all <code>ENGINE</code>s support loading private keys.
        ///   </para>
        ///   <para>
        ///     The syntax for <paramref name="keyId"/> is determined by each individual
        ///     <code>ENGINE</code>.
        ///   </para>
        /// </remarks>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public static SafeEvpPKeyHandle OpenPrivateKeyFromEngine(string engineName, string keyId)
        {
            ArgumentException.ThrowIfNullOrEmpty(engineName);
            ArgumentException.ThrowIfNullOrEmpty(keyId);

            if (!Interop.OpenSslNoInit.OpenSslIsAvailable)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
            }

            return Interop.Crypto.LoadPrivateKeyFromEngine(engineName, keyId);
        }

        /// <summary>
        ///   Open a named public key using a named OpenSSL <code>ENGINE</code>.
        /// </summary>
        /// <param name="engineName">
        ///   The name of the <code>ENGINE</code> to process the public key open request.
        /// </param>
        /// <param name="keyId">
        ///   The name of the key to open.
        /// </param>
        /// <returns>
        ///   The opened key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="engineName"/> or <paramref name="keyId"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="engineName"/> or <paramref name="keyId"/> is the empty string.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the key could not be opened via the specified ENGINE.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     This operation will fail if OpenSSL cannot successfully load the named <code>ENGINE</code>,
        ///     or if the named <code>ENGINE</code> cannot load the named key.
        ///   </para>
        ///   <para>
        ///     Not all <code>ENGINE</code>s support loading public keys, even ones that support
        ///     loading private keys.
        ///   </para>
        ///   <para>
        ///     The syntax for <paramref name="keyId"/> is determined by each individual
        ///     <code>ENGINE</code>.
        ///   </para>
        /// </remarks>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public static SafeEvpPKeyHandle OpenPublicKeyFromEngine(string engineName, string keyId)
        {
            ArgumentException.ThrowIfNullOrEmpty(engineName);
            ArgumentException.ThrowIfNullOrEmpty(keyId);

            if (!Interop.OpenSslNoInit.OpenSslIsAvailable)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
            }

            return Interop.Crypto.LoadPublicKeyFromEngine(engineName, keyId);
        }

        /// <summary>
        ///   Open a named public key using a named <c>OSSL_PROVIDER</c>.
        /// </summary>
        /// <param name="providerName">
        ///   The name of the <c>OSSL_PROVIDER</c> to process the key open request.
        /// </param>
        /// <param name="keyUri">
        ///   The URI assigned by the <c>OSSL_PROVIDER</c> of the key to open.
        /// </param>
        /// <returns>
        ///   The opened key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="providerName"/> or <paramref name="keyUri"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="providerName"/> or <paramref name="keyUri"/> is the empty string.
        ///   -or-
        ///   <paramref name="providerName"/> or <paramref name="keyUri"/> contains an embedded null character.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the key could not be opened via the specified named <c>OSSL_PROVIDER</c>.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     Both <paramref name="providerName" /> and <paramref name="keyUri" /> must be trusted inputs.
        ///   </para>
        ///   <para>
        ///     This operation will fail if OpenSSL cannot successfully load the named <c>OSSL_PROVIDER</c>,
        ///     or if the named <c>OSSL_PROVIDER</c> cannot load the named key.
        ///   </para>
        ///   <para>
        ///     The syntax for <paramref name="keyUri"/> is determined by each individual
        ///     named <c>OSSL_PROVIDER</c>.
        ///   </para>
        /// </remarks>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public static SafeEvpPKeyHandle OpenKeyFromProvider(string providerName, string keyUri)
        {
            ValidateProviderName(providerName, nameof(providerName));
            ValidateKeyUri(keyUri);

            if (!Interop.OpenSslNoInit.OpenSslIsAvailable)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
            }

            string[] providerNames = [providerName];
            return OpenKeyFromProviderCore(
                providerNames,
                keyUri,
                propertyQuery: null);
        }

        /// <summary>
        ///   Opens a named key using named <c>OSSL_PROVIDER</c>s.
        /// </summary>
        /// <param name="providerNames">
        ///   The names of the <c>OSSL_PROVIDER</c>s to load for the key open request.
        /// </param>
        /// <param name="keyUri">
        ///   The URI assigned by the <c>OSSL_PROVIDER</c> of the key to open.
        /// </param>
        /// <param name="propertyQuery">
        ///   The property query to use for the <c>OSSL_STORE_open_ex</c> operation.
        /// </param>
        /// <returns>
        ///   The opened key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="providerNames"/> or <paramref name="keyUri"/> is <see langword="null" />.
        ///   -or-
        ///   <paramref name="providerNames"/> contains a <see langword="null" /> value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="providerNames"/> contains no entries.
        ///   -or-
        ///   <paramref name="providerNames"/> contains an empty string or a string containing an embedded null character.
        ///   -or-
        ///   <paramref name="providerNames"/> contains a duplicate entry.
        ///   -or-
        ///   <paramref name="propertyQuery"/> contains an embedded null character.
        ///   -or-
        ///   <paramref name="keyUri"/> is the empty string or contains an embedded null character.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The key could not be opened via the specified named <c>OSSL_PROVIDER</c>s.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     <paramref name="providerNames" />, <paramref name="keyUri" />, and
        ///     <paramref name="propertyQuery" /> must be trusted inputs.
        ///   </para>
        ///   <para>
        ///     This operation will fail if OpenSSL cannot successfully load all specified
        ///     <c>OSSL_PROVIDER</c>s, or if the specified <c>OSSL_PROVIDER</c>s cannot load the named key.
        ///   </para>
        ///   <para>
        ///     The syntax for <paramref name="keyUri"/> is determined by each individual
        ///     named <c>OSSL_PROVIDER</c>.
        ///   </para>
        ///   <para>
        ///     The <paramref name="propertyQuery"/> value is used only for the
        ///     <c>OSSL_STORE_open_ex</c> operation.
        ///   </para>
        ///   <para>
        ///     The order in which the providers are loaded is the same order in which the <paramref name="providerNames" />
        ///     enumerable returns them. When loading multiple providers, it is more efficient to ensure the same order
        ///     is used consistently. For example, with the providers <c>[a, b]</c>, passing <c>a</c> and <c>b</c> in the
        ///     same order may be more efficient than doing <c>[a, b]</c> followed by <c>[b, a]</c>.
        ///   </para>
        /// </remarks>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public static SafeEvpPKeyHandle OpenKeyFromProvider(
            IEnumerable<string> providerNames,
            string keyUri,
            string? propertyQuery = null)
        {
            ArgumentNullException.ThrowIfNull(providerNames);
            ValidateKeyUri(keyUri);
            ThrowIfPropertyQueryContainsNullCharacter(propertyQuery);

            if (!Interop.OpenSslNoInit.OpenSslIsAvailable)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
            }

            // Preserve provider order while checking for duplicates.
            List<string> providersList = new();

            foreach (string provider in providerNames)
            {
                ValidateProviderName(provider, nameof(providerNames));

                // The number of providers is expected to be in the single digits. While this Contains then Add is worst
                // case O(n^2), we expect the inputs to be small. The API also explicitly documents that all inputs
                // "must be trusted inputs".
                if (providersList.Contains(provider))
                {
                    throw new ArgumentException(SR.InvalidOperation_DuplicateItemNotAllowed, nameof(providerNames));
                }

                providersList.Add(provider);
            }

            if (providersList.Count == 0)
            {
                throw new ArgumentException(SR.Arg_EmptyCollection, nameof(providerNames));
            }

            string[] providerNameArray = providersList.ToArray();
            return OpenKeyFromProviderCore(providerNameArray, keyUri, propertyQuery);
        }

        private static SafeEvpPKeyHandle OpenKeyFromProviderCore(
            string[] providerNames,
            string keyUri,
            string? propertyQuery)
        {
            string cacheKey = CreateProviderCacheKey(providerNames);
            IntPtr extraHandle;

            lock (s_contextCacheLock)
            {
                if (!s_contextCache.TryGetValue(cacheKey, out extraHandle))
                {
                    // Allocate capacity before native code creates the process-lifetime context.
                    s_contextCache.EnsureCapacity(s_contextCache.Count + 1);

                    try
                    {
                        extraHandle = IntPtr.Zero;
                        return Interop.Crypto.LoadKeyFromProvider(providerNames, keyUri, propertyQuery, ref extraHandle);
                    }
                    finally
                    {
                        // LoadKeyFromProvider may still have given an extraHandle even if it threw an exception because
                        // the key couldn't be found. We still want to cache the extra handle in that circumstance.
                        if (extraHandle != IntPtr.Zero)
                        {
                            s_contextCache.Add(cacheKey, extraHandle);
                        }
                    }
                }
            }

            return Interop.Crypto.LoadKeyFromProvider(providerNames, keyUri, propertyQuery, ref extraHandle);
        }

        private static void ValidateProviderName(string providerName, string paramName)
        {
            ArgumentException.ThrowIfNullOrEmpty(providerName, paramName);

            if (providerName.Contains('\0'))
            {
                throw new ArgumentException(SR.Argument_InvalidValue, paramName);
            }
        }

        private static void ValidateKeyUri(string keyUri)
        {
            ArgumentException.ThrowIfNullOrEmpty(keyUri);

            if (keyUri.Contains('\0'))
            {
                throw new ArgumentException(SR.Argument_InvalidValue, nameof(keyUri));
            }
        }

        private static void ThrowIfPropertyQueryContainsNullCharacter(string? propertyQuery)
        {
            if (propertyQuery is not null && propertyQuery.Contains('\0'))
            {
                throw new ArgumentException(SR.Argument_InvalidValue, nameof(propertyQuery));
            }
        }

        private static string CreateProviderCacheKey(string[] providerNames)
        {
            // U+0000 is not valid in provider names, so it's a valid discriminator between provider names. The cache
            // key is never given to native code; it's only used on the managed side. This way provider sets
            // [foo, bar] and [fo, obar] are distinct since they result in foo\0bar and fo\0obar, respectively.
            return string.Join('\0', providerNames);
        }
    }
}
