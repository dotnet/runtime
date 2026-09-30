// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mail
{
    internal sealed class SmtpTransport
    {
        internal const int DefaultPort = 25;

        private readonly ISmtpAuthenticationModule[] _authenticationModules;
        private SmtpConnection? _connection;
        private readonly SmtpClient _client;
        private ICredentialsByHost? _credentials;
        private bool _shouldAbort;
        // Written (set true) from property setters without holding the transport lock and read
        // from IsConnected on the send path, so it is volatile to make an invalidating
        // configuration change reliably observable across threads without widening locking.
        private volatile bool _stale;

        private bool _enableSsl;

        internal SmtpTransport(SmtpClient client) : this(client, SmtpAuthenticationManager.GetModules())
        {
        }

        internal SmtpTransport(SmtpClient client, ISmtpAuthenticationModule[] authenticationModules)
        {
            ArgumentNullException.ThrowIfNull(authenticationModules);

            _client = client;
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
                if (!ReferenceEquals(value, _credentials))
                {
                    _credentials = value;
                    InvalidateCachedConnection();
                }
            }
        }

        internal bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsConnected && !_stale;
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
                if (value != _enableSsl)
                {
                    _enableSsl = value;
                    InvalidateCachedConnection();
                }
            }
        }

        internal X509CertificateCollection ClientCertificates => field ??= new X509CertificateCollection();

        internal bool ServerSupportsEai
        {
            get { return _connection != null && _connection.ServerSupportsEai; }
        }

        internal void GetConnection(string host, int port)
        {
            GetConnectionAsync<SyncReadWriteAdapter>(host, port).GetAwaiter().GetResult();
        }

        internal async Task GetConnectionAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            await GetConnectionAsync<AsyncReadWriteAdapter>(host, port, cancellationToken).ConfigureAwait(false);
        }

        internal Task GetConnectionAsync<TIOAdapter>(string host, int port, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            lock (this)
            {
                // Abort any previously cached connection (for example one that became stale after a
                // configuration change, or one whose connect attempt failed) so its socket is not
                // leaked. Abort() only force-closes the socket without any network round-trip, so it
                // is safe to run under the lock and does not block this async send path. A graceful
                // QUIT is unnecessary for a connection we are discarding. Sends are serialized by
                // SmtpClient._inCall, so no other GetConnectionAsync can run concurrently here.
                _connection?.Abort();
                _stale = false;

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

            return _connection.GetConnectionAsync<TIOAdapter>(host, port, cancellationToken);
        }

        internal async Task<(MailWriter, List<SmtpFailedRecipientException>?)> SendMailAsync<TIOAdapter>(MailAddress sender, MailAddressCollection recipients, string deliveryNotify, bool allowUnicode, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(recipients);

            await MailCommand.SendAsync<TIOAdapter>(_connection!, SmtpCommands.Mail, sender, allowUnicode, cancellationToken).ConfigureAwait(false);
            List<SmtpFailedRecipientException>? failedRecipientExceptions = null;

            foreach (MailAddress address in recipients)
            {
                string smtpAddress = address.GetSmtpAddress(allowUnicode);
                string to = smtpAddress + (_connection!.DSNEnabled ? deliveryNotify : string.Empty);
                (bool success, string? response) = await RecipientCommand.SendAsync<TIOAdapter>(_connection, to, cancellationToken).ConfigureAwait(false);
                if (!success)
                {
                    (failedRecipientExceptions ??= new()).Add(
                        new SmtpFailedRecipientException(_connection.Reader!.StatusCode, smtpAddress, response));
                }
            }

            if (failedRecipientExceptions?.Count > 0 && failedRecipientExceptions.Count == recipients.Count)
            {
                var exception = failedRecipientExceptions.Count == 1
                    ? failedRecipientExceptions[0]
                    : new SmtpFailedRecipientsException(failedRecipientExceptions, true);
                exception.fatal = true;
                throw exception;
            }

            await DataCommand.SendAsync<TIOAdapter>(_connection!, cancellationToken).ConfigureAwait(false);
            return (new MailWriter(_connection!.GetClosableStream(), encodeForTransport: true), failedRecipientExceptions);
        }

        internal void ReleaseConnection()
        {
            _connection?.ReleaseConnection();
        }

        // Marks any cached connection as stale without performing blocking work. The connection is
        // aborted and replaced the next time one is established (see GetConnectionAsync). This is
        // called when a property that affects how the connection is established (host, port,
        // credentials, SSL settings, target name) changes.
        internal void InvalidateCachedConnection()
        {
            _stale = true;
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
    }
}
