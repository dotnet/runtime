// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mail
{
    public delegate void SendCompletedEventHandler(object sender, AsyncCompletedEventArgs e);

    public enum SmtpDeliveryMethod
    {
        Network,
        SpecifiedPickupDirectory,
        PickupDirectoryFromIis
    }

    // EAI Settings
    public enum SmtpDeliveryFormat
    {
        SevenBit = 0, // Legacy
        International = 1, // SMTPUTF8 - Email Address Internationalization (EAI)
    }

    [UnsupportedOSPlatform("browser")]
    public class SmtpClient : IDisposable
    {
#pragma warning disable CS0067      // Field is not used
        [UnsupportedOSPlatform("browser")]
        public event SendCompletedEventHandler? SendCompleted;
#pragma warning restore CS0067
        [UnsupportedOSPlatform("browser")]
        public SmtpClient()
        {
            Initialize();
        }

        [UnsupportedOSPlatform("browser")]
        public SmtpClient(string? host)
        {
            Initialize();
        }

        [UnsupportedOSPlatform("browser")]
        public SmtpClient(string? host, int port)
        {
            Initialize();
        }

        private void Initialize()
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public string? Host
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public int Port
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public bool UseDefaultCredentials
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public ICredentialsByHost? Credentials
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public int Timeout
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public ServicePoint ServicePoint
        {
            get => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public SmtpDeliveryMethod DeliveryMethod
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public SmtpDeliveryFormat DeliveryFormat
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public string? PickupDirectoryLocation
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        /// <summary>
        ///    <para>Set to true if we need SSL</para>
        /// </summary>
        [UnsupportedOSPlatform("browser")]
        public bool EnableSsl
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Certificates used by the client for establishing an SSL connection with the server.
        /// </summary>
        [UnsupportedOSPlatform("browser")]
        public X509CertificateCollection ClientCertificates => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("browser")]
        public string? TargetName
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        private bool ServerSupportsEai => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("browser")]
        public void Send(string from, string recipients, string? subject, string? body) => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("browser")]
        public void Send(MailMessage message) => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("browser")]
        public void SendAsync(string from, string recipients, string? subject, string? body, object? userToken) => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("browser")]
        public void SendAsync(MailMessage message, object? userToken) => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("browser")]
        public void SendAsyncCancel()  => throw new PlatformNotSupportedException();

        //************* Task-based async public methods *************************
        [UnsupportedOSPlatform("browser")]
        public Task SendMailAsync(string from, string recipients, string? subject, string? body) => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("browser")]
        public Task SendMailAsync(MailMessage message) => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("browser")]
        public Task SendMailAsync(string from, string recipients, string? subject, string? body, CancellationToken cancellationToken) => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("browser")]
        public Task SendMailAsync(MailMessage message, CancellationToken cancellationToken) => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("browser")]
        protected void OnSendCompleted(AsyncCompletedEventArgs e) => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("browser")]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [UnsupportedOSPlatform("browser")]
        protected virtual void Dispose(bool disposing)
        { }
    }
}
