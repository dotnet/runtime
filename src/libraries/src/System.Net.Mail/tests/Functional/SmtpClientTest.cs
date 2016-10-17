// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// SmtpClientTest.cs - NUnit Test Cases for System.Net.Mail.SmtpClient
//
// Authors:
//   John Luke (john.luke@gmail.com)
//
// (C) 2006 John Luke
//

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class SmtpClientTest : IDisposable
    {
        private SmtpClient _smtp;
        private string _tempFolder;

        private SmtpClient Smtp
        {
            get
            {
                return _smtp ?? (_smtp = new SmtpClient());
            }
        }

        private string TempFolder
        {
            get
            {
                if (_tempFolder == null)
                {
                    _tempFolder = Path.Combine(Path.GetTempPath(), GetType().FullName, Guid.NewGuid().ToString());
                    if (Directory.Exists(_tempFolder))
                        Directory.Delete(_tempFolder, true);

                    Directory.CreateDirectory(_tempFolder);
                }

                return _tempFolder;
            }
        }

        public void Dispose()
        {
            if (_smtp != null)
            {
                _smtp.Dispose();
            }

            if (Directory.Exists(_tempFolder))
                Directory.Delete(_tempFolder, true);
        }

        [Theory]
        [InlineData(SmtpDeliveryMethod.SpecifiedPickupDirectory)]
        [InlineData(SmtpDeliveryMethod.PickupDirectoryFromIis)]
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
            Assert.Throws<ArgumentException>(() => Smtp.Host = "");
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
        public void Send_Message_Null()
        {
            Assert.Throws<ArgumentNullException>(() => Smtp.Send(null));
        }

        [Fact]
        public void Send_Network_Host_Null()
        {
            Assert.Throws<InvalidOperationException>(() => Smtp.Send("mono@novell.com", "everyone@novell.com", "introduction", "hello"));
        }

        [Fact]
        public void Send_Network_Host_Whitespace()
        {
            Smtp.Host = " \r\n ";
            Assert.Throws<InvalidOperationException>(() => Smtp.Send("mono@novell.com", "everyone@novell.com", "introduction", "hello"));
        }

        [Fact]
        public void Send_SpecifiedPickupDirectory()
        {
            Smtp.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            Smtp.PickupDirectoryLocation = TempFolder;
            Smtp.Send("mono@novell.com", "everyone@novell.com", "introduction", "hello");

            string[] files = Directory.GetFiles(TempFolder, "*");
            Assert.Equal(1, files.Length);
            Assert.Equal(".eml", Path.GetExtension(files[0]));
        }

        [Theory]
        [InlineData("some_path_not_exist")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("\0abc")]
        public void Send_SpecifiedPickupDirectoryInvalid(string location)
        {
            Smtp.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            Smtp.PickupDirectoryLocation = location;
            Assert.Throws<SmtpException>(() => Smtp.Send("mono@novell.com", "everyone@novell.com", "introduction", "hello"));
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

        //[Fact]
        public void TestMailDelivery()
        {
            SmtpServer server = new SmtpServer();
            SmtpClient client = new SmtpClient("localhost", server.EndPoint.Port);
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo\r\n");

            Thread t = new Thread(server.Run);
            t.Start();
            client.Send(msg);
            t.Join();

            Assert.Equal("<foo@example.com>", server.MailFrom);
            Assert.Equal("<bar@example.com>", server.MailTo);
        }

        //[Fact]
        public void TestMailDeliveryAsync()
        {
            SmtpServer server = new SmtpServer();
            SmtpClient client = new SmtpClient("localhost", server.EndPoint.Port);
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo\r\n");

            Thread t = new Thread(server.Run);
            t.Start();
            Task task = client.SendMailAsync(msg);
            t.Join();

            Assert.Equal("<foo@example.com>", server.MailFrom);
            Assert.Equal("<bar@example.com>", server.MailTo);

            Assert.True(task.Wait(1000));
            Assert.True(task.IsCompleted, "task");
        }
    }
}
