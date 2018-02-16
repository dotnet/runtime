// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientTestBase : RemoteExecutorTestBase
    {
        protected virtual bool UseSocketsHttpHandler => false;

        protected bool IsWinHttpHandler => !UseSocketsHttpHandler && PlatformDetection.IsWindows && !PlatformDetection.IsUap && !PlatformDetection.IsFullFramework;
        protected bool IsCurlHandler => !UseSocketsHttpHandler && !PlatformDetection.IsWindows;
        protected bool IsNetfxHandler => !UseSocketsHttpHandler && PlatformDetection.IsWindows && PlatformDetection.IsFullFramework;
        protected bool IsUapHandler => !UseSocketsHttpHandler && PlatformDetection.IsWindows && PlatformDetection.IsUap;

        protected HttpClient CreateHttpClient() => new HttpClient(CreateHttpClientHandler());

        protected HttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseSocketsHttpHandler);

        protected static HttpClient CreateHttpClient(string useSocketsHttpHandlerBoolString) =>
            new HttpClient(CreateHttpClientHandler(useSocketsHttpHandlerBoolString));

        protected static HttpClientHandler CreateHttpClientHandler(string useSocketsHttpHandlerBoolString) =>
            CreateHttpClientHandler(bool.Parse(useSocketsHttpHandlerBoolString));

        protected static HttpClientHandler CreateHttpClientHandler(bool useSocketsHttpHandler)
        {
            if (!PlatformDetection.IsNetCore) // SocketsHttpHandler only exists on .NET Core
            {
                return new HttpClientHandler();
            }

            ConstructorInfo ctor = typeof(HttpClientHandler).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(bool) }, null);
            Debug.Assert(ctor != null, "Couldn't find test constructor on HttpClientHandler");

            HttpClientHandler handler = (HttpClientHandler)ctor.Invoke(new object[] { useSocketsHttpHandler });
            Debug.Assert(useSocketsHttpHandler == IsSocketsHttpHandler(handler), "Unexpected handler.");

            return handler;
        }

        protected static bool IsSocketsHttpHandler(HttpClientHandler handler)
        {
            FieldInfo field = typeof(HttpClientHandler).GetField("_socketsHttpHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return false;
            }

            object socketsHttpHandler = field.GetValue(handler);
            if (socketsHttpHandler == null)
            {
                return false;
            }

            return true;
        }
    }
}
