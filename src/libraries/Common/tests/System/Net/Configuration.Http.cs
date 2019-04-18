// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace System.Net.Test.Common
{
    public static partial class Configuration
    {
        public static partial class Http
        {
            private static readonly string DefaultHttp2AzureServer = "corefx-net-http2.azurewebsites.net";

            public static string Host => GetValue("COREFX_HTTPHOST", DefaultAzureServer);

            public static string SecureHost => GetValue("COREFX_SECUREHTTPHOST", DefaultAzureServer);

            public static string Http2Host => GetValue("COREFX_HTTP2HOST", DefaultHttp2AzureServer);

            // This server doesn't use HTTP/2 server push (push promise) feature. Some HttpClient implementations
            // don't support servers that use push right now.
            public static string Http2NoPushHost => GetValue("COREFX_HTTP2NOPUSHHOST", "www.microsoft.com");

            // Domain server environment.
            public static string DomainJoinedHttpHost => GetValue("COREFX_DOMAINJOINED_HTTPHOST");
            public static string DomainJoinedProxyHost => GetValue("COREFX_DOMAINJOINED_PROXYHOST");
            public static string DomainJoinedProxyPort => GetValue("COREFX_DOMAINJOINED_PROXYPORT");

            // Standalone server environment.
            public static string WindowsServerHttpHost => GetValue("COREFX_WINDOWSSERVER_HTTPHOST");

            public static string SSLv2RemoteServer => GetValue("COREFX_HTTPHOST_SSL2", "https://www.ssllabs.com:10200/");
            public static string SSLv3RemoteServer => GetValue("COREFX_HTTPHOST_SSL3", "https://www.ssllabs.com:10300/");
            public static string TLSv10RemoteServer => GetValue("COREFX_HTTPHOST_TLS10", "https://www.ssllabs.com:10301/");
            public static string TLSv11RemoteServer => GetValue("COREFX_HTTPHOST_TLS11", "https://www.ssllabs.com:10302/");
            public static string TLSv12RemoteServer => GetValue("COREFX_HTTPHOST_TLS12", "https://www.ssllabs.com:10303/");

            public static string ExpiredCertRemoteServer => GetValue("COREFX_HTTPHOST_EXPIREDCERT", "https://expired.badssl.com/");
            public static string WrongHostNameCertRemoteServer => GetValue("COREFX_HTTPHOST_WRONGHOSTNAME", "https://wrong.host.badssl.com/");
            public static string SelfSignedCertRemoteServer => GetValue("COREFX_HTTPHOST_SELFSIGNEDCERT", "https://self-signed.badssl.com/");
            public static string RevokedCertRemoteServer => GetValue("COREFX_HTTPHOST_REVOKEDCERT", "https://revoked.badssl.com/");

            public static string EchoClientCertificateRemoteServer => GetValue("COREFX_HTTPHOST_ECHOCLIENTCERT", "https://corefx-net-tls.azurewebsites.net/EchoClientCertificate.ashx");
            public static string Http2ForceUnencryptedLoopback => GetValue("COREFX_HTTP2_FORCEUNENCRYPTEDLOOPBACK");

            private const string HttpScheme = "http";
            private const string HttpsScheme = "https";

            private const string EchoHandler = "Echo.ashx";
            private const string EmptyContentHandler = "EmptyContent.ashx";
            private const string RedirectHandler = "Redirect.ashx";
            private const string VerifyUploadHandler = "VerifyUpload.ashx";
            private const string DeflateHandler = "Deflate.ashx";
            private const string GZipHandler = "GZip.ashx";

            public static readonly Uri RemoteEchoServer = new Uri("http://" + Host + "/" + EchoHandler);
            public static readonly Uri SecureRemoteEchoServer = new Uri("https://" + SecureHost + "/" + EchoHandler);
            public static readonly Uri Http2RemoteEchoServer = new Uri("https://" + Http2Host + "/" + EchoHandler);
            public static readonly Uri[] EchoServerList = new Uri[] { RemoteEchoServer, SecureRemoteEchoServer, Http2RemoteEchoServer };

            public static readonly Uri RemoteVerifyUploadServer = new Uri("http://" + Host + "/" + VerifyUploadHandler);
            public static readonly Uri SecureRemoteVerifyUploadServer = new Uri("https://" + SecureHost + "/" + VerifyUploadHandler);
            public static readonly Uri Http2RemoteVerifyUploadServer = new Uri("https://" + Http2Host + "/" + VerifyUploadHandler);
            public static readonly Uri[] VerifyUploadServerList = new Uri[] { RemoteVerifyUploadServer, SecureRemoteVerifyUploadServer, Http2RemoteVerifyUploadServer };

            public static readonly Uri RemoteEmptyContentServer = new Uri("http://" + Host + "/" + EmptyContentHandler);
            public static readonly Uri RemoteDeflateServer = new Uri("http://" + Host + "/" + DeflateHandler);
            public static readonly Uri RemoteGZipServer = new Uri("http://" + Host + "/" + GZipHandler);
            public static readonly Uri Http2RemoteDeflateServer = new Uri("https://" + Http2Host + "/" + DeflateHandler);
            public static readonly Uri Http2RemoteGZipServer = new Uri("https://" + Http2Host + "/" + GZipHandler);

            public static readonly object[][] EchoServers = EchoServerList.Select(x => new object[] { x }).ToArray();
            public static readonly object[][] VerifyUploadServers = { new object[] { RemoteVerifyUploadServer }, new object[] { SecureRemoteVerifyUploadServer }, new object[] { Http2RemoteVerifyUploadServer } };
            public static readonly object[][] CompressedServers = { new object[] { RemoteDeflateServer }, new object[] { RemoteGZipServer }, new object[] { Http2RemoteDeflateServer }, new object[] { Http2RemoteGZipServer } };

            public static readonly object[][] Http2Servers = { new object[] { new Uri("https://" + Http2Host) } };
            public static readonly object[][] Http2NoPushServers = { new object[] { new Uri("https://" + Http2NoPushHost) } };

            public static Uri NegotiateAuthUriForDefaultCreds(bool secure)
            {
                return new Uri(
                    string.Format(
                        "{0}://{1}/{2}?auth=negotiate",
                        secure ? HttpsScheme : HttpScheme,
                        Host,
                        EchoHandler));
            }

            public static Uri BasicAuthUriForCreds(bool secure, string userName, string password)
            {
                return new Uri(
                    string.Format(
                        "{0}://{1}/{2}?auth=basic&user={3}&password={4}",
                        secure ? HttpsScheme : HttpScheme,
                        Host,
                        EchoHandler,
                        userName,
                        password));
            }

            public static Uri RedirectUriForDestinationUri(bool secure, int statusCode, Uri destinationUri, int hops, bool relative = false)
            {
                string uriString;
                string destination = Uri.EscapeDataString(relative ? destinationUri.PathAndQuery : destinationUri.AbsoluteUri);

                if (hops > 1)
                {
                    uriString = string.Format("{0}://{1}/{2}?statuscode={3}&uri={4}&hops={5}",
                        secure ? HttpsScheme : HttpScheme,
                        Host,
                        RedirectHandler,
                        statusCode,
                        destination,
                        hops);
                }
                else
                {
                    uriString = string.Format("{0}://{1}/{2}?statuscode={3}&uri={4}",
                        secure ? HttpsScheme : HttpScheme,
                        Host,
                        RedirectHandler,
                        statusCode,
                        destination);
                }

                return new Uri(uriString);
            }

            public static Uri RedirectUriForCreds(bool secure, int statusCode, string userName, string password)
            {
                Uri destinationUri = BasicAuthUriForCreds(secure, userName, password);
                string destination = Uri.EscapeDataString(destinationUri.AbsoluteUri);

                return new Uri(string.Format("{0}://{1}/{2}?statuscode={3}&uri={4}",
                    secure ? HttpsScheme : HttpScheme,
                    Host,
                    RedirectHandler,
                    statusCode,
                    destination));
            }
        }
    }
}
