﻿#region Copyright notice and license

// Copyright 2015-2016 gRPC authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Testing;
using Empty = Grpc.Testing.Empty;
using System.Security.Authentication;

namespace Grpc.Shared.TestAssets
{
    public class ClientOptions
    {
        public string? ClientType { get; set; }
        public string? ServerHost { get; set; }
        public string? ServerHostOverride { get; set; }
        public int ServerPort { get; set; }
        public string? TestCase { get; set; }
        public bool UseTls { get; set; }
        public bool UseTestCa { get; set; }
        public string? DefaultServiceAccount { get; set; }
        public string? OAuthScope { get; set; }
        public string? ServiceAccountKeyFile { get; set; }
        public bool UseHttp3 { get; set; }
    }

    public class InteropClient
    {
        internal const string CompressionRequestAlgorithmMetadataKey = "grpc-internal-encoding-request";

        private readonly ClientOptions options;

        public InteropClient(ClientOptions options)
        {
            this.options = options;
        }

        public async Task Run()
        {
            var channel = HttpClientCreateChannel();

            var message = "Running " + options.TestCase;
            await RunTestCaseAsync(channel, options);

            await channel.ShutdownAsync();
        }

        private IChannelWrapper HttpClientCreateChannel()
        {
            var credentials = CreateCredentials(useTestCaOverride: false);

            string scheme;
            if (!options.UseTls)
            {
                scheme = "http";
            }
            else
            {
                scheme = "https";
            }

            HttpMessageHandler httpMessageHandler = CreateHttpClientHandler();
            if (options.UseHttp3)
            {
#if NET6_0_OR_GREATER
                httpMessageHandler = new Http3DelegatingHandler(httpMessageHandler);
#else
                throw new Exception("HTTP/3 requires .NET 6 or later.");
#endif
            }

            var channel = GrpcChannel.ForAddress($"{scheme}://{options.ServerHost}:{options.ServerPort}", new GrpcChannelOptions
            {
                Credentials = credentials,
                HttpHandler = httpMessageHandler,
            });

            return new GrpcChannelWrapper(channel);
        }

#if NET6_0_OR_GREATER
        private class Http3DelegatingHandler : DelegatingHandler
        {
            private static readonly Version Http3Version = new Version(3, 0);

            public Http3DelegatingHandler(HttpMessageHandler innerHandler)
            {
                InnerHandler = innerHandler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Version = Http3Version;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
                return base.SendAsync(request, cancellationToken);
            }
        }
#endif

        private HttpClientHandler CreateHttpClientHandler()
        {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (request, cert, chain, sslPolicyErrors) => true
            };
        }

        private ChannelCredentials CreateCredentials(bool? useTestCaOverride = null)
            => options.UseTls ? new SslCredentials() : ChannelCredentials.Insecure;

        private bool IsHttpClient() => string.Equals(options.ClientType, "httpclient", StringComparison.OrdinalIgnoreCase);

        private static TClient CreateClient<TClient>(IChannelWrapper channel) where TClient : ClientBase
        {
            return (TClient)Activator.CreateInstance(typeof(TClient), channel.Channel)!;
        }

        public static IEnumerable<string> TestNames => Tests.Keys;

        private static readonly Dictionary<string, Func<IChannelWrapper, ClientOptions, Task>> Tests = new Dictionary<string, Func<IChannelWrapper, ClientOptions, Task>>
        {
            ["empty_unary"] = RunEmptyUnary,
            ["large_unary"] = RunLargeUnary,
            ["client_streaming"] = RunClientStreamingAsync,
            ["server_streaming"] = RunServerStreamingAsync,
            ["ping_pong"] = RunPingPongAsync,
            ["empty_stream"] = RunEmptyStreamAsync,
            ["compute_engine_creds"] = RunComputeEngineCreds,
            ["cancel_after_begin"] = RunCancelAfterBeginAsync,
            ["cancel_after_first_response"] = RunCancelAfterFirstResponseAsync,
            ["timeout_on_sleeping_server"] = RunTimeoutOnSleepingServerAsync,
            ["custom_metadata"] = RunCustomMetadataAsync,
            ["status_code_and_message"] = RunStatusCodeAndMessageAsync,
            ["special_status_message"] = RunSpecialStatusMessageAsync,
            ["unimplemented_service"] = RunUnimplementedService,
            ["unimplemented_method"] = RunUnimplementedMethod,
            ["client_compressed_unary"] = RunClientCompressedUnary,
            ["client_compressed_streaming"] = RunClientCompressedStreamingAsync,
            ["server_compressed_unary"] = RunServerCompressedUnary,
            ["server_compressed_streaming"] = RunServerCompressedStreamingAsync
        };

        private async Task RunTestCaseAsync(IChannelWrapper channel, ClientOptions options)
        {
            if (!Tests.TryGetValue(options.TestCase!, out var test))
            {
                throw new ArgumentException("Unknown test case " + options.TestCase);
            }

            await test(channel, options);
        }

        public static async Task RunEmptyUnary(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            var response = await client.EmptyCallAsync(new Empty());
            Assert.IsNotNull(response);
        }

        public static async Task RunLargeUnary(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            var request = new SimpleRequest
            {
                ResponseSize = 314159,
                Payload = CreateZerosPayload(271828)
            };
            var response = await client.UnaryCallAsync(request);

            Assert.AreEqual(314159, response.Payload.Body.Length);
        }

        public static async Task RunClientStreamingAsync(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            var bodySizes = new List<int> { 27182, 8, 1828, 45904 }.Select((size) => new StreamingInputCallRequest { Payload = CreateZerosPayload(size) });

            using (var call = client.StreamingInputCall())
            {
                await call.RequestStream.WriteAllAsync(bodySizes);

                var response = await call.ResponseAsync;
                Assert.AreEqual(74922, response.AggregatedPayloadSize);
            }
        }

        public static async Task RunServerStreamingAsync(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            var bodySizes = new List<int> { 31415, 9, 2653, 58979 };

            var request = new StreamingOutputCallRequest
            {
                ResponseParameters = { bodySizes.Select((size) => new ResponseParameters { Size = size }) }
            };

            using (var call = client.StreamingOutputCall(request))
            {
                var responseList = await call.ResponseStream.ToListAsync();
                CollectionAssert.AreEqual(bodySizes, responseList.Select((item) => item.Payload.Body.Length).ToList());
            }
        }

        public static async Task RunPingPongAsync(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            using (var call = client.FullDuplexCall())
            {
                await call.RequestStream.WriteAsync(new StreamingOutputCallRequest
                {
                    ResponseParameters = { new ResponseParameters { Size = 31415 } },
                    Payload = CreateZerosPayload(27182)
                });

                Assert.IsTrue(await call.ResponseStream.MoveNext());
                Assert.AreEqual(31415, call.ResponseStream.Current.Payload.Body.Length);

                await call.RequestStream.WriteAsync(new StreamingOutputCallRequest
                {
                    ResponseParameters = { new ResponseParameters { Size = 9 } },
                    Payload = CreateZerosPayload(8)
                });

                Assert.IsTrue(await call.ResponseStream.MoveNext());
                Assert.AreEqual(9, call.ResponseStream.Current.Payload.Body.Length);

                await call.RequestStream.WriteAsync(new StreamingOutputCallRequest
                {
                    ResponseParameters = { new ResponseParameters { Size = 2653 } },
                    Payload = CreateZerosPayload(1828)
                });

                Assert.IsTrue(await call.ResponseStream.MoveNext());
                Assert.AreEqual(2653, call.ResponseStream.Current.Payload.Body.Length);

                await call.RequestStream.WriteAsync(new StreamingOutputCallRequest
                {
                    ResponseParameters = { new ResponseParameters { Size = 58979 } },
                    Payload = CreateZerosPayload(45904)
                });

                Assert.IsTrue(await call.ResponseStream.MoveNext());
                Assert.AreEqual(58979, call.ResponseStream.Current.Payload.Body.Length);

                await call.RequestStream.CompleteAsync();

                Assert.IsFalse(await call.ResponseStream.MoveNext());
            }
        }

        public static async Task RunEmptyStreamAsync(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            using (var call = client.FullDuplexCall())
            {
                await call.RequestStream.CompleteAsync();

                var responseList = await call.ResponseStream.ToListAsync();
                Assert.AreEqual(0, responseList.Count);
            }
        }

        public static async Task RunComputeEngineCreds(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);
            var defaultServiceAccount = options.DefaultServiceAccount!;
            var oauthScope = options.OAuthScope!;

            var request = new SimpleRequest
            {
                ResponseSize = 314159,
                Payload = CreateZerosPayload(271828),
                FillUsername = true,
                FillOauthScope = true
            };

            // not setting credentials here because they were set on channel already
            var response = await client.UnaryCallAsync(request);

            Assert.AreEqual(314159, response.Payload.Body.Length);
            Assert.IsFalse(string.IsNullOrEmpty(response.OauthScope));
            Assert.IsTrue(oauthScope.Contains(response.OauthScope));
            Assert.AreEqual(defaultServiceAccount, response.Username);
        }

        public static async Task RunCancelAfterBeginAsync(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            var cts = new CancellationTokenSource();
            using (var call = client.StreamingInputCall(cancellationToken: cts.Token))
            {
                // TODO(jtattermusch): we need this to ensure call has been initiated once we cancel it.
                await Task.Delay(1000);
                cts.Cancel();

                var ex = await Assert.ThrowsAsync<RpcException>(() => call.ResponseAsync);
                Assert.AreEqual(StatusCode.Cancelled, ex.Status.StatusCode);
            }
        }

        public static async Task RunCancelAfterFirstResponseAsync(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            var cts = new CancellationTokenSource();
            using (var call = client.FullDuplexCall(cancellationToken: cts.Token))
            {
                await call.RequestStream.WriteAsync(new StreamingOutputCallRequest
                {
                    ResponseParameters = { new ResponseParameters { Size = 31415 } },
                    Payload = CreateZerosPayload(27182)
                });

                Assert.IsTrue(await call.ResponseStream.MoveNext());
                Assert.AreEqual(31415, call.ResponseStream.Current.Payload.Body.Length);

                cts.Cancel();

                try
                {
                    // cannot use Assert.ThrowsAsync because it uses Task.Wait and would deadlock.
                    await call.ResponseStream.MoveNext();
                    Assert.Fail();
                }
                catch (RpcException ex)
                {
                    Assert.AreEqual(StatusCode.Cancelled, ex.Status.StatusCode);
                }
            }
        }

        public static async Task RunTimeoutOnSleepingServerAsync(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            var deadline = DateTime.UtcNow.AddMilliseconds(1);
            using (var call = client.FullDuplexCall(deadline: deadline))
            {
                try
                {
                    await call.RequestStream.WriteAsync(new StreamingOutputCallRequest { Payload = CreateZerosPayload(27182) });
                }
                catch (InvalidOperationException)
                {
                    // Deadline was reached before write has started. Eat the exception and continue.
                }
                catch (RpcException)
                {
                    // Deadline was reached before write has started. Eat the exception and continue.
                }

                try
                {
                    await call.ResponseStream.MoveNext();
                    Assert.Fail();
                }
                catch (RpcException ex)
                {
                    Assert.AreEqual(StatusCode.DeadlineExceeded, ex.StatusCode);
                }
            }
        }

        public static async Task RunCustomMetadataAsync(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            {
                // step 1: test unary call
                var request = new SimpleRequest
                {
                    ResponseSize = 314159,
                    Payload = CreateZerosPayload(271828)
                };

                var call = client.UnaryCallAsync(request, headers: CreateTestMetadata());
                await call.ResponseAsync;

                var responseHeaders = await call.ResponseHeadersAsync;
                var responseTrailers = call.GetTrailers();

                Assert.AreEqual("test_initial_metadata_value", responseHeaders.GetValue("x-grpc-test-echo-initial")!);
                CollectionAssert.AreEqual(new byte[] { 0xab, 0xab, 0xab }, responseTrailers.GetValueBytes("x-grpc-test-echo-trailing-bin")!);
            }

            {
                // step 2: test full duplex call
                var request = new StreamingOutputCallRequest
                {
                    ResponseParameters = { new ResponseParameters { Size = 31415 } },
                    Payload = CreateZerosPayload(27182)
                };

                var call = client.FullDuplexCall(headers: CreateTestMetadata());

                await call.RequestStream.WriteAsync(request);
                await call.RequestStream.CompleteAsync();
                await call.ResponseStream.ToListAsync();

                var responseHeaders = await call.ResponseHeadersAsync;
                var responseTrailers = call.GetTrailers();

                Assert.AreEqual("test_initial_metadata_value", responseHeaders.GetValue("x-grpc-test-echo-initial")!);
                CollectionAssert.AreEqual(new byte[] { 0xab, 0xab, 0xab }, responseTrailers.GetValueBytes("x-grpc-test-echo-trailing-bin")!);
            }
        }

        public static async Task RunStatusCodeAndMessageAsync(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            var echoStatus = new EchoStatus
            {
                Code = 2,
                Message = "test status message"
            };

            {
                // step 1: test unary call
                var request = new SimpleRequest { ResponseStatus = echoStatus };

                var e = await ExceptionAssert.ThrowsAsync<RpcException>(async () => await client.UnaryCallAsync(request));
                Assert.AreEqual(StatusCode.Unknown, e.Status.StatusCode);
                Assert.AreEqual(echoStatus.Message, e.Status.Detail);
            }
        }

        public static async Task RunSpecialStatusMessageAsync(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            var echoStatus = new EchoStatus
            {
                Code = 2,
                Message = "\t\ntest with whitespace\r\nand Unicode BMP \u263A and non-BMP \uD83D\uDE08\t\n"
            };

            try
            {
                await client.UnaryCallAsync(new SimpleRequest
                {
                    ResponseStatus = echoStatus
                });
                Assert.Fail();
            }
            catch (RpcException e)
            {
                Assert.AreEqual(StatusCode.Unknown, e.Status.StatusCode);
                Assert.AreEqual(echoStatus.Message, e.Status.Detail);
            }
        }

        public static async Task RunUnimplementedService(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<UnimplementedService.UnimplementedServiceClient>(channel);

            var e = await ExceptionAssert.ThrowsAsync<RpcException>(async () => await client.UnimplementedCallAsync(new Empty()));

            Assert.AreEqual(StatusCode.Unimplemented, e.Status.StatusCode);
        }

        public static async Task RunUnimplementedMethod(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            var e = await ExceptionAssert.ThrowsAsync<RpcException>(async () => await client.UnimplementedCallAsync(new Empty()));

            Assert.AreEqual(StatusCode.Unimplemented, e.Status.StatusCode);
        }

        public static async Task RunClientCompressedUnary(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            var probeRequest = new SimpleRequest
            {
                ExpectCompressed = new BoolValue
                {
                    Value = true  // lie about compression
                },
                ResponseSize = 314159,
                Payload = CreateZerosPayload(271828)
            };
            var e = await ExceptionAssert.ThrowsAsync<RpcException>(async () => await client.UnaryCallAsync(probeRequest, CreateClientCompressionMetadata(false)));
            Assert.AreEqual(StatusCode.InvalidArgument, e.Status.StatusCode);

            var compressedRequest = new SimpleRequest
            {
                ExpectCompressed = new BoolValue
                {
                    Value = true
                },
                ResponseSize = 314159,
                Payload = CreateZerosPayload(271828)
            };
            var response1 = await client.UnaryCallAsync(compressedRequest, CreateClientCompressionMetadata(true));
            Assert.AreEqual(314159, response1.Payload.Body.Length);

            var uncompressedRequest = new SimpleRequest
            {
                ExpectCompressed = new BoolValue
                {
                    Value = false
                },
                ResponseSize = 314159,
                Payload = CreateZerosPayload(271828)
            };
            var response2 = await client.UnaryCallAsync(uncompressedRequest, CreateClientCompressionMetadata(false));
            Assert.AreEqual(314159, response2.Payload.Body.Length);
        }

        public static async Task RunClientCompressedStreamingAsync(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            try
            {
                var probeCall = client.StreamingInputCall(CreateClientCompressionMetadata(false));
                await probeCall.RequestStream.WriteAsync(new StreamingInputCallRequest
                {
                    ExpectCompressed = new BoolValue
                    {
                        Value = true
                    },
                    Payload = CreateZerosPayload(27182)
                });

                // cannot use Assert.ThrowsAsync because it uses Task.Wait and would deadlock.
                await probeCall;
                Assert.Fail();
            }
            catch (RpcException e)
            {
                Assert.AreEqual(StatusCode.InvalidArgument, e.Status.StatusCode);
            }

            var call = client.StreamingInputCall(CreateClientCompressionMetadata(true));
            await call.RequestStream.WriteAsync(new StreamingInputCallRequest
            {
                ExpectCompressed = new BoolValue
                {
                    Value = true
                },
                Payload = CreateZerosPayload(27182)
            });

            call.RequestStream.WriteOptions = new WriteOptions(WriteFlags.NoCompress);
            await call.RequestStream.WriteAsync(new StreamingInputCallRequest
            {
                ExpectCompressed = new BoolValue
                {
                    Value = false
                },
                Payload = CreateZerosPayload(45904)
            });
            await call.RequestStream.CompleteAsync();

            var response = await call.ResponseAsync;
            Assert.AreEqual(73086, response.AggregatedPayloadSize);
        }

        public static async Task RunServerCompressedUnary(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            var request = new SimpleRequest
            {
                ResponseSize = 314159,
                Payload = CreateZerosPayload(271828),
                ResponseCompressed = new BoolValue { Value = true }
            };
            var response = await client.UnaryCallAsync(request);

            // Compression of response message is not verified because there is no API available
            Assert.AreEqual(314159, response.Payload.Body.Length);

            request = new SimpleRequest
            {
                ResponseSize = 314159,
                Payload = CreateZerosPayload(271828),
                ResponseCompressed = new BoolValue { Value = false }
            };
            response = await client.UnaryCallAsync(request);

            // Compression of response message is not verified because there is no API available
            Assert.AreEqual(314159, response.Payload.Body.Length);
        }

        public static async Task RunServerCompressedStreamingAsync(IChannelWrapper channel, ClientOptions options)
        {
            var client = CreateClient<TestService.TestServiceClient>(channel);

            var bodySizes = new List<int> { 31415, 92653 };

            var request = new StreamingOutputCallRequest
            {
                ResponseParameters = { bodySizes.Select((size) => new ResponseParameters { Size = size, Compressed = new BoolValue { Value = true } }) }
            };

            using (var call = client.StreamingOutputCall(request))
            {
                // Compression of response message is not verified because there is no API available
                var responseList = await call.ResponseStream.ToListAsync();
                CollectionAssert.AreEqual(bodySizes, responseList.Select((item) => item.Payload.Body.Length).ToList());
            }
        }

        private static Payload CreateZerosPayload(int size)
        {
            return new Payload { Body = ByteString.CopyFrom(new byte[size]) };
        }

        private static Metadata CreateClientCompressionMetadata(bool compressed)
        {
            var algorithmName = compressed ? "gzip" : "identity";
            return new Metadata
            {
                { new Metadata.Entry(CompressionRequestAlgorithmMetadataKey, algorithmName) }
            };
        }

        private static Metadata CreateTestMetadata()
        {
            return new Metadata
            {
                {"x-grpc-test-echo-initial", "test_initial_metadata_value"},
                {"x-grpc-test-echo-trailing-bin", new byte[] {0xab, 0xab, 0xab}}
            };
        }
    }
}
