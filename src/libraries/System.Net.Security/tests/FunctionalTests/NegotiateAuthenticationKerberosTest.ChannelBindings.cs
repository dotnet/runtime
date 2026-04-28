// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security.Kerberos;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    public partial class NegotiateAuthenticationKerberosTest
    {
        [Fact]
        public Task ChannelBindings_Matching_Endpoint_Succeeds() => RunMatchingTest(ChannelBindingKind.Endpoint);

        [Fact]
        public Task ChannelBindings_Matching_Unique_Succeeds() => RunMatchingTest(ChannelBindingKind.Unique);

        [Fact]
        public Task ChannelBindings_Mismatched_Endpoint_FailsWithBadBinding() => RunMismatchTest(ChannelBindingKind.Endpoint);

        [Fact]
        public Task ChannelBindings_Mismatched_Unique_FailsWithBadBinding() => RunMismatchTest(ChannelBindingKind.Unique);

        private async Task RunMatchingTest(ChannelBindingKind kind)
        {
            using var kerberosExecutor = new KerberosExecutor(_testOutputHelper, "LINUX.CONTOSO.COM");

            kerberosExecutor.AddService("HTTP/linux.contoso.com");
            kerberosExecutor.AddUser("user");

            if (kind == ChannelBindingKind.Endpoint)
            {
                await kerberosExecutor.Invoke(static () => RunMatching(ChannelBindingKind.Endpoint));
            }
            else
            {
                await kerberosExecutor.Invoke(static () => RunMatching(ChannelBindingKind.Unique));
            }
        }

        private async Task RunMismatchTest(ChannelBindingKind kind)
        {
            using var kerberosExecutor = new KerberosExecutor(_testOutputHelper, "LINUX.CONTOSO.COM");

            kerberosExecutor.AddService("HTTP/linux.contoso.com");
            kerberosExecutor.AddUser("user");

            if (kind == ChannelBindingKind.Endpoint)
            {
                await kerberosExecutor.Invoke(static () => RunMismatch(ChannelBindingKind.Endpoint));
            }
            else
            {
                await kerberosExecutor.Invoke(static () => RunMismatch(ChannelBindingKind.Unique));
            }
        }

        private static void RunMatching(ChannelBindingKind kind)
        {
            using SafeChannelBindingHandle clientBinding = CreateChannelBinding(kind, hashSeed: 0x42);
            using SafeChannelBindingHandle serverBinding = CreateChannelBinding(kind, hashSeed: 0x42);

            NegotiateAuthentication client = new(new NegotiateAuthenticationClientOptions
            {
                Credential = new NetworkCredential("user", KerberosExecutor.DefaultUserPassword, "LINUX.CONTOSO.COM"),
                TargetName = "HTTP/linux.contoso.com",
                Binding = clientBinding,
            });
            NegotiateAuthentication server = new(new NegotiateAuthenticationServerOptions
            {
                Binding = serverBinding,
            });

            RunExchange(client, server, expectAuthenticated: true, out _, out _);

            Assert.True(client.IsAuthenticated);
            Assert.True(server.IsAuthenticated);
        }

        private static void RunMismatch(ChannelBindingKind kind)
        {
            using SafeChannelBindingHandle clientBinding = CreateChannelBinding(kind, hashSeed: 0x42);
            using SafeChannelBindingHandle serverBinding = CreateChannelBinding(kind, hashSeed: 0x99);

            NegotiateAuthentication client = new(new NegotiateAuthenticationClientOptions
            {
                Credential = new NetworkCredential("user", KerberosExecutor.DefaultUserPassword, "LINUX.CONTOSO.COM"),
                TargetName = "HTTP/linux.contoso.com",
                Binding = clientBinding,
            });
            NegotiateAuthentication server = new(new NegotiateAuthenticationServerOptions
            {
                Binding = serverBinding,
            });

            RunExchange(client, server, expectAuthenticated: false, out _, out NegotiateAuthenticationStatusCode lastServerStatus);

            Assert.Equal(NegotiateAuthenticationStatusCode.BadBinding, lastServerStatus);
        }

        private static SafeChannelBindingHandle CreateChannelBinding(ChannelBindingKind kind, byte hashSeed)
        {
            const int HashLength = 32;
            SafeChannelBindingHandle handle = new SafeChannelBindingHandle(kind);
            byte[] hash = new byte[HashLength];
            Array.Fill(hash, hashSeed);
            handle.SetCertHash(hash);
            return handle;
        }

        private static void RunExchange(
            NegotiateAuthentication client,
            NegotiateAuthentication server,
            bool expectAuthenticated,
            out NegotiateAuthenticationStatusCode lastClientStatus,
            out NegotiateAuthenticationStatusCode lastServerStatus)
        {
            byte[]? serverBlob = null;
            byte[]? clientBlob;
            lastClientStatus = NegotiateAuthenticationStatusCode.ContinueNeeded;
            lastServerStatus = NegotiateAuthenticationStatusCode.ContinueNeeded;

            const int MaxIterations = 20;
            for (int i = 0; i < MaxIterations; i++)
            {
                clientBlob = client.GetOutgoingBlob(serverBlob, out lastClientStatus);
                if (lastClientStatus >= NegotiateAuthenticationStatusCode.GenericFailure)
                {
                    return;
                }
                if (clientBlob is null)
                {
                    return;
                }

                serverBlob = server.GetOutgoingBlob(clientBlob, out lastServerStatus);
                if (lastServerStatus >= NegotiateAuthenticationStatusCode.GenericFailure)
                {
                    return;
                }
                if (lastServerStatus == NegotiateAuthenticationStatusCode.Completed &&
                    lastClientStatus == NegotiateAuthenticationStatusCode.Completed)
                {
                    return;
                }
            }

            if (expectAuthenticated)
            {
                Assert.Fail("Authentication did not complete within iteration limit.");
            }
        }
    }
}