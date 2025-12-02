// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
        private string? _host;
        private int _port;
        private int _timeout = 100000;
        private bool _inCall;
        private bool _timedOut;
        private string? _targetName;
        private SmtpDeliveryMethod _deliveryMethod = SmtpDeliveryMethod.Network;
        private SmtpDeliveryFormat _deliveryFormat = SmtpDeliveryFormat.SevenBit; // Non-EAI default
        private string? _pickupDirectoryLocation;
        private SmtpTransport _transport;
        private const int DefaultPort = 25;
        internal string _clientDomain;
        private bool _disposed;
        private CancellationTokenSource _pendingSendCts;
        private ServicePoint? _servicePoint;
        // ports above this limit are invalid
        private const int MaxPortValue = 65535;
        public event SendCompletedEventHandler? SendCompleted;
        private bool _useDefaultCredentials;
        private ICredentialsByHost? _customCredentials;

        public SmtpClient()
        {
            Initialize();
        }

        public SmtpClient(string? host)
        {
            _host = host;
            Initialize();
        }

        public SmtpClient(string? host, int port)
        {
            try
            {
                ArgumentOutOfRangeException.ThrowIfNegative(port);

                _host = host;
                _port = port;
                Initialize();
            }
            finally
            {
            }
        }

        [MemberNotNull(nameof(_transport))]
        [MemberNotNull(nameof(_clientDomain))]
        [MemberNotNull(nameof(_pendingSendCts))]
        private void Initialize()
        {
            _transport = new SmtpTransport(this);
            _pendingSendCts = new CancellationTokenSource();
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Associate(this, _transport);

            if (!string.IsNullOrEmpty(_host))
            {
                _host = _host.Trim();
            }

            if (_port == 0)
            {
                _port = DefaultPort;
            }

            _targetName ??= "SMTPSVC/" + _host;

            if (_clientDomain == null)
            {
                // We use the local host name as the default client domain
                // for the client's EHLO or HELO message. This limits the
                // information about the host that we share. Additionally, the
                // FQDN is not available to us or useful to the server (internal
                // machine connecting to public server).

                // SMTP RFC's require ASCII only host names in the HELO/EHLO message.
                string clientDomainRaw = IPGlobalProperties.GetIPGlobalProperties().HostName;

                IdnMapping mapping = new IdnMapping();
                try
                {
                    clientDomainRaw = mapping.GetAscii(clientDomainRaw);
                }
                catch (ArgumentException) { }

                // For some inputs GetAscii may fail (bad Unicode, etc).  If that happens
                // we must strip out any non-ASCII characters.
                // If we end up with no characters left, we use the string "LocalHost".  This
                // matches Outlook behavior.
                StringBuilder sb = new StringBuilder();
                char ch;
                for (int i = 0; i < clientDomainRaw.Length; i++)
                {
                    ch = clientDomainRaw[i];
                    if (Ascii.IsValid(ch))
                        sb.Append(ch);
                }
                if (sb.Length > 0)
                    _clientDomain = sb.ToString();
                else
                    _clientDomain = "LocalHost";
            }
        }

        [DisallowNull]
        public string? Host
        {
            get
            {
                return _host;
            }
            set
            {
                if (_inCall)
                {
                    throw new InvalidOperationException(SR.SmtpInvalidOperationDuringSend);
                }

                ArgumentException.ThrowIfNullOrEmpty(value);

                value = value.Trim();

                if (value != _host)
                {
                    _host = value;
                    _servicePoint = null;
                }
            }
        }

        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                if (_inCall)
                {
                    throw new InvalidOperationException(SR.SmtpInvalidOperationDuringSend);
                }

                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);

                if (value != _port)
                {
                    _port = value;
                    _servicePoint = null;
                }
            }
        }

        public bool UseDefaultCredentials
        {
            get
            {
                return _useDefaultCredentials;
            }
            set
            {
                if (_inCall)
                {
                    throw new InvalidOperationException(SR.SmtpInvalidOperationDuringSend);
                }

                _useDefaultCredentials = value;
                UpdateTransportCredentials();
            }
        }

        public ICredentialsByHost? Credentials
        {
            get
            {
                return _transport.Credentials;
            }
            set
            {
                if (_inCall)
                {
                    throw new InvalidOperationException(SR.SmtpInvalidOperationDuringSend);
                }

                _customCredentials = value;
                UpdateTransportCredentials();
            }
        }

        private void UpdateTransportCredentials()
        {
            _transport.Credentials = _useDefaultCredentials ? CredentialCache.DefaultNetworkCredentials : _customCredentials;
        }

        public int Timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                if (_inCall)
                {
                    throw new InvalidOperationException(SR.SmtpInvalidOperationDuringSend);
                }

                ArgumentOutOfRangeException.ThrowIfNegative(value);

                _timeout = value;
            }
        }

        public ServicePoint ServicePoint
        {
            get
            {
                CheckHostAndPort();

                // This differs from desktop, where it uses an internal overload of FindServicePoint that just
                // takes a string host and an int port, bypassing the need for a Uri. We workaround that here by
                // creating an http Uri, simply for the purposes of getting an appropriate ServicePoint instance.
                // This has some subtle impact on behavior, e.g. the returned ServicePoint's Address property will
                // be usable, whereas in .NET Framework it throws an exception that "This property is not supported for
                // protocols that do not use URI."
#pragma warning disable SYSLIB0014
                return _servicePoint ??= ServicePointManager.FindServicePoint(new Uri($"mailto:{_host}:{_port}"));
#pragma warning restore SYSLIB0014
            }
        }

        public SmtpDeliveryMethod DeliveryMethod
        {
            get
            {
                return _deliveryMethod;
            }
            set
            {
                _deliveryMethod = value;
            }
        }

        public SmtpDeliveryFormat DeliveryFormat
        {
            get
            {
                return _deliveryFormat;
            }
            set
            {
                _deliveryFormat = value;
            }
        }

        public string? PickupDirectoryLocation
        {
            get
            {
                return _pickupDirectoryLocation;
            }
            set
            {
                _pickupDirectoryLocation = value;
            }
        }

        /// <summary>
        ///    <para>Set to true if we need SSL</para>
        /// </summary>
        public bool EnableSsl
        {
            get
            {
                return _transport.EnableSsl;
            }
            set
            {
                _transport.EnableSsl = value;
            }
        }

        /// <summary>
        /// Certificates used by the client for establishing an SSL connection with the server.
        /// </summary>
        public X509CertificateCollection ClientCertificates
        {
            get
            {
                return _transport.ClientCertificates;
            }
        }

        public string? TargetName
        {
            get { return _targetName; }
            set { _targetName = value; }
        }

        private bool ServerSupportsEai
        {
            get
            {
                return _transport.ServerSupportsEai;
            }
        }

        private bool IsUnicodeSupported()
        {
            if (DeliveryMethod == SmtpDeliveryMethod.Network)
            {
                return (ServerSupportsEai && (DeliveryFormat == SmtpDeliveryFormat.International));
            }
            else
            { // Pickup directories
                return (DeliveryFormat == SmtpDeliveryFormat.International);
            }
        }

        internal MailWriter GetFileMailWriter(string? pickupDirectory)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"{nameof(pickupDirectory)}={pickupDirectory}");

            if (!Path.IsPathRooted(pickupDirectory))
                throw new SmtpException(SR.SmtpNeedAbsolutePickupDirectory);
            string filename;
            string pathAndFilename;
            while (true)
            {
                filename = $"{Guid.NewGuid()}.eml";
                pathAndFilename = Path.Combine(pickupDirectory, filename);
                if (!File.Exists(pathAndFilename))
                    break;
            }

            FileStream fileStream = new FileStream(pathAndFilename, FileMode.CreateNew);
            return new MailWriter(fileStream, encodeForTransport: false);
        }

        protected void OnSendCompleted(AsyncCompletedEventArgs e)
        {
            SendCompleted?.Invoke(this, e);
        }

        public void Send(string from, string recipients, string? subject, string? body)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            //validation happens in MailMessage constructor
            MailMessage mailMessage = new MailMessage(from, recipients, subject, body);
            Send(mailMessage);
        }
        public void Send(MailMessage message)
        {
            (Exception? ex, bool _) = SendAsyncInternal<SyncReadWriteAdapter>(message, false, null).GetAwaiter().GetResult();
            if (ex != null)
            {
                ExceptionDispatchInfo.Throw(ex);
            }
        }

        /// <summary>
        /// Sends the specified message asynchronously.
        /// </summary>
        /// <typeparam name="TIOAdapter">The type of the I/O adapter to use for sending the message.</typeparam>
        /// <param name="message">The <see cref="MailMessage"/> to send.</param>
        /// <param name="invokeSendCompleted">Whether to invoke the SendCompleted event after sending. This applies only to asynchronous completions of the operations, synchronous failures (such as argument validations) are thrown directly from this method.</param>
        /// <param name="userToken">An optional user token to pass to the SendCompleted event, ignored if <paramref name="invokeSendCompleted"/> is false.</param>
        /// <param name="forceWrapExceptions">If true, wrap exceptions in SmtpException.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the send operation.</param>
        private async Task<(Exception? ex, bool synchronous)> SendAsyncInternal<TIOAdapter>(MailMessage message, bool invokeSendCompleted, object? userToken, bool forceWrapExceptions = false, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            if (_disposed)
            {
                return (ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(typeof(SmtpClient).FullName)), true);
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, $"DeliveryMethod={DeliveryMethod}");
                NetEventSource.Associate(this, message);
            }

            CancellationTokenSource? cts = null;
            if (cancellationToken.CanBeCanceled)
            {
                // If the caller provided a cancellation token, we link it to our pending send cancellation token source.
                cts = CancellationTokenSource.CreateLinkedTokenSource(_pendingSendCts.Token, cancellationToken);
                cancellationToken = cts.Token;
            }
            else
            {
                cancellationToken = _pendingSendCts.Token;
            }

            if (Interlocked.Exchange(ref _inCall, true))
            {
                return (ExceptionDispatchInfo.SetCurrentStackTrace(new InvalidOperationException(SR.net_inasync)), true);
            }

            // initial exceptions should be thrown directly, not via callback
            bool synchronous = true;
            bool canceled = false;
            Timer? timer = null;
            Exception? exception = null;
            try
            {
                ArgumentNullException.ThrowIfNull(message);

                if (DeliveryMethod == SmtpDeliveryMethod.Network)
                    CheckHostAndPort();

                MailAddressCollection recipients = new MailAddressCollection();

                if (message.From == null)
                {
                    throw new InvalidOperationException(SR.SmtpFromRequired);
                }

                if (message.To != null)
                {
                    foreach (MailAddress address in message.To)
                    {
                        recipients.Add(address);
                    }
                }
                if (message.Bcc != null)
                {
                    foreach (MailAddress address in message.Bcc)
                    {
                        recipients.Add(address);
                    }
                }
                if (message.CC != null)
                {
                    foreach (MailAddress address in message.CC)
                    {
                        recipients.Add(address);
                    }
                }

                if (recipients.Count == 0)
                {
                    throw new InvalidOperationException(SR.SmtpRecipientRequired);
                }

                // argument validation is done, wrap all exceptions below this point
                forceWrapExceptions = true;

                _timedOut = false;
                timer = new Timer(new TimerCallback(TimeOutCallback), null, Timeout, Timeout);
                bool allowUnicode = false;
                string? pickupDirectory = PickupDirectoryLocation;

                MailWriter writer;
                List<SmtpFailedRecipientException>? failedRecipientExceptions = null;
                switch (DeliveryMethod)
                {
                    case SmtpDeliveryMethod.PickupDirectoryFromIis:
                        throw new NotSupportedException(SR.SmtpGetIisPickupDirectoryNotSupported);

                    case SmtpDeliveryMethod.SpecifiedPickupDirectory:
                        if (EnableSsl)
                        {
                            throw new SmtpException(SR.SmtpPickupDirectoryDoesnotSupportSsl);
                        }

                        allowUnicode = IsUnicodeSupported(); // Determined by the DeliveryFormat parameter
                        ValidateUnicodeRequirement(message, recipients, allowUnicode);
                        writer = GetFileMailWriter(pickupDirectory);
                        break;

                    case SmtpDeliveryMethod.Network:
                    default:
                        synchronous = false;
                        await EnsureConnection<TIOAdapter>(cancellationToken).ConfigureAwait(false);
                        // Detected during EnsureConnection(), restrictable using the DeliveryFormat parameter
                        allowUnicode = IsUnicodeSupported();
                        ValidateUnicodeRequirement(message, recipients, allowUnicode);
                        (writer, failedRecipientExceptions) = await _transport.SendMailAsync<TIOAdapter>(message.Sender ?? message.From, recipients,
                            message.BuildDeliveryStatusNotificationString(), allowUnicode, cancellationToken).ConfigureAwait(false);
                        break;
                }
                synchronous = false;
                await message.SendAsync<TIOAdapter>(writer, DeliveryMethod != SmtpDeliveryMethod.Network, allowUnicode, cancellationToken).ConfigureAwait(false);
                writer.Close();

                //throw if we couldn't send to any of the recipients
                if (failedRecipientExceptions != null)
                {
                    var e = failedRecipientExceptions.Count == 1
                        ? failedRecipientExceptions[0]
                        : new SmtpFailedRecipientsException(failedRecipientExceptions, false);
                    throw e;
                }
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, e);

                exception = ProcessException(e, ref canceled, forceWrapExceptions, _timedOut);
                Exception ProcessException(Exception e, ref bool canceled, bool forceWrapExceptions, bool timedOut)
                {
                    if (e is SmtpFailedRecipientException && !((SmtpFailedRecipientException)e).fatal)
                    {
                        return e;
                    }

                    canceled = e is OperationCanceledException;

                    Abort();
                    if (timedOut)
                    {
                        return ExceptionDispatchInfo.SetCurrentStackTrace(new SmtpException(SR.net_timeout));
                    }

                    if (!forceWrapExceptions ||
                        // for compatibility reasons, don't wrap these exceptions during sync executions
                        (typeof(TIOAdapter) == typeof(SyncReadWriteAdapter) && (e is SecurityException or AuthenticationException)) ||
                        e is SmtpException ||
                        e is OperationCanceledException)
                    {
                        return e;
                    }

                    return ExceptionDispatchInfo.SetCurrentStackTrace(new SmtpException(SR.SmtpSendMailFailure, e));
                }
            }
            finally
            {
                _inCall = false;
                timer?.Dispose();

                // SendCompleted event should ever be invoked only for asynchronous send completions.
                if (invokeSendCompleted && !synchronous)
                {
                    AsyncCompletedEventArgs eventArgs = new(canceled ? null : exception, canceled, userToken);
                    OnSendCompleted(eventArgs);
                }
            }

            return (exception, synchronous);
        }

        public void SendAsync(string from, string recipients, string? subject, string? body, object? userToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            SendAsync(new MailMessage(from, recipients, subject, body), userToken);
        }

        public void SendAsync(MailMessage message, object? userToken)
        {
            Task<(Exception? ex, bool _)> task = SendAsyncInternal<AsyncReadWriteAdapter>(message, true, userToken, true);

            if (task.IsCompleted)
            {
                // If the task completed unwrap the exception (if any)
                var (ex, sync) = task.GetAwaiter().GetResult();

                if (ex != null && sync)
                {
                    ExceptionDispatchInfo.Throw(ex);
                }
            }
        }

        private static bool IsSystemNetworkCredentialInCache(CredentialCache cache)
        {
            // Check if SystemNetworkCredential is in given cache.
            foreach (NetworkCredential credential in cache)
            {
                if (ReferenceEquals(credential, CredentialCache.DefaultNetworkCredentials))
                {
                    return true;
                }
            }

            return false;
        }

        public void SendAsyncCancel()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_inCall)
            {
                return;
            }

            // With every request we link this cancellation token source.
            CancellationTokenSource currentCts = Interlocked.Exchange(ref _pendingSendCts, new CancellationTokenSource());

            currentCts.Cancel();
            currentCts.Dispose();
        }


        //************* Task-based async public methods *************************
        public Task SendMailAsync(string from, string recipients, string? subject, string? body)
        {
            var message = new MailMessage(from, recipients, subject, body);
            return SendMailAsync(message, cancellationToken: default);
        }

        public Task SendMailAsync(MailMessage message)
        {
            return SendMailAsync(message, cancellationToken: default);
        }

        public Task SendMailAsync(string from, string recipients, string? subject, string? body, CancellationToken cancellationToken)
        {
            var message = new MailMessage(from, recipients, subject, body);
            return SendMailAsync(message, cancellationToken);
        }

        public Task SendMailAsync(MailMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            Task<(Exception?, bool)> task = SendAsyncInternal<AsyncReadWriteAdapter>(message, false, null, true, cancellationToken);

            if (task.IsCompleted)
            {
                // If the task completed unwrap the exception (if any)
                var (ex, sync) = task.GetAwaiter().GetResult();

                if (ex != null)
                {
                    if (sync)
                    {
                        ExceptionDispatchInfo.Throw(ex);
                    }

                    return Task.FromException(ex);
                }

                return Task.CompletedTask;
            }

            return WaitAndRethrowIfNeeded(task);
            static async Task WaitAndRethrowIfNeeded(Task<(Exception? ex, bool _)> task)
            {
                var (ex, _) = await task.ConfigureAwait(false);
                if (ex != null)
                {
                    ExceptionDispatchInfo.Throw(ex);
                }
            }
        }

        //*********************************
        // private methods
        //********************************
        private void CheckHostAndPort()
        {
            if (string.IsNullOrEmpty(_host))
            {
                throw new InvalidOperationException(SR.UnspecifiedHost);
            }
            if (_port <= 0 || _port > MaxPortValue)
            {
                throw new InvalidOperationException(SR.InvalidPort);
            }
        }

        private void TimeOutCallback(object? state)
        {
            if (!_timedOut)
            {
                _timedOut = true;
                Abort();
            }
        }

        // After we've estabilished a connection and initialized ServerSupportsEai,
        // check all the addresses for one that contains unicode in the username/localpart.
        // The localpart is the only thing we cannot successfully downgrade.
        private static void ValidateUnicodeRequirement(MailMessage message, MailAddressCollection recipients, bool allowUnicode)
        {
            // Check all recipients, to, from, sender, bcc, cc, etc...
            // GetSmtpAddress will throw if !allowUnicode and the username contains non-ascii
            foreach (MailAddress address in recipients)
            {
                address.GetSmtpAddress(allowUnicode);
            }

            message.Sender?.GetSmtpAddress(allowUnicode);
            message.From!.GetSmtpAddress(allowUnicode);
        }

        private void GetConnection()
        {
            if (!_transport.IsConnected)
            {
                _transport.GetConnection(_host!, _port);
            }
        }

        private Task EnsureConnection<TIOAdapter>(CancellationToken cancellationToken) where TIOAdapter : IReadWriteAdapter
        {
            if (_transport.IsConnected)
            {
                return Task.CompletedTask;
            }

            return _transport.GetConnectionAsync<TIOAdapter>(_host!, _port, cancellationToken);
        }

        private void Abort() => _transport.Abort();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                if (_inCall)
                {
                    _pendingSendCts.Cancel();
                    Abort();
                }
                else
                {
                    _transport?.ReleaseConnection();
                }
            }
        }
    }
}
