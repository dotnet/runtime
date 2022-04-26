// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
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
        private bool _cancelled;
        private bool _timedOut;
        private string? _targetName;
        private SmtpDeliveryMethod _deliveryMethod = SmtpDeliveryMethod.Network;
        private SmtpDeliveryFormat _deliveryFormat = SmtpDeliveryFormat.SevenBit; // Non-EAI default
        private string? _pickupDirectoryLocation;
        private SmtpTransport _transport;
        private MailMessage? _message; //required to prevent premature finalization
        private MailWriter? _writer;
        private MailAddressCollection? _recipients;
        private SendOrPostCallback _onSendCompletedDelegate;
        private Timer? _timer;
        private ContextAwareResult? _operationCompletedResult;
        private AsyncOperation? _asyncOp;
        private static readonly AsyncCallback s_contextSafeCompleteCallback = new AsyncCallback(ContextSafeCompleteCallback);
        private const int DefaultPort = 25;
        internal string _clientDomain;
        private bool _disposed;
        private ServicePoint? _servicePoint;
        // (async only) For when only some recipients fail.  We still send the e-mail to the others.
        private SmtpFailedRecipientException? _failedRecipientException;
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
                if (port < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(port));
                }

                _host = host;
                _port = port;
                Initialize();
            }
            finally
            {
            }
        }

        [MemberNotNull(nameof(_transport))]
        [MemberNotNull(nameof(_onSendCompletedDelegate))]
        [MemberNotNull(nameof(_clientDomain))]
        private void Initialize()
        {
            _transport = new SmtpTransport(this);
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Associate(this, _transport);
            _onSendCompletedDelegate = new SendOrPostCallback(SendCompletedWaitCallback);

            if (_host != null && _host.Length != 0)
            {
                _host = _host.Trim();
            }

            if (_port == 0)
            {
                _port = DefaultPort;
            }

            if (_targetName == null)
                _targetName = "SMTPSVC/" + _host;

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
                    if ((ushort)ch <= 0x7F)
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
                if (InCall)
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
                if (InCall)
                {
                    throw new InvalidOperationException(SR.SmtpInvalidOperationDuringSend);
                }

                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

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
                if (InCall)
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
                if (InCall)
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
                if (InCall)
                {
                    throw new InvalidOperationException(SR.SmtpInvalidOperationDuringSend);
                }

                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

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
                return _servicePoint ??= ServicePointManager.FindServicePoint(new Uri("mailto:" + _host + ":" + _port));
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

        private void SendCompletedWaitCallback(object? operationState)
        {
            OnSendCompleted((AsyncCompletedEventArgs)operationState!);
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
            ArgumentNullException.ThrowIfNull(message);

            ObjectDisposedException.ThrowIf(_disposed, this);

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, $"DeliveryMethod={DeliveryMethod}");
                NetEventSource.Associate(this, message);
            }

            SmtpFailedRecipientException? recipientException = null;

            if (InCall)
            {
                throw new InvalidOperationException(SR.net_inasync);
            }

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

            _transport.IdentityRequired = false;  // everything completes on the same thread.

            try
            {
                InCall = true;
                _timedOut = false;
                _timer = new Timer(new TimerCallback(TimeOutCallback), null, Timeout, Timeout);
                bool allowUnicode = false;
                string? pickupDirectory = PickupDirectoryLocation;

                MailWriter writer;
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
                        GetConnection();
                        // Detected during GetConnection(), restrictable using the DeliveryFormat parameter
                        allowUnicode = IsUnicodeSupported();
                        ValidateUnicodeRequirement(message, recipients, allowUnicode);
                        writer = _transport.SendMail(message.Sender ?? message.From, recipients,
                            message.BuildDeliveryStatusNotificationString(), allowUnicode, out recipientException);
                        break;
                }
                _message = message;
                message.Send(writer, DeliveryMethod != SmtpDeliveryMethod.Network, allowUnicode);
                writer.Close();

                //throw if we couldn't send to any of the recipients
                if (DeliveryMethod == SmtpDeliveryMethod.Network && recipientException != null)
                {
                    throw recipientException;
                }
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, e);

                if (e is SmtpFailedRecipientException && !((SmtpFailedRecipientException)e).fatal)
                {
                    throw;
                }

                Abort();
                if (_timedOut)
                {
                    throw new SmtpException(SR.net_timeout);
                }

                if (e is SecurityException ||
                    e is AuthenticationException ||
                    e is SmtpException)
                {
                    throw;
                }

                throw new SmtpException(SR.SmtpSendMailFailure, e);
            }
            finally
            {
                InCall = false;
                if (_timer != null)
                {
                    _timer.Dispose();
                }
            }
        }

        public void SendAsync(string from, string recipients, string? subject, string? body, object? userToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            SendAsync(new MailMessage(from, recipients, subject, body), userToken);
        }

        public void SendAsync(MailMessage message, object? userToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            try
            {
                if (InCall)
                {
                    throw new InvalidOperationException(SR.net_inasync);
                }

                ArgumentNullException.ThrowIfNull(message);

                if (DeliveryMethod == SmtpDeliveryMethod.Network)
                    CheckHostAndPort();

                _recipients = new MailAddressCollection();

                if (message.From == null)
                {
                    throw new InvalidOperationException(SR.SmtpFromRequired);
                }

                if (message.To != null)
                {
                    foreach (MailAddress address in message.To)
                    {
                        _recipients.Add(address);
                    }
                }
                if (message.Bcc != null)
                {
                    foreach (MailAddress address in message.Bcc)
                    {
                        _recipients.Add(address);
                    }
                }
                if (message.CC != null)
                {
                    foreach (MailAddress address in message.CC)
                    {
                        _recipients.Add(address);
                    }
                }

                if (_recipients.Count == 0)
                {
                    throw new InvalidOperationException(SR.SmtpRecipientRequired);
                }

                InCall = true;
                _cancelled = false;
                _message = message;
                string? pickupDirectory = PickupDirectoryLocation;

                CredentialCache? cache;
                // Skip token capturing if no credentials are used or they don't include a default one.
                // Also do capture the token if ICredential is not of CredentialCache type so we don't know what the exact credential response will be.
                _transport.IdentityRequired = Credentials != null && (ReferenceEquals(Credentials, CredentialCache.DefaultNetworkCredentials) || (cache = Credentials as CredentialCache) == null || IsSystemNetworkCredentialInCache(cache));

                _asyncOp = AsyncOperationManager.CreateOperation(userToken);
                switch (DeliveryMethod)
                {
                    case SmtpDeliveryMethod.PickupDirectoryFromIis:
                        throw new NotSupportedException(SR.SmtpGetIisPickupDirectoryNotSupported);

                    case SmtpDeliveryMethod.SpecifiedPickupDirectory:
                        {
                            if (EnableSsl)
                            {
                                throw new SmtpException(SR.SmtpPickupDirectoryDoesnotSupportSsl);
                            }

                            _writer = GetFileMailWriter(pickupDirectory);
                            bool allowUnicode = IsUnicodeSupported();
                            ValidateUnicodeRequirement(message, _recipients, allowUnicode);
                            message.Send(_writer, true, allowUnicode);

                            if (_writer != null)
                                _writer.Close();

                            AsyncCompletedEventArgs eventArgs = new AsyncCompletedEventArgs(null, false, _asyncOp.UserSuppliedState);
                            InCall = false;
                            _asyncOp.PostOperationCompleted(_onSendCompletedDelegate, eventArgs);
                            break;
                        }

                    case SmtpDeliveryMethod.Network:
                    default:
                        _operationCompletedResult = new ContextAwareResult(_transport.IdentityRequired, true, null, this, s_contextSafeCompleteCallback);
                        lock (_operationCompletedResult.StartPostingAsyncOp())
                        {
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Calling BeginConnect. Transport: {_transport}");
                            _transport.BeginGetConnection(_operationCompletedResult, ConnectCallback, _operationCompletedResult, Host!, Port);
                            _operationCompletedResult.FinishPostingAsyncOp();
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                InCall = false;

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, e);

                if (e is SmtpFailedRecipientException && !((SmtpFailedRecipientException)e).fatal)
                {
                    throw;
                }

                Abort();

                if (e is SecurityException ||
                    e is AuthenticationException ||
                    e is SmtpException)
                {
                    throw;
                }

                throw new SmtpException(SR.SmtpSendMailFailure, e);
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

            if (!InCall || _cancelled)
            {
                return;
            }

            _cancelled = true;
            Abort();
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

            // Create a TaskCompletionSource to represent the operation
            var tcs = new TaskCompletionSource();

            CancellationTokenRegistration ctr = default;

            // Indicates whether the CTR has been set - captured in handler
            int state = 0;

            // Register a handler that will transfer completion results to the TCS Task
            SendCompletedEventHandler? handler = null;
            handler = (sender, e) =>
            {
                if (e.UserState == tcs)
                {
                    try
                    {
                        ((SmtpClient)sender).SendCompleted -= handler;
                        if (Interlocked.Exchange(ref state, 1) != 0)
                        {
                            // A CTR has been set, we have to wait until it completes before completing the task
                            ctr.Dispose();
                        }
                    }
                    catch (ObjectDisposedException) { } // SendAsyncCancel will throw if SmtpClient was disposed
                    finally
                    {
                        if (e.Error != null) tcs.TrySetException(e.Error);
                        else if (e.Cancelled) tcs.TrySetCanceled();
                        else tcs.TrySetResult();
                    }
                }
            };
            SendCompleted += handler;

            // Start the async operation.
            try
            {
                SendAsync(message, tcs);
            }
            catch
            {
                SendCompleted -= handler;
                throw;
            }

            ctr = cancellationToken.Register(s =>
            {
                ((SmtpClient)s!).SendAsyncCancel();
            }, this);

            if (Interlocked.Exchange(ref state, 1) != 0)
            {
                // SendCompleted was already invoked, ensure the CTR completes before returning the task
                ctr.Dispose();
            }

            // Return the task to represent the asynchronous operation
            return tcs.Task;
        }


        //*********************************
        // private methods
        //********************************
        internal bool InCall
        {
            get
            {
                return _inCall;
            }
            set
            {
                _inCall = value;
            }
        }

        private void CheckHostAndPort()
        {
            if (_host == null || _host.Length == 0)
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

        private void Complete(Exception? exception, IAsyncResult result)
        {
            ContextAwareResult operationCompletedResult = (ContextAwareResult)result.AsyncState!;
            try
            {
                if (_cancelled)
                {
                    //any exceptions were probably caused by cancellation, clear it.
                    exception = null;
                    Abort();
                }
                // An individual failed recipient exception is benign, only abort here if ALL the recipients failed.
                else if (exception != null && (!(exception is SmtpFailedRecipientException) || ((SmtpFailedRecipientException)exception).fatal))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, exception);
                    Abort();

                    if (!(exception is SmtpException))
                    {
                        exception = new SmtpException(SR.SmtpSendMailFailure, exception);
                    }
                }
                else
                {
                    if (_writer != null)
                    {
                        try
                        {
                            _writer.Close();
                        }
                        // Close may result in a DataStopCommand and the server may return error codes at this time.
                        catch (SmtpException se)
                        {
                            exception = se;
                        }
                    }
                }
            }
            finally
            {
                operationCompletedResult.InvokeCallback(exception);
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Complete");
        }

        private static void ContextSafeCompleteCallback(IAsyncResult ar)
        {
            ContextAwareResult result = (ContextAwareResult)ar;
            SmtpClient client = (SmtpClient)ar.AsyncState!;
            Exception? exception = result.Result as Exception;
            AsyncOperation asyncOp = client._asyncOp!;
            AsyncCompletedEventArgs eventArgs = new AsyncCompletedEventArgs(exception, client._cancelled, asyncOp.UserSuppliedState);
            client.InCall = false;
            client._failedRecipientException = null; // Reset before the next send.
            asyncOp.PostOperationCompleted(client._onSendCompletedDelegate, eventArgs);
        }

        private void SendMessageCallback(IAsyncResult result)
        {
            try
            {
                _message!.EndSend(result);
                // If some recipients failed but not others, throw AFTER sending the message.
                Complete(_failedRecipientException, result);
            }
            catch (Exception e)
            {
                Complete(e, result);
            }
        }


        private void SendMailCallback(IAsyncResult result)
        {
            try
            {
                _writer = SmtpTransport.EndSendMail(result);
                // If some recipients failed but not others, send the e-mail anyway, but then return the
                // "Non-fatal" exception reporting the failures.  The sync code path does it this way.
                // Fatal exceptions would have thrown above at transport.EndSendMail(...)
                SendMailAsyncResult sendResult = (SendMailAsyncResult)result;
                // Save these and throw them later in SendMessageCallback, after the message has sent.
                _failedRecipientException = sendResult.GetFailedRecipientException();
            }
            catch (Exception e)
            {
                Complete(e, result);
                return;
            }

            try
            {
                if (_cancelled)
                {
                    Complete(null, result);
                }
                else
                {
                    _message!.BeginSend(_writer, DeliveryMethod != SmtpDeliveryMethod.Network,
                        IsUnicodeSupported(), new AsyncCallback(SendMessageCallback), result.AsyncState!);
                }
            }
            catch (Exception e)
            {
                Complete(e, result);
            }
        }

        private void ConnectCallback(IAsyncResult result)
        {
            try
            {
                SmtpTransport.EndGetConnection(result);
                if (_cancelled)
                {
                    Complete(null, result);
                }
                else
                {
                    // Detected during Begin/EndGetConnection, restrictable using DeliveryFormat
                    bool allowUnicode = IsUnicodeSupported();
                    ValidateUnicodeRequirement(_message!, _recipients!, allowUnicode);
                    _transport.BeginSendMail(_message!.Sender ?? _message.From!, _recipients!,
                        _message.BuildDeliveryStatusNotificationString(), allowUnicode,
                        new AsyncCallback(SendMailCallback), result.AsyncState!);
                }
            }
            catch (Exception e)
            {
                Complete(e, result);
            }
        }

        // After we've estabilished a connection and initilized ServerSupportsEai,
        // check all the addresses for one that contains unicode in the username/localpart.
        // The localpart is the only thing we cannot succesfully downgrade.
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
                if (InCall && !_cancelled)
                {
                    _cancelled = true;
                    Abort();
                }
                else
                {
                    _transport?.ReleaseConnection();
                }
                _timer?.Dispose();
                _disposed = true;
            }
        }
    }
}
