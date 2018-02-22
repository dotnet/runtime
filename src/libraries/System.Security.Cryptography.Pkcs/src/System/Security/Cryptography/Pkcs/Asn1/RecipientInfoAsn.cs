// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-6.2
    //
    // RecipientInfo ::= CHOICE {
    //   ktri KeyTransRecipientInfo,
    //   kari[1] KeyAgreeRecipientInfo,
    //   kekri[2] KEKRecipientInfo,
    //   pwri[3] PasswordRecipientinfo,
    //   ori[4] OtherRecipientInfo }
    [StructLayout(LayoutKind.Sequential)]
    [Choice]
    internal struct RecipientInfoAsn
    {
        internal KeyTransRecipientInfoAsn Ktri;

        [ExpectedTag(1)]
        internal KeyAgreeRecipientInfoAsn Kari;

        // By not declaring the rest of the types here we get an ASN deserialization
        // error for unsupported recipient types
    }
}
