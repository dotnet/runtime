// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    internal partial struct TimeAsn
    {
        public TimeAsn(DateTimeOffset dateTimeOffset)
        {
            DateTime utcValue = dateTimeOffset.UtcDateTime;

            // Since the date encoding is effectively a DER rule (ensuring that two encoders
            // produce the same result), no option exists to encode the validity field as a
            // GeneralizedTime when it fits in the UTCTime constraint.
            if (utcValue.Year >= 1950 && utcValue.Year < 2050)
            {
                UtcTime = utcValue;
                GeneralTime = null;
            }
            else
            {
                UtcTime = null;
                GeneralTime = utcValue;
            }
        }

        public DateTimeOffset GetValue() => (UtcTime ?? GeneralTime).GetValueOrDefault();
    }
}
