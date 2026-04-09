// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    internal static partial class FindPal
    {
        private static partial IFindPal OpenPal(
            X509Certificate2Collection findFrom,
            X509Certificate2Collection copyTo,
            bool validOnly)
        {
            return new OpenSslCertificateFinder(findFrom, copyTo, validOnly);
        }
    }
}
