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

        protected static HttpClientHandler CreateHttpClientHandler(Version useVersion = null)
        {
            useVersion ??= HttpVersion.Version11;

            HttpClientHandler handler = new HttpClientHandler();

            if (useVersion >= HttpVersion.Version20)
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
