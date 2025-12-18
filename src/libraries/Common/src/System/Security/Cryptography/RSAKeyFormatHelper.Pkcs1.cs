// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    internal static partial class RSAKeyFormatHelper
    {
        internal delegate TRet RSAParametersCallback<TRet>(RSAParameters parameters);

        internal static unsafe TRet FromPkcs1PrivateKey<TRet>(
            ReadOnlySpan<byte> keyData,
            RSAParametersCallback<TRet> parametersReader,
            bool pinAndClearParameters = true)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(keyData))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, keyData.Length))
                {
                    return FromPkcs1PrivateKey(manager.Memory, parametersReader, pinAndClearParameters);
                }
            }
        }

        internal static TRet FromPkcs1PrivateKey<TRet>(
            ReadOnlyMemory<byte> keyData,
            RSAParametersCallback<TRet> parametersReader,
            bool pinAndClearParameters = true)
        {
            RSAPrivateKeyAsn key = RSAPrivateKeyAsn.Decode(keyData, AsnEncodingRules.BER);

            const int MaxSupportedVersion = 0;

            if (key.Version > MaxSupportedVersion)
            {
                throw new CryptographicException(
                    SR.Format(
                        SR.Cryptography_RSAPrivateKey_VersionTooNew,
                        key.Version,
                        MaxSupportedVersion));
            }

            // The modulus size determines the encoded output size of the CRT parameters.
            byte[] n = key.Modulus.ToUnsignedIntegerBytes();
            int halfModulusLength = (n.Length + 1) / 2;

            RSAParameters parameters = new RSAParameters
            {
                Modulus = n,
                Exponent = key.PublicExponent.ToUnsignedIntegerBytes(),

                D = new byte[n.Length],
                P = new byte[halfModulusLength],
                Q = new byte[halfModulusLength],
                DP = new byte[halfModulusLength],
                DQ = new byte[halfModulusLength],
                InverseQ = new byte[halfModulusLength],
            };

            if (pinAndClearParameters)
            {
                using (PinAndClear.Track(parameters.D))
                using (PinAndClear.Track(parameters.P))
                using (PinAndClear.Track(parameters.Q))
                using (PinAndClear.Track(parameters.DP))
                using (PinAndClear.Track(parameters.DQ))
                using (PinAndClear.Track(parameters.InverseQ))
                {
                    return ExtractParametersWithCallback(parametersReader, ref key, ref parameters);
                }
            }
            else
            {
                return ExtractParametersWithCallback(parametersReader, ref key, ref parameters);
            }

            static TRet ExtractParametersWithCallback(RSAParametersCallback<TRet> parametersReader, ref RSAPrivateKeyAsn key, ref RSAParameters parameters)
            {
                key.PrivateExponent.ToUnsignedIntegerBytes(parameters.D);
                key.Prime1.ToUnsignedIntegerBytes(parameters.P);
                key.Prime2.ToUnsignedIntegerBytes(parameters.Q);
                key.Exponent1.ToUnsignedIntegerBytes(parameters.DP);
                key.Exponent2.ToUnsignedIntegerBytes(parameters.DQ);
                key.Coefficient.ToUnsignedIntegerBytes(parameters.InverseQ);

                return parametersReader(parameters);
            }
        }

        internal static unsafe TRet FromPkcs1PublicKey<TRet>(
            ReadOnlySpan<byte> keyData,
            RSAParametersCallback<TRet> parametersReader)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(keyData))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, keyData.Length))
                {
                    return FromPkcs1PublicKey(manager.Memory, parametersReader);
                }
            }
        }

        internal static TRet FromPkcs1PublicKey<TRet>(ReadOnlyMemory<byte> keyData, RSAParametersCallback<TRet> parametersReader)
        {
            RSAPublicKeyAsn key = RSAPublicKeyAsn.Decode(keyData, AsnEncodingRules.BER);

            RSAParameters parameters = new RSAParameters
            {
                Modulus = key.Modulus.ToUnsignedIntegerBytes(),
                Exponent = key.PublicExponent.ToUnsignedIntegerBytes(),
            };

            return parametersReader(parameters);
        }

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
