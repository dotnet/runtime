// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography
{
    internal sealed class OpenSslAsnFormatter : AsnFormatter
    {
        protected override string FormatNative(Oid oid, byte[] rawData, bool multiLine)
        {
            if (oid == null || string.IsNullOrEmpty(oid.Value))
            {
                return EncodeHexString(rawData, true);
            }

            // The established behavior for this method is to return the native answer, if possible,
            // or to return null and let rawData get hex-encoded.  CryptographicException should not
            // be raised.

            using (SafeAsn1ObjectHandle asnOid = Interop.libcrypto.OBJ_txt2obj(oid.Value, true))
            using (SafeAsn1OctetStringHandle octetString = Interop.libcrypto.ASN1_OCTET_STRING_new())
            {
                if (asnOid.IsInvalid || octetString.IsInvalid)
                {
                    return null;
                }

                if (!Interop.libcrypto.ASN1_OCTET_STRING_set(octetString, rawData, rawData.Length))
                {
                    return null;
                }

                using (SafeBioHandle bio = Interop.libcrypto.BIO_new(Interop.libcrypto.BIO_s_mem()))
                using (SafeX509ExtensionHandle x509Ext = Interop.libcrypto.X509_EXTENSION_create_by_OBJ(IntPtr.Zero, asnOid, false, octetString))
                {
                    if (bio.IsInvalid || x509Ext.IsInvalid)
                    {
                        return null;
                    }

                    if (!Interop.libcrypto.X509V3_EXT_print(bio, x509Ext, Interop.libcrypto.X509V3ExtPrintFlags.None, 0))
                    {
                        return null;
                    }

                    int printLen = Interop.libcrypto.GetMemoryBioSize(bio);

                    // Account for the null terminator that it'll want to write.
                    StringBuilder builder = new StringBuilder(printLen + 1);
                    Interop.libcrypto.BIO_gets(bio, builder, builder.Capacity);

                    return builder.ToString();
                }
            }
        }
    }
}