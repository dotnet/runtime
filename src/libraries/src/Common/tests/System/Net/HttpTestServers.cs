// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Test.Common
{
    internal class HttpTestServers
    {
        public readonly static string Host = TestSettings.Http.Host;
        public readonly static string SecureHost = TestSettings.Http.SecureHost;
        public readonly static string Http2Host = TestSettings.Http.Http2Host;

        public const string SSLv2RemoteServer = "https://www.ssllabs.com:10200/";
        public const string SSLv3RemoteServer = "https://www.ssllabs.com:10300/";
        public const string TLSv10RemoteServer = "https://www.ssllabs.com:10301/";
        public const string TLSv11RemoteServer = "https://www.ssllabs.com:10302/";
        public const string TLSv12RemoteServer = "https://www.ssllabs.com:10303/";

        public const string ExpiredCertRemoteServer = "https://expired.badssl.com/";
        public const string WrongHostNameCertRemoteServer = "https://wrong.host.badssl.com/";
        public const string SelfSignedCertRemoteServer = "https://self-signed.badssl.com/";
        public const string RevokedCertRemoteServer = "https://revoked.grc.com/";

        private const string HttpScheme = "http";
        private const string HttpsScheme = "https";

        private const string EchoHandler = "Echo.ashx";
        private const string EmptyContentHandler = "EmptyContent.ashx";
        private const string StatusCodeHandler = "StatusCode.ashx";
        private const string RedirectHandler = "Redirect.ashx";
        private const string VerifyUploadHandler = "VerifyUpload.ashx";
        private const string DeflateHandler = "Deflate.ashx";
        private const string GZipHandler = "GZip.ashx";

        public readonly static Uri RemoteEchoServer = new Uri("http://" + Host + "/" + EchoHandler);
        public readonly static Uri SecureRemoteEchoServer = new Uri("https://" + SecureHost + "/" + EchoHandler);

        public readonly static Uri RemoteVerifyUploadServer = new Uri("http://" + Host + "/" + VerifyUploadHandler);
        public readonly static Uri SecureRemoteVerifyUploadServer = new Uri("https://" + SecureHost + "/" + VerifyUploadHandler);

        public readonly static Uri RemoteEmptyContentServer = new Uri("http://" + Host + "/" + EmptyContentHandler);
        public readonly static Uri RemoteDeflateServer = new Uri("http://" + Host + "/" + DeflateHandler);
        public readonly static Uri RemoteGZipServer = new Uri("http://" + Host + "/" + GZipHandler);

        public readonly static object[][] EchoServers = { new object[] { RemoteEchoServer }, new object[] { SecureRemoteEchoServer } };
        public readonly static object[][] VerifyUploadServers = { new object[] { RemoteVerifyUploadServer }, new object[] { SecureRemoteVerifyUploadServer } };
        public readonly static object[][] CompressedServers = { new object[] { RemoteDeflateServer }, new object[] { RemoteGZipServer } };
        public readonly static object[][] Http2Servers = { new object[] { new Uri("https://" + Http2Host) } };

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

        public static Uri StatusCodeUri(bool secure, int statusCode)
        {
            return new Uri(
                string.Format(
                    "{0}://{1}/{2}?statuscode={3}",
                    secure ? HttpsScheme : HttpScheme,
                    Host,
                    StatusCodeHandler,
                    statusCode));
        }

        public static Uri StatusCodeUri(bool secure, int statusCode, string statusDescription)
        {
            return new Uri(
                string.Format(
                    "{0}://{1}/{2}?statuscode={3}&statusDescription={4}",
                    secure ? HttpsScheme : HttpScheme,
                    Host,
                    StatusCodeHandler,
                    statusCode,
                    statusDescription));
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
