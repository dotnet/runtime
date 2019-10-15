// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class FindPal
    {
        internal static IFindPal OpenPal(X509Certificate2Collection findFrom, X509Certificate2Collection copyTo, bool validOnly)
        {
            return new OpenSslCertificateFinder(findFrom, copyTo, validOnly);
        }
    }
}
