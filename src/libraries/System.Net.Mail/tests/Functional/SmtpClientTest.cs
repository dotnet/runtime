// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// SmtpClientTest.cs - Unit Test Cases for System.Net.Mail.SmtpClient
//
// Authors:
//   John Luke (john.luke@gmail.com)
//
// (C) 2006 John Luke
//

using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using System.Net.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Mail.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "SmtpClient is not supported on Browser")]
    public class SmtpClientTest : FileCleanupTestBase
    {
        private SmtpClient _smtp;

        private SmtpClient Smtp
        {
            get
            {
                return _smtp ??= new SmtpClient();
            }
        }

        private string TempFolder
        {
            get
            {
                return TestDirectory;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_smtp != null)
            {
                _smtp.Dispose();
            }
            base.Dispose(disposing);
        }

        ITestOutputHelper _output;

        public SmtpClientTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(SmtpDeliveryMethod.SpecifiedPickupDirectory)]
        [InlineData(SmtpDeliveryMethod.PickupDirectoryFromIis)]
        public void DeliveryMethodTest(SmtpDeliveryMethod method)
        {
            Smtp.DeliveryMethod = method;
            Assert.Equal(method, Smtp.DeliveryMethod);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EnableSslTest(bool value)
        {
            Smtp.EnableSsl = value;
            Assert.Equal(value, Smtp.EnableSsl);
        }

        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("smtp.ximian.com")]
        public void HostTest(string host)
        {
            Smtp.Host = host;
            Assert.Equal(host, Smtp.Host);
        }

        [Fact]
        public void InvalidHostTest()
        {
            Assert.Throws<ArgumentNullException>(() => Smtp.Host = null);
            AssertExtensions.Throws<ArgumentException>("value", () => Smtp.Host = "");
        }

        [Fact]
        public void ServicePoint_GetsCachedInstanceSpecificToHostPort()
        {
            using (var smtp1 = new SmtpClient("localhost1", 25))
            using (var smtp2 = new SmtpClient("localhost1", 25))
            using (var smtp3 = new SmtpClient("localhost2", 25))
            using (var smtp4 = new SmtpClient("localhost2", 26))
            {
                ServicePoint s1 = smtp1.ServicePoint;
                ServicePoint s2 = smtp2.ServicePoint;
                ServicePoint s3 = smtp3.ServicePoint;
                ServicePoint s4 = smtp4.ServicePoint;

                Assert.NotNull(s1);
                Assert.NotNull(s2);
                Assert.NotNull(s3);
                Assert.NotNull(s4);

                Assert.Same(s1, s2);
                Assert.NotSame(s2, s3);
                Assert.NotSame(s2, s4);
                Assert.NotSame(s3, s4);
            }
        }

        [Fact]
        public void ServicePoint_NetCoreApp_AddressIsAccessible()
        {
            using (var smtp = new SmtpClient("localhost", 25))
            {
                Assert.Equal("mailto", smtp.ServicePoint.Address.Scheme);
                Assert.Equal("localhost", smtp.ServicePoint.Address.Host);
                Assert.Equal(25, smtp.ServicePoint.Address.Port);
            }
        }

        [Fact]
        public void ServicePoint_ReflectsHostAndPortChange()
        {
            using (var smtp = new SmtpClient("localhost1", 25))
            {
                ServicePoint s1 = smtp.ServicePoint;

                smtp.Host = "localhost2";
                ServicePoint s2 = smtp.ServicePoint;
                smtp.Host = "localhost2";
                ServicePoint s3 = smtp.ServicePoint;

                Assert.NotSame(s1, s2);
                Assert.Same(s2, s3);

                smtp.Port = 26;
                ServicePoint s4 = smtp.ServicePoint;
                smtp.Port = 26;
                ServicePoint s5 = smtp.ServicePoint;

                Assert.NotSame(s3, s4);
                Assert.Same(s4, s5);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("shouldnotexist")]
        [InlineData("\0")]
        [InlineData("C:\\some\\path\\like\\string")]
        public void PickupDirectoryLocationTest(string folder)
        {
            Smtp.PickupDirectoryLocation = folder;
            Assert.Equal(folder, Smtp.PickupDirectoryLocation);
        }

        [Theory]
        [InlineData(25)]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public void PortTest(int value)
        {
            Smtp.Port = value;
            Assert.Equal(value, Smtp.Port);
        }

        [Fact]
        public void TestDefaultsOnProperties()
        {
            Assert.Equal(25, Smtp.Port);
            Assert.Equal(100000, Smtp.Timeout);
            Assert.Null(Smtp.Host);
            Assert.Null(Smtp.Credentials);
            Assert.False(Smtp.EnableSsl);
            Assert.False(Smtp.UseDefaultCredentials);
            Assert.Equal(SmtpDeliveryMethod.Network, Smtp.DeliveryMethod);
            Assert.Null(Smtp.PickupDirectoryLocation);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Port_Value_Invalid(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Smtp.Port = value);
        }

        [Fact]
        public void Send_Network_Host_Null()
        {
            Assert.Throws<InvalidOperationException>(() => Smtp.Send("mono@novell.com", "everyone@novell.com", "introduction", "hello"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        public void TestTimeout(int value)
        {
            if (value < 0)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => Smtp.Timeout = value);
                return;
            }

            Smtp.Timeout = value;
            Assert.Equal(value, Smtp.Timeout);
        }

        [Fact]
        public void Send_ServerDoesntExist_Throws()
        {
            using (var smtp = new SmtpClient(Guid.NewGuid().ToString("N")))
            {
                Assert.Throws<SmtpException>(() => smtp.Send("anyone@anyone.com", "anyone@anyone.com", "subject", "body"));
            }
        }

        [Fact]
        public async Task SendAsync_ServerDoesntExist_Throws()
        {
            using (var smtp = new SmtpClient(Guid.NewGuid().ToString("N")))
            {
                await Assert.ThrowsAsync<SmtpException>(() => smtp.SendMailAsync("anyone@anyone.com", "anyone@anyone.com", "subject", "body"));
            }
        }

        [Fact]
        public void TestMailDelivery()
        {
            using var server = new LoopbackSmtpServer(_output);
            using SmtpClient client = server.CreateClient();
            client.Credentials = new NetworkCredential("foo", "bar");
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            client.Send(msg);

            Assert.Equal("<foo@example.com>", server.MailFrom);
            Assert.Equal("<bar@example.com>", Assert.Single(server.MailTo));
            Assert.Equal("hello", server.Message.Subject);
            Assert.Equal("howdydoo", server.Message.Body);
            Assert.Equal(GetClientDomain(), server.ClientDomain);
            Assert.Equal("foo", server.Username);
            Assert.Equal("bar", server.Password);
            Assert.Equal("LOGIN", server.AuthMethodUsed, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX, "on OSX, not all synchronous operations (e.g. connect) can be aborted by closing the socket.")]
        public void TestZeroTimeout()
        {
            var testTask = Task.Run(() =>
            {
                using (Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    serverSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    serverSocket.Listen(1);

                    SmtpClient smtpClient = new SmtpClient("localhost", (serverSocket.LocalEndPoint as IPEndPoint).Port);
                    smtpClient.Timeout = 0;

                    MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "test");
                    Assert.Throws<SmtpException>(() => smtpClient.Send(msg));
                }
            });
            // Abort in order to get a coredump if this test takes too long.
            if (!testTask.Wait(TimeSpan.FromMinutes(5)))
            {
                Environment.FailFast(nameof(TestZeroTimeout));
            }
        }

        [Fact]
        public void SendMailAsync_CanBeCanceled_CancellationToken_SetAlready()
        {
            using var server = new LoopbackSmtpServer(_output);
            using SmtpClient client = server.CreateClient();

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            var message = new MailMessage("foo@internet.com", "bar@internet.com", "Foo", "Bar");

            Task sendTask = client.SendMailAsync(message, cts.Token);

            // Tests an implementation detail - if a CT is already set a canceled task will be returned
            Assert.True(sendTask.IsCanceled);
        }

        [Fact]
        public async Task SendMailAsync_CanBeCanceled_CancellationToken()
        {
            using var server = new LoopbackSmtpServer(_output);
            using SmtpClient client = server.CreateClient();

            server.ReceiveMultipleConnections = true;

            // The server will introduce some fake latency so that the operation can be canceled before the request completes
            CancellationTokenSource cts = new CancellationTokenSource();
            
            server.OnConnected += _ => cts.Cancel();

            var message = new MailMessage("foo@internet.com", "bar@internet.com", "Foo", "Bar");

            Task sendTask = Task.Run(() => client.SendMailAsync(message, cts.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await sendTask).WaitAsync(TestHelper.PassingTestTimeout);

            // We should still be able to send mail on the SmtpClient instance
            await Task.Run(() => client.SendMailAsync(message)).WaitAsync(TestHelper.PassingTestTimeout);

            Assert.Equal("<foo@internet.com>", server.MailFrom);
            Assert.Equal("<bar@internet.com>", Assert.Single(server.MailTo));
            Assert.Equal("Foo", server.Message.Subject);
            Assert.Equal("Bar", server.Message.Body);
            Assert.Equal(GetClientDomain(), server.ClientDomain);
        }

        [Fact]
        public async Task SendAsync_CanBeCanceled_SendAsyncCancel()
        {
            using var server = new LoopbackSmtpServer(_output);
            using SmtpClient client = server.CreateClient();

            server.ReceiveMultipleConnections = true;

            bool first = true;

            server.OnConnected += _ =>
            {
                if (first)
                {
                    first = false;
                    client.SendAsyncCancel();
                }
            };

            var message = new MailMessage("foo@internet.com", "bar@internet.com", "Foo", "Bar");

            TaskCompletionSource<AsyncCompletedEventArgs> tcs = new TaskCompletionSource<AsyncCompletedEventArgs>();
            client.SendCompleted += (s, e) =>
            {
                tcs.SetResult(e);
            };

            client.SendAsync(message, null);
            AsyncCompletedEventArgs e = await tcs.Task.WaitAsync(TestHelper.PassingTestTimeout);
            Assert.True(e.Cancelled, "SendAsync should have been canceled");
            Assert.Null(e.Error);

            // We should still be able to send mail on the SmtpClient instance
            await client.SendMailAsync(message).WaitAsync(TestHelper.PassingTestTimeout);

            Assert.Equal("<foo@internet.com>", server.MailFrom);
            Assert.Equal("<bar@internet.com>", Assert.Single(server.MailTo));
            Assert.Equal("Foo", server.Message.Subject);
            Assert.Equal("Bar", server.Message.Body);
            Assert.Equal(GetClientDomain(), server.ClientDomain);
        }

        private static string GetClientDomain() => IPGlobalProperties.GetIPGlobalProperties().HostName.Trim().ToLower();
    }
}
