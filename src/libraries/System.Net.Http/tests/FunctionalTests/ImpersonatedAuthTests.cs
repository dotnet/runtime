// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Principal;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public class ImpersonatedAuthTests: IClassFixture<WindowsIdentityFixture>
    {
        private readonly WindowsIdentityFixture _fixture;
        private readonly ITestOutputHelper _output;

        public  ImpersonatedAuthTests(WindowsIdentityFixture windowsIdentityFixture, ITestOutputHelper output)
        {
            _output = output;
            _fixture = windowsIdentityFixture;

            Assert.False(_fixture.TestAccount.AccountTokenHandle.IsInvalid);
            Assert.False(string.IsNullOrEmpty(_fixture.TestAccount.AccountName));
        }

        [OuterLoop]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.CanRunImpersonatedTests))]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task DefaultHandler_ImpersonatedUser_Success(bool useNtlm)
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                    requestMessage.Version = new Version(1, 1);

                    var handler = new HttpClientHandler();
                    handler.UseDefaultCredentials = true;

                    using (var client = new HttpClient(handler))
                    {
                        HttpResponseMessage response = await client.SendAsync(requestMessage);
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal("foo", await response.Content.ReadAsStringAsync());

                        string initialUser = response.Headers.GetValues(NtAuthTests.UserHeaderName).First();

                        using (WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent())
                        {
                            _output.WriteLine($"Starting test as {currentIdentity.Name}");
                        }

                        // get token and run another request as different user.
                        WindowsIdentity.RunImpersonated(_fixture.TestAccount.AccountTokenHandle, () =>
                        {
                            using (WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent())
                            {
                                _output.WriteLine($"Running test as {currentIdentity.Name}");
                                Assert.Equal(_fixture.TestAccount.AccountName, currentIdentity.Name);
                            }

                            requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                            requestMessage.Version = new Version(1, 1);

                            HttpResponseMessage response = client.SendAsync(requestMessage).GetAwaiter().GetResult();
                            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                            Assert.Equal("foo", response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

                            string newUser = response.Headers.GetValues(NtAuthTests.UserHeaderName).First();
                            Assert.Equal(_fixture.TestAccount.AccountName, newUser);
                        });
                    }
                },
                async server =>
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        Task t = useNtlm ? NtAuthTests.HandleNtlmAuthenticationRequest(connection, closeConnection: false) : NtAuthTests.HandleNegotiateAuthenticationRequest(connection, closeConnection: false);
                        await t;
                        _output.WriteLine("Finished first request");

                        // Second request should use new connection as it runs as different user.
                        // We keep first connection open so HttpClient may be tempted top use it.
                        await server.AcceptConnectionAsync(async connection =>
                        {
                            Task t = useNtlm ? NtAuthTests.HandleNtlmAuthenticationRequest(connection, closeConnection: false) : NtAuthTests.HandleNegotiateAuthenticationRequest(connection, closeConnection: false);
                            await t;
                        }).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                });

        }
    }
}
