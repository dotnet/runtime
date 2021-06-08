// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net.Mail
{
    internal sealed class SmtpTransport
    {
        internal const int DefaultPort = 25;

        private readonly ISmtpAuthenticationModule[] _authenticationModules;
        private SmtpConnection? _connection;
        private readonly SmtpClient _client;
        private ICredentialsByHost? _credentials;
        private readonly List<SmtpFailedRecipientException> _failedRecipientExceptions = new List<SmtpFailedRecipientException>();
        private bool _identityRequired;
        private bool _shouldAbort;

        private bool _enableSsl;
        private X509CertificateCollection? _clientCertificates;

        internal SmtpTransport(SmtpClient client) : this(client, SmtpAuthenticationManager.GetModules())
        {
        }

        internal SmtpTransport(SmtpClient client, ISmtpAuthenticationModule[] authenticationModules)
        {
            _client = client;

            if (authenticationModules == null)
            {
                throw new ArgumentNullException(nameof(authenticationModules));
            }

            _authenticationModules = authenticationModules;
        }

        internal ICredentialsByHost? Credentials
        {
            get
            {
                return _credentials;
            }
            set
            {
                _credentials = value;
            }
        }

        internal bool IdentityRequired
        {
            get
            {
                return _identityRequired;
            }

            set
            {
                _identityRequired = value;
            }
        }

        internal bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsConnected;
            }
        }

        internal bool EnableSsl
        {
            get
            {
                return _enableSsl;
            }
            set
            {
                _enableSsl = value;
            }
        }

        internal X509CertificateCollection ClientCertificates => _clientCertificates ??= new X509CertificateCollection();

        internal bool ServerSupportsEai
        {
            get { return _connection != null && _connection.ServerSupportsEai; }
        }

        internal void GetConnection(string host, int port)
        {
            try
            {
                lock (this)
                {
                    _connection = new SmtpConnection(this, _client, _credentials, _authenticationModules);
                    if (_shouldAbort)
                    {
                        _connection.Abort();
                    }
                    _shouldAbort = false;
                }

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Associate(this, _connection);

                if (EnableSsl)
                {
                    _connection.EnableSsl = true;
                    _connection.ClientCertificates = ClientCertificates;
                }

                _connection.GetConnection(host, port);
            }
            finally { }
        }

        internal IAsyncResult BeginGetConnection(ContextAwareResult outerResult, AsyncCallback? callback, object? state, string host, int port)
        {
            IAsyncResult? result = null;
            try
            {
                _connection = new SmtpConnection(this, _client, _credentials, _authenticationModules);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Associate(this, _connection);
                if (EnableSsl)
                {
                    _connection.EnableSsl = true;
                    _connection.ClientCertificates = ClientCertificates;
                }

                result = _connection.BeginGetConnection(outerResult, callback, state, host, port);
            }
            catch (Exception innerException)
            {
                throw new SmtpException(SR.MailHostNotFound, innerException);
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Sync completion");

            return result;
        }

        internal void EndGetConnection(IAsyncResult result)
        {
            _connection!.EndGetConnection(result);
        }

        internal IAsyncResult BeginSendMail(MailAddress sender, MailAddressCollection recipients,
            string deliveryNotify, bool allowUnicode, AsyncCallback? callback, object? state)
        {
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            if (recipients == null)
            {
                throw new ArgumentNullException(nameof(recipients));
            }

            SendMailAsyncResult result = new SendMailAsyncResult(_connection!, sender, recipients,
                allowUnicode, _connection!.DSNEnabled ? deliveryNotify : null,
                callback, state);
            result.Send();
            return result;
        }

        internal void ReleaseConnection()
        {
            _connection?.ReleaseConnection();
        }

        internal void Abort()
        {
            lock (this)
            {
                if (_connection != null)
                {
                    _connection.Abort();
                }
                else
                {
                    _shouldAbort = true;
                }
            }
        }

        internal MailWriter EndSendMail(IAsyncResult result)
        {
            try
            {
                return SendMailAsyncResult.End(result);
            }
            finally
            {
            }
        }

        internal MailWriter SendMail(MailAddress sender, MailAddressCollection recipients, string deliveryNotify,
            bool allowUnicode, out SmtpFailedRecipientException? exception)
        {
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            if (recipients == null)
            {
                throw new ArgumentNullException(nameof(recipients));
            }

            MailCommand.Send(_connection!, SmtpCommands.Mail, sender, allowUnicode);
            _failedRecipientExceptions.Clear();

            exception = null;
            string response;
            foreach (MailAddress address in recipients)
            {
                string smtpAddress = address.GetSmtpAddress(allowUnicode);
                string to = smtpAddress + (_connection!.DSNEnabled ? deliveryNotify : string.Empty);
                if (!RecipientCommand.Send(_connection, to, out response))
                {
                    _failedRecipientExceptions.Add(
                        new SmtpFailedRecipientException(_connection.Reader!.StatusCode, smtpAddress, response));
                }
            }

            if (_failedRecipientExceptions.Count > 0)
            {
                if (_failedRecipientExceptions.Count == 1)
                {
                    exception = _failedRecipientExceptions[0];
                }
                else
                {
                    exception = new SmtpFailedRecipientsException(_failedRecipientExceptions, _failedRecipientExceptions.Count == recipients.Count);
                }

                if (_failedRecipientExceptions.Count == recipients.Count)
                {
                    exception.fatal = true;
                    throw exception;
                }
            }

            DataCommand.Send(_connection!);
            return new MailWriter(_connection!.GetClosableStream(), encodeForTransport: true);
        }
    }

    internal sealed class SendMailAsyncResult : LazyAsyncResult
    {
        private readonly SmtpConnection _connection;
        private readonly MailAddress _from;
        private readonly string? _deliveryNotify;
        private static readonly AsyncCallback s_sendMailFromCompleted = new AsyncCallback(SendMailFromCompleted);
        private static readonly AsyncCallback s_sendToCollectionCompleted = new AsyncCallback(SendToCollectionCompleted);
        private static readonly AsyncCallback s_sendDataCompleted = new AsyncCallback(SendDataCompleted);
        private readonly List<SmtpFailedRecipientException> _failedRecipientExceptions = new List<SmtpFailedRecipientException>();
        private Stream? _stream;
        private readonly MailAddressCollection _toCollection;
        private int _toIndex;
        private readonly bool _allowUnicode;


        internal SendMailAsyncResult(SmtpConnection connection, MailAddress from, MailAddressCollection toCollection,
            bool allowUnicode, string? deliveryNotify, AsyncCallback? callback, object? state)
            : base(null, state, callback)
        {
            _toCollection = toCollection;
            _connection = connection;
            _from = from;
            _deliveryNotify = deliveryNotify;
            _allowUnicode = allowUnicode;
        }

        internal void Send()
        {
            SendMailFrom();
        }

        internal static MailWriter End(IAsyncResult result)
        {
            SendMailAsyncResult thisPtr = (SendMailAsyncResult)result;
            object? sendMailResult = thisPtr.InternalWaitForCompletion();

            // Note the difference between the singular and plural FailedRecipient exceptions.
            // Only fail immediately if we couldn't send to any recipients.
            if ((sendMailResult is Exception e)
                && (!(sendMailResult is SmtpFailedRecipientException)
                    || ((SmtpFailedRecipientException)sendMailResult).fatal))
            {
                ExceptionDispatchInfo.Throw(e);
            }

            return new MailWriter(thisPtr._stream!, encodeForTransport: true);
        }
        private void SendMailFrom()
        {
            IAsyncResult result = MailCommand.BeginSend(_connection, SmtpCommands.Mail, _from, _allowUnicode,
                s_sendMailFromCompleted, this);
            if (!result.CompletedSynchronously)
            {
                return;
            }

            MailCommand.EndSend(result);
            SendToCollection();
        }

        private static void SendMailFromCompleted(IAsyncResult result)
        {
            if (!result.CompletedSynchronously)
            {
                SendMailAsyncResult thisPtr = (SendMailAsyncResult)result.AsyncState!;
                try
                {
                    MailCommand.EndSend(result);
                    thisPtr.SendToCollection();
                }
                catch (Exception e)
                {
                    thisPtr.InvokeCallback(e);
                }
            }
        }

        private void SendToCollection()
        {
            while (_toIndex < _toCollection.Count)
            {
                MultiAsyncResult result = (MultiAsyncResult)RecipientCommand.BeginSend(_connection,
                    _toCollection[_toIndex++].GetSmtpAddress(_allowUnicode) + _deliveryNotify,
                    s_sendToCollectionCompleted, this);
                if (!result.CompletedSynchronously)
                {
                    return;
                }
                string response;
                if (!RecipientCommand.EndSend(result, out response))
                {
                    _failedRecipientExceptions.Add(new SmtpFailedRecipientException(_connection.Reader!.StatusCode,
                        _toCollection[_toIndex - 1].GetSmtpAddress(_allowUnicode), response));
                }
            }
            SendData();
        }

        private static void SendToCollectionCompleted(IAsyncResult result)
        {
            if (!result.CompletedSynchronously)
            {
                SendMailAsyncResult thisPtr = (SendMailAsyncResult)result.AsyncState!;
                try
                {
                    string response;
                    if (!RecipientCommand.EndSend(result, out response))
                    {
                        thisPtr._failedRecipientExceptions.Add(
                            new SmtpFailedRecipientException(thisPtr._connection.Reader!.StatusCode,
                                thisPtr._toCollection[thisPtr._toIndex - 1].GetSmtpAddress(thisPtr._allowUnicode),
                                response));

                        if (thisPtr._failedRecipientExceptions.Count == thisPtr._toCollection.Count)
                        {
                            SmtpFailedRecipientException exception = thisPtr._toCollection.Count == 1 ?
                                (SmtpFailedRecipientException)thisPtr._failedRecipientExceptions[0] :
                                new SmtpFailedRecipientsException(thisPtr._failedRecipientExceptions, true);
                            exception.fatal = true;
                            thisPtr.InvokeCallback(exception);
                            return;
                        }
                    }
                    thisPtr.SendToCollection();
                }
                catch (Exception e)
                {
                    thisPtr.InvokeCallback(e);
                }
            }
        }

        private void SendData()
        {
            IAsyncResult result = DataCommand.BeginSend(_connection, s_sendDataCompleted, this);
            if (!result.CompletedSynchronously)
            {
                return;
            }
            DataCommand.EndSend(result);
            _stream = _connection.GetClosableStream();
            if (_failedRecipientExceptions.Count > 1)
            {
                InvokeCallback(new SmtpFailedRecipientsException(_failedRecipientExceptions, _failedRecipientExceptions.Count == _toCollection.Count));
            }
            else if (_failedRecipientExceptions.Count == 1)
            {
                InvokeCallback(_failedRecipientExceptions[0]);
            }
            else
            {
                InvokeCallback();
            }
        }

        private static void SendDataCompleted(IAsyncResult result)
        {
            if (!result.CompletedSynchronously)
            {
                SendMailAsyncResult thisPtr = (SendMailAsyncResult)result.AsyncState!;
                try
                {
                    DataCommand.EndSend(result);
                    thisPtr._stream = thisPtr._connection.GetClosableStream();
                    if (thisPtr._failedRecipientExceptions.Count > 1)
                    {
                        thisPtr.InvokeCallback(new SmtpFailedRecipientsException(thisPtr._failedRecipientExceptions, thisPtr._failedRecipientExceptions.Count == thisPtr._toCollection.Count));
                    }
                    else if (thisPtr._failedRecipientExceptions.Count == 1)
                    {
                        thisPtr.InvokeCallback(thisPtr._failedRecipientExceptions[0]);
                    }
                    else
                    {
                        thisPtr.InvokeCallback();
                    }
                }
                catch (Exception e)
                {
                    thisPtr.InvokeCallback(e);
                }
            }
        }

        // Return the list of non-terminal failures (some recipients failed but not others).
        internal SmtpFailedRecipientException? GetFailedRecipientException()
        {
            if (_failedRecipientExceptions.Count == 1)
            {
                return (SmtpFailedRecipientException)_failedRecipientExceptions[0];
            }
            else if (_failedRecipientExceptions.Count > 1)
            {
                // Aggregate exception, multiple failures
                return new SmtpFailedRecipientsException(_failedRecipientExceptions, false);
            }
            return null;
        }
    }
}
