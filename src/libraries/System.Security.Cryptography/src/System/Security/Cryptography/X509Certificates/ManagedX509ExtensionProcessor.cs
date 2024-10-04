// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    internal class ManagedX509ExtensionProcessor
    {
        public virtual byte[] EncodeX509BasicConstraints2Extension(
            bool certificateAuthority,
            bool hasPathLengthConstraint,
            int pathLengthConstraint)
        {
            BasicConstraintsAsn constraints = default;

            constraints.CA = certificateAuthority;
            if (hasPathLengthConstraint)
                constraints.PathLengthConstraint = pathLengthConstraint;

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            constraints.Encode(writer);
            return writer.Encode();
        }

        public virtual bool SupportsLegacyBasicConstraintsExtension => false;

        public virtual void DecodeX509BasicConstraintsExtension(
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

        public virtual void DecodeX509BasicConstraints2Extension(
                byte[] encoded,
                out bool certificateAuthority,
                out bool hasPathLengthConstraint,
                out int pathLengthConstraint)
        {
            BasicConstraintsAsn constraints = BasicConstraintsAsn.Decode(encoded, AsnEncodingRules.BER);
            certificateAuthority = constraints.CA;
            hasPathLengthConstraint = constraints.PathLengthConstraint.HasValue;
            pathLengthConstraint = constraints.PathLengthConstraint.GetValueOrDefault();
        }
    }
}
