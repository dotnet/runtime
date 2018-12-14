// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
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

    public class SmtpClient : IDisposable
    {
        private string _host;
        private int _port;
        private bool _inCall;
        private bool _cancelled;
        private bool _timedOut;
        private string _targetName = null;
        private SmtpDeliveryMethod _deliveryMethod = SmtpDeliveryMethod.Network;
        private SmtpDeliveryFormat _deliveryFormat = SmtpDeliveryFormat.SevenBit; // Non-EAI default
        private string _pickupDirectoryLocation = null;
        private SmtpTransport _transport;
        private MailMessage _message; //required to prevent premature finalization
        private MailWriter _writer;
        private MailAddressCollection _recipients;
        private SendOrPostCallback _onSendCompletedDelegate;
        private Timer _timer;
        private ContextAwareResult _operationCompletedResult = null;
        private AsyncOperation _asyncOp = null;
        private static AsyncCallback s_contextSafeCompleteCallback = new AsyncCallback(ContextSafeCompleteCallback);
        private const int DefaultPort = 25;
        internal string clientDomain = null;
        private bool _disposed = false;
        private ServicePoint _servicePoint;
        // (async only) For when only some recipients fail.  We still send the e-mail to the others.
        private SmtpFailedRecipientException _failedRecipientException;
        // ports above this limit are invalid
        private const int MaxPortValue = 65535;
        public event SendCompletedEventHandler SendCompleted;

        public SmtpClient()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            try
            {
                Initialize();
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        public SmtpClient(string host)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, host);
            try
            {
                _host = host;
                Initialize();
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        public SmtpClient(string host, int port)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, host, port);
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
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        private void Initialize()
        {
            _transport = new SmtpTransport(this);
            if (NetEventSource.IsEnabled) NetEventSource.Associate(this, _transport);
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

            if (clientDomain == null)
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
                    clientDomain = sb.ToString();
                else
                    clientDomain = "LocalHost";
            }
        }


        public string Host
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

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value == string.Empty)
                {
                    throw new ArgumentException(SR.net_emptystringset, nameof(value));
                }

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
                return ReferenceEquals(_transport.Credentials, CredentialCache.DefaultNetworkCredentials);
            }
            set
            {
                if (InCall)
                {
                    throw new InvalidOperationException(SR.SmtpInvalidOperationDuringSend);
                }

                _transport.Credentials = value ? CredentialCache.DefaultNetworkCredentials : null;
            }
        }

        public ICredentialsByHost Credentials
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

                _transport.Credentials = value;
            }
        }

        public int Timeout
        {
            get
            {
                return _transport.Timeout;
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

                _transport.Timeout = value;
            }
        }

        public ServicePoint ServicePoint
        {
            get
            {
                CheckHostAndPort();
                if (_servicePoint == null)
                {
                    // This differs from desktop, where it uses an internal overload of FindServicePoint that just
                    // takes a string host and an int port, bypassing the need for a Uri. We workaround that here by
                    // creating an http Uri, simply for the purposes of getting an appropriate ServicePoint instance.
                    // This has some subtle impact on behavior, e.g. the returned ServicePoint's Address property will
                    // be usable, whereas in desktop it throws an exception that "This property is not supported for
                    // protocols that do not use URI."
                    _servicePoint = ServicePointManager.FindServicePoint(new Uri("mailto:" + _host + ":" + _port));
                }
                return _servicePoint;
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

        public string PickupDirectoryLocation
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

        public string TargetName
        {
            set { _targetName = value; }
            get { return _targetName; }
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

        internal MailWriter GetFileMailWriter(string pickupDirectory)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"{nameof(pickupDirectory)}={pickupDirectory}");

            if (!Path.IsPathRooted(pickupDirectory))
                throw new SmtpException(SR.SmtpNeedAbsolutePickupDirectory);
            string filename;
            string pathAndFilename;
            while (true)
            {
                filename = Guid.NewGuid().ToString() + ".eml";
                pathAndFilename = Path.Combine(pickupDirectory, filename);
                if (!File.Exists(pathAndFilename))
                    break;
            }

            FileStream fileStream = new FileStream(pathAndFilename, FileMode.CreateNew);
            return new MailWriter(fileStream);
        }

        protected void OnSendCompleted(AsyncCompletedEventArgs e)
        {
            SendCompleted?.Invoke(this, e);
        }

        private void SendCompletedWaitCallback(object operationState)
        {
            OnSendCompleted((AsyncCompletedEventArgs)operationState);
        }

        public void Send(string from, string recipients, string subject, string body)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            //validation happends in MailMessage constructor
            MailMessage mailMessage = new MailMessage(from, recipients, subject, body);
            Send(mailMessage);
        }

        public void Send(MailMessage message)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, message);

            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            try
            {
                if (NetEventSource.IsEnabled)
                {
                    NetEventSource.Info(this, $"DeliveryMethod={DeliveryMethod}");
                    NetEventSource.Associate(this, message);
                }

                SmtpFailedRecipientException recipientException = null;

                if (InCall)
                {
                    throw new InvalidOperationException(SR.net_inasync);
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
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
                    string pickupDirectory = PickupDirectoryLocation;

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

                            allowUnicode = IsUnicodeSupported(); // Determend by the DeliveryFormat paramiter
                            ValidateUnicodeRequirement(message, recipients, allowUnicode);
                            writer = GetFileMailWriter(pickupDirectory);
                            break;

                        case SmtpDeliveryMethod.Network:
                        default:
                            GetConnection();
                            // Detected durring GetConnection(), restrictable using the DeliveryFormat paramiter
                            allowUnicode = IsUnicodeSupported();
                            ValidateUnicodeRequirement(message, recipients, allowUnicode);
                            writer = _transport.SendMail(message.Sender ?? message.From, recipients,
                                message.BuildDeliveryStatusNotificationString(), allowUnicode, out recipientException);
                            break;
                    }
                    _message = message;
                    message.Send(writer, DeliveryMethod != SmtpDeliveryMethod.Network, allowUnicode);
                    writer.Close();
                    _transport.ReleaseConnection();

                    //throw if we couldn't send to any of the recipients
                    if (DeliveryMethod == SmtpDeliveryMethod.Network && recipientException != null)
                    {
                        throw recipientException;
                    }
                }
                catch (Exception e)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Error(this, e);

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
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        public void SendAsync(string from, string recipients, string subject, string body, object userToken)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            SendAsync(new MailMessage(from, recipients, subject, body), userToken);
        }

        public void SendAsync(MailMessage message, object userToken)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, message, userToken, _transport);

            try
            {
                if (InCall)
                {
                    throw new InvalidOperationException(SR.net_inasync);
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

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

                try
                {
                    InCall = true;
                    _cancelled = false;
                    _message = message;
                    string pickupDirectory = PickupDirectoryLocation;

                    CredentialCache cache;
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

                                _transport.ReleaseConnection();
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
                                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"Calling BeginConnect. Transport: {_transport}");
                                _transport.BeginGetConnection(_operationCompletedResult, ConnectCallback, _operationCompletedResult, Host, Port);
                                _operationCompletedResult.FinishPostingAsyncOp();
                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    InCall = false;

                    if (NetEventSource.IsEnabled) NetEventSource.Error(this, e);

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
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        private bool IsSystemNetworkCredentialInCache(CredentialCache cache)
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
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            try
            {
                if (!InCall || _cancelled)
                {
                    return;
                }

                _cancelled = true;
                Abort();
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }


        //************* Task-based async public methods *************************
        public Task SendMailAsync(string from, string recipients, string subject, string body)
        {
            var message = new MailMessage(from, recipients, subject, body);
            return SendMailAsync(message);
        }

        public Task SendMailAsync(MailMessage message)
        {
            // Create a TaskCompletionSource to represent the operation
            var tcs = new TaskCompletionSource<object>();

            // Register a handler that will transfer completion results to the TCS Task
            SendCompletedEventHandler handler = null;
            handler = (sender, e) => HandleCompletion(tcs, e, handler);
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

            // Return the task to represent the asynchronous operation
            return tcs.Task;
        }

        private void HandleCompletion(TaskCompletionSource<object> tcs, AsyncCompletedEventArgs e, SendCompletedEventHandler handler)
        {
            if (e.UserState == tcs)
            {
                try { SendCompleted -= handler; }
                finally
                {
                    if (e.Error != null) tcs.TrySetException(e.Error);
                    else if (e.Cancelled) tcs.TrySetCanceled();
                    else tcs.TrySetResult(null);
                }
            }
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

        private void TimeOutCallback(object state)
        {
            if (!_timedOut)
            {
                _timedOut = true;
                Abort();
            }
        }

        private void Complete(Exception exception, IAsyncResult result)
        {
            ContextAwareResult operationCompletedResult = (ContextAwareResult)result.AsyncState;
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
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
                    if (NetEventSource.IsEnabled) NetEventSource.Error(this, exception);
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
                    _transport.ReleaseConnection();
                }
            }
            finally
            {
                operationCompletedResult.InvokeCallback(exception);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Complete");
        }

        private static void ContextSafeCompleteCallback(IAsyncResult ar)
        {
            ContextAwareResult result = (ContextAwareResult)ar;
            SmtpClient client = (SmtpClient)ar.AsyncState;
            Exception exception = result.Result as Exception;
            AsyncOperation asyncOp = client._asyncOp;
            AsyncCompletedEventArgs eventArgs = new AsyncCompletedEventArgs(exception, client._cancelled, asyncOp.UserSuppliedState);
            client.InCall = false;
            client._failedRecipientException = null; // Reset before the next send.
            asyncOp.PostOperationCompleted(client._onSendCompletedDelegate, eventArgs);
        }

        private void SendMessageCallback(IAsyncResult result)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            try
            {
                _message.EndSend(result);
                // If some recipients failed but not others, throw AFTER sending the message.
                Complete(_failedRecipientException, result);
            }
            catch (Exception e)
            {
                Complete(e, result);
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }


        private void SendMailCallback(IAsyncResult result)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            try
            {
                _writer = _transport.EndSendMail(result);
                // If some recipients failed but not others, send the e-mail anyways, but then return the
                // "Non-fatal" exception reporting the failures.  The sync code path does it this way.
                // Fatal exceptions would have thrown above at transport.EndSendMail(...)
                SendMailAsyncResult sendResult = (SendMailAsyncResult)result;
                // Save these and throw them later in SendMessageCallback, after the message has sent.
                _failedRecipientException = sendResult.GetFailedRecipientException();
            }
            catch (Exception e)
            {
                Complete(e, result);
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
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
                    _message.BeginSend(_writer, DeliveryMethod != SmtpDeliveryMethod.Network,
                        IsUnicodeSupported(), new AsyncCallback(SendMessageCallback), result.AsyncState);
                }
            }
            catch (Exception e)
            {
                Complete(e, result);
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        private void ConnectCallback(IAsyncResult result)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            try
            {
                _transport.EndGetConnection(result);
                if (_cancelled)
                {
                    Complete(null, result);
                }
                else
                {
                    // Detected durring Begin/EndGetConnection, restrictable using DeliveryFormat
                    bool allowUnicode = IsUnicodeSupported();
                    ValidateUnicodeRequirement(_message, _recipients, allowUnicode);
                    _transport.BeginSendMail(_message.Sender ?? _message.From, _recipients,
                        _message.BuildDeliveryStatusNotificationString(), allowUnicode,
                        new AsyncCallback(SendMailCallback), result.AsyncState);
                }
            }
            catch (Exception e)
            {
                Complete(e, result);
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        // After we've estabilished a connection and initilized ServerSupportsEai,
        // check all the addresses for one that contains unicode in the username/localpart.
        // The localpart is the only thing we cannot succesfully downgrade.
        private void ValidateUnicodeRequirement(MailMessage message, MailAddressCollection recipients, bool allowUnicode)
        {
            // Check all recipients, to, from, sender, bcc, cc, etc...
            // GetSmtpAddress will throw if !allowUnicode and the username contains non-ascii
            foreach (MailAddress address in recipients)
            {
                address.GetSmtpAddress(allowUnicode);
            }
            if (message.Sender != null)
            {
                message.Sender.GetSmtpAddress(allowUnicode);
            }
            message.From.GetSmtpAddress(allowUnicode);
        }

        private void GetConnection()
        {
            if (!_transport.IsConnected)
            {
                _transport.GetConnection(_host, _port);
            }
        }

        private void Abort()
        {
            try
            {
                _transport.Abort();
            }
            catch
            {
            }
        }

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

                if ((_transport != null))
                {
                    _transport.ReleaseConnection();
                }

                if (_timer != null)
                {
                    _timer.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
