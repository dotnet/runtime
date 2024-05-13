// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    public partial class X509Certificate
    {
        private static readonly Pkcs12LoaderLimits s_legacyLimits = MakeLegacyLimits();

        internal static Pkcs12LoaderLimits GetPkcs12Limits(bool fromFile, SafePasswordHandle safePasswordHandle)
        {
            if (fromFile || safePasswordHandle.PasswordProvided)
            {
                return Pkcs12LoaderLimits.DangerousNoLimits;
            }

            return s_legacyLimits;
        }

        private static Pkcs12LoaderLimits MakeLegacyLimits()
        {
            Pkcs12LoaderLimits limits = new Pkcs12LoaderLimits
            {
                MacIterationLimit = 600_000,
                IndividualKdfIterationLimit = 600_000,
                MaxCertificates = null,
                MaxKeys = null,
                PreserveCertificateAlias = true,
                PreserveKeyName = true,
                PreserveStorageProvider = true,
                PreserveUnknownAttributes = true,
            };

            long totalKdfLimit = LocalAppContextSwitches.Pkcs12UnspecifiedPasswordIterationLimit;

            if (totalKdfLimit == -1)
            {
                limits.TotalKdfIterationLimit = null;
            }
            else if (totalKdfLimit < 0)
            {
                limits.TotalKdfIterationLimit = LocalAppContextSwitches.DefaultPkcs12UnspecifiedPasswordIterationLimit;
            }
            else
            {
                limits.TotalKdfIterationLimit = (int)long.Min(int.MaxValue, totalKdfLimit);
            }

            limits.MakeReadOnly();
            return limits;
        }
    }
}
