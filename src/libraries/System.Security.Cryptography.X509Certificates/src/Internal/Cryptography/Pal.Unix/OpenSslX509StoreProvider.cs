// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal class OpenSslX509StoreProvider : IStorePal
    {
        private readonly X509Certificate2Collection _certs;

        internal OpenSslX509StoreProvider(X509Certificate2Collection certs)
        {
            _certs = certs;
        }

        public void Dispose()
        {
        }

        public void FindAndCopyTo(X509FindType findType, object findValue, bool validOnly, X509Certificate2Collection collection)
        {
        }

        public byte[] Export(X509ContentType contentType, string password)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(X509Certificate2Collection collection)
        {
            Debug.Assert(collection != null);

            collection.AddRange(_certs);
        }

        public void Add(ICertificatePal cert)
        {
            throw new NotImplementedException();
        }

        public void Remove(ICertificatePal cert)
        {
            throw new NotImplementedException();
        }
    }
}
