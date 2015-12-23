// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal static class X500NameEncoder
    {
        private static readonly char[] s_quoteNeedingChars =
        {
            ',',
            '+',
            '=',
            '\"',
            '\n',
            // \r is NOT in this list, because it isn't in Windows.
            '<',
            '>',
            '#',
            ';',
        };

        internal static string X500DistinguishedNameDecode(
            byte[] encodedName,
            bool printOid,
            X500DistinguishedNameFlags flags,
            bool addTrailingDelimieter=false)
        {
            bool reverse = (flags & X500DistinguishedNameFlags.Reversed) == X500DistinguishedNameFlags.Reversed;
            bool quoteIfNeeded = (flags & X500DistinguishedNameFlags.DoNotUseQuotes) != X500DistinguishedNameFlags.DoNotUseQuotes;
            string dnSeparator;

            if ((flags & X500DistinguishedNameFlags.UseSemicolons) == X500DistinguishedNameFlags.UseSemicolons)
            {
                dnSeparator = "; ";
            }
            else if ((flags & X500DistinguishedNameFlags.UseNewLines) == X500DistinguishedNameFlags.UseNewLines)
            {
                dnSeparator = Environment.NewLine;
            }
            else
            {
                // This is matching Windows (native) behavior, UseCommas does not need to be asserted,
                // it is just what happens if neither UseSemicolons nor UseNewLines is specified.
                dnSeparator = ", ";
            }

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

                if (addTrailingDelimieter)
                {
                    decodedName.Append(dnSeparator);
                }

                return decodedName.ToString();
            }
        }
        
        private static bool NeedsQuoting(string rdnValue)
        {
            if (string.IsNullOrEmpty(rdnValue))
            {
                return true;
            }

            if (IsQuotableWhitespace(rdnValue[0]) ||
                IsQuotableWhitespace(rdnValue[rdnValue.Length - 1]))
            {
                return true;
            }

            int index = rdnValue.IndexOfAny(s_quoteNeedingChars);

            return index != -1;
        }

        private static bool IsQuotableWhitespace(char c)
        {
            // There's a whole lot of Unicode whitespace that isn't covered here; but this
            // matches what Windows deems quote-worthy.
            //
            // 0x20: Space
            // 0x09: Character Tabulation (tab)
            // 0x0A: Line Feed
            // 0x0B: Line Tabulation (vertical tab)
            // 0x0C: Form Feed
            // 0x0D: Carriage Return
            return (c == ' ' || (c >= 0x09 && c <= 0x0D));
        }

        private static void AppendOid(StringBuilder decodedName, string oidValue)
        {
            Oid oid = new Oid(oidValue);

            if (StringComparer.Ordinal.Equals(oid.FriendlyName, oidValue) ||
                string.IsNullOrEmpty(oid.FriendlyName))
            {
                decodedName.Append("OID.");
                decodedName.Append(oid.Value);
            }
            else
            {
                decodedName.Append(oid.FriendlyName);
            }

            decodedName.Append('=');
        }
    }
}
