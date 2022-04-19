// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
        public event SendCompletedEventHandler? SendCompleted;
#pragma warning restore CS0067
        public SmtpClient()
        {
            Initialize();
        }

        public SmtpClient(string? host)
        {
            Initialize();
        }

        public SmtpClient(string? host, int port)
        {
            Initialize();
        }

        private static void Initialize()
        {
            throw new PlatformNotSupportedException();
        }

        [DisallowNull]
        public string? Host
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public int Port
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public bool UseDefaultCredentials
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public ICredentialsByHost? Credentials
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public int Timeout
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public ServicePoint ServicePoint
        {
            get => throw new PlatformNotSupportedException();
        }

        public SmtpDeliveryMethod DeliveryMethod
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public SmtpDeliveryFormat DeliveryFormat
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public string? PickupDirectoryLocation
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        /// <summary>
        ///    <para>Set to true if we need SSL</para>
        /// </summary>
        public bool EnableSsl
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Certificates used by the client for establishing an SSL connection with the server.
        /// </summary>
        public X509CertificateCollection ClientCertificates => throw new PlatformNotSupportedException();

        public string? TargetName
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        private static bool ServerSupportsEai => throw new PlatformNotSupportedException();

        public void Send(string from, string recipients, string? subject, string? body) => throw new PlatformNotSupportedException();

        public void Send(MailMessage message) => throw new PlatformNotSupportedException();

        public void SendAsync(string from, string recipients, string? subject, string? body, object? userToken) => throw new PlatformNotSupportedException();

        public void SendAsync(MailMessage message, object? userToken) => throw new PlatformNotSupportedException();

        public void SendAsyncCancel()  => throw new PlatformNotSupportedException();

        //************* Task-based async public methods *************************
        public Task SendMailAsync(string from, string recipients, string? subject, string? body) => throw new PlatformNotSupportedException();

        public Task SendMailAsync(MailMessage message) => throw new PlatformNotSupportedException();

        public Task SendMailAsync(string from, string recipients, string? subject, string? body, CancellationToken cancellationToken) => throw new PlatformNotSupportedException();

        public Task SendMailAsync(MailMessage message, CancellationToken cancellationToken) => throw new PlatformNotSupportedException();

        protected void OnSendCompleted(AsyncCompletedEventArgs e) => throw new PlatformNotSupportedException();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        { }
    }
}
