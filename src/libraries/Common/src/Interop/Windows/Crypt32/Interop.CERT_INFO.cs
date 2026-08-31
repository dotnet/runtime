// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CERT_INFO
        {
            internal int dwVersion;
            internal DATA_BLOB SerialNumber;
            internal CRYPT_ALGORITHM_IDENTIFIER SignatureAlgorithm;
            internal DATA_BLOB Issuer;
            internal FILETIME NotBefore;
            internal FILETIME NotAfter;
            internal DATA_BLOB Subject;
            internal CERT_PUBLIC_KEY_INFO SubjectPublicKeyInfo;
            internal CRYPT_BIT_BLOB IssuerUniqueId;
            internal CRYPT_BIT_BLOB SubjectUniqueId;
            internal int cExtension;
            internal IntPtr rgExtension;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct FILETIME
        {
            internal uint ftTimeLow;
            internal uint ftTimeHigh;

            internal DateTime ToDateTime()
            {
                long fileTime = (((long)ftTimeHigh) << 32) + ftTimeLow;
                return DateTime.FromFileTime(fileTime);
            }

            internal static FILETIME FromDateTime(DateTime dt)
            {
                long fileTime = dt.ToFileTime();

                unchecked
                {
                    return new FILETIME()
                    {
                        ftTimeLow = (uint)fileTime,
                        ftTimeHigh = (uint)(fileTime >> 32),
                    };
                }
            }
        }
    }
}
