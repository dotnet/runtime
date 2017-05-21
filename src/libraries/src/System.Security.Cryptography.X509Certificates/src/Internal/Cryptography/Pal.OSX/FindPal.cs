// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class FindPal
    {
        internal static IFindPal OpenPal(X509Certificate2Collection findFrom, X509Certificate2Collection copyTo, bool validOnly)
        {
            return new AppleCertificateFinder(findFrom, copyTo, validOnly);
        }

        private sealed class AppleCertificateFinder : ManagedCertificateFinder
        {
            public AppleCertificateFinder(X509Certificate2Collection findFrom, X509Certificate2Collection copyTo, bool validOnly)
                : base(findFrom, copyTo, validOnly)
            {
            }

            protected override string DerStringToManagedString(byte[] anyString)
            {
                DerSequenceReader reader = DerSequenceReader.CreateForPayload(anyString);

                var tag = (DerSequenceReader.DerTag)reader.PeekTag();
                string value = null;

                switch (tag)
                {
                    case DerSequenceReader.DerTag.BMPString:
                        value = reader.ReadBMPString();
                        break;
                    case DerSequenceReader.DerTag.IA5String:
                        value = reader.ReadIA5String();
                        break;
                    case DerSequenceReader.DerTag.PrintableString:
                        value = reader.ReadPrintableString();
                        break;
                    case DerSequenceReader.DerTag.UTF8String:
                        value = reader.ReadUtf8String();
                        break;

                    // Ignore anything we don't know how to read.
                }

                return value;
            }

            protected override byte[] GetSubjectPublicKeyInfo(X509Certificate2 cert)
            {
                AppleCertificatePal pal = (AppleCertificatePal)cert.Pal;
                return pal.SubjectPublicKeyInfo;
            }

            protected override X509Certificate2 CloneCertificate(X509Certificate2 cert)
            {
                var clone = new X509Certificate2(cert.Handle);
                GC.KeepAlive(cert); // ensure cert's safe handle isn't finalized while raw handle is in use
                return clone;
            }
        }
    }
}
