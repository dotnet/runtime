// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates
{
    internal static class LegacyBasicConstraintsDecoder
    {
        internal static bool IsSupported => false;

        internal static void DecodeX509BasicConstraintsExtension(
            byte[] encoded,
            out bool certificateAuthority,
            out bool hasPathLengthConstraint,
            out int pathLengthConstraint)
        {
            // No RFC nor ITU document describes the layout of the 2.5.29.10 structure,
            // and OpenSSL doesn't have a decoder for it, either.
            //
            // Since it was never published as a standard (2.5.29.19 replaced it before publication)
            // there shouldn't be too many people upset that we can't decode it for them on Unix.
            throw new PlatformNotSupportedException(SR.NotSupported_LegacyBasicConstraints);
        }
    }
}
