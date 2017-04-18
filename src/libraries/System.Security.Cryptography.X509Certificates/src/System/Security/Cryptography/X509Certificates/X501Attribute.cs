// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.X509Certificates
{
    internal class X501Attribute : AsnEncodedData
    {
        internal X501Attribute(string oid, byte[] rawData)
            : base(oid, rawData)
        {
        }
    }
}
