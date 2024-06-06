// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    ///   Represents a set of constraints to apply when loading PKCS#12/PFX contents.
    /// </summary>
    public sealed class Pkcs12LoaderLimits
    {
        private bool _isReadOnly;
        private int? _macIterationLimit = 300_000;
        private int? _individualKdfIterationLimit = 300_000;
        private int? _totalKdfIterationLimit = 1_000_000;
        private int? _maxKeys = 200;
        private int? _maxCertificates = 200;
        private bool _preserveStorageProvider;
        private bool _preserveKeyName;
        private bool _preserveCertificateAlias;
        private bool _preserveUnknownAttributes;
        private bool _ignorePrivateKeys;
        private bool _ignoreEncryptedAuthSafes;

        /// <summary>
        ///   Gets a shared reference to the default loader limits.
        /// </summary>
        /// <remarks>
        ///   The singleton instance returned from this property is equivalent to an
        ///   instance produced via the default constructor, except the properties
        ///   prohibit reassignment.  As with the default constructor, the individual
        ///   property values may change over time.
        /// </remarks>
        /// <value>A shared reference to the default loader limits.</value>
        /// <seealso cref="IsReadOnly" />
        public static Pkcs12LoaderLimits Defaults { get; } = MakeReadOnly(new Pkcs12LoaderLimits());

        /// <summary>
        ///   Gets a shared reference to loader limits that indicate no
        ///   filtering or restrictions of the contents should be applied
        ///   before sending them to the underlying system loader.
        /// </summary>
        /// <value>
        ///   A shared reference to loader limits that indicate no
        ///   filtering or restrictions of the contents should be applied
        ///   before sending them to the underlying system loader.
        /// </value>
        /// <remarks>
        ///   <para>
        ///     The system loader may have its own limits where only part
        ///     of the contents are respected, or where the load is rejected.
        ///     Using this set of limits only affects the .NET layer of filtering.
        ///   </para>
        ///   <para>
        ///     The <see cref="X509CertificateLoader" /> class checks for reference
        ///     equality to this property to determine if filtering should be bypassed.
        ///     Making a new Pkcs12LoaderLimits value that has all of the same property
        ///     values may give different results for certain inputs.
        ///   </para>
        /// </remarks>
        public static Pkcs12LoaderLimits DangerousNoLimits { get; } =
            MakeReadOnly(
                new Pkcs12LoaderLimits
                {
                    MacIterationLimit = null,
                    IndividualKdfIterationLimit = null,
                    TotalKdfIterationLimit = null,
                    MaxKeys = null,
                    MaxCertificates = null,
                    PreserveStorageProvider = true,
                    PreserveKeyName = true,
                    PreserveCertificateAlias = true,
                    PreserveUnknownAttributes = true,
                });

        /// <summary>
        ///   Initializes a new instance of the <see cref="Pkcs12LoaderLimits"/> class
        ///   with default values.
        /// </summary>
        /// <remarks>
        ///   The default values for each property on a default instance of this class
        ///   are chosen as a compromise between maximizing compatibility and minimizing
        ///   "nuisance" work.  The defaults for any given property may vary over time.
        /// </remarks>
        public Pkcs12LoaderLimits()
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Pkcs12LoaderLimits"/> class
        ///   by copying the values from another instance.
        /// </summary>
        /// <param name="copyFrom">The instance to copy the values from.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="copyFrom"/> is <see langword="null" />.
        /// </exception>
        public Pkcs12LoaderLimits(Pkcs12LoaderLimits copyFrom)
        {
#if NET
            ArgumentNullException.ThrowIfNull(copyFrom);
#else
            if (copyFrom is null)
                throw new ArgumentNullException(nameof(copyFrom));
#endif

            // Do not copy _isReadOnly.

            _macIterationLimit = copyFrom._macIterationLimit;
            _individualKdfIterationLimit = copyFrom._individualKdfIterationLimit;
            _totalKdfIterationLimit = copyFrom._totalKdfIterationLimit;
            _maxKeys = copyFrom._maxKeys;
            _maxCertificates = copyFrom._maxCertificates;
            _preserveStorageProvider = copyFrom._preserveStorageProvider;
            _preserveKeyName = copyFrom._preserveKeyName;
            _preserveCertificateAlias = copyFrom._preserveCertificateAlias;
            _preserveUnknownAttributes = copyFrom._preserveUnknownAttributes;
            _ignorePrivateKeys = copyFrom._ignorePrivateKeys;
            _ignoreEncryptedAuthSafes = copyFrom._ignoreEncryptedAuthSafes;
        }

        /// <summary>
        ///   Gets a value indicating whether the instance is read-only.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> if the instance is read-only; otherwise, <see langword="false" />.
        /// </value>
        public bool IsReadOnly => _isReadOnly;

        /// <summary>
        ///   Makes the <see cref="Pkcs12LoaderLimits"/> instance read-only.
        /// </summary>
        public void MakeReadOnly()
        {
            _isReadOnly = true;
        }

        private static Pkcs12LoaderLimits MakeReadOnly(Pkcs12LoaderLimits limits)
        {
            limits.MakeReadOnly();
            return limits;
        }

        private void CheckReadOnly()
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException(SR.Cryptography_X509_PKCS12_LimitsReadOnly);
            }
        }

        /// <summary>
        ///   Gets or sets the iteration limit for the MAC calculation.
        /// </summary>
        /// <value>The iteration limit for the MAC calculation, or <see langword="null" /> for no limit.</value>
        public int? MacIterationLimit
        {
            get => _macIterationLimit;
            set
            {
                CheckNonNegative(value);
                CheckReadOnly();
                _macIterationLimit = value;
            }
        }

        /// <summary>
        ///   Gets or sets the iteration limit for the individual Key Derivation Function (KDF) calculations.
        /// </summary>
        /// <value>
        ///   The iteration limit for the individual Key Derivation Function (KDF) calculations,
        ///   or <see langword="null" /> for no limit.
        /// </value>
        public int? IndividualKdfIterationLimit
        {
            get => _individualKdfIterationLimit;
            set
            {
                CheckNonNegative(value);
                CheckReadOnly();
                _individualKdfIterationLimit = value;
            }
        }

        /// <summary>
        ///   Gets or sets the total iteration limit for the Key Derivation Function (KDF) calculations.
        /// </summary>
        /// <value>
        ///   The total iteration limit for the Key Derivation Function (KDF) calculations,
        ///   or <see langword="null" /> for no limit.
        /// </value>
        public int? TotalKdfIterationLimit
        {
            get => _totalKdfIterationLimit;
            set
            {
                CheckNonNegative(value);
                CheckReadOnly();
                _totalKdfIterationLimit = value;
            }
        }

        /// <summary>
        ///   Gets or sets the maximum number of keys permitted.
        /// </summary>
        /// <value>
        ///   The maximum number of keys permitted, or <see langword="null" /> for no maximum.
        /// </value>
        public int? MaxKeys
        {
            get => _maxKeys;
            set
            {
                CheckNonNegative(value);
                CheckReadOnly();
                _maxKeys = value;
            }
        }

        /// <summary>
        ///   Gets or sets the maximum number of certificates permitted.
        /// </summary>
        /// <value>
        ///   The maximum number of certificates permitted, or <see langword="null" /> for no maximum.
        /// </value>
        public int? MaxCertificates
        {
            get => _maxCertificates;
            set
            {
                CheckNonNegative(value);
                CheckReadOnly();
                _maxCertificates = value;
            }
        }

        /// <summary>
        ///   Gets or sets a value indicating whether to preserve the storage provider.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> to respect the storage provider identifier for a
        ///   private key; <see langword="false" /> to ignore the storage provider
        ///   information and use the system defaults.
        ///   The default is <see langword="false" />.
        /// </value>
        /// <remarks>
        ///   Storage Provider values from the PFX are only processed on the
        ///   Microsoft Windows family of operating systems.
        ///   This property has no effect on non-Windows systems.
        /// </remarks>
        public bool PreserveStorageProvider
        {
            get => _preserveStorageProvider;
            set
            {
                CheckReadOnly();
                _preserveStorageProvider = value;
            }
        }

        /// <summary>
        ///   Gets or sets a value indicating whether to preserve the key name.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> to respect the key name identifier for a
        ///   private key; <see langword="false" /> to ignore the key name
        ///   information and use a randomly generated identifier.
        ///   The default is <see langword="false" />.
        /// </value>
        /// <remarks>
        ///   Key name identifier values from the PFX are only processed on the
        ///   Microsoft Windows family of operating systems.
        ///   This property has no effect on non-Windows systems.
        /// </remarks>
        public bool PreserveKeyName
        {
            get => _preserveKeyName;
            set
            {
                CheckReadOnly();
                _preserveKeyName = value;
            }
        }

        /// <summary>
        ///   Gets or sets a value indicating whether to preserve the certificate alias,
        ///   also known as the friendly name.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> to respect the alias for a
        ///   certificate; <see langword="false" /> to ignore the alias
        ///   information.
        ///   The default is <see langword="false" />.
        /// </value>
        /// <remarks>
        ///   Certificate alias values from the PFX are only processed on the
        ///   Microsoft Windows family of operating systems.
        ///   This property has no effect on non-Windows systems.
        /// </remarks>
        /// <seealso cref="X509Certificate2.FriendlyName"/>
        public bool PreserveCertificateAlias
        {
            get => _preserveCertificateAlias;
            set
            {
                CheckReadOnly();
                _preserveCertificateAlias = value;
            }
        }

        /// <summary>
        ///   Gets or sets a value indicating whether to preserve unknown attributes.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> to keep any attributes of a certificate or
        ///   private key that are not described by another property on this type intact
        ///   when invoking the system PKCS#12/PFX loader;
        ///   <see langword="false" /> to remove the unknown attributes prior to invoking
        ///   the system loader.
        ///   The default is <see langword="false" />.
        /// </value>
        public bool PreserveUnknownAttributes
        {
            get => _preserveUnknownAttributes;
            set
            {
                CheckReadOnly();
                _preserveUnknownAttributes = value;
            }
        }

        /// <summary>
        ///   Gets or sets a value indicating whether to ignore private keys.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> to skip loading private keys;
        ///   <see langword="false" /> to load both certificates and private keys.
        ///   The default is <see langword="false" />.
        /// </value>
        public bool IgnorePrivateKeys
        {
            get => _ignorePrivateKeys;
            set
            {
                CheckReadOnly();
                _ignorePrivateKeys = value;
            }
        }

        /// <summary>
        ///   Gets or sets a value indicating whether to ignore encrypted authentication safes.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> to skip over encrypted PFX AuthSafe values;
        ///   <see langword="false" /> to decrypt encrypted PFX AuthSafe values to process their
        ///   contents.
        ///   The default is <see langword="false" />.
        /// </value>
        public bool IgnoreEncryptedAuthSafes
        {
            get => _ignoreEncryptedAuthSafes;
            set
            {
                CheckReadOnly();
                _ignoreEncryptedAuthSafes = value;
            }
        }

        private static void CheckNonNegative(
            int? value,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            // Null turns to 0, 0 is non-negative, so null is non-negative.
            CheckNonNegative(value.GetValueOrDefault(), paramName);
        }

        private static void CheckNonNegative(
            int value,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
#if NET
            ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
#else
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, SR.ArgumentOutOfRange_NeedNonNegNum);
            }
#endif
        }

    }
}
