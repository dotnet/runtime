// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class SocksProxyTest : HttpClientHandlerTestBase
    {
        public SocksProxyTest(ITestOutputHelper helper) : base(helper) { }

        private static string[] Hosts(string socksScheme) => socksScheme == "socks5"
            ? new[] { "localhost", "127.0.0.1", "::1" }
            : new[] { "localhost", "127.0.0.1" };

        public static IEnumerable<object[]> TestLoopbackAsync_MemberData() =>
            from scheme in new[] { "socks4", "socks4a", "socks5" }
            from useSsl in BoolValues
            from useAuth in BoolValues
            from host in Hosts(scheme)
            select new object[] { scheme, useSsl, useAuth, host };

        [Theory]
        [MemberData(nameof(TestLoopbackAsync_MemberData))]
        public async Task TestLoopbackAsync(string scheme, bool useSsl, bool useAuth, string host)
        {
            if (useSsl && UseVersion == HttpVersion.Version20 && !PlatformDetection.SupportsAlpn)
            {
                return;
            }

            List<(int, long)> times = new();
            Stopwatch s = Stopwatch.StartNew();

            try
            {
                times.Add((0, s.ElapsedMilliseconds));
                await LoopbackServerFactory.CreateClientAndServerAsync(
                    async uri =>
                    {
                        times.Add((10, s.ElapsedMilliseconds));
                        LoopbackSocksServer proxy = useAuth ? LoopbackSocksServer.Create("DOTNET", "424242") : LoopbackSocksServer.Create();
                        try
                        {
                            times.Add((11, s.ElapsedMilliseconds));
                            HttpClientHandler handler = CreateHttpClientHandler();
                            try
                            {
                                times.Add((12, s.ElapsedMilliseconds));
                                HttpClient client = CreateHttpClient(handler);
                                try
                                {
                                    times.Add((13, s.ElapsedMilliseconds));

                                    handler.Proxy = new WebProxy($"{scheme}://127.0.0.1:{proxy.Port}");
                                    handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

                                    if (useAuth)
                                    {
                                        handler.Proxy.Credentials = new NetworkCredential("DOTNET", "424242");
                                    }

                                    uri = new UriBuilder(uri) { Host = host }.Uri;

                                    HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);

                                    times.Add((14, s.ElapsedMilliseconds));

                                    HttpResponseMessage response = await client.SendAsync(TestAsync, request);
                                    try
                                    {
                                        times.Add((15, s.ElapsedMilliseconds));
                                        string responseString = await response.Content.ReadAsStringAsync();
                                        times.Add((16, s.ElapsedMilliseconds));
                                        Assert.Equal("Echo", responseString);
                                        times.Add((17, s.ElapsedMilliseconds));
                                    }
                                    catch
                                    {
                                        times.Add((43, s.ElapsedMilliseconds));
                                        throw;
                                    }
                                    finally
                                    {
                                        times.Add((36, s.ElapsedMilliseconds));
                                        response.Dispose();
                                        times.Add((37, s.ElapsedMilliseconds));
                                    }
                                }
                                catch
                                {
                                    times.Add((42, s.ElapsedMilliseconds));
                                    throw;
                                }
                                finally
                                {
                                    times.Add((34, s.ElapsedMilliseconds));
                                    client.Dispose();
                                    times.Add((35, s.ElapsedMilliseconds));
                                }
                            }
                            catch
                            {
                                times.Add((41, s.ElapsedMilliseconds));
                                throw;
                            }
                            finally
                            {
                                times.Add((32, s.ElapsedMilliseconds));
                                //handler.Dispose();
                                times.Add((33, s.ElapsedMilliseconds));
                            }
                        }
                        catch
                        {
                            times.Add((40, s.ElapsedMilliseconds));
                            throw;
                        }
                        finally
                        {
                            times.Add((30, s.ElapsedMilliseconds));
                            proxy.Dispose();
                            times.Add((31, s.ElapsedMilliseconds));
                        }
                    },
                    async server =>
                    {
                        times.Add((20, s.ElapsedMilliseconds));
                        await server.HandleRequestAsync(content: "Echo");
                        times.Add((21, s.ElapsedMilliseconds));
                    },
                    options: new GenericLoopbackOptions
                    {
                        UseSsl = useSsl,
                        Address = host == "::1" ? IPAddress.IPv6Loopback : IPAddress.Loopback
                    });
            }
            catch (Exception ex)
            {
                times.Add((1, s.ElapsedMilliseconds));
                throw new Exception(string.Join('\n', times.Select(t => $"{t.Item1,2} {t.Item2}")), ex);
            }
        }

        public static IEnumerable<object[]> TestExceptionalAsync_MemberData()
        {
            foreach (string scheme in new[] { "socks4", "socks4a" })
            {
                yield return new object[] { scheme, "[::1]", false, null, "SOCKS4 does not support IPv6 addresses." };
                yield return new object[] { scheme, "localhost", true, null, "Failed to authenticate with the SOCKS server." };
                yield return new object[] { scheme, "localhost", true, new NetworkCredential("bad_username", "bad_password"), "Failed to authenticate with the SOCKS server." };
                yield return new object[] { scheme, "localhost", true, new NetworkCredential(new string('a', 256), "foo"), "Encoding the UserName took more than the maximum of 255 bytes." };
            }

            yield return new object[] { "socks4", new string('a', 256), false, null, "Failed to resolve the destination host to an IPv4 address." };

            foreach (string scheme in new[] { "socks4a", "socks5" })
            {
                yield return new object[] { scheme, new string('a', 256), false, null, "Encoding the host took more than the maximum of 255 bytes." };
            }

            yield return new object[] { "socks5", "localhost", true, null, "SOCKS server did not return a suitable authentication method." };
            yield return new object[] { "socks5", "localhost", true, new NetworkCredential("bad_username", "bad_password"), "Failed to authenticate with the SOCKS server." };
            yield return new object[] { "socks5", "localhost", true, new NetworkCredential(new string('a', 256), "foo"), "Encoding the UserName took more than the maximum of 255 bytes." };
            yield return new object[] { "socks5", "localhost", true, new NetworkCredential("foo", new string('a', 256)), "Encoding the Password took more than the maximum of 255 bytes." };
        }

        [Theory]
        [MemberData(nameof(TestExceptionalAsync_MemberData))]
        public async Task TestExceptionalAsync(string scheme, string host, bool useAuth, ICredentials? credentials, string exceptionMessage)
        {
            using LoopbackSocksServer proxy = useAuth ? LoopbackSocksServer.Create("DOTNET", "424242") : LoopbackSocksServer.Create();
            using HttpClientHandler handler = CreateHttpClientHandler();
            using HttpClient client = CreateHttpClient(handler);

            handler.Proxy = new WebProxy($"{scheme}://127.0.0.1:{proxy.Port}")
            {
                Credentials = credentials
            };

            HttpRequestMessage request = CreateRequest(HttpMethod.Get, new Uri($"http://{host}/"), UseVersion, exactVersion: true);

            // SocksException is not public
            var ex = await Assert.ThrowsAnyAsync<HttpRequestException>(() => client.SendAsync(TestAsync, request));
            var innerException = ex.InnerException;
            Assert.Equal(exceptionMessage, innerException.Message);
            Assert.Equal("SocksException", innerException.GetType().Name);
        }
    }


    [SkipOnPlatform(TestPlatforms.Browser, "UseProxy not supported on Browser")]
    public sealed class SocksProxyTest_Http1_Async : SocksProxyTest
    {
        public SocksProxyTest_Http1_Async(ITestOutputHelper helper) : base(helper) { }
        protected override Version UseVersion => HttpVersion.Version11;
    }

    [SkipOnPlatform(TestPlatforms.Browser, "UseProxy not supported on Browser")]
    public sealed class SocksProxyTest_Http1_Sync : SocksProxyTest
    {
        public SocksProxyTest_Http1_Sync(ITestOutputHelper helper) : base(helper) { }
        protected override Version UseVersion => HttpVersion.Version11;
        protected override bool TestAsync => false;
    }

    [SkipOnPlatform(TestPlatforms.Browser, "UseProxy not supported on Browser")]
    public sealed class SocksProxyTest_Http2 : SocksProxyTest
    {
        public SocksProxyTest_Http2(ITestOutputHelper helper) : base(helper) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }
}
