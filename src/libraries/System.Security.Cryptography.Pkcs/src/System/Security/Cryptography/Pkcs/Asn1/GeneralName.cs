// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    internal struct GeneralName
    {
        [ExpectedTag(0, ExplicitTag = true)]
        internal OtherName? OtherName;

        [ExpectedTag(1, ExplicitTag = true)]
        [IA5String]
        internal string Rfc822Name;

        [ExpectedTag(2, ExplicitTag = true)]
        [IA5String]
        internal string DnsName;

        [ExpectedTag(3, ExplicitTag = true)]
        [AnyValue]
        internal ReadOnlyMemory<byte>? X400Address;

        [ExpectedTag(4, ExplicitTag = true)]
        [AnyValue]
        internal ReadOnlyMemory<byte>? DirectoryName;

        [ExpectedTag(5, ExplicitTag = true)]
        internal EdiPartyName? EdiPartyName;

        [ExpectedTag(6, ExplicitTag = true)]
        [IA5String]
        internal string Uri;

        [ExpectedTag(7, ExplicitTag = true)]
        [OctetString]
        internal ReadOnlyMemory<byte>? IPAddress;

        [ExpectedTag(8, ExplicitTag = true)]
        [ObjectIdentifier]
        internal string RegisteredId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct OtherName
    {
        internal string TypeId;

        [ExpectedTag(0, ExplicitTag = true)]
        [AnyValue]
        internal ReadOnlyMemory<byte> Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EdiPartyName
    {
        [OptionalValue]
        internal DirectoryString? NameAssigner;

        internal DirectoryString PartyName;
    }

    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    internal struct DirectoryString
    {
        [ExpectedTag(TagClass.Universal, (int)UniversalTagNumber.TeletexString)]
        internal ReadOnlyMemory<byte>? TeletexString;

        [PrintableString]
        internal string PrintableString;

        [ExpectedTag(TagClass.Universal, (int)UniversalTagNumber.UniversalString)]
        internal ReadOnlyMemory<byte>? UniversalString;

        [UTF8String]
        internal string Utf8String;

        [BMPString]
        internal string BMPString;
    }
}
