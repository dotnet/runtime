// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal static partial class X500NameEncoder
    {
        private static string X500DistinguishedNameDecode(
            byte[] encodedName,
            bool printOid,
            bool reverse,
            bool quoteIfNeeded,
            string dnSeparator,
            string multiValueSeparator,
            bool addTrailingDelimiter)
        {
            using (SafeX509NameHandle x509Name = Interop.Crypto.DecodeX509Name(encodedName, encodedName.Length))
            {
                if (x509Name.IsInvalid)
                {
                    return "";
                }

                // We need to allocate a StringBuilder to hold the data as we're building it, and there's the usual
                // arbitrary process of choosing a number that's "big enough" to minimize reallocations without wasting
                // too much space in the average case.
                //
                // So, let's look at an example of what our output might be.
                //
                // GitHub.com's SSL cert has a "pretty long" subject (partially due to the unknown OIDs):
                //   businessCategory=Private Organization
                //   1.3.6.1.4.1.311.60.2.1.3=US
                //   1.3.6.1.4.1.311.60.2.1.2=Delaware
                //   serialNumber=5157550
                //   street=548 4th Street
                //   postalCode=94107
                //   C=US
                //   ST=California
                //   L=San Francisco
                //   O=GitHub, Inc.
                //   CN=github.com
                //
                // Which comes out to 228 characters using OpenSSL's default pretty-print
                // (openssl x509 -in github.cer -text -noout)
                // Throw in some "maybe-I-need-to-quote-this" quotes, and a couple of extra/extra-long O/OU values
                // and round that up to the next programmer number, and you get that 512 should avoid reallocations
                // in all but the most dire of cases.
                StringBuilder decodedName = new StringBuilder(512);
                int entryCount = Interop.Crypto.GetX509NameEntryCount(x509Name);
                bool printSpacing = false;

                for (int i = 0; i < entryCount; i++)
                {
                    int loc = reverse ? entryCount - i - 1 : i;

                    using (SafeSharedX509NameEntryHandle nameEntry = Interop.Crypto.GetX509NameEntry(x509Name, loc))
                    {
                        Interop.Crypto.CheckValidOpenSslHandle(nameEntry);

                        string thisOidValue;

                        using (SafeSharedAsn1ObjectHandle oidHandle = Interop.Crypto.GetX509NameEntryOid(nameEntry))
                        {
                            thisOidValue = Interop.Crypto.GetOidValue(oidHandle);
                        }

                        if (printSpacing)
                        {
                            decodedName.Append(dnSeparator);
                        }
                        else
                        {
                            printSpacing = true;
                        }

                        if (printOid)
                        {
                            AppendOid(decodedName, thisOidValue);
                        }

                        string rdnValue;

                        using (SafeSharedAsn1StringHandle valueHandle = Interop.Crypto.GetX509NameEntryData(nameEntry))
                        {
                            rdnValue = Interop.Crypto.Asn1StringToManagedString(valueHandle);
                        }

                        bool quote = quoteIfNeeded && NeedsQuoting(rdnValue);

                        if (quote)
                        {
                            decodedName.Append('"');

                            // If the RDN itself had a quote within it, that quote needs to be escaped
                            // with another quote.
                            rdnValue = rdnValue.Replace("\"", "\"\"");
                        }

                        decodedName.Append(rdnValue);

                        if (quote)
                        {
                            decodedName.Append('"');
                        }
                    }
                }

                if (addTrailingDelimiter)
                {
                    decodedName.Append(dnSeparator);
                }

                return decodedName.ToString();
            }
        }
    }
}
