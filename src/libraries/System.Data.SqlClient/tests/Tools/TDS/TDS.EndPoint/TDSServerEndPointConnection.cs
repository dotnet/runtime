// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Microsoft.SqlServer.TDS.EndPoint
{
    /// <summary>
    /// A delegate for client connection termination
    /// </summary>
    public delegate void ConnectionClosedEventHandler(object sender, EventArgs e);

    /// <summary>
    /// Connection to a single client that handles TDS data
    /// </summary>
    public class TDSServerEndPointConnection : ServerEndPointConnection
    {
        private TDSServerParser _parser;

        public TDSServerEndPointConnection(ITDSServer server, TcpClient connection)
            : base(server, connection)
        {
        }

        public override void PrepareForProcessingData(Stream rawStream)
        {
            // Create a server TDS parser
            Debug.Assert(_parser == null, "PrepareForProcessingData should not be called twice");
            _parser = new TDSServerParser(Server, Session, rawStream);
        }

        public override void ProcessData(Stream rawStream)
        {
            _parser.Run();
        }
    }

    /// <summary>
    /// Connection to a single client
    /// </summary>
    public abstract class ServerEndPointConnection
    {
        /// <summary>
        /// Worker thread
        /// </summary>
        protected Thread ProcessorThread { get; set; }

        /// <summary>
        /// Gets/Sets the event log for the proxy server
        /// </summary>
        public TextWriter EventLog { get; set; }

        /// <summary>
        /// TDS Server to which this connection is established
        /// </summary>
        public ITDSServer Server { get; protected set; }

        /// <summary>
        /// TDS Server session that is assigned to this physical connection
        /// </summary>
        public ITDSServerSession Session { get; protected set; }

        /// <summary>
        /// Event that is fired when connection is closed
        /// </summary>
        public event ConnectionClosedEventHandler OnConnectionClosed;

        /// <summary>
        /// Connection itself
        /// </summary>
        protected TcpClient Connection { get; set; }

        /// <summary>
        /// The flag indicates whether server is being stopped
        /// </summary>
        protected bool StopRequested { get; set; }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public ServerEndPointConnection(ITDSServer server, TcpClient connection)
        {
            // Save server
            Server = server;

            // Save TCP connection
            Connection = connection;

            // Configure timeouts
            Connection.ReceiveTimeout = 1000;

            // Create a new TDS server session
            Session = server.OpenSession();

            // Check if local end-point is recognized
            if (Connection.Client.LocalEndPoint is IPEndPoint)
            {
                // Cast to IP end-point
                IPEndPoint endPoint = Connection.Client.LocalEndPoint as IPEndPoint;

                // Update TDS session
                Session.ServerEndPointInfo = new TDSEndPointInfo(endPoint.Address, endPoint.Port, TDSEndPointTransportType.TCP);
            }

            // Check if remote end-point is recognized
            if (Connection.Client.RemoteEndPoint is IPEndPoint)
            {
                // Cast to IP end-point
                IPEndPoint endPoint = Connection.Client.RemoteEndPoint as IPEndPoint;

                // Update server context
                Session.ClientEndPointInfo = new TDSEndPointInfo(endPoint.Address, endPoint.Port, TDSEndPointTransportType.TCP);
            }
        }

        /// <summary>
        /// Start the connection 
        /// </summary>
        internal void Start()
        {
            // Start with active connection
            StopRequested = false;

            // Prepare and start a thread
            ProcessorThread = new Thread(new ThreadStart(_ConnectionHandler));
            ProcessorThread.Name = string.Format("TDS Server Connection {0} Thread", Connection.Client.RemoteEndPoint);
            ProcessorThread.Start();
        }

        /// <summary>
        /// Stop the connection
        /// </summary>
        internal void Stop()
        {
            // Request the listener thread to stop
            StopRequested = true;

            // If connection failed to start there's no processor thread
            if (ProcessorThread != null)
            {
                // Wait for termination
                ProcessorThread.Join();
            }
        }

        /// <summary>
        /// Called when the data processing thread is first started
        /// </summary>
        public abstract void PrepareForProcessingData(Stream rawStream);

        /// <summary>
        /// Called every time there is new data available
        /// </summary>
        public abstract void ProcessData(Stream rawStream);

        /// <summary>
        /// Worker thread
        /// </summary>
        private void _ConnectionHandler()
        {
            try
            {
                // Get network stream
                NetworkStream rawStream = Connection.GetStream();
                PrepareForProcessingData(rawStream);

                // Process the packet sequence
                while (Connection.Connected && !StopRequested)
                {
                    // Check incoming buffer
                    if (rawStream.DataAvailable)
                    {
                        ProcessData(rawStream);
                    }
                    else
                    {
                        // Poll the socket for data
                        if (Connection.Client.Poll(100, SelectMode.SelectRead) && !rawStream.DataAvailable)
                        {
                            break;
                        }

                        // Sleep a bit to reduce load on CPU
                        Thread.Sleep(10);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception
                Log(ex.ToString());
            }

            try
            {
                // Disconnect the client
                Connection.Close();
            }
            catch (Exception)
            {
                // Do nothing there
            }

            // Notify subscribers that this connection is closed
            if (OnConnectionClosed != null)
            {
                OnConnectionClosed(this, null);
            }
        }

        /// <summary>
        /// Write a string to the log
        /// </summary>
        internal void Log(string text, params object[] args)
        {
            if (EventLog != null)
            {
                EventLog.WriteLine(text, args);
            }
        }
    }
}
