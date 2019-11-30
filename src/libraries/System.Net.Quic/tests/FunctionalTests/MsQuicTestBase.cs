using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Quic.Tests
{
    public class MsQuicTestBase
    {
        public MsQuicTestBase()
        {
        }

        public SslServerAuthenticationOptions GetSslServerAuthenticationOptions()
        {
            // TODO figure out how to supply a fake cert here.
            using (X509Store store = new X509Store(StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection cers = store.Certificates.Find(X509FindType.FindBySubjectName, "localhost", false);
                return new SslServerAuthenticationOptions()
                {
                    ServerCertificate = cers[0],
                    ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") }
                };
            }
        }

        public SslClientAuthenticationOptions GetSslClientAuthenticationOptions()
        {
            return new SslClientAuthenticationOptions()
            {
                ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") }
            };
        }

        public QuicConnection CreateQuicConnection(IPEndPoint endpoint)
        {
            return new QuicConnection(QuicImplementationProviders.MsQuic, endpoint, GetSslClientAuthenticationOptions());
        }

        public QuicListener CreateQuicListener(IPEndPoint endpoint)
        {
            return new QuicListener(QuicImplementationProviders.MsQuic, endpoint, GetSslServerAuthenticationOptions());
        }
    }
}
