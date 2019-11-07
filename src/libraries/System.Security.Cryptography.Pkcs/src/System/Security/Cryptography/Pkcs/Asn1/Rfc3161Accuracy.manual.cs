// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    internal partial struct Rfc3161Accuracy
    {
        // Parameter name (and exception) match the Rfc3161TimestampTokenInfo ctor.
        internal Rfc3161Accuracy(long accuracyInMicroseconds)
        {
            if (accuracyInMicroseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(accuracyInMicroseconds));
            }

            long totalMillis = Math.DivRem(accuracyInMicroseconds, 1000, out long micros);
            long seconds = Math.DivRem(totalMillis, 1000, out long millis);

            if (seconds != 0)
            {
                Seconds = checked((int)seconds);
            }
            else
            {
                Seconds = null;
            }

            if (millis != 0)
            {
                Millis = (int)millis;
            }
            else
            {
                Millis = null;
            }

            if (micros != 0)
            {
                Micros = (int)micros;
            }
            else
            {
                Micros = null;
            }
        }

        internal long TotalMicros =>
            1_000_000L * Seconds.GetValueOrDefault() +
            1000L * Millis.GetValueOrDefault() +
            Micros.GetValueOrDefault();
    }
}
