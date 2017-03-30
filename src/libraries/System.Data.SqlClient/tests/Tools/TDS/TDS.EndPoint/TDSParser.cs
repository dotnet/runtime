// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.SqlServer.TDS.EndPoint
{
    /// <summary>
    /// Generic TDS parser
    /// </summary>
    public class TDSParser
    {
        /// <summary>
        /// Transport stream that was assigned to this parser prior to connection encryption
        /// </summary>
        private Stream _originalTransport;

        /// <summary>
        /// Writer to log events to
        /// </summary>
        public TextWriter EventLog { get; set; }

        /// <summary>
        /// Encryption protocol for server to use with AuthenticateAsServer
        /// </summary>
        public static SslProtocols ServerSslProtocol { get; set; }

        /// <summary>
        /// Protocol stream between the client and the server
        /// </summary>
        protected TDSStream Transport { get; set; }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSParser(Stream transport)
        {
            // Save original transport
            _originalTransport = transport;

            ServerSslProtocol = SslProtocols.Tls12;

            // Wrap transport layer with TDS
            Transport = new TDSStream(transport, false);
        }

        /// <summary>
        /// Set PreWriteCallback func in Transport (TDSStream)
        /// </summary>
        public void SetTDSStreamPreWriteCallback(Func<byte[], int, int, ushort> funcTDSStreamPreWriteCallBack)
        {
            Transport.PreWriteCallBack = funcTDSStreamPreWriteCallBack;
        }

        /// <summary>
        /// Resets the targeted encryption protocol for the server.
        /// </summary>
        public static void ResetTargetProtocol()
        {
            ServerSslProtocol = SslProtocols.Tls12;
        }

        /// <summary>
        /// Enable transport encryption
        /// </summary>
        protected void EnableClientTransportEncryption(string server)
        {
            // Check if transport encryption is applied
            if (Transport.InnerStream is SslStream)
            {
                return;
            }

            Log("Enabling client transport encryption...");

            // Wrap TDS stream with auto TDS stream
            AutoTDSStream tds = new AutoTDSStream(Transport, false);

            // We want to wrap each TLS message with PreLogin
            tds.OutgoingMessageType = TDSMessageType.PreLogin;

            // Create multiplexer stream to be able to remove TDS from under SSL
            PlaceholderStream multiplexer = new PlaceholderStream(tds, false);

            // Wrap multiplexer stream with SSL stream
            SslStream ssl = new SslStream(multiplexer, true, new RemoteCertificateValidationCallback(_ValidateServerCertificate));

            // Secure the channel
            ssl.AuthenticateAsClient(server);

            // Replace TDS stream with raw transport stream in multiplexer
            multiplexer.InnerStream = Transport.InnerStream;

            // Upgrade transport stream to SSL
            Transport.InnerStream = ssl;

            Log("Client transport encryption enabled");
        }

        /// <summary>
        /// Enable transport encryption
        /// </summary>
        protected void EnableServerTransportEncryption(X509Certificate certificate)
        {
            // Check if transport encryption is applied
            if (Transport.InnerStream is SslStream)
            {
                return;
            }

            Log("Enabling server transport encryption...");

            // Wrap TDS stream with auto TDS stream
            AutoTDSStream tds = new AutoTDSStream(Transport, false);

            // We want to wrap each TLS message with PreLogin
            tds.OutgoingMessageType = TDSMessageType.PreLogin;

            // Create multiplexer stream to be able to remove TDS from under SSL
            PlaceholderStream multiplexer = new PlaceholderStream(tds, false);

            // Wrap multiplexer stream with SSL stream
            SslStream ssl = new SslStream(multiplexer, true);

            // Secure the channel
            ssl.AuthenticateAsServer(certificate, false, ServerSslProtocol, false);

            // Replace TDS stream with raw transport stream in multiplexer
            multiplexer.InnerStream = Transport.InnerStream;

            // Upgrade transport stream to SSL
            Transport.InnerStream = ssl;

            Log("Server transport encryption enabled");
        }

        /// <summary>
        /// Disable transport encryption
        /// </summary>
        protected void DisableTransportEncryption()
        {
            // Check if transport stream is using SSL
            if (!(Transport.InnerStream is SslStream))
            {
                return;
            }

            Log("Disabling transport encryption...");

            // Close the SSL stream
            Transport.InnerStream.Close();

            // Use original transport stream
            Transport.InnerStream = _originalTransport;

            Log("Transport encryption disabled");
        }

        /// <summary>
        /// Validate server certificate
        /// </summary>
        private bool _ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // We always trust server certificate
            return true;
        }

        /// <summary>
        /// Write a string to the log
        /// </summary>
        protected void Log(string text, params object[] args)
        {
            if (EventLog != null)
            {
                EventLog.WriteLine("[TDSParser]: " + text, args);
            }
        }
    }
}
