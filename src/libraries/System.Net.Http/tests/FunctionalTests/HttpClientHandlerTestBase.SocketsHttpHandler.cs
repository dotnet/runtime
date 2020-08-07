// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            if (useVersion == HttpVersion30)
            {
                SetUsePrenegotiatedHttp3(handler, usePrenegotiatedHttp3: true);
            }

            return handler;
        }

        /// <summary>
        /// Used to bypass Alt-Svc until https://github.com/dotnet/runtime/issues/987
        /// </summary>
        protected static void SetUsePrenegotiatedHttp3(HttpClientHandler handler, bool usePrenegotiatedHttp3)
        {
            object socketsHttpHandler = GetUnderlyingSocketsHttpHandler(handler);
            object settings = socketsHttpHandler.GetType().GetField("_settings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(socketsHttpHandler);
            settings.GetType().GetField("_assumePrenegotiatedHttp3ForTesting", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(settings, usePrenegotiatedHttp3);
        }

        protected static object GetUnderlyingSocketsHttpHandler(HttpClientHandler handler)
        {
            FieldInfo field = typeof(HttpClientHandler).GetField("_underlyingHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(handler);
        }

        protected static HttpRequestMessage CreateRequest(HttpMethod method, Uri uri, Version version, bool exactVersion = false) =>
            new HttpRequestMessage(method, uri)
            {
                Version = version,
            };
    }
}
