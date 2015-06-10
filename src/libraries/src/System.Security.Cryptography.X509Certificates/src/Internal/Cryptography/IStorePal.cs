// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal interface IStorePal : IDisposable
    {
        IEnumerable<X509Certificate2> Find(X509FindType findType, Object findValue, bool validOnly);
        byte[] Export(X509ContentType contentType, String password);
        IEnumerable<X509Certificate2> Certificates { get; }
        void Add(ICertificatePal cert);
        void Remove(ICertificatePal cert);
    }
}
