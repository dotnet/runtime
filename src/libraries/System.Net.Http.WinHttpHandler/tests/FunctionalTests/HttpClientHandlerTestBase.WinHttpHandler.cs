// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Net.Http.Functional.Tests
{
    public abstract partial class HttpClientHandlerTestBase : FileCleanupTestBase
    {
        protected static bool IsWinHttpHandler => true;

        protected WinHttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseHttp2);

        protected static WinHttpClientHandler CreateHttpClientHandler(string useHttp2LoopbackServerString) =>
            CreateHttpClientHandler(bool.Parse(useHttp2LoopbackServerString));

        protected static WinHttpClientHandler CreateHttpClientHandler(bool useHttp2LoopbackServer = false)
        {
            WinHttpClientHandler handler = new WinHttpClientHandler();

            if (useHttp2LoopbackServer)
            {
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            }

            return handler;
        }
    }
}