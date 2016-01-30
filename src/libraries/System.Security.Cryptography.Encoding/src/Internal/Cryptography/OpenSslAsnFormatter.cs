// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            using (SafeAsn1ObjectHandle asnOid = Interop.Crypto.ObjTxt2Obj(oid.Value))
            using (SafeAsn1OctetStringHandle octetString = Interop.Crypto.Asn1OctetStringNew())
            {
                if (asnOid.IsInvalid || octetString.IsInvalid)
                {
                    return null;
                }

                if (!Interop.Crypto.Asn1OctetStringSet(octetString, rawData, rawData.Length))
                {
                    return null;
                }

                using (SafeBioHandle bio = Interop.Crypto.CreateMemoryBio())
                using (SafeX509ExtensionHandle x509Ext = Interop.Crypto.X509ExtensionCreateByObj(asnOid, false, octetString))
                {
                    if (bio.IsInvalid || x509Ext.IsInvalid)
                    {
                        return null;
                    }

                    if (!Interop.Crypto.X509V3ExtPrint(bio, x509Ext))
                    {
                        return null;
                    }

                    int printLen = Interop.Crypto.GetMemoryBioSize(bio);

                    // Account for the null terminator that it'll want to write.
                    var buf = new byte[printLen + 1];
                    int read = Interop.Crypto.BioGets(bio, buf, buf.Length);

                    if (read < 0)
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    return Encoding.UTF8.GetString(buf, 0, read);
                }
            }
        }
    }
}
