// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509ChainPolicy
    {
        public X509ChainPolicy()
        {
            Reset();
        }

        public OidCollection ApplicationPolicy { get; private set; }

        public OidCollection CertificatePolicy { get; private set; }

        public X509RevocationMode RevocationMode
        {
            get
            {
                return _revocationMode;
            }
            set
            {
                if (value < X509RevocationMode.NoCheck || value > X509RevocationMode.Offline)
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Arg_EnumIllegalVal, nameof(value)));
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
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Arg_EnumIllegalVal, nameof(value)));
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
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Arg_EnumIllegalVal, nameof(value)));
                _verificationFlags = value;
            }
        }

        public DateTime VerificationTime { get; set; }

        public TimeSpan UrlRetrievalTimeout { get; set; }

        public X509Certificate2Collection ExtraStore { get; private set; }

        public void Reset()
        {
            ApplicationPolicy = new OidCollection();
            CertificatePolicy = new OidCollection();
            _revocationMode = X509RevocationMode.Online;
            _revocationFlag = X509RevocationFlag.ExcludeRoot;
            _verificationFlags = X509VerificationFlags.NoFlag;
            VerificationTime = DateTime.Now;
            UrlRetrievalTimeout = new TimeSpan(0, 0, 0); // default timeout
            ExtraStore = new X509Certificate2Collection();
        }

        private X509RevocationMode _revocationMode;
        private X509RevocationFlag _revocationFlag;
        private X509VerificationFlags _verificationFlags;
    }
}

