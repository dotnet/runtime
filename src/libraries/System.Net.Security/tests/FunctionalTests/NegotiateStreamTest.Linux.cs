// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;
using Xunit.NetCore.Extensions;

namespace System.Net.Security.Tests
{
    public class KDCSetup : IDisposable
    {
        private const string Krb5ConfigFile = "/etc/krb5.conf";
        private const string KDestroyCmd = "kdestroy";
        private const string ScriptName = "setup-kdc.sh";
        private const string ScriptUninstallArgs = "--uninstall --yes";
        private const string ScriptInstallArgs = "--password {0} --yes";
        private const int InstalledButUnconfiguredExitCode = 2;
        private readonly bool _isKrbPreInstalled;
        public readonly string password;
        private const string NtlmUserFile = "NTLM_USER_FILE";
        private readonly bool _successfulSetup = true;

        public KDCSetup()
        {
            _isKrbPreInstalled = File.Exists(Krb5ConfigFile) &&
                File.ReadAllText(Krb5ConfigFile).Contains(TestConfiguration.Realm);
            if (!_isKrbPreInstalled)
            {
                password = Guid.NewGuid().ToString("N");
                int exitCode = RunSetupScript(string.Format(ScriptInstallArgs, password));
                if (exitCode != 0)
                {
                    if (exitCode != InstalledButUnconfiguredExitCode)
                    {
                        Dispose();
                    }
                    _successfulSetup = false;
                }
            }
            else
            {
                password = TestConfiguration.DefaultPassword;
            }
        }

        public void Dispose()
        {
            if (!_isKrbPreInstalled)
            {
                RunSetupScript(ScriptUninstallArgs);
            }
        }

        // checks for availability of Kerberos related infrastructure
        // on the host. Returns true available, false otherwise
        public bool CheckAndClearCredentials(ITestOutputHelper output)
        {
            if (!_successfulSetup)
            {
                return false;
            }

            // Clear the credentials
            var startInfo = new ProcessStartInfo(KDestroyCmd);
            startInfo.CreateNoWindow = true;
            startInfo.Arguments = "-A";
            using (Process clearCreds = Process.Start(startInfo))
            {
                clearCreds.WaitForExit();
                output.WriteLine("kdestroy returned {0}", clearCreds.ExitCode);
                return (clearCreds.ExitCode == 0);
            }
        }

        public bool CheckAndInitializeNtlm(bool isKrbAvailable)
        {
            if (!_successfulSetup || !isKrbAvailable)
            {
                return false;
            }

            if (File.Exists(TestConfiguration.NtlmUserFilePath))
            {
                Environment.SetEnvironmentVariable(NtlmUserFile, TestConfiguration.NtlmUserFilePath);
                return true;
            }

            return false;
        }

        private static int RunSetupScript(string args = null)
        {
            try
            {
                return AdminHelpers.RunAsSudo($"bash {ScriptName} {args}");
            }
            catch
            {
                // Could not find the file
                return 1;
            }
        }
    }

    [Trait(XunitConstants.Category, XunitConstants.RequiresElevation)]
    public class NegotiateStreamTest : IDisposable, IClassFixture<KDCSetup>
    {
        private readonly byte[] _firstMessage = Encoding.UTF8.GetBytes("Sample First Message");
        private readonly byte[] _secondMessage = Encoding.UTF8.GetBytes("Sample Second Message");
        private readonly bool _isKrbAvailable; // tests are no-op if kerberos is not available on the host machine
        private readonly bool _isNtlmAvailable; // tests are no-op if ntlm is not available on the host machine
        private readonly KDCSetup _fixture;
        private readonly ITestOutputHelper _output;
        private readonly string _emptyTarget = string.Empty;
        private readonly string _testTarget = "TestTarget";
        private static readonly string _ntlmPassword = TestConfiguration.NtlmPassword;

        private void AssertClientPropertiesForTarget(NegotiateStream client, string target)
        {
            Assert.True(client.IsAuthenticated, "client.IsAuthenticated");
            Assert.Equal(TokenImpersonationLevel.Identification, client.ImpersonationLevel);
            Assert.True(client.IsEncrypted, "client.IsEncrypted");
            Assert.True(client.IsMutuallyAuthenticated, "client.IsMutuallyAuthenticated");
            Assert.False(client.IsServer, "client.IsServer");
            Assert.True(client.IsSigned, "client.IsSigned");
            Assert.False(client.LeaveInnerStreamOpen, "client.LeaveInnerStreamOpen");

            IIdentity serverIdentity = client.RemoteIdentity;
            Assert.Equal("Kerberos", serverIdentity.AuthenticationType);
            Assert.True(serverIdentity.IsAuthenticated, "serverIdentity.IsAuthenticated");
            IdentityValidator.AssertHasName(serverIdentity, target);
        }

        public NegotiateStreamTest(KDCSetup fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            _isKrbAvailable = _fixture.CheckAndClearCredentials(_output);
            _isNtlmAvailable = _fixture.CheckAndInitializeNtlm(_isKrbAvailable);
        }

        [Fact, OuterLoop]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task NegotiateStream_StreamToStream_KerberosAuthentication_Success()
        {
            if (!_isKrbAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_StreamToStream_KerberosAuthentication_Success");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new UnixGssFakeNegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated, "client is not authenticated");

                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}", TestConfiguration.HostTarget, TestConfiguration.Realm);
                NetworkCredential credential = new NetworkCredential(user, _fixture.password);
                Task[] auth = new Task[] {
                    client.AuthenticateAsClientAsync(credential, target),
                    server.AuthenticateAsServerAsync()
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);
                AssertClientPropertiesForTarget(client, target);
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task NegotiateStream_StreamToStream_AuthToHttpTarget_Success()
        {
            if (!_isKrbAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_StreamToStream_AuthToHttpTarget_Success");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new UnixGssFakeNegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated);

                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                NetworkCredential credential = new NetworkCredential(user, _fixture.password);
                Task[] auth = new Task[] {
                    client.AuthenticateAsClientAsync(credential, TestConfiguration.HttpTarget),
                    server.AuthenticateAsServerAsync()
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

                AssertClientPropertiesForTarget(client, TestConfiguration.HttpTarget);
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task NegotiateStream_StreamToStream_KerberosAuthWithoutRealm_Success()
        {
            if (!_isKrbAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_StreamToStream_KerberosAuthWithoutRealm_Success");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new UnixGssFakeNegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated);

                NetworkCredential credential = new NetworkCredential(TestConfiguration.KerberosUser, _fixture.password);
                Task[] auth = new Task[] {
                    client.AuthenticateAsClientAsync(credential, TestConfiguration.HostTarget),
                    server.AuthenticateAsServerAsync()
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

                AssertClientPropertiesForTarget(client, TestConfiguration.HostTarget);
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task NegotiateStream_StreamToStream_KerberosAuthDefaultCredentials_Success()
        {
            if (!_isKrbAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_StreamToStream_KerberosAuthDefaultCredentials_Success");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new UnixGssFakeNegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated, "client is not authenticated before AuthenticateAsClient call");

                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}", TestConfiguration.HostTarget, TestConfiguration.Realm);
                // Seed the default Kerberos cache with the TGT
                UnixGssFakeNegotiateStream.GetDefaultKerberosCredentials(user, _fixture.password);
                Task[] auth = new Task[] {
                    client.AuthenticateAsClientAsync(CredentialCache.DefaultNetworkCredentials, target),
                    server.AuthenticateAsServerAsync()
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

                AssertClientPropertiesForTarget(client, target);
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task NegotiateStream_EchoServer_ClientWriteRead_Successive_Sync_Success()
        {
            if (!_isKrbAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_EchoServer_ClientWriteRead_Successive_Sync_Success");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();
            byte[] firstRecvBuffer = new byte[_firstMessage.Length];
            byte[] secondRecvBuffer = new byte[_secondMessage.Length];

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new UnixGssFakeNegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated, "client is not authenticated before AuthenticateAsClient call");

                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}", TestConfiguration.HostTarget, TestConfiguration.Realm);
                NetworkCredential credential = new NetworkCredential(user, _fixture.password);
                Task[] auth = new Task[] {
                    client.AuthenticateAsClientAsync(credential, target),
                    server.AuthenticateAsServerAsync()
                };
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

                Task svrMsgTask = server.PollMessageAsync(2);

                client.Write(_firstMessage, 0, _firstMessage.Length);
                client.Write(_secondMessage, 0, _secondMessage.Length);
                client.Read(firstRecvBuffer, 0, firstRecvBuffer.Length);
                client.Read(secondRecvBuffer, 0, secondRecvBuffer.Length);
                Assert.True(_firstMessage.SequenceEqual(firstRecvBuffer), "first message received is as expected");
                Assert.True(_secondMessage.SequenceEqual(secondRecvBuffer), "second message received is as expected");
                await svrMsgTask.TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task NegotiateStream_EchoServer_ClientWriteRead_Successive_Async_Success()
        {
            if (!_isKrbAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_EchoServer_ClientWriteRead_Successive_Async_Success");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();
            byte[] firstRecvBuffer = new byte[_firstMessage.Length];
            byte[] secondRecvBuffer = new byte[_secondMessage.Length];

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new UnixGssFakeNegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated, "client is not authenticated before AuthenticateAsClient call");

                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}", TestConfiguration.HostTarget, TestConfiguration.Realm);
                NetworkCredential credential = new NetworkCredential(user, _fixture.password);
                Task[] auth = new Task[] {
                    client.AuthenticateAsClientAsync(credential, target),
                    server.AuthenticateAsServerAsync()
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

                Task serverTask = server.PollMessageAsync(2);
                Task[] msgTasks = new Task[] {
                 client.WriteAsync(_firstMessage, 0, _firstMessage.Length).ContinueWith((t) =>
                    client.WriteAsync(_secondMessage, 0, _secondMessage.Length)).Unwrap(),
                 ReadAllAsync(client, firstRecvBuffer, 0, firstRecvBuffer.Length).ContinueWith((t) =>
                   ReadAllAsync(client, secondRecvBuffer, 0, secondRecvBuffer.Length)).Unwrap(),
                 serverTask
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(msgTasks);

                Assert.True(_firstMessage.SequenceEqual(firstRecvBuffer), "The first message received is as expected");
                Assert.True(_secondMessage.SequenceEqual(secondRecvBuffer), "The second message received is as expected");
            }
        }

        private static async Task ReadAllAsync(Stream source, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int bytesRead = await source.ReadAsync(buffer, offset, count).ConfigureAwait(false);
                if (bytesRead == 0) break;
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void NegotiateStream_StreamToStream_KerberosAuthDefaultCredentialsNoSeed_Failure()
        {
            if (!_isKrbAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_StreamToStream_KerberosAuthDefaultCredentialsNoSeed_Failure");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var client = new NegotiateStream(clientStream))
            {
                Assert.False(client.IsAuthenticated, "client is not authenticated before AuthenticateAsClient call");

                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}", TestConfiguration.HostTarget, TestConfiguration.Realm);
                Assert.ThrowsAsync<AuthenticationException>(() => client.AuthenticateAsClientAsync(CredentialCache.DefaultNetworkCredentials, target));
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void NegotiateStream_StreamToStream_KerberosAuthInvalidUser_Failure()
        {
            if (!_isKrbAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_StreamToStream_KerberosAuthInvalidUser_Failure");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new UnixGssFakeNegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated, "client is not authenticated by default");

                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}", TestConfiguration.HostTarget, TestConfiguration.Realm);
                NetworkCredential credential = new NetworkCredential(user.Substring(1), _fixture.password);
                Assert.ThrowsAsync<AuthenticationException>(() => client.AuthenticateAsClientAsync(credential, target));
                Assert.ThrowsAsync<AuthenticationException>(() => server.AuthenticateAsServerAsync());
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void NegotiateStream_StreamToStream_KerberosAuthInvalidPassword_Failure()
        {
            if (!_isKrbAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_StreamToStream_KerberosAuthInvalidPassword_Failure");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new UnixGssFakeNegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated, "client stream is not authenticated by default");

                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}", TestConfiguration.HostTarget, TestConfiguration.Realm);
                NetworkCredential credential = new NetworkCredential(user, _fixture.password.Substring(1));
                Task serverAuth = server.AuthenticateAsServerAsync();
                Assert.ThrowsAsync<AuthenticationException>(() => client.AuthenticateAsClientAsync(credential, target));
                Assert.ThrowsAsync<AuthenticationException>(() => server.AuthenticateAsServerAsync());
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void NegotiateStream_StreamToStream_KerberosAuthInvalidTarget_Failure()
        {
            if (!_isKrbAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_StreamToStream_KerberosAuthInvalidTarget_Failure");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var client = new NegotiateStream(clientStream))
            {
                Assert.False(client.IsAuthenticated, "client stream is not authenticated by default");

                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}", TestConfiguration.HostTarget, TestConfiguration.Realm);
                NetworkCredential credential = new NetworkCredential(user, _fixture.password);
                Assert.ThrowsAsync<AuthenticationException>(() => client.AuthenticateAsClientAsync(credential, target.Substring(1)));
            }
        }

        public static IEnumerable<object[]> ValidNtlmCredentials()
        {

            yield return new object[] { new NetworkCredential(TestConfiguration.NtlmUser, _ntlmPassword, TestConfiguration.Domain) };
            yield return new object[] { new NetworkCredential(TestConfiguration.NtlmUser, _ntlmPassword) };
            yield return new object[]
            {
                    new NetworkCredential($@"{TestConfiguration.Domain}\{TestConfiguration.NtlmUser}", _ntlmPassword)
            };
            yield return new object[]
            {
                    new NetworkCredential($"{TestConfiguration.NtlmUser}@{TestConfiguration.Domain}", _ntlmPassword)
            };
        }

        public static IEnumerable<object[]> InvalidNtlmCredentials
        {
            get
            {
                yield return new object[] { new NetworkCredential(TestConfiguration.NtlmUser, _ntlmPassword, TestConfiguration.Domain.Substring(1)) };
                yield return new object[] { new NetworkCredential(TestConfiguration.NtlmUser.Substring(1), _ntlmPassword, TestConfiguration.Domain) };
                yield return new object[] { new NetworkCredential(TestConfiguration.NtlmUser, _ntlmPassword.Substring(1), TestConfiguration.Domain) };
                yield return new object[] { new NetworkCredential($@"{TestConfiguration.Domain}\{TestConfiguration.NtlmUser}", _ntlmPassword, TestConfiguration.Domain.Substring(1)) };
                yield return new object[] { new NetworkCredential($"{TestConfiguration.NtlmUser}@{TestConfiguration.Domain.Substring(1)}", _ntlmPassword) };
                yield return new object[] { new NetworkCredential($@"{TestConfiguration.Domain.Substring(1)}\{TestConfiguration.NtlmUser}", _ntlmPassword, TestConfiguration.Domain) };
                yield return new object[] { new NetworkCredential(TestConfiguration.NtlmUser, _ntlmPassword, TestConfiguration.Realm) };

            }
        }


        [Theory, OuterLoop]
        [MemberData(nameof(ValidNtlmCredentials))]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task NegotiateStream_StreamToStream_NtlmAuthentication_ValidCredentials_Success(NetworkCredential credential)
        {
            if (!_isNtlmAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_StreamToStream_NtlmAuthentication_ValidCredentials_Success");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new UnixGssFakeNegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated, "client.IsAuthenticated");
                Assert.False(server.IsAuthenticated, "server.IsAuthenticated");

                Task[] auth = new Task[] {
                    client.AuthenticateAsClientAsync(credential, _testTarget, ProtectionLevel.None, TokenImpersonationLevel.Identification),
                    server.AuthenticateAsServerAsync()
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

                // Expected Client property values:
                Assert.True(client.IsAuthenticated, "client.IsAuthenticated");
                Assert.Equal(TokenImpersonationLevel.Identification, client.ImpersonationLevel);
                Assert.False(client.IsEncrypted, "client.IsEncrypted");
                Assert.False(client.IsMutuallyAuthenticated, "client.IsMutuallyAuthenticated");
                Assert.False(client.IsServer, "client.IsServer");
                Assert.False(client.IsSigned, "client.IsSigned");
                Assert.False(client.LeaveInnerStreamOpen, "client.LeaveInnerStreamOpen");

                IIdentity serverIdentity = client.RemoteIdentity;
                Assert.Equal("NTLM", serverIdentity.AuthenticationType);
                Assert.True(serverIdentity.IsAuthenticated, "serverIdentity.IsAuthenticated");
                IdentityValidator.AssertHasName(serverIdentity, _testTarget);
            }
        }


        [Fact, OuterLoop]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task NegotiateStream_StreamToStream_NtlmAuthentication_Fallback_Success()
        {
            if (!_isNtlmAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_StreamToStream_NtlmAuthentication_EmptyTarget_KerberosUser_Fallback_Success");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new UnixGssFakeNegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated, "client.IsAuthenticated");
                Assert.False(server.IsAuthenticated, "server.IsAuthenticated");

                string user = string.Format("{0}@{1}", TestConfiguration.NtlmUser, TestConfiguration.Domain);
                NetworkCredential credential = new NetworkCredential(user, TestConfiguration.NtlmPassword);
                Task[] auth = new Task[] {
                    client.AuthenticateAsClientAsync(credential, TestConfiguration.HostTarget),
                    server.AuthenticateAsServerAsync()
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

                // Expected Client property values:
                Assert.True(client.IsAuthenticated, "client.IsAuthenticated");
                Assert.Equal(TokenImpersonationLevel.Identification, client.ImpersonationLevel);
                Assert.True(client.IsEncrypted, "client.IsEncrypted");
                Assert.False(client.IsMutuallyAuthenticated, "client.IsMutuallyAuthenticated");
                Assert.False(client.IsServer, "client.IsServer");
                Assert.True(client.IsSigned, "client.IsSigned");
                Assert.False(client.LeaveInnerStreamOpen, "client.LeaveInnerStreamOpen");

                IIdentity serverIdentity = client.RemoteIdentity;
                Assert.Equal("NTLM", serverIdentity.AuthenticationType);
                Assert.False(serverIdentity.IsAuthenticated, "serverIdentity.IsAuthenticated");
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task NegotiateStream_StreamToStream_NtlmAuthentication_KerberosCreds_Success()
        {
            if (!_isNtlmAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_StreamToStream_NtlmAuthentication_KerberosCreds_Success");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new UnixGssFakeNegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated, "client.IsAuthenticated");
                Assert.False(server.IsAuthenticated, "server.IsAuthenticated");

                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                NetworkCredential credential = new NetworkCredential(user, _fixture.password);
                Task[] auth = new Task[] {
                    client.AuthenticateAsClientAsync(credential, TestConfiguration.HttpTarget, ProtectionLevel.None, TokenImpersonationLevel.Identification),
                    server.AuthenticateAsServerAsync()
                };
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

                // Expected Client property values:
                Assert.True(client.IsAuthenticated, "client.IsAuthenticated");
                Assert.Equal(TokenImpersonationLevel.Identification, client.ImpersonationLevel);
                Assert.False(client.IsEncrypted, "client.IsEncrypted");
                Assert.False(client.IsMutuallyAuthenticated, "client.IsMutuallyAuthenticated");
                Assert.False(client.IsServer, "client.IsServer");
                Assert.False(client.IsSigned, "client.IsSigned");
                Assert.False(client.LeaveInnerStreamOpen, "client.LeaveInnerStreamOpen");

                IIdentity serverIdentity = client.RemoteIdentity;
                Assert.Equal("NTLM", serverIdentity.AuthenticationType);
                Assert.True(serverIdentity.IsAuthenticated, "server identity is authenticated");
                IdentityValidator.AssertHasName(serverIdentity, TestConfiguration.HttpTarget);
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task NegotiateStream_EchoServer_NTLM_ClientWriteRead_Successive_Sync_Success()
        {
            if (!_isNtlmAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_EchoServer_NTLM_ClientWriteRead_Successive_Sync_Success");
                return;
            }

            byte[] firstRecvBuffer = new byte[_firstMessage.Length];
            byte[] secondRecvBuffer = new byte[_secondMessage.Length];

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new UnixGssFakeNegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated, "client.IsAuthenticated");
                Assert.False(server.IsAuthenticated, "server.IsAuthenticated");

                string user = string.Format("{0}@{1}", TestConfiguration.NtlmUser, TestConfiguration.Domain);
                NetworkCredential credential = new NetworkCredential(user, TestConfiguration.NtlmPassword);
                Task[] auth = new Task[] {
                    client.AuthenticateAsClientAsync(credential, TestConfiguration.HostTarget),
                    server.AuthenticateAsServerAsync()
                };
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

                //clearing message queue
                byte[] junkBytes = new byte[5];
                int j = clientStream.Read(junkBytes, 0, 5);
                Task svrMsgTask = server.PollMessageAsync(2);

                client.Write(_firstMessage, 0, _firstMessage.Length);
                client.Write(_secondMessage, 0, _secondMessage.Length);
                client.Read(firstRecvBuffer, 0, firstRecvBuffer.Length);
                client.Read(secondRecvBuffer, 0, secondRecvBuffer.Length);
                Assert.True(_firstMessage.SequenceEqual(firstRecvBuffer), "first message received is as expected");
                Assert.True(_secondMessage.SequenceEqual(secondRecvBuffer), "second message received is as expected");
                await svrMsgTask.TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task NegotiateStream_EchoServer_NTLM_ClientWriteRead_Successive_Async_Success()
        {
            if (!_isNtlmAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_EchoServer_NTLM_ClientWriteRead_Successive_Async_Success");
                return;
            }

            byte[] firstRecvBuffer = new byte[_firstMessage.Length];
            byte[] secondRecvBuffer = new byte[_secondMessage.Length];

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new UnixGssFakeNegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated, "client.IsAuthenticated");
                Assert.False(server.IsAuthenticated, "server.IsAuthenticated");

                string user = string.Format("{0}@{1}", TestConfiguration.NtlmUser, TestConfiguration.Domain);
                NetworkCredential credential = new NetworkCredential(user, TestConfiguration.NtlmPassword);
                Task[] auth = new Task[] {
                    client.AuthenticateAsClientAsync(credential, TestConfiguration.HostTarget),
                    server.AuthenticateAsServerAsync()
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

                //clearing message queue
                byte[] junkBytes = new byte[5];
                int j = clientStream.Read(junkBytes, 0, 5);
                Task serverTask = server.PollMessageAsync(2);

                Task[] msgTasks = new Task[] {
                 client.WriteAsync(_firstMessage, 0, _firstMessage.Length).ContinueWith((t) =>
                    client.WriteAsync(_secondMessage, 0, _secondMessage.Length)).Unwrap(),
                 ReadAllAsync(client, firstRecvBuffer, 0, firstRecvBuffer.Length).ContinueWith((t) =>
                   ReadAllAsync(client, secondRecvBuffer, 0, secondRecvBuffer.Length)).Unwrap(),
                 serverTask
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(msgTasks);
                Assert.True(_firstMessage.SequenceEqual(firstRecvBuffer), "The first message received is as expected");
                Assert.True(_secondMessage.SequenceEqual(secondRecvBuffer), "The second message received is as expected");
            }
        }

        [Theory, OuterLoop]
        [MemberData(nameof(InvalidNtlmCredentials))]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void NegotiateStream_StreamToStream_NtlmAuthentication_NtlmUser_InvalidCredentials_Fail(NetworkCredential credential)
        {
            if (!_isNtlmAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_StreamToStream_NtlmAuthentication_NtlmUser_InvalidCredentials_Fail");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();
            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new UnixGssFakeNegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated, "client.IsAuthenticated");
                Assert.ThrowsAsync<AuthenticationException>(() => server.AuthenticateAsServerAsync());
                Assert.ThrowsAsync<AuthenticationException>(() => client.AuthenticateAsClientAsync(credential, _testTarget, ProtectionLevel.None, TokenImpersonationLevel.Identification));
            }
        }

        public void Dispose()
        {
            try
            {
                _fixture.CheckAndClearCredentials(_output);
            }
            catch
            {
            }
        }
    }
}
