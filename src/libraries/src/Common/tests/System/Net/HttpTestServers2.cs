// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Net.Tests
{
    internal class HttpTestServers2
    {
        // TODO: This class will be used to replace the HttpTestServers class. For now, it is separate
        // while the test infrastructure issues (#2383 et. al) are being worked out.
        public const string Host = "corefx-networking.azurewebsites.net";

        private const string EchoHandler = "Echo.ashx";
        private const string EmptyContentHandler = "EmptyContent.ashx";
        
        public readonly static Uri RemoteEchoServer = new Uri("http://" + Host + "/" + EchoHandler);
        public readonly static Uri SecureRemoteEchoServer = new Uri("https://" + Host + "/" + EchoHandler);
        
        public readonly static Uri RemoteEmptyContentServer = new Uri("http://" + Host + "/" + EmptyContentHandler);

        public readonly static object[][] EchoServers = { new object[] { RemoteEchoServer }, new object[] { SecureRemoteEchoServer } };

        public static Uri BasicAuthUriForCreds(bool secure, string userName, string password)
        {
            string scheme;
            
            if (secure)
            {
                scheme = "https";
            }
            else
            {
                scheme = "http";
            }
            
            return new Uri(
                string.Format(
                    "{0}://{1}/{2}?auth=basic&user={3}&password={4}",
                    scheme,
                    Host,
                    EchoHandler,
                    userName,
                    password));
        }
    }
}
