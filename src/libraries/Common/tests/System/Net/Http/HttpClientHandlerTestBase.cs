// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
#if !NETCOREAPP
using System.Diagnostics;
#endif
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract partial class HttpClientHandlerTestBase : FileCleanupTestBase
    {
        // This file is shared with the WinHttpHandler implementation, which supports .NET Framework
        // So, define this so derived tests can use it.
        public static readonly Version HttpVersion30 = new Version(3, 0);

        public readonly ITestOutputHelper _output;

        protected virtual Version UseVersion => HttpVersion.Version11;

        protected virtual bool TestAsync => true;

        public HttpClientHandlerTestBase(ITestOutputHelper output)
        {
            _output = output;
        }

        protected virtual HttpClient CreateHttpClient() => CreateHttpClient(CreateHttpClientHandler());

        protected HttpClient CreateHttpClient(HttpMessageHandler handler) =>
            new HttpClient(handler) {
#if !NETFRAMEWORK
                DefaultRequestVersion = UseVersion
#endif
            };

        protected static HttpClient CreateHttpClient(string useVersionString) =>
            CreateHttpClient(CreateHttpClientHandler(useVersionString), useVersionString);

        protected static HttpClient CreateHttpClient(HttpMessageHandler handler, string useVersionString) =>
            new HttpClient(handler) {
#if !NETFRAMEWORK
                DefaultRequestVersion = Version.Parse(useVersionString)
#endif
            };

        public const int DefaultInitialWindowSize = 65535;

        public static readonly bool[] BoolValues = new[] { true, false };

        // For use by remote server tests
        public static readonly IEnumerable<object[]> RemoteServersMemberData = Configuration.Http.RemoteServersMemberData;

        protected HttpClient CreateHttpClientForRemoteServer(Configuration.Http.RemoteServer remoteServer)
        {
            return CreateHttpClientForRemoteServer(remoteServer, CreateHttpClientHandler());
        }

        protected HttpClient CreateHttpClientForRemoteServer(Configuration.Http.RemoteServer remoteServer, HttpMessageHandler httpClientHandler)
        {
            HttpMessageHandler wrappedHandler = httpClientHandler;

            // WinHttpHandler will downgrade to 1.1 if you set Transfer-Encoding: chunked.
            // So, skip this verification if we're not using SocketsHttpHandler.
            if (PlatformDetection.SupportsAlpn && !IsWinHttpHandler)
            {
                wrappedHandler = new VersionCheckerHttpHandler(httpClientHandler, remoteServer.HttpVersion);
            }

            return new HttpClient(wrappedHandler) {
#if !NETFRAMEWORK
                DefaultRequestVersion = remoteServer.HttpVersion
#endif
            };
        }

        private sealed class VersionCheckerHttpHandler : DelegatingHandler
        {
            private readonly Version _expectedVersion;

            public VersionCheckerHttpHandler(HttpMessageHandler innerHandler, Version expectedVersion)
                : base(innerHandler)
            {
                _expectedVersion = expectedVersion;
            }

#if NETCOREAPP
            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Version != _expectedVersion)
                {
                    throw new Exception($"Unexpected request version: expected {_expectedVersion}, saw {request.Version}");
                }

                HttpResponseMessage response = base.Send(request, cancellationToken);

                if (response.Version != _expectedVersion)
                {
                    throw new Exception($"Unexpected response version: expected {_expectedVersion}, saw {response.Version}");
                }

                return response;
            }
#endif

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Version != _expectedVersion)
                {
                    throw new Exception($"Unexpected request version: expected {_expectedVersion}, saw {request.Version}");
                }

                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

                if (response.Version != _expectedVersion)
                {
                    throw new Exception($"Unexpected response version: expected {_expectedVersion}, saw {response.Version}");
                }

                return response;
            }
        }
    }

    public static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> SendAsync(this HttpClient client, bool async, HttpRequestMessage request, HttpCompletionOption completionOption = default, CancellationToken cancellationToken = default)
        {
            if (async)
            {
                return client.SendAsync(request, completionOption, cancellationToken);
            }
            else
            {
#if NETCOREAPP
                // Note that the sync call must be done on a different thread because it blocks until the server replies.
                // However, the server-side of the request handling is in many cases invoked after the client, thus deadlocking the test.
                return Task.Run(() => client.Send(request, completionOption, cancellationToken));
#else
                // Framework won't ever have the sync API.
                // This shouldn't be called due to AsyncBoolValues returning only true on Framework.
                Debug.Fail("Framework doesn't have Sync API and it shouldn't be attempted to be tested.");
                throw new Exception("Shouldn't be reachable");
#endif
            }
        }

        public static Task<HttpResponseMessage> SendAsync(this HttpMessageInvoker invoker, bool async, HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            if (async)
            {
                return invoker.SendAsync(request, cancellationToken);
            }
            else
            {
#if NETCOREAPP
                // Note that the sync call must be done on a different thread because it blocks until the server replies.
                // However, the server-side of the request handling is in many cases invoked after the client, thus deadlocking the test.
                return Task.Run(() => invoker.Send(request, cancellationToken));
#else
                // Framework won't ever have the sync API.
                // This shouldn't be called due to AsyncBoolValues returning only true on Framework.
                Debug.Fail("Framework doesn't have Sync API and it shouldn't be attempted to be tested.");
                throw new Exception("Shouldn't be reachable");
#endif
            }
        }

        public static Task<Stream> ReadAsStreamAsync(this HttpContent content, bool async, CancellationToken cancellationToken = default)
        {
            if (async)
            {
#if NETCOREAPP
                // No CancellationToken accepting overload on NETFX.
                return content.ReadAsStreamAsync(cancellationToken);
#else
                return content.ReadAsStreamAsync();
#endif

            }
            else
            {
#if NETCOREAPP
                return Task.FromResult(content.ReadAsStream(cancellationToken));
#else
                // Framework won't ever have the sync API.
                // This shouldn't be called due to AsyncBoolValues returning only true on Framework.
                Debug.Fail("Framework doesn't have Sync API and it shouldn't be attempted to be tested.");
                throw new Exception("Shouldn't be reachable");
#endif
            }
        }

        public static Task<byte[]> GetByteArrayAsync(this HttpClient client, bool async, bool useCopyTo, Uri uri)
        {
#if NETCOREAPP
            return Task.Run(async () =>
            {
                var m = new HttpRequestMessage(HttpMethod.Get, uri);
                using HttpResponseMessage r = async ? await client.SendAsync(m, HttpCompletionOption.ResponseHeadersRead) : client.Send(m, HttpCompletionOption.ResponseHeadersRead);
                using Stream s = async ? await r.Content.ReadAsStreamAsync() : r.Content.ReadAsStream();

                var result = new MemoryStream();
                if (useCopyTo)
                {
                    if (async)
                    {
                        await s.CopyToAsync(result);
                    }
                    else
                    {
                        s.CopyTo(result);
                    }
                }
                else
                {
                    byte[] buffer = new byte[100];
                    while (true)
                    {
                        int bytesRead = async ? await s.ReadAsync(buffer) : s.Read(buffer);
                        if (bytesRead == 0)
                        {
                            break;
                        }
                        result.Write(buffer.AsSpan(0, bytesRead));
                    }
                }
                return result.ToArray();
            });
#else
            // For WinHttpHandler on .NET Framework, we fall back to ignoring async and useCopyTo.
            return client.GetByteArrayAsync(uri);
#endif
        }
    }
}
