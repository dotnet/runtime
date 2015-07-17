// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Net.Tests
{
    internal class HttpTestServers
    {
        // Issue 2383: Figure out how to provide parameters to unit tests at build/run-time.
        //
        // It would be very nice if the server to used for testing was configurable at build/run-time
        // in order to avoid a strict dependency on network access when running tests.
        public const string Host = "httpbin.org";

        public readonly static Uri RemoteGetServer = new Uri("http://" + Host + "/get");
        public readonly static Uri RemotePostServer = new Uri("http://" + Host + "/post");
        public readonly static Uri SecureRemoteGetServer = new Uri("https://" + Host + "/get");
        public readonly static Uri SecureRemotePostServer = new Uri("https://" + Host + "/post");

        public const string RemoteStatusCodeServerFormat = "http://" + Host + "/status/{0}";
        public const string SecureRemoteStatusCodeServerFormat = "https://" + Host + "/status/{0}";

        public readonly static object[][] GetServers = { new object[] { RemoteGetServer }, new object[] { SecureRemoteGetServer } };
        public readonly static object[][] PostServers = { new object[] { RemotePostServer }, new object[] { SecureRemotePostServer } };
    }
}
