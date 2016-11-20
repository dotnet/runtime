// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net.Sockets;
using System.IO.Pipes;
using System.Security.Principal;

namespace Microsoft.SqlServer.TDS.EndPoint
{
    /// <summary>
    /// Client that talks TDS
    /// </summary>
    public class TDSClientEndPoint
    {
        /// <summary>
        /// Gets/Sets the event log for the proxy server
        /// </summary>
        public TextWriter EventLog { get; set; }

        /// <summary>
        /// Client
        /// </summary>
        public ITDSClient TDSClient { get; private set; }

        /// <summary>
        /// Socket that talks to TDS server
        /// </summary>
        private TcpClient ClientSocket { get; set; }

        //
        // Named pipes client
        //
        private NamedPipeClientStream ClientPipe { get; set; }

        /// <summary>
        /// TDS parser
        /// </summary>
        private TDSClientParser ClientParser { get; set; }

        /// <summary>
        /// TDSStream Prewrite call back
        /// </summary>
        private Func<byte[], int, int, ushort> _funcTDSStreamPreWriteCallBack;

        /// <summary>
        /// PostConnect call back for socket attributes setting
        /// </summary>
        private Action<TcpClient> _funcPostConnect;

        /// <summary>
        /// PostConnect call back for socket attributes setting
        /// </summary>
        private Action<TcpClient> _funcTCPClientDisconnect;

        /// <summary>
        /// Initialization constructor
        /// </summary>
        /// <param name="client">TDS client instance that will drive the communication with the TDS server</param>
        public TDSClientEndPoint(ITDSClient client)
            : this(client, null, null, null)
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSClientEndPoint(ITDSClient client, Func<byte[], int, int, ushort> funcTDSStreamPreWriteCallBack, Action<TcpClient> funcPostConnect, Action<TcpClient> funcTCPClientDisconnect)
        {
            // Save client instance
            TDSClient = client;

            _funcTDSStreamPreWriteCallBack = funcTDSStreamPreWriteCallBack;
            _funcPostConnect = funcPostConnect;
            _funcTCPClientDisconnect = funcTCPClientDisconnect;
        }

        /// <summary>
        /// Establish connection and log into the SQL Server
        /// </summary>
        public void Connect()
        {
            // Initialize context
            TDSClient.OnPreConnect();

            // Loop while we reach logged-in state. This accounts for connection failures and routing.
            while (TDSClient.State != TDSClientState.LoggedIn)
            {
                Log("Connecting to the server {0} port {1}...", TDSClient.Context.ServerHost, TDSClient.Context.ServerPort);

                // Check if server pipe is specified
                if (string.IsNullOrEmpty(TDSClient.Context.ServerPipe))
                {
                    try
                    {
                        // Establish transport to the TDS Server
                        ClientSocket = new TcpClient(AddressFamily.InterNetwork);
                        ClientSocket.Connect(TDSClient.Context.ServerHost, (int)TDSClient.Context.ServerPort);
                    }
                    catch (SocketException e)
                    {
                        // Check error code
                        if (e.ErrorCode != 10057)
                        {
                            // We don't recognize it
                            throw e;
                        }

                        // We are going to retry with IPv6 now because of won't fix bug
                        // http://bugcheck/bugs/VSWhidbey/285220
                        ClientSocket = new TcpClient(AddressFamily.InterNetworkV6);
                        ClientSocket.Connect(TDSClient.Context.ServerHost, (int)TDSClient.Context.ServerPort);
                    }

                    // Callback of PostConnect 
                    if (_funcPostConnect != null)
                    {
                        _funcPostConnect(ClientSocket);
                    }
                }
                else
                {
                    // Use named pipes transport
                    ClientPipe = new NamedPipeClientStream(TDSClient.Context.ServerHost, TDSClient.Context.ServerPipe, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation);
                    ClientPipe.Connect();
                }

                Log("Connected");

                try
                {
                    // Check if we have a client socket
                    if (ClientSocket != null)
                    {
                        // Create a client TDS parser with TCP transport
                        ClientParser = new TDSClientParser(TDSClient, ClientSocket.GetStream());
                    }
                    else
                    {
                        // Create a client TDS parser through named pipes transort
                        ClientParser = new TDSClientParser(TDSClient, ClientPipe);
                    }

                    if (_funcTDSStreamPreWriteCallBack != null)
                    {
                        ClientParser.SetTDSStreamPreWriteCallback(_funcTDSStreamPreWriteCallBack);
                    }

                    // Assign event log
                    ClientParser.EventLog = EventLog;

                    // Run login sequence
                    ClientParser.Login();

                    // Check if connection is being re-routed
                    if (TDSClient.State == TDSClientState.ReConnect)
                    {
                        Log("Client is being routed");

                        // Close established connection
                        Disconnect();
                    }
                }
                catch (Exception)
                {
                    // Disconnect client
                    Disconnect();

                    // Bubble up the exception
                    throw;
                }
            }
        }

        /// <summary>
        /// Dispatch a request to the server
        /// </summary>
        public void SendRequest()
        {
            // Check if we're connected
            if (ClientParser == null || TDSClient.State != TDSClientState.LoggedIn)
            {
                throw new Exception("Client must be connected and logged in");
            }

            // Delegate to the parser
            ClientParser.SendRequest();
        }

        /// <summary>
        /// Log out and disconnect from SQL Server
        /// </summary>
        public void Disconnect()
        {
            // Check client state
            if (TDSClient.State != TDSClientState.Final)
            {
                // Logout
                ClientParser.Logout();
            }

            // Check if client socket is available
            if (ClientSocket != null)
            {
                // Check if client is connected
                if (ClientSocket.Connected)
                {
                    Log("Disconnecting...");

                    // Close the connection
                    if (_funcTCPClientDisconnect == null)
                    {
                        ClientSocket.Client.Disconnect(true);
                    }
                    else
                    {
                        _funcTCPClientDisconnect(ClientSocket);
                    }

                    Log("Disconnected");
                }

                // Reset client socket
                ClientSocket = null;
            }

            // Check if client pipe is available
            if (ClientPipe != null)
            {
                // Check if client is connected
                if (ClientPipe.IsConnected)
                {
                    Log("Disconnecting...");

                    // Close the connection
                    ClientPipe.Close();

                    Log("Disconnected");
                }

                // Reset client pipe
                ClientPipe = null;
            }
        }

        /// <summary>
        /// Write a string to the log
        /// </summary>
        internal void Log(string text, params object[] args)
        {
            if (EventLog != null)
            {
                EventLog.WriteLine("[TDSClientEndPoint]: " + text, args);
            }
        }
    }
}
