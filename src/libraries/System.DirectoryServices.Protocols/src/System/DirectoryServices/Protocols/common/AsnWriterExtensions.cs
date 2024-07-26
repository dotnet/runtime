// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Text;

namespace System.DirectoryServices.Protocols
{
    internal static class AsnWriterExtensions
    {
        public static void WriteLdapString(this AsnWriter writer, string value, Encoding stringEncoding, bool mandatory = true, Asn1Tag? tag = null)
        {
            // A typical stack allocation threshold would be 256 bytes. A higher threshold has been chosen because an LdapString can be
            // used to serialize server names. A server name is defined by RF1035, which specifies that a label in a domain name should
            // be < 64 characters. If a server name is specified as an FQDN, this will be at least three labels in an AD environment -
            // up to 192 characters. Doubling this to allow for Unicode encoding, then rounding to the nearest power of two yields 512.
            const int StackAllocationThreshold = 512;

            if (!string.IsNullOrEmpty(value))
            {
                int octetStringLength = stringEncoding.GetByteCount(value);
                // Allocate space on the stack. There's a modest codegen advantage to a constant-value stackalloc.
                Span<byte> tmpValue = octetStringLength <= StackAllocationThreshold
                    ? stackalloc byte[StackAllocationThreshold].Slice(0, octetStringLength)
                    : new byte[octetStringLength];

                stringEncoding.GetBytes(value, tmpValue);
                writer.WriteOctetString(tmpValue, tag);
            }
            else if (mandatory)
            {
                writer.WriteOctetString([], tag);
            }
        }
    }
}
