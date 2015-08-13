// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal interface IStorePal : IDisposable
    {
        void FindAndCopyTo(X509FindType findType, object findValue, bool validOnly, X509Certificate2Collection collection);
        byte[] Export(X509ContentType contentType, String password);
        void CopyTo(X509Certificate2Collection collection);
        void Add(ICertificatePal cert);
        void Remove(ICertificatePal cert);
    }
}
