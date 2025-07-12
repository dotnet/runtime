// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    internal static partial class RSAKeyFormatHelper
    {
        internal static AsnWriter WritePkcs1PublicKey(in RSAParameters rsaParameters)
        {
            if (rsaParameters.Modulus == null || rsaParameters.Exponent == null)
            {
                throw new CryptographicException(SR.Cryptography_InvalidRsaParameters);
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSequence();
            writer.WriteKeyParameterInteger(rsaParameters.Modulus);
            writer.WriteKeyParameterInteger(rsaParameters.Exponent);
            writer.PopSequence();

            return writer;
        }

        internal static AsnWriter WritePkcs1PrivateKey(in RSAParameters rsaParameters)
        {
            if (rsaParameters.Modulus == null || rsaParameters.Exponent == null)
            {
                throw new CryptographicException(SR.Cryptography_InvalidRsaParameters);
            }

            if (rsaParameters.D == null ||
                rsaParameters.P == null ||
                rsaParameters.Q == null ||
                rsaParameters.DP == null ||
                rsaParameters.DQ == null ||
                rsaParameters.InverseQ == null)
            {
                throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            writer.PushSequence();

            // Format version 0
            writer.WriteInteger(0);
            writer.WriteKeyParameterInteger(rsaParameters.Modulus);
            writer.WriteKeyParameterInteger(rsaParameters.Exponent);
            writer.WriteKeyParameterInteger(rsaParameters.D);
            writer.WriteKeyParameterInteger(rsaParameters.P);
            writer.WriteKeyParameterInteger(rsaParameters.Q);
            writer.WriteKeyParameterInteger(rsaParameters.DP);
            writer.WriteKeyParameterInteger(rsaParameters.DQ);
            writer.WriteKeyParameterInteger(rsaParameters.InverseQ);

            writer.PopSequence();
            return writer;
        }
    }
}
