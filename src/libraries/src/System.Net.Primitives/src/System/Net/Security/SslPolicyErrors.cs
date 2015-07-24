// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
