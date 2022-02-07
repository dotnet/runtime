// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509ChainPolicy
    {
        private X509RevocationMode _revocationMode;
        private X509RevocationFlag _revocationFlag;
        private X509VerificationFlags _verificationFlags;
        private X509ChainTrustMode _trustMode;
        internal OidCollection? _applicationPolicy;
        internal OidCollection? _certificatePolicy;
        internal X509Certificate2Collection? _extraStore;
        internal X509Certificate2Collection? _customTrustStore;

        public X509ChainPolicy()
        {
            Reset();
        }

        /// <summary>
        ///   Gets or sets a value that indicates whether the chain engine can use the
        ///   Authority Information Access (AIA) extension to locate unknown issuer certificates.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> if using the AIA extension is disabled;
        ///   otherwise, <see langword="false" />.
        ///   The default is <see langword="false" />.
        /// </value>
        public bool DisableCertificateDownloads { get; set; }

        public OidCollection ApplicationPolicy => _applicationPolicy ??= new OidCollection();

        public OidCollection CertificatePolicy => _certificatePolicy ??= new OidCollection();

        public X509Certificate2Collection ExtraStore => _extraStore ??= new X509Certificate2Collection();

        public X509Certificate2Collection CustomTrustStore => _customTrustStore ??= new X509Certificate2Collection();

        public X509RevocationMode RevocationMode
        {
            get
            {
                return _revocationMode;
            }
            set
            {
                if (value < X509RevocationMode.NoCheck || value > X509RevocationMode.Offline)
                    throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, nameof(value)));
                _revocationMode = value;
            }
        }

        public X509RevocationFlag RevocationFlag
        {
            get
            {
                return _revocationFlag;
            }
            set
            {
                if (value < X509RevocationFlag.EndCertificateOnly || value > X509RevocationFlag.ExcludeRoot)
                    throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, nameof(value)));
                _revocationFlag = value;
            }
        }

        public X509VerificationFlags VerificationFlags
        {
            get
            {
                return _verificationFlags;
            }
            set
            {
                if (value < X509VerificationFlags.NoFlag || value > X509VerificationFlags.AllFlags)
                    throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, nameof(value)));
                _verificationFlags = value;
            }
        }

        public X509ChainTrustMode TrustMode
        {
            get
            {
                return _trustMode;
            }
            set
            {
                if (value < X509ChainTrustMode.System || value > X509ChainTrustMode.CustomRootTrust)
                    throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, nameof(value)));
                _trustMode = value;
            }
        }

        public DateTime VerificationTime { get; set; }

        public TimeSpan UrlRetrievalTimeout { get; set; }

        public void Reset()
        {
            _applicationPolicy = null;
            _certificatePolicy = null;
            _extraStore = null;
            _customTrustStore = null;
            DisableCertificateDownloads = false;
            _revocationMode = X509RevocationMode.Online;
            _revocationFlag = X509RevocationFlag.ExcludeRoot;
            _verificationFlags = X509VerificationFlags.NoFlag;
            _trustMode = X509ChainTrustMode.System;
            VerificationTime = DateTime.Now;
            UrlRetrievalTimeout = TimeSpan.Zero; // default timeout
        }
    }
}
