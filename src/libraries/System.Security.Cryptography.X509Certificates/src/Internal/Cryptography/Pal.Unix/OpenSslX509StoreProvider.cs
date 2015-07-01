using System;
using System.Collections.Generic;
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

        public IEnumerable<X509Certificate2> Find(X509FindType findType, object findValue, bool validOnly)
        {
            return Array.Empty<X509Certificate2>();
        }

        public byte[] Export(X509ContentType contentType, string password)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<X509Certificate2> Certificates
        {
            get
            {
                foreach (X509Certificate2 cert in _certs)
                {
                    yield return cert;
                }
            }
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
