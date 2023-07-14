// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.Http.Metrics;
using System.Net.Test.Common;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpMetricsTestBase : HttpClientHandlerTestBase
    {
        protected static class InstrumentNames
        {
            public const string RequestDuration = "http-client-request-duration";
            public const string CurrentRequests = "http-client-current-requests";
            public const string FailedRequests = "http-client-failed-requests";
        }
        
        protected HttpMetricsTestBase(ITestOutputHelper output) : base(output)
        {
        }

        protected static void VerifyOptionalTag<T>(KeyValuePair<string, object?>[] tags, string name, T value)
        {
            if (value is null)
            {
                Assert.DoesNotContain(tags, t => t.Key == name);
            }
            else
            {
                Assert.Equal(value, (T)tags.Single(t => t.Key == name).Value);
            }
        }

        protected static void VerifyRequestDuration(Measurement<double> measurement, Uri uri, string? protocol, int? statusCode, string method = "GET")
        {
            Assert.True(measurement.Value > 0);

            string scheme = uri.Scheme;
            string host = uri.IdnHost;
            int? port = uri.Port;
            KeyValuePair<string, object?>[] tags = measurement.Tags.ToArray();

            Assert.Equal(scheme, tags.Single(t => t.Key == "scheme").Value);
            Assert.Equal(host, tags.Single(t => t.Key == "host").Value);
            Assert.Equal(method, tags.Single(t => t.Key == "method").Value);
            VerifyOptionalTag(tags, "port", port);
            VerifyOptionalTag(tags, "protocol", protocol);
            VerifyOptionalTag(tags, "status-code", statusCode);
        }

        protected static void VerifyCurrentRequest(Measurement<long> measurement, long expectedValue, Uri uri)
        {
            Assert.Equal(expectedValue, measurement.Value);

            string scheme = uri.Scheme;
            string host = uri.Host;
            int? port = uri.Port;
            KeyValuePair<string, object?>[] tags = measurement.Tags.ToArray();

            Assert.Equal(scheme, tags.Single(t => t.Key == "scheme").Value);
            Assert.Equal(host, tags.Single(t => t.Key == "host").Value);
            VerifyOptionalTag(tags, "port", port);
        }

        protected static void VerifyFailedRequests(Measurement<long> measurement, long expectedValue, Uri uri, string? protocol, int? statusCode, string method = "GET")
        {
            Assert.Equal(expectedValue, measurement.Value);

            string scheme = uri.Scheme;
            string host = uri.IdnHost;
            int? port = uri.Port;
            KeyValuePair<string, object?>[] tags = measurement.Tags.ToArray();

            Assert.Equal(scheme, tags.Single(t => t.Key == "scheme").Value);
            Assert.Equal(host, tags.Single(t => t.Key == "host").Value);
            Assert.Equal(method, tags.Single(t => t.Key == "method").Value);
            VerifyOptionalTag(tags, "port", port);
            VerifyOptionalTag(tags, "protocol", protocol);
            VerifyOptionalTag(tags, "status-code", statusCode);
        }

        protected sealed class InstrumentRecorder<T> : IDisposable where T : struct
        {
            private readonly string _instrumentName;
            private readonly MeterListener _meterListener = new MeterListener();
            private readonly List<Measurement<T>> _values = new List<Measurement<T>>();
            private Meter? _meter;

            public InstrumentRecorder(string instrumentName)
            {
                _instrumentName = instrumentName;
                _meterListener.InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == "System.Net.Http" && instrument.Name == _instrumentName)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                };
                _meterListener.SetMeasurementEventCallback<T>(OnMeasurementRecorded);
                _meterListener.Start();
            }

            public InstrumentRecorder(IMeterFactory meterFactory, string instrumentName)
            {
                _meter = meterFactory.Create("System.Net.Http");
                _instrumentName = instrumentName;
                _meterListener.InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter == _meter && instrument.Name == _instrumentName)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                };
                _meterListener.SetMeasurementEventCallback<T>(OnMeasurementRecorded);
                _meterListener.Start();
            }

            private void OnMeasurementRecorded(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) => _values.Add(new Measurement<T>(measurement, tags));
            public IReadOnlyList<Measurement<T>> GetMeasurements() => _values.ToArray();
            public void Dispose() => _meterListener.Dispose();
        }
    }

    public abstract class HttpMetricsTest : HttpMetricsTestBase
    {
        public static readonly bool SupportsSeparateHttpSpansForRedirects = PlatformDetection.IsNotMobile && PlatformDetection.IsNotBrowser;
        private IMeterFactory _meterFactory = new TestMeterFactory();
        protected HttpClientHandler Handler { get; }
        protected virtual bool TestHttpMessageInvoker => false;
        public HttpMetricsTest(ITestOutputHelper output) : base(output)
        {
            Handler = CreateHttpClientHandler();
        }

        [Fact]
        public Task CurrentRequests_Success_Recorded()
        {
            return LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpMessageInvoker client = CreateHttpMessageInvoker();
                using InstrumentRecorder<long> recorder = SetupInstrumentRecorder<long>(InstrumentNames.CurrentRequests);
                using HttpRequestMessage request = new(HttpMethod.Get, uri) { Version = UseVersion };

                HttpResponseMessage response = await SendAsync(client, request);
                response.Dispose(); // Make sure disposal doesn't interfere with recording by enforcing early disposal.

                Assert.Collection(recorder.GetMeasurements(),
                    m => VerifyCurrentRequest(m, 1, uri),
                    m => VerifyCurrentRequest(m, -1, uri));
            }, async server =>
            {
                await server.AcceptConnectionSendResponseAndCloseAsync();
            });
        }

        [Fact]
        [OuterLoop("Uses Task.Delay")]
        public async Task CurrentRequests_InstrumentEnabledAfterSending_NotRecorded()
        {
            SemaphoreSlim instrumentEnabledSemaphore = new SemaphoreSlim(0);

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpMessageInvoker client = CreateHttpMessageInvoker();

                // Enable recording request-duration to test the path with metrics enabled.
                using InstrumentRecorder<double> unrelatedRecorder = SetupInstrumentRecorder<double>(InstrumentNames.RequestDuration);

                using HttpRequestMessage request = new(HttpMethod.Get, uri) { Version = UseVersion };
                Task<HttpResponseMessage> clientTask = SendAsync(client, request);
                await Task.Delay(100);
                using InstrumentRecorder<long> recorder = new(Handler.MeterFactory, InstrumentNames.CurrentRequests);
                instrumentEnabledSemaphore.Release();

                using HttpResponseMessage response = await clientTask;

                Assert.Empty(recorder.GetMeasurements());
            }, async server =>
            {
                await instrumentEnabledSemaphore.WaitAsync();
                await server.AcceptConnectionSendResponseAndCloseAsync();
            });
        }

        [Theory]
        [InlineData("GET", HttpStatusCode.OK)]
        [InlineData("PUT", HttpStatusCode.Created)]
        public Task RequestDuration_Success_Recorded(string method, HttpStatusCode statusCode)
        {
            return LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpMessageInvoker client = CreateHttpMessageInvoker();
                using InstrumentRecorder<double> recorder = SetupInstrumentRecorder<double>(InstrumentNames.RequestDuration);
                using HttpRequestMessage request = new(new HttpMethod(method), uri) { Version = UseVersion };

                using HttpResponseMessage response = await SendAsync(client, request);

                Measurement<double> m = recorder.GetMeasurements().Single();
                VerifyRequestDuration(m, uri, ExpectedProtocolString, (int)statusCode, method);

            }, async server =>
            {
                await server.AcceptConnectionSendResponseAndCloseAsync(statusCode);
            });
        }

        [Fact]
        public Task RequestDuration_CustomTags_Recorded()
        {
            return LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpMessageInvoker client = CreateHttpMessageInvoker();
                using InstrumentRecorder<double> recorder = SetupInstrumentRecorder<double>(InstrumentNames.RequestDuration);
                using HttpRequestMessage request = new(HttpMethod.Get, uri) { Version = UseVersion };

                HttpMetricsEnrichmentContext.AddCallback(request, static ctx =>
                {
                    ctx.AddCustomTag("route", "/test");
                });
                
                using HttpResponseMessage response = await SendAsync(client, request);

                Measurement<double> m = recorder.GetMeasurements().Single();
                VerifyRequestDuration(m, uri, ExpectedProtocolString, 200);
                Assert.Equal("/test", m.Tags.ToArray().Single(t => t.Key == "route").Value);

            }, async server =>
            {
                await server.AcceptConnectionSendResponseAndCloseAsync();
            });
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("System.Net.Http.HttpRequestOut.Start")]
        [InlineData("System.Net.Http.Request")]
        [InlineData("System.Net.Http.HttpRequestOut.Stop")]
        public void RequestDuration_CustomTags_DiagnosticListener_Recorded(string eventName)
        {
            RemoteExecutor.Invoke(static async (testClassName, eventNameInner) =>
            {
                using HttpMetricsTest test = (HttpMetricsTest)Activator.CreateInstance(Type.GetType(testClassName), (ITestOutputHelper)null);
                await test.RequestDuration_CustomTags_DiagnosticListener_Recorded_Core(eventNameInner);
            }, GetType().FullName, eventName).Dispose();
        }

        private async Task RequestDuration_CustomTags_DiagnosticListener_Recorded_Core(string eventName)
        {
            FakeDiagnosticListenerObserver diagnosticListenerObserver = new(kv =>
            {
                if (kv.Key == eventName)
                {
                    HttpRequestMessage request = GetProperty<HttpRequestMessage>(kv.Value, "Request");
                    HttpMetricsEnrichmentContext.AddCallback(request, static ctx =>
                    {
                        ctx.AddCustomTag("observed?", "observed!");
                        Assert.NotNull(ctx.Response);
                    });
                }
            });

            using IDisposable subscription = DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver);

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                diagnosticListenerObserver.Enable();
                using HttpMessageInvoker client = CreateHttpMessageInvoker();
                using InstrumentRecorder<double> recorder = SetupInstrumentRecorder<double>(InstrumentNames.RequestDuration);
                using HttpRequestMessage request = new(HttpMethod.Get, uri) { Version = UseVersion };
                HttpMetricsEnrichmentContext.AddCallback(request, static ctx =>
                {
                    ctx.AddCustomTag("route", "/test");
                });

                using HttpResponseMessage response = await SendAsync(client, request);

                Measurement<double> m = recorder.GetMeasurements().Single();
                VerifyRequestDuration(m, uri, ExpectedProtocolString, 200);
                Assert.Equal("/test", m.Tags.ToArray().Single(t => t.Key == "route").Value);
                Assert.Equal("observed!", m.Tags.ToArray().Single(t => t.Key == "observed?").Value);

            }, async server =>
            {
                await server.AcceptConnectionSendResponseAndCloseAsync();
            });

            static T GetProperty<T>(object obj, string propertyName)
            {
                Type t = obj.GetType();

                PropertyInfo p = t.GetRuntimeProperty(propertyName);

                object propertyValue = p.GetValue(obj);
                Assert.NotNull(propertyValue);
                Assert.IsAssignableFrom<T>(propertyValue);

                return (T)propertyValue;
            }
        }

        public enum ResponseContentType
        {
            Empty,
            ContentLength,
            TransferEncodingChunked
        }

        [Theory]
        [InlineData(HttpCompletionOption.ResponseContentRead, ResponseContentType.Empty)]
        [InlineData(HttpCompletionOption.ResponseContentRead, ResponseContentType.ContentLength)]
        [InlineData(HttpCompletionOption.ResponseContentRead, ResponseContentType.TransferEncodingChunked)]
        [InlineData(HttpCompletionOption.ResponseHeadersRead, ResponseContentType.Empty)]
        [InlineData(HttpCompletionOption.ResponseHeadersRead, ResponseContentType.ContentLength)]
        [InlineData(HttpCompletionOption.ResponseHeadersRead, ResponseContentType.TransferEncodingChunked)]
        public async Task RequestDuration_EnrichmentHandler_Success_Recorded(HttpCompletionOption completionOption, ResponseContentType responseContentType)
        {
            if (TestHttpMessageInvoker)
            {
                // HttpCompletionOption not supported for HttpMessageInvoker, skipping.
                return;
            }

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient(new EnrichmentHandler(Handler));
                using InstrumentRecorder<double> recorder = SetupInstrumentRecorder<double>(InstrumentNames.RequestDuration);
                using HttpRequestMessage request = new(HttpMethod.Get, uri) { Version = UseVersion };
                using HttpResponseMessage response = await client.SendAsync(TestAsync, request, completionOption);

                Measurement<double> m = recorder.GetMeasurements().Single();
                VerifyRequestDuration(m, uri, ExpectedProtocolString, 200); ;
                Assert.Equal("before!", m.Tags.ToArray().Single(t => t.Key == "before").Value);
            }, async server =>
            {
                if (responseContentType == ResponseContentType.ContentLength)
                {
                    string content = string.Join(' ', Enumerable.Range(0, 100));
                    int contentLength = Encoding.ASCII.GetByteCount(content);
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: content, additionalHeaders: new[] { new HttpHeaderData("Content-Length", $"{contentLength}") });
                }
                else if (responseContentType == ResponseContentType.TransferEncodingChunked)
                {
                    string content = "3\r\nfoo\r\n3\r\nbar\r\n0\r\n\r\n";
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: content, additionalHeaders: new[] { new HttpHeaderData("Transfer-Encoding", "chunked") });
                }
                else
                {
                    // Empty
                    await server.AcceptConnectionSendResponseAndCloseAsync();
                }
            });
        }

        [ConditionalFact(nameof(SupportsSeparateHttpSpansForRedirects))]
        public Task CurrentRequests_Redirect_RecordedForEachHttpSpan()
        {
            return LoopbackServerFactory.CreateServerAsync((originalServer, originalUri) =>
            {
                return LoopbackServerFactory.CreateServerAsync(async (redirectServer, redirectUri) =>
                {
                    using HttpMessageInvoker client = CreateHttpMessageInvoker();
                    using InstrumentRecorder<long> recorder = SetupInstrumentRecorder<long>(InstrumentNames.CurrentRequests);
                    using HttpRequestMessage request = new(HttpMethod.Get, originalUri) { Version = UseVersion };

                    Task clientTask = SendAsync(client, request);
                    Task serverTask = originalServer.HandleRequestAsync(HttpStatusCode.Redirect, new[] { new HttpHeaderData("Location", redirectUri.AbsoluteUri) });

                    await Task.WhenAny(clientTask, serverTask);
                    Assert.False(clientTask.IsCompleted, $"{clientTask.Status}: {clientTask.Exception}");
                    await serverTask;

                    serverTask = redirectServer.HandleRequestAsync();
                    await TestHelper.WhenAllCompletedOrAnyFailed(clientTask, serverTask);
                    await clientTask;

                    Assert.Collection(recorder.GetMeasurements(),
                        m => VerifyCurrentRequest(m, 1, originalUri),
                        m => VerifyCurrentRequest(m, -1, originalUri),
                        m => VerifyCurrentRequest(m, 1, redirectUri),
                        m => VerifyCurrentRequest(m, -1, redirectUri));
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Handler.Dispose();
                _meterFactory.Dispose();
            }

            base.Dispose(disposing);
        }

        protected Task<HttpResponseMessage> SendAsync(HttpMessageInvoker invoker, HttpRequestMessage request) =>
            TestHttpMessageInvoker ?
            invoker.SendAsync(request, default) :
            ((HttpClient)invoker).SendAsync(TestAsync, request);

        protected HttpMessageInvoker CreateHttpMessageInvoker(HttpMessageHandler? handler = null) =>
            TestHttpMessageInvoker ?
            new HttpMessageInvoker(handler ?? Handler) :
            CreateHttpClient(handler ?? Handler);

        protected InstrumentRecorder<T> SetupInstrumentRecorder<T>(string instrumentName)
            where T : struct
        {
            Handler.MeterFactory = _meterFactory;
            return new InstrumentRecorder<T>(_meterFactory, instrumentName);
        }

        protected string ExpectedProtocolString => (UseVersion.Major, UseVersion.Minor) switch
        {
            (1, 1) => "HTTP/1.1",
            (2, 0) => "HTTP/2",
            (3, 0) => "HTTP/3",
            _ => throw new Exception("Unknown version.")
        };

        protected sealed class EnrichmentHandler : DelegatingHandler
        {
            public EnrichmentHandler(HttpMessageHandler innerHandler) : base(innerHandler)
            {
            }

            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpMetricsEnrichmentContext.AddCallback(request, Enrich);
                return base.Send(request, cancellationToken);
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpMetricsEnrichmentContext.AddCallback(request, Enrich);
                return base.SendAsync(request, cancellationToken);
            }

            private static void Enrich(HttpMetricsEnrichmentContext context) => context.AddCustomTag("before", "before!");
        }
    }

    public abstract class HttpMetricsTest_Http11 : HttpMetricsTest
    {
        protected override Version UseVersion => HttpVersion.Version11;
        public HttpMetricsTest_Http11(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task RequestDuration_EnrichmentHandler_ContentLengthError_Recorded()
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpMessageInvoker client = CreateHttpMessageInvoker(new EnrichmentHandler(Handler));
                using InstrumentRecorder<double> recorder = SetupInstrumentRecorder<double>(InstrumentNames.RequestDuration);
                using HttpRequestMessage request = new(HttpMethod.Get, uri) { Version = UseVersion };

                if (TestHttpMessageInvoker)
                {
                    using HttpResponseMessage response = await SendAsync(client, request);
                }
                else
                {
                    await Assert.ThrowsAsync<HttpRequestException>(async () =>
                    {
                        using HttpResponseMessage response = await SendAsync(client, request);
                    });
                }
                
                Measurement<double> m = recorder.GetMeasurements().Single();
                VerifyRequestDuration(m, uri, ExpectedProtocolString, 200); ;
                Assert.Equal("before!", m.Tags.ToArray().Single(t => t.Key == "before").Value);

            }, server => server.HandleRequestAsync(headers: new[] {
                new HttpHeaderData("Content-Length", "1000")
            }, content: "x"));
        }

        [Fact]
        public async Task FailedRequests_ConnectionClosedWhileReceivingHeaders_Recorded()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(30);
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpMessageInvoker client = CreateHttpMessageInvoker();
                using InstrumentRecorder<long> recorder = SetupInstrumentRecorder<long>(InstrumentNames.FailedRequests);
                using HttpRequestMessage request = new(HttpMethod.Get, uri) { Version = UseVersion };

                await Assert.ThrowsAsync<HttpRequestException>(async () =>
                {
                    using HttpResponseMessage response = await SendAsync(client, request);
                }).WaitAsync(timeout);

                Measurement<long> m = recorder.GetMeasurements().Single();
                VerifyFailedRequests(m, 1, uri, null, null);
            }, async server =>
            {
                var connection = (LoopbackServer.Connection)await server.EstablishGenericConnectionAsync().WaitAsync(timeout);
                connection.Socket.Close();
            });
        }
    }

    public class HttpMetricsTest_Http11_Async : HttpMetricsTest_Http11
    {
        public HttpMetricsTest_Http11_Async(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RequestDuration_HttpVersionDowngrade_LogsActualProtocol(bool malformedResponse)
        {
            await LoopbackServer.CreateServerAsync(async server =>
            {
                using HttpMessageInvoker client = CreateHttpMessageInvoker();
                using InstrumentRecorder<double> recorder = SetupInstrumentRecorder<double>(InstrumentNames.RequestDuration);
                using HttpRequestMessage request = new(HttpMethod.Get, server.Address)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
                };

                Task<HttpResponseMessage> clientTask = SendAsync(client, request);

                await server.AcceptConnectionAsync(async connection =>
                {
                    if (malformedResponse)
                    {
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync("!malformed!");
                    }
                    else
                    {
                        await connection.ReadRequestHeaderAndSendResponseAsync();
                    }

                });

                if (malformedResponse)
                {
                    await Assert.ThrowsAsync<HttpRequestException>(() => clientTask);
                    Measurement<double> m = recorder.GetMeasurements().Single();
                    VerifyRequestDuration(m, server.Address, null, null); // Protocol is not logged.
                }
                else
                {
                    using HttpResponseMessage response = await clientTask;

                    Measurement<double> m = recorder.GetMeasurements().Single();
                    VerifyRequestDuration(m, server.Address, "HTTP/1.1", 200);
                }

            }, new LoopbackServer.Options() { UseSsl = true });
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
    public class HttpMetricsTest_Http11_Async_HttpMessageInvoker : HttpMetricsTest_Http11_Async
    {
        protected override bool TestHttpMessageInvoker => true;
        public HttpMetricsTest_Http11_Async_HttpMessageInvoker(ITestOutputHelper output) : base(output)
        {
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
    public class HttpMetricsTest_Http11_Sync : HttpMetricsTest_Http11
    {
        protected override bool TestAsync => false;
        public HttpMetricsTest_Http11_Sync(ITestOutputHelper output) : base(output)
        {
        }
    }

    [ConditionalClass(typeof(HttpMetricsTest_Http20), nameof(IsEnabled))]
    public class HttpMetricsTest_Http20 : HttpMetricsTest
    {
        public static bool IsEnabled = PlatformDetection.IsNotMobile && PlatformDetection.SupportsAlpn;
        protected override Version UseVersion => HttpVersion.Version20;
        public HttpMetricsTest_Http20(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact(nameof(SupportsSeparateHttpSpansForRedirects))]
        public Task RequestDuration_Redirect_RecordedForEachHttpSpan()
        {
            return GetFactoryForVersion(HttpVersion.Version11).CreateServerAsync((originalServer, originalUri) =>
            {
                return GetFactoryForVersion(HttpVersion.Version20).CreateServerAsync(async (redirectServer, redirectUri) =>
                {
                    using HttpMessageInvoker client = CreateHttpMessageInvoker();
                    using InstrumentRecorder<double> recorder = SetupInstrumentRecorder<double>(InstrumentNames.RequestDuration);
                    using HttpRequestMessage request = new(HttpMethod.Get, originalUri) { Version = HttpVersion.Version20 };

                    Task clientTask = SendAsync(client, request);
                    Task serverTask = originalServer.HandleRequestAsync(HttpStatusCode.Redirect, new[] { new HttpHeaderData("Location", redirectUri.AbsoluteUri) });

                    await Task.WhenAny(clientTask, serverTask);
                    Assert.False(clientTask.IsCompleted, $"{clientTask.Status}: {clientTask.Exception}");
                    await serverTask;

                    serverTask = redirectServer.HandleRequestAsync();
                    await TestHelper.WhenAllCompletedOrAnyFailed(clientTask, serverTask);
                    await clientTask;

                    Assert.Collection(recorder.GetMeasurements(), m0 =>
                    {
                        VerifyRequestDuration(m0, originalUri, $"HTTP/1.1", (int)HttpStatusCode.Redirect);
                    }, m1 =>
                    {
                        VerifyRequestDuration(m1, redirectUri, $"HTTP/2", (int)HttpStatusCode.OK);
                    });

                }, options: new GenericLoopbackOptions() { UseSsl = true });
            }, options: new GenericLoopbackOptions() { UseSsl = false});
        }

        [Fact]
        public async Task RequestDuration_ProtocolError_Recorded()
        {
            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            using HttpMessageInvoker client = CreateHttpMessageInvoker();
            using InstrumentRecorder<double> recorder = SetupInstrumentRecorder<double>(InstrumentNames.RequestDuration);

            using HttpRequestMessage request = new(HttpMethod.Get, server.Address) { Version = HttpVersion.Version20 };
            Task<HttpResponseMessage> sendTask = SendAsync(client, request);

            Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
            int streamId = await connection.ReadRequestHeaderAsync();

            // Send a reset stream frame so that the stream moves to a terminal state.
            RstStreamFrame resetStream = new RstStreamFrame(FrameFlags.None, (int)ProtocolErrors.INTERNAL_ERROR, streamId);
            await connection.WriteFrameAsync(resetStream);

            await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                using HttpResponseMessage response = await sendTask;
            });

            Measurement<double> m = recorder.GetMeasurements().Single();
            VerifyRequestDuration(m, server.Address, null, null); // Protocol is not recorded
        }

        [Fact]
        public async Task FailedRequests_ProtocolError_Recorded()
        {
            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            using HttpMessageInvoker client = CreateHttpMessageInvoker();
            using InstrumentRecorder<long> recorder = SetupInstrumentRecorder<long>(InstrumentNames.FailedRequests);

            using HttpRequestMessage request = new(HttpMethod.Get, server.Address) { Version = HttpVersion.Version20 };
            Task<HttpResponseMessage> sendTask = SendAsync(client, request);

            Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
            int streamId = await connection.ReadRequestHeaderAsync();

            // Send a reset stream frame so that the stream moves to a terminal state.
            RstStreamFrame resetStream = new RstStreamFrame(FrameFlags.None, (int)ProtocolErrors.INTERNAL_ERROR, streamId);
            await connection.WriteFrameAsync(resetStream);

            await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                using HttpResponseMessage response = await sendTask;
            });

            Measurement<long> m = recorder.GetMeasurements().Single();
            VerifyFailedRequests(m, 1, server.Address, null, null);
        }
    }

    public class HttpMetricsTest_Http20_HttpMessageInvoker : HttpMetricsTest_Http20
    {
        protected override bool TestHttpMessageInvoker => true;
        public HttpMetricsTest_Http20_HttpMessageInvoker(ITestOutputHelper output) : base(output)
        {
        }
    }

    [Collection(nameof(DisableParallelization))]
    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsQuicSupported))]
    public class HttpMetricsTest_Http30 : HttpMetricsTest
    {
        protected override Version UseVersion => HttpVersion.Version30;
        public HttpMetricsTest_Http30(ITestOutputHelper output) : base(output)
        {
        }
    }

    public class HttpMetricsTest_Http30_HttpMessageInvoker : HttpMetricsTest_Http30
    {
        protected override bool TestHttpMessageInvoker => true;
        public HttpMetricsTest_Http30_HttpMessageInvoker(ITestOutputHelper output) : base(output)
        {
        }
    }

    // Make sure the instruments are working with the default Meter.
    public class HttpMetricsTest_DefaultMeter : HttpMetricsTestBase
    {
        public HttpMetricsTest_DefaultMeter(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CurrentRequests_Success_Recorded()
        {
            RemoteExecutor.Invoke(static async Task () =>
            {
                using HttpMetricsTest_DefaultMeter test = new(null);
                await test.LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
                {
                    using HttpClient client = test.CreateHttpClient();
                    using InstrumentRecorder<long> recorder = new InstrumentRecorder<long>(InstrumentNames.CurrentRequests);
                    using HttpRequestMessage request = new(HttpMethod.Get, uri) { Version = test.UseVersion };

                    HttpResponseMessage response = await client.SendAsync(request);
                    response.Dispose(); // Make sure disposal doesn't interfere with recording by enforcing early disposal.

                    Assert.Collection(recorder.GetMeasurements(),
                        m => VerifyCurrentRequest(m, 1, uri),
                        m => VerifyCurrentRequest(m, -1, uri));
                }, async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync();
                });
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void RequestDuration_Success_Recorded()
        {
            RemoteExecutor.Invoke(static async Task () =>
            {
                using HttpMetricsTest_DefaultMeter test = new(null);
                await test.LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
                {
                    using HttpClient client = test.CreateHttpClient();
                    using InstrumentRecorder<double> recorder = new InstrumentRecorder<double>(InstrumentNames.RequestDuration);
                    using HttpRequestMessage request = new(HttpMethod.Get, uri) { Version = test.UseVersion };

                    using HttpResponseMessage response = await client.SendAsync(request);
                    Measurement<double> m = recorder.GetMeasurements().Single();
                    VerifyRequestDuration(m, uri, "HTTP/1.1", (int)HttpStatusCode.OK, "GET");
                }, async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.OK);
                });
            }).Dispose();
        }
    }

    public class HttpMetricsTest_General
    {
        [ConditionalFact(typeof(SocketsHttpHandler), nameof(SocketsHttpHandler.IsSupported))]
        public void SocketsHttpHandler_Dispose_DoesNotDisposeMeterFactory()
        {
            using TestMeterFactory factory = new();
            SocketsHttpHandler handler = new()
            {
                MeterFactory = factory
            };
            handler.Dispose();
            Assert.False(factory.IsDisposed);
        }

        [Fact]
        public void HttpClientHandler_Dispose_DoesNotDisposeMeter()
        {
            using TestMeterFactory factory = new();
            HttpClientHandler handler = new()
            {
                MeterFactory = factory
            };
            handler.Dispose();
            Assert.False(factory.IsDisposed);
        }

        [Fact]
        public void HttpClientHandler_SetMeterFactoryAfterDispose_ThrowsObjectDisposedException()
        {
            HttpClientHandler handler = new();
            handler.Dispose();
            Assert.ThrowsAny<ObjectDisposedException>(() => handler.MeterFactory = new TestMeterFactory());
        }

        [ConditionalFact(typeof(SocketsHttpHandler), nameof(SocketsHttpHandler.IsSupported))]
        public void SocketsHttpHandler_SetMeterFactoryAfterDispose_ThrowsObjectDisposedException()
        {
            SocketsHttpHandler handler = new();
            handler.Dispose();
            Assert.ThrowsAny<ObjectDisposedException>(() => handler.MeterFactory = new TestMeterFactory());
        }

        [Fact]
        public void HttpClientHandler_SetMeterFactoryAfterStart_ThrowsInvalidOperationException()
        {
            Http11LoopbackServerFactory.Singleton.CreateClientAndServerAsync(async uri =>
            {
                using HttpClientHandler handler = new();
                using HttpClient client = new HttpClient(handler);
                await client.GetAsync(uri);

                Assert.Throws<InvalidOperationException>(() => handler.MeterFactory = new TestMeterFactory());
            }, server => server.AcceptConnectionSendResponseAndCloseAsync());
        }

        [ConditionalFact(typeof(SocketsHttpHandler), nameof(SocketsHttpHandler.IsSupported))]
        public void SocketsHttpHandler_SetMeterFactoryAfterStart_ThrowsInvalidOperationException()
        {
            Http11LoopbackServerFactory.Singleton.CreateClientAndServerAsync(async uri =>
            {
                using SocketsHttpHandler handler = new();
                using HttpClient client = new HttpClient(handler);
                await client.GetAsync(uri);

                Assert.Throws<InvalidOperationException>(() => handler.MeterFactory = new TestMeterFactory());
            }, server => server.AcceptConnectionSendResponseAndCloseAsync());
        }
    }

    internal sealed class TestMeterFactory : IMeterFactory
    {
        private Meter? _meter;
        public bool IsDisposed => _meter is null;

        public TestMeterFactory() => _meter = new Meter("System.Net.Http", null, null, this);
        public Meter Create(MeterOptions options)
        {
            Assert.Equal("System.Net.Http", options.Name);
            Assert.Same(this, options.Scope);
            Assert.False(IsDisposed);

            return _meter;
        }
        public void Dispose()
        {
            _meter?.Dispose();
            _meter = null;
        }
    }

}
