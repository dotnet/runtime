// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace System.Net.Test.Common
{
    public static partial class Configuration
    {
        public static partial class Http
        {
            private static readonly string DefaultHttp2AzureServer = "corefx-net-http2.azurewebsites.net";

            // for the local server hosted in XHarness we are passing also port as part of the environment variables, because it's bound to random port number
            public static string Host => GetValue("DOTNET_TEST_HTTPHOST", DefaultAzureServer);
            public static string SecureHost => GetValue("DOTNET_TEST_SECUREHTTPHOST", DefaultAzureServer);
            public static string Http2Host => GetValue("DOTNET_TEST_HTTP2HOST", DefaultHttp2AzureServer);
            public static int Port = GetPortValue("DOTNET_TEST_HTTPHOST", 80);
            public static int SecurePort = GetPortValue("DOTNET_TEST_SECUREHTTPHOST", 443);

            // This server doesn't use HTTP/2 server push (push promise) feature. Some HttpClient implementations
            // don't support servers that use push right now.
            public static string Http2NoPushHost => GetValue("DOTNET_TEST_HTTP2NOPUSHHOST", "www.microsoft.com");

            // Domain server environment.
            public static string DomainJoinedHttpHost => GetValue("DOTNET_TEST_DOMAINJOINED_HTTPHOST");
            public static string DomainJoinedProxyHost => GetValue("DOTNET_TEST_DOMAINJOINED_PROXYHOST");
            public static string DomainJoinedProxyPort => GetValue("DOTNET_TEST_DOMAINJOINED_PROXYPORT");

            // Standalone server environment.
            public static string WindowsServerHttpHost => GetValue("DOTNET_TEST_WINDOWSSERVER_HTTPHOST");

            public static string SSLv2RemoteServer => GetValue("DOTNET_TEST_HTTPHOST_SSL2", "https://www.ssllabs.com:10200/");
            public static string SSLv3RemoteServer => GetValue("DOTNET_TEST_HTTPHOST_SSL3", "https://www.ssllabs.com:10300/");
            public static string TLSv10RemoteServer => GetValue("DOTNET_TEST_HTTPHOST_TLS10", "https://www.ssllabs.com:10301/");
            public static string TLSv11RemoteServer => GetValue("DOTNET_TEST_HTTPHOST_TLS11", "https://www.ssllabs.com:10302/");
            public static string TLSv12RemoteServer => GetValue("DOTNET_TEST_HTTPHOST_TLS12", "https://www.ssllabs.com:10303/");

            public static string ExpiredCertRemoteServer => GetValue("DOTNET_TEST_HTTPHOST_EXPIREDCERT", "https://expired.badssl.com/");
            public static string WrongHostNameCertRemoteServer => GetValue("DOTNET_TEST_HTTPHOST_WRONGHOSTNAME", "https://wrong.host.badssl.com/");
            public static string SelfSignedCertRemoteServer => GetValue("DOTNET_TEST_HTTPHOST_SELFSIGNEDCERT", "https://self-signed.badssl.com/");
            public static string RevokedCertRemoteServer => GetValue("DOTNET_TEST_HTTPHOST_REVOKEDCERT", "https://revoked.badssl.com/");

            public static string EchoClientCertificateRemoteServer => GetValue("DOTNET_TEST_HTTPHOST_ECHOCLIENTCERT", "https://corefx-net-tls.azurewebsites.net/EchoClientCertificate.ashx");
            public static string Http2ForceUnencryptedLoopback => GetValue("DOTNET_TEST_HTTP2_FORCEUNENCRYPTEDLOOPBACK");

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

            public static readonly RemoteServer RemoteHttp11Server = new RemoteServer(new Uri("http://" + Host + "/"), HttpVersion.Version11);
            public static readonly RemoteServer RemoteSecureHttp11Server = new RemoteServer(new Uri("https://" + SecureHost + "/"), HttpVersion.Version11);
            public static readonly RemoteServer RemoteHttp2Server = new RemoteServer(new Uri("https://" + Http2Host + "/"), new Version(2, 0));

            public static readonly IEnumerable<RemoteServer> RemoteServers = new RemoteServer[] { RemoteHttp11Server, RemoteSecureHttp11Server, RemoteHttp2Server };

            public static readonly IEnumerable<object[]> RemoteServersMemberData = RemoteServers.Select(s => new object[] { s });

            public sealed class RemoteServer
            {
                public RemoteServer(Uri baseUri, Version httpVersion)
                {
                    BaseUri = baseUri;
                    HttpVersion = httpVersion;
                }

                public Uri BaseUri { get; }

                public Version HttpVersion { get; }

                public bool IsSecure => BaseUri.Scheme == Uri.UriSchemeHttps;

                public Uri EchoUri => new Uri(BaseUri, $"/{EchoHandler}");

                public Uri VerifyUploadUri => new Uri(BaseUri, $"/{VerifyUploadHandler}");

                public Uri GZipUri => new Uri(BaseUri, $"/{GZipHandler}");

                public Uri DeflateUri => new Uri(BaseUri, $"/{DeflateHandler}");

                public Uri NegotiateAuthUriForDefaultCreds =>
                    new Uri(BaseUri, $"/{EchoHandler}?auth=negotiate");

                public Uri BasicAuthUriForCreds(string userName, string password) =>
                    new Uri(BaseUri, $"/{EchoHandler}?auth=basic&user={userName}&password={password}");

                public Uri RedirectUriForDestinationUri(int statusCode, Uri destinationUri, int hops, bool relative = false)
                {
                    string destination = Uri.EscapeDataString(relative ? destinationUri.PathAndQuery : destinationUri.AbsoluteUri);

                    if (hops > 1)
                    {
                        return new Uri(BaseUri, $"/{RedirectHandler}?statuscode={statusCode}&uri={destination}&hops={hops}");
                    }
                    else
                    {
                        return new Uri(BaseUri, $"/{RedirectHandler}?statuscode={statusCode}&uri={destination}");
                    }
                }

                public Uri RedirectUriForCreds(int statusCode, string userName, string password)
                {
                    Uri destinationUri = BasicAuthUriForCreds(userName, password);
                    string destination = Uri.EscapeDataString(destinationUri.AbsoluteUri);

                    return new Uri(BaseUri, $"/{RedirectHandler}?statuscode={statusCode}&uri={destination}");
                }

                public override string ToString()
                {
                    return $"(BaseUri: {BaseUri}, HttpVersion: {HttpVersion})";
                }
            }
        }
    }
}
