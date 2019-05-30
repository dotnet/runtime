// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Net.Test.Common;

using Xunit.Abstractions;
using System.Collections.Generic;
using System.Text;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandlerTestBase : FileCleanupTestBase
    {
        public readonly ITestOutputHelper _output;

        protected virtual bool UseSocketsHttpHandler => true;
        protected virtual bool UseHttp2 => false;

        protected bool IsWinHttpHandler => !UseSocketsHttpHandler && PlatformDetection.IsWindows && !PlatformDetection.IsUap;
        protected bool IsCurlHandler => !UseSocketsHttpHandler && !PlatformDetection.IsWindows;
        protected bool IsNetfxHandler => false;
        protected bool IsUapHandler => PlatformDetection.IsWindows && PlatformDetection.IsUap;

        public HttpClientHandlerTestBase(ITestOutputHelper output)
        {
            _output = output;
        }

        protected Version VersionFromUseHttp2 => GetVersion(UseHttp2);

        private static Version GetVersion(bool http2) => http2 ? new Version(2, 0) : HttpVersion.Version11;

        protected HttpClient CreateHttpClient() => CreateHttpClient(CreateHttpClientHandler());

        protected HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler);
            SetDefaultRequestVersion(client, VersionFromUseHttp2);
            return client;
        }

        protected static HttpClient CreateHttpClient(string useSocketsHttpHandlerBoolString, string useHttp2String) =>
            CreateHttpClient(CreateHttpClientHandler(useSocketsHttpHandlerBoolString, useHttp2String), useHttp2String);

        protected static HttpClient CreateHttpClient(HttpMessageHandler handler, string useHttp2String)
        {
            var client = new HttpClient(handler);
            SetDefaultRequestVersion(client, GetVersion(bool.Parse(useHttp2String)));
            return client;
        }

        protected HttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseSocketsHttpHandler, UseHttp2);

        protected static HttpClientHandler CreateHttpClientHandler(string useSocketsHttpHandlerBoolString, string useHttp2LoopbackServerString) =>
            CreateHttpClientHandler(bool.Parse(useSocketsHttpHandlerBoolString), bool.Parse(useHttp2LoopbackServerString));

        protected static void SetDefaultRequestVersion(HttpClient client, Version version)
        {
            PropertyInfo pi = client.GetType().GetProperty("DefaultRequestVersion", BindingFlags.Public | BindingFlags.Instance);
            Debug.Assert(pi != null || !PlatformDetection.IsNetCore);
            pi?.SetValue(client, version);
        }

        protected static HttpClientHandler CreateHttpClientHandler(bool useSocketsHttpHandler, bool useHttp2LoopbackServer = false)
        {
            HttpClientHandler handler;

            if (PlatformDetection.IsUap || useSocketsHttpHandler)
            {
                handler = new HttpClientHandler();
            }
            else
            {
                // Create platform specific handler.
                ConstructorInfo ctor = typeof(HttpClientHandler).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(bool) }, null);
                Debug.Assert(ctor != null, "Couldn't find test constructor on HttpClientHandler");

                handler = (HttpClientHandler)ctor.Invoke(new object[] { useSocketsHttpHandler });
                Debug.Assert(useSocketsHttpHandler == IsSocketsHttpHandler(handler), "Unexpected handler.");
            }

            if (useHttp2LoopbackServer)
            {
                TestHelper.EnableUnencryptedHttp2IfNecessary(handler);
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            }

            return handler;
        }

        protected static bool IsSocketsHttpHandler(HttpClientHandler handler) =>
            GetUnderlyingSocketsHttpHandler(handler) != null;

        protected static object GetUnderlyingSocketsHttpHandler(HttpClientHandler handler)
        {
            FieldInfo field = typeof(HttpClientHandler).GetField("_socketsHttpHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(handler);
        }

        protected LoopbackServerFactory LoopbackServerFactory =>
#if netcoreapp
            UseHttp2 ?
                (LoopbackServerFactory)Http2LoopbackServerFactory.Singleton :
#endif
                Http11LoopbackServerFactory.Singleton;
    }
}
