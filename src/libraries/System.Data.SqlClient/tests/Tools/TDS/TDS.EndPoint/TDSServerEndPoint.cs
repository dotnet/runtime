// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Microsoft.SqlServer.TDS.EndPoint
{
    /// <summary>
    /// Server that talks TDS
    /// </summary>
    public class TDSServerEndPoint : ServerEndPointHandler<TDSServerEndPointConnection>
    {
        public TDSServerEndPoint(ITDSServer server)
            : base(server)
        {
        }

        public override TDSServerEndPointConnection CreateConnection(TcpClient newConnection)
        {
            return new TDSServerEndPointConnection(TDSServer, newConnection);
        }
    }

    /// <summary>
    /// General server handler
    /// </summary>
    public abstract class ServerEndPointHandler<T> where T : ServerEndPointConnection
    {
        /// <summary>
        /// Gets/Sets the event log for the proxy server
        /// </summary>
        public TextWriter EventLog { get; set; }

        /// <summary>
        /// Server
        /// </summary>
        public ITDSServer TDSServer { get; private set; }

        /// <summary>
        /// The list of connections spawned by the server
        /// </summary>
        internal IList<T> Connections { get; set; }

        /// <summary>
        /// End-point which TDS server is listening on
        /// </summary>
        public IPEndPoint ServerEndPoint { get; set; }

        /// <summary>
        /// Gets/Sets the listener
        /// </summary>
        private TcpListener ListenerSocket { get; set; }

        /// <summary>
        /// Gets/Sets the listener thread
        /// </summary>
        private Thread ListenerThread { get; set; }

        /// <summary>
        /// Gets/Sets the flag whether the stop is requested
        /// </summary>
        internal bool StopRequested { get; set; }


        /// <summary>
        /// Identifier to recognize the client of the Endpoint.
        /// </summary>
        public string EndpointName { get; set; }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        /// <param name="server">TDS server instance that will process requests</param>
        public ServerEndPointHandler(ITDSServer server)
        {
            // Prepare connections container
            Connections = new List<T>();

            // Save server instance
            TDSServer = server;
        }

        /// <summary>
        /// Start the listener thread
        /// </summary>
        public void Start()
        {
            StopRequested = false;

            // Start the server synchronously to ensure that instantiation exception is thrown (if any)
            ListenerSocket = new TcpListener(ServerEndPoint);
            ListenerSocket.Start();

            // Update ServerEndpoint with the actual address/port, e.g. if port=0 was given
            ServerEndPoint = (IPEndPoint)ListenerSocket.LocalEndpoint;

            Log($"{GetType().Name} {EndpointName} Is Server Socket Bound: {ListenerSocket.Server.IsBound} Testing connectivity to the endpoint created for the server.");
            using (TcpClient client = new TcpClient())
            {
                try
                {
                    client.Connect("localhost", ServerEndPoint.Port);
                }
                catch (Exception e)
                {
                    Log($"{GetType().Name} {EndpointName} Error occured while testing server endpoint {e.Message}");
                    throw;
                }
            }
            Log($"{GetType().Name} {EndpointName} Endpoint test successful.");

            // Initialize the listener
            ListenerThread = new Thread(new ThreadStart(_RequestListener)) { IsBackground = true };
            ListenerThread.Name = "TDS Server EndPoint Listener";
            ListenerThread.Start();

            Log($"{GetType().Name} {EndpointName} Listener Thread Started ");
        }

        /// <summary>
        /// Stop the listener thread
        /// </summary>
        public void Stop()
        {
            // Request the listener thread to stop
            StopRequested = true;

            // A copy of the list of connections to avoid locking
            IList<T> unlockedConnections = new List<T>();

            // Synchronize access to connections collection
            lock (Connections)
            {
                // Iterate over all connections and copy into the local list
                foreach (T connection in Connections)
                {
                    unlockedConnections.Add(connection);
                }
            }

            // Iterate over all connections and request each one to stop
            foreach (T connection in unlockedConnections)
            {
                // Request to stop
                connection.Stop();
            }

            // If server failed to start there is no thread to join
            if (ListenerThread != null)
            {
                // Wait for termination
                ListenerThread.Join();
            }

            // If server failed to start there is no listener associated with it
            if (ListenerSocket != null)
            {
                // Stop the server
                ListenerSocket.Stop();
                ListenerSocket = null;
            }
        }

        /// <summary>
        /// Processes all incoming requests
        /// </summary>
        private void _RequestListener()
        {
            try
            {
                // Accept connection as long as stop request is not posted
                while (!StopRequested)
                {
                    // Check if we have a connection request pending
                    if (ListenerSocket.Pending())
                    {
                        try
                        {
                            // Accept the connection
                            TcpClient newConnection = ListenerSocket.AcceptTcpClient();

                            // Create a new connection
                            T connection = CreateConnection(newConnection);

                            // Assign a log
                            connection.EventLog = EventLog;

                            // Subscribe for notifications
                            connection.OnConnectionClosed += new ConnectionClosedEventHandler(_OnConnectionClosed);

                            // Start a connection
                            connection.Start();

                            // Synchronize access to connections collection
                            lock (Connections)
                            {
                                // Register a new connection
                                Connections.Add(connection);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log(ex.ToString());
                        }
                    }
                    else
                    {
                        // Pause a bit
                        Thread.Sleep(10);
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        /// <summary>
        /// Creates a new connection handler for the given TCP connection
        /// </summary>
        public abstract T CreateConnection(TcpClient newConnection);

        /// <summary>
        /// Event handler for client connection termination
        /// </summary>
        private void _OnConnectionClosed(object sender, EventArgs e)
        {
            // Synchronize access to connection collection
            lock (Connections)
            {
                // Remove the existing connection from the list
                Connections.Remove(sender as T);
                Log($"{GetType().Name} {EndpointName} Connection Closed");
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
