// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;

namespace System.Net.Http.Functional.Tests
{
    public abstract partial class HttpClientHandlerTestBase : FileCleanupTestBase
    {
        protected static bool IsWinHttpHandler => true;

        protected WinHttpHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseHttp2);

        protected static WinHttpHandler CreateHttpClientHandler(string useHttp2LoopbackServerString) =>
            CreateHttpClientHandler(bool.Parse(useHttp2LoopbackServerString));

        protected static WinHttpHandler CreateHttpClientHandler(bool useHttp2LoopbackServer = false)
        {
            WinHttpHandler handler = new WinHttpHandler();

            if (useHttp2LoopbackServer)
            {
                handler.ServerCertificateValidationCallback = TestHelper.AllowAllCertificates;
            }

            return handler;
        }
    }
}
