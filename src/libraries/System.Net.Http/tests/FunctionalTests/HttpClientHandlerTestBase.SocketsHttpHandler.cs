// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;

namespace System.Net.Http.Functional.Tests
{
    public abstract partial class HttpClientHandlerTestBase : FileCleanupTestBase
    {
        protected static bool IsWinHttpHandler => false;

        protected HttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseHttp2);

        protected static HttpClientHandler CreateHttpClientHandler(string useHttp2LoopbackServerString) =>
            CreateHttpClientHandler(bool.Parse(useHttp2LoopbackServerString));

        protected static HttpClientHandler CreateHttpClientHandler(bool useHttp2LoopbackServer = false)
        {
            HttpClientHandler handler = new HttpClientHandler();

            if (useHttp2LoopbackServer)
            {
                TestHelper.EnableUnencryptedHttp2IfNecessary(handler);
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            }

            return handler;
        }

        protected static object GetUnderlyingSocketsHttpHandler(HttpClientHandler handler)
        {
            FieldInfo field = typeof(HttpClientHandler).GetField("_socketsHttpHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(handler);
        }
    }
}
