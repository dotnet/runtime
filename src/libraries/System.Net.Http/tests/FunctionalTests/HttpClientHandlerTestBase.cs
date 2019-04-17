// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Net.Test.Common;

using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandlerTestBase : FileCleanupTestBase
    {
        public readonly ITestOutputHelper _output;

        protected virtual bool UseSocketsHttpHandler => true;
        protected virtual bool UseHttp2LoopbackServer => false;

        protected bool IsWinHttpHandler => !UseSocketsHttpHandler && PlatformDetection.IsWindows && !PlatformDetection.IsUap && !PlatformDetection.IsFullFramework;
        protected bool IsCurlHandler => !UseSocketsHttpHandler && !PlatformDetection.IsWindows;
        protected bool IsNetfxHandler => PlatformDetection.IsWindows && PlatformDetection.IsFullFramework;
        protected bool IsUapHandler => PlatformDetection.IsWindows && PlatformDetection.IsUap;

        public HttpClientHandlerTestBase(ITestOutputHelper output)
        {
            _output = output;
        }

        protected HttpClient CreateHttpClient() => new HttpClient(CreateHttpClientHandler());

        protected HttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseSocketsHttpHandler, UseHttp2LoopbackServer);

        protected static HttpClient CreateHttpClient(string useSocketsHttpHandlerBoolString) =>
            new HttpClient(CreateHttpClientHandler(useSocketsHttpHandlerBoolString));

        protected static HttpClientHandler CreateHttpClientHandler(string useSocketsHttpHandlerBoolString) =>
            CreateHttpClientHandler(bool.Parse(useSocketsHttpHandlerBoolString));

        protected static HttpClientHandler CreateHttpClientHandler(bool useSocketsHttpHandler, bool useHttp2LoopbackServer = false)
        {
            HttpClientHandler handler;

            if (PlatformDetection.IsUap || PlatformDetection.IsFullFramework || useSocketsHttpHandler)
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

            TestHelper.EnsureHttp2Feature(handler, useHttp2LoopbackServer);

            if (useHttp2LoopbackServer)
            {
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

#if netcoreapp
        protected LoopbackServerFactory LoopbackServerFactory => UseHttp2LoopbackServer ? 
                                                                (LoopbackServerFactory)Http2LoopbackServerFactory.Singleton : 
                                                                (LoopbackServerFactory)Http11LoopbackServerFactory.Singleton;
#else
        protected LoopbackServerFactory LoopbackServerFactory => Http11LoopbackServerFactory.Singleton;
#endif
    }
}
