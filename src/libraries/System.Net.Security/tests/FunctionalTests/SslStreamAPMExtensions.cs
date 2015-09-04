// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// Extensions that add the legacy APM Pattern (Begin/End) for generic Streams
/// </summary>


using System.Threading.Tasks;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public static class SslStreamAPMExtensions
    {
        public static IAsyncResult BeginAuthenticateAsClient(
            this SslStream s,
            string targetHost,
            X509CertificateCollection clientCertificates,
            SslProtocols enabledSslProtocols,
            bool checkCertificateRevocation,
            AsyncCallback asyncCallback,
            Object asyncState)
        {
            return s.AuthenticateAsClientAsync(
                targetHost,
                clientCertificates,
                enabledSslProtocols,
                checkCertificateRevocation).ToApm(asyncCallback, asyncState);
        }

        public static void EndAuthenticateAsClient(this SslStream s, IAsyncResult asyncResult)
        {
            Task t = (Task)asyncResult;
            t.GetAwaiter().GetResult();
        }

        public static IAsyncResult BeginAuthenticateAsServer(
            this SslStream s,
            X509Certificate serverCertificate,
            bool clientCertificateRequired,
            SslProtocols enabledSslProtocols,
            bool checkCertificateRevocation,
            AsyncCallback asyncCallback,
            Object asyncState)
        {
            return s.AuthenticateAsServerAsync(
                serverCertificate,
                clientCertificateRequired,
                enabledSslProtocols,
                checkCertificateRevocation).ToApm(asyncCallback, asyncState);
        }

        public static void EndAuthenticateAsServer(this SslStream s, IAsyncResult asyncResult)
        {
            Task t = (Task)asyncResult;
            t.GetAwaiter().GetResult();
        }
    }
}
