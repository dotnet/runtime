// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // https://tools.ietf.org/html/rfc3279#section-2.3.2
    //
    // Dss-Parms  ::=  SEQUENCE  {
    //   p             INTEGER,
    //   q             INTEGER,
    //   g             INTEGER  }
    [StructLayout(LayoutKind.Sequential)]
    internal struct DssParms
    {
        public BigInteger P;
        public BigInteger Q;
        public BigInteger G;
    }
}
