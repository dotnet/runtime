// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace System.Net.Security.Tests
{
    public class KDCSetup : IDisposable
    {
        private const string Krb5ConfigFile = "/etc/krb5.conf";
        private const string KDestroyCmd = "kdestroy";
        private const string SudoCommand = "sudo";
        private const string ScriptName = "setup-kdc.sh";
        private const string ScriptUninstallArgs = "--uninstall --yes";
        private const string ScriptInstallArgs = "--password {0} --yes";
        private const int InstalledButUnconfiguredExitCode = 2;
        private readonly bool _isKrbPreInstalled ;
        public readonly string password;

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

                    Assert.True(false, "KDC setup failure");
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

        // checks for avilability of Kerberos related infrastructure
        // on the host. Returns true available, false otherwise
        public bool CheckAndClearCredentials(ITestOutputHelper output)
        {
            // Clear the credentials
            var startInfo = new ProcessStartInfo(KDestroyCmd);
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = true;
            startInfo.Arguments = "-A";
            using (Process clearCreds = Process.Start(startInfo))
            {
                clearCreds.WaitForExit();
                output.WriteLine("kdestroy returned {0}", clearCreds.ExitCode);
                return (clearCreds.ExitCode == 0);
            }
        }

        private static int RunSetupScript(string args = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();

            // since ProcessStartInfo does not support Verb, we use sudo as
            // the program to be run
            startInfo.FileName = SudoCommand;
            startInfo.Arguments = string.Format("bash {0} {1}", ScriptName, args);
            using (Process kdcSetup = Process.Start(startInfo))
            {
                kdcSetup.WaitForExit();
                return kdcSetup.ExitCode;
            }
        }
    }

    [PlatformSpecific(PlatformID.Linux)]
    public class KerberosTest : IDisposable, IClassFixture<KDCSetup>
    {
        private readonly byte[] _firstMessage = Encoding.UTF8.GetBytes("Sample First Message");
        private readonly byte[] _secondMessage = Encoding.UTF8.GetBytes("Sample Second Message");
        private readonly bool _isKrbAvailable; // tests are no-op if kerberos is not available on the host machine
        private readonly KDCSetup _fixture;
        private readonly ITestOutputHelper _output;

        public KerberosTest(KDCSetup fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            _isKrbAvailable = _fixture.CheckAndClearCredentials(_output);
        }

        [Fact, OuterLoop]
        [PlatformSpecific(PlatformID.Linux)]
        public void NegotiateStream_StreamToStream_KerberosAuthentication_Success()
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

                Task[] auth = new Task[2];
                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}", TestConfiguration.HostTarget, TestConfiguration.Realm);
                NetworkCredential credential = new NetworkCredential(user, _fixture.password);
                auth[0] = client.AuthenticateAsClientAsync(credential, target);
                auth[1] = server.AuthenticateAsServerAsync();

                bool finished = Task.WaitAll(auth, TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Handshake completed in the allotted time");

                // Expected Client property values:
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
        }

        [Fact, OuterLoop]
        [PlatformSpecific(PlatformID.Linux)]
        public void NegotiateStream_StreamToStream_AuthToHttpTarget_Success()
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

                Task[] auth = new Task[2];
                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}",TestConfiguration.HttpTarget, TestConfiguration.Realm);
                NetworkCredential credential = new NetworkCredential(user, _fixture.password);
                auth[0] = client.AuthenticateAsClientAsync(credential, target);
                auth[1] = server.AuthenticateAsServerAsync();

                bool finished = Task.WaitAll(auth, TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Handshake completed in the allotted time");

                // Expected Client property values:
                Assert.True(client.IsAuthenticated, "client.IsAuthenticated");
                Assert.Equal(TokenImpersonationLevel.Identification, client.ImpersonationLevel);
                Assert.True(client.IsEncrypted, "client.IsEncrypted");
                Assert.True(client.IsMutuallyAuthenticated, "client.IsMutuallyAuthenticated");
                Assert.False(client.IsServer, "client.IsServer");
                Assert.True(client.IsSigned, "client.IsSigned");
                Assert.False(client.LeaveInnerStreamOpen, "client.LeaveInnerStream");

                IIdentity serverIdentity = client.RemoteIdentity;
                Assert.Equal("Kerberos", serverIdentity.AuthenticationType);
                Assert.True(serverIdentity.IsAuthenticated, "serverIdentity.IsAuthenticated");
                IdentityValidator.AssertHasName(serverIdentity, target);
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(PlatformID.Linux)]
        public void NegotiateStream_StreamToStream_KerberosAuthWithoutRealm_Success()
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

                Task[] auth = new Task[2];
                NetworkCredential credential = new NetworkCredential(TestConfiguration.KerberosUser, _fixture.password);
                auth[0] = client.AuthenticateAsClientAsync(credential, TestConfiguration.HostTarget);
                auth[1] = server.AuthenticateAsServerAsync();

                bool finished = Task.WaitAll(auth, TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Handshake completed in the allotted time");

                // Expected Client property values:
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
                IdentityValidator.AssertHasName(serverIdentity, TestConfiguration.HostTarget);
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(PlatformID.Linux)]
        public void NegotiateStream_StreamToStream_KerberosAuthDefaultCredentials_Success()
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

                Task[] auth = new Task[2];
                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}", TestConfiguration.HostTarget, TestConfiguration.Realm);
                // Seed the default Kerberos cache with the TGT
                UnixGssFakeNegotiateStream.GetDefaultKerberosCredentials(user, _fixture.password);
                auth[0] = client.AuthenticateAsClientAsync(CredentialCache.DefaultNetworkCredentials, target);
                auth[1] = server.AuthenticateAsServerAsync();

                bool finished = Task.WaitAll(auth, TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Handshake completed in the allotted time");

                // Expected Client property values:
                Assert.True(client.IsAuthenticated, "client.IsAuthenticated");
                Assert.Equal(TokenImpersonationLevel.Identification, client.ImpersonationLevel);
                Assert.True(client.IsEncrypted, "client.IsEncrypted");
                Assert.True(client.IsMutuallyAuthenticated, "client.IsMutuallyAuthenticated");
                Assert.False(client.IsServer, "client.IsServer");
                Assert.True(client.IsSigned, "client.IsSigned");
                Assert.False(client.LeaveInnerStreamOpen, "client.LeaveInnerStreamOpen");

                IIdentity serverIdentity = client.RemoteIdentity;
                Assert.Equal("Kerberos", serverIdentity.AuthenticationType);
                Assert.True(serverIdentity.IsAuthenticated,"serverIdentity.IsAuthenticated");
                IdentityValidator.AssertHasName(serverIdentity, target);
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(PlatformID.Linux)]
        public void NegotiateStream_EchoServer_ClientWriteRead_Successive_Sync_Success()
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

                Task[] auth = new Task[2];
                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}", TestConfiguration.HostTarget, TestConfiguration.Realm);
                NetworkCredential credential = new NetworkCredential(user, _fixture.password);
                auth[0] = client.AuthenticateAsClientAsync(credential, target);
                auth[1] = server.AuthenticateAsServerAsync();
                bool finished = Task.WaitAll(auth, TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Handshake completed in the allotted time");

                Task svrMsgTask = server.PollMessageAsync(2);

                client.Write(_firstMessage, 0, _firstMessage.Length);
                client.Write(_secondMessage, 0, _secondMessage.Length);
                client.Read(firstRecvBuffer, 0, firstRecvBuffer.Length);
                client.Read(secondRecvBuffer, 0, secondRecvBuffer.Length);
                Assert.True(_firstMessage.SequenceEqual(firstRecvBuffer), "first message received is as expected");
                Assert.True(_secondMessage.SequenceEqual(secondRecvBuffer), "second message received is as expected");
                finished = svrMsgTask.Wait(TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Message roundtrip completed in the allotted time");
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(PlatformID.Linux)]
        public void NegotiateStream_EchoServer_ClientWriteRead_Successive_Async_Success()
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

                Task[] auth = new Task[2];
                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}", TestConfiguration.HostTarget, TestConfiguration.Realm);
                NetworkCredential credential = new NetworkCredential(user, _fixture.password);
                auth[0] = client.AuthenticateAsClientAsync(credential, target);
                auth[1] = server.AuthenticateAsServerAsync();
                bool finished = Task.WaitAll(auth, TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Handshake completed in the allotted time");

                Task serverTask = server.PollMessageAsync(2);
                Task[] msgTasks = new Task[3];
                msgTasks[0] = client.WriteAsync(_firstMessage, 0, _firstMessage.Length).ContinueWith((t) =>
                    client.WriteAsync(_secondMessage, 0, _secondMessage.Length));
                msgTasks[1] =  client.ReadAsync(firstRecvBuffer, 0, firstRecvBuffer.Length).ContinueWith((t) =>
                    client.ReadAsync(secondRecvBuffer, 0, secondRecvBuffer.Length));
                msgTasks[2] = serverTask;
                finished = Task.WaitAll(msgTasks, TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Messages sent and received in the allotted time");
                Assert.True(_firstMessage.SequenceEqual(firstRecvBuffer), "The first message received is as expected");
                Assert.True(_secondMessage.SequenceEqual(secondRecvBuffer), "The second message received is as expected");
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(PlatformID.Linux)]
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
        [PlatformSpecific(PlatformID.Linux)]
        public void NegotiateStream_StreamToStream_KerberosAuthInvalidUser_Failure()
        {
            if (!_isKrbAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_StreamToStream_KerberosAuthInvalidUser_Failure");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var client = new NegotiateStream(clientStream))
            {
                Assert.False(client.IsAuthenticated, "client is not authenticated by default");

                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}", TestConfiguration.HostTarget, TestConfiguration.Realm);
                NetworkCredential credential = new NetworkCredential(user.Substring(1), _fixture.password);
                Assert.Throws<AuthenticationException>(() =>
                {
                    client.AuthenticateAsClientAsync(credential, target);
                });
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(PlatformID.Linux)]
        public void NegotiateStream_StreamToStream_KerberosAuthInvalidPassword_Failure()
        {
            if (!_isKrbAvailable)
            {
                _output.WriteLine("skipping NegotiateStream_StreamToStream_KerberosAuthInvalidPassword_Failure");
                return;
            }

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var client = new NegotiateStream(clientStream))
            {
                Assert.False(client.IsAuthenticated, "client stream is not authenticated by default");

                string user = string.Format("{0}@{1}", TestConfiguration.KerberosUser, TestConfiguration.Realm);
                string target = string.Format("{0}@{1}", TestConfiguration.HostTarget, TestConfiguration.Realm);
                NetworkCredential credential = new NetworkCredential(user, _fixture.password.Substring(1));
                Assert.Throws<AuthenticationException>(() =>
                {
                    client.AuthenticateAsClientAsync(credential, target);
                });
            }
        }

        [Fact, OuterLoop]
        [PlatformSpecific(PlatformID.Linux)]
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
