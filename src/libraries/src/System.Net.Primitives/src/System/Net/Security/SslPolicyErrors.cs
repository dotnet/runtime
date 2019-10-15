// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Security
{
    [Flags]
    public enum SslPolicyErrors
    {
        None = 0x0,
        RemoteCertificateNotAvailable = 0x1,
        RemoteCertificateNameMismatch = 0x2,
        RemoteCertificateChainErrors = 0x4
    }
}
