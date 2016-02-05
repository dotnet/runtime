// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Net.Tests
{
    internal class HttpTestServers
    {
        public const string Host = "corefx-net.cloudapp.net";
        public const string Http2Host = "http2.akamai.com";

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
        public readonly static Uri SecureRemoteEchoServer = new Uri("https://" + Host + "/" + EchoHandler);

        public readonly static Uri RemoteVerifyUploadServer = new Uri("http://" + Host + "/" + VerifyUploadHandler);
        public readonly static Uri SecureRemoteVerifyUploadServer = new Uri("https://" + Host + "/" + VerifyUploadHandler);

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

        public static Uri RedirectUriForDestinationUri(bool secure, Uri destinationUri, int hops)
        {
            string uriString;
            string destination = Uri.EscapeDataString(destinationUri.AbsoluteUri);
            
            if (hops > 1)
            {
                uriString = string.Format("{0}://{1}/{2}?uri={3}&hops={4}",
                    secure ? HttpsScheme : HttpScheme,
                    Host,
                    RedirectHandler,
                    destination,
                    hops);
            }
            else
            {
                uriString = string.Format("{0}://{1}/{2}?uri={3}",
                    secure ? HttpsScheme : HttpScheme,
                    Host,
                    RedirectHandler,
                    destination);
            }
            
            return new Uri(uriString);
        }

        public static Uri RedirectUriForCreds(bool secure, string userName, string password)
        {
                Uri destinationUri = BasicAuthUriForCreds(secure, userName, password);
                string destination = Uri.EscapeDataString(destinationUri.AbsoluteUri);
                
                return new Uri(string.Format("{0}://{1}/{2}?uri={3}",
                    secure ? HttpsScheme : HttpScheme,
                    Host,
                    RedirectHandler,
                    destination));
        }
    }
}
