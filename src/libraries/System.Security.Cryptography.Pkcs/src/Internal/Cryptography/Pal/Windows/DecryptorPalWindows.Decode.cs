// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Win32.SafeHandles;

using static Interop.Crypt32;

namespace Internal.Cryptography.Pal.Windows
{
    internal sealed partial class DecryptorPalWindows : DecryptorPal
    {
        internal static DecryptorPalWindows Decode(
            ReadOnlySpan<byte> encodedMessage,
            out int version,
            out ContentInfo contentInfo,
            out AlgorithmIdentifier contentEncryptionAlgorithm,
            out X509Certificate2Collection originatorCerts,
            out CryptographicAttributeObjectCollection unprotectedAttributes
            )
        {
            SafeCryptMsgHandle? hCryptMsg = null;
            try
            {
                hCryptMsg = Interop.Crypt32.CryptMsgOpenToDecode(MsgEncodingType.All, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (hCryptMsg == null || hCryptMsg.IsInvalid)
                    throw Marshal.GetLastWin32Error().ToCryptographicException();

                if (!Interop.Crypt32.CryptMsgUpdate(
                    hCryptMsg,
                    ref MemoryMarshal.GetReference(encodedMessage),
                    encodedMessage.Length,
                    fFinal: true))
                {
                    throw Marshal.GetLastWin32Error().ToCryptographicException();
                }

                CryptMsgType cryptMsgType = hCryptMsg.GetMessageType();
                if (cryptMsgType != CryptMsgType.CMSG_ENVELOPED)
                    throw ErrorCode.CRYPT_E_INVALID_MSG_TYPE.ToCryptographicException();

                version = hCryptMsg.GetVersion();

                contentInfo = hCryptMsg.GetContentInfo();

                AlgorithmIdentifierAsn contentEncryptionAlgorithmAsn;
                using (SafeHandle sh = hCryptMsg.GetMsgParamAsMemory(CryptMsgParamType.CMSG_ENVELOPE_ALGORITHM_PARAM))
                {
                    unsafe
                    {
                        CRYPT_ALGORITHM_IDENTIFIER* pCryptAlgorithmIdentifier = (CRYPT_ALGORITHM_IDENTIFIER*)(sh.DangerousGetHandle());
                        contentEncryptionAlgorithm = (*pCryptAlgorithmIdentifier).ToAlgorithmIdentifier();
                        contentEncryptionAlgorithmAsn.Algorithm = contentEncryptionAlgorithm.Oid.Value!;
                        contentEncryptionAlgorithmAsn.Parameters = (*pCryptAlgorithmIdentifier).Parameters.ToByteArray();
                    }
                }

                originatorCerts = hCryptMsg.GetOriginatorCerts();
                unprotectedAttributes = hCryptMsg.GetUnprotectedAttributes();

                RecipientInfoCollection recipientInfos = CreateRecipientInfos(hCryptMsg);
                return new DecryptorPalWindows(hCryptMsg, recipientInfos, contentEncryptionAlgorithmAsn);
            }
            catch
            {
                hCryptMsg?.Dispose();
                throw;
            }
        }
    }
}
