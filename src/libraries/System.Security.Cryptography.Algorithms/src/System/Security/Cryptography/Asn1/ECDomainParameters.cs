// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // https://www.secg.org/sec1-v2.pdf, C.2
    //
    // ECDomainParameters{ECDOMAIN:IOSet} ::= CHOICE {
    //   specified SpecifiedECDomain,
    //   named ECDOMAIN.&id({IOSet}),
    //   implicitCA NULL
    // }
    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ECDomainParameters
    {
        public SpecifiedECDomain? Specified;

        [ObjectIdentifier(PopulateFriendlyName = true)]
        public Oid Named;
    }
}
