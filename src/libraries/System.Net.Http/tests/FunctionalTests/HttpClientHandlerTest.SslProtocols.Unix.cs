// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public partial class HttpClientHandler_SslProtocols_Test
    {
        private bool BackendSupportsSslConfiguration =>
            UseManagedHandler ||
            (CurlSslVersionDescription()?.StartsWith("OpenSSL") ?? false);

        private bool SSLv3DisabledByDefault =>
            BackendSupportsSslConfiguration ||
            Version.Parse(CurlVersionDescription()) >= new Version(7, 39); // libcurl disables SSLv3 by default starting in v7.39

        [DllImport("System.Net.Http.Native", EntryPoint = "HttpNative_GetVersionDescription")]
        private static extern string CurlVersionDescription();

        [DllImport("System.Net.Http.Native", EntryPoint = "HttpNative_GetSslVersionDescription")]
        private static extern string CurlSslVersionDescription();
    }
}
