// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    /// <summary>
    /// This is a simple network listener that redirects traffic
    /// It is used to simulate network delay
    /// </summary>
    public class ProxyServer : IDisposable
    {
        private volatile bool _stopRequested;
        private StringBuilder _eventLog;
        private static string s_logHeader = "======== ProxyServer Log Start ========\n";
        private static string s_logTrailer = "======== ProxyServer Log End ========\n";

        /// <summary>
        /// Gets/Sets the event log for the proxy server
        /// </summary>
        internal StringBuilder EventLog
        {
            get
            {
                if (_eventLog == null)
                    _eventLog = new StringBuilder();
                if (_eventLog.Length == 0)
                    _eventLog.Append(s_logHeader);
                return _eventLog;
            }
            set
            {
                _eventLog = value;
            }
        }

        /// <summary>
        /// The list of connections spawned by the server
        /// </summary>
        internal IList<ProxyServerConnection> Connections { get; private set; }

        /// <summary>
        /// Synchronization object on the list
        /// </summary>
        internal object SyncRoot { get; private set; }

        /// <summary>
        /// Gets the local port that is being listened on
        /// </summary>
        public int LocalPort { get; private set; }

        /// <summary>
        /// Gets/Sets the remote end point to connect to
        /// </summary>
        public IPEndPoint RemoteEndpoint { get; set; }

        /// <summary>
        /// Gets/Sets the listener
        /// </summary>
        protected TcpListener ListenerSocket { get; set; }

        /// <summary>
        /// Gets/Sets the listener thread
        /// </summary>
        protected Thread ListenerThread { get; set; }

        /// <summary>
        /// Delay incoming 
        /// </summary>
        public bool SimulatedInDelay { get; set; }

        /// <summary>
        /// Delay outgoing
        /// </summary>
        public bool SimulatedOutDelay { get; set; }

        /// <summary>
        /// Simulated delay in milliseconds between message being received and message being sent. This simulates network latency.
        /// </summary>
        public int SimulatedNetworkDelay { get; set; }

        /// <summary>
        /// Simulated delay in milliseconds between each packet being written out. This simulates low bandwidth connection.
        /// </summary>
        public int SimulatedPacketDelay { get; set; }

        /// <summary>
        /// Size of Buffer
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Gets/Sets the flag whether the stop is requested
        /// </summary>
        internal bool StopRequested { get { return _stopRequested; } set { _stopRequested = value; } }

        /// <summary>
        /// Gets a reset event that signals if copying is currently permitted (if it is not signaled, then all copying must wait)
        /// </summary>
        internal ManualResetEventSlim PermitCopying { get; private set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ProxyServer(int simulatedNetworkDelay = 0, int simulatedPacketDelay = 0, bool simulatedInDelay = false, bool simulatedOutDelay = false, int bufferSize = 8192)
        {
            SyncRoot = new object();
            Connections = new List<ProxyServerConnection>();
            SimulatedNetworkDelay = simulatedNetworkDelay;
            SimulatedPacketDelay = simulatedPacketDelay;
            SimulatedInDelay = simulatedInDelay;
            SimulatedOutDelay = simulatedOutDelay;
            BufferSize = bufferSize;
            PermitCopying = new ManualResetEventSlim(true);
        }

        /// <summary>
        /// Start the listener thread
        /// </summary>
        public void Start()
        {
            StopRequested = false;

            Log("Starting the server...");

            // Listen on any port
            ListenerSocket = new TcpListener(IPAddress.Loopback, 0);
            ListenerSocket.Start();

            Log("Server is running on {0}...", ListenerSocket.LocalEndpoint);

            LocalPort = ((IPEndPoint)ListenerSocket.LocalEndpoint).Port;

            ListenerThread = new Thread(new ThreadStart(_RequestListener));
            ListenerThread.Name = "Proxy Server Listener";
            ListenerThread.Start();
        }

        /// <summary>
        /// Stop the listener thread
        /// </summary>
        public void Stop()
        {
            // Resume copying to allow buffers to flush
            ResumeCopying();

            // Request the listener thread to stop
            StopRequested = true;

            // Wait for termination
            ListenerThread.Join(1000);
        }

        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// This method is used internally to notify server about the client disconnection
        /// </summary>
        internal void NotifyClientDisconnection(ProxyServerConnection connection)
        {
            lock (SyncRoot)
            {
                // Remove the existing connection from the list
                Connections.Remove(connection);
            }
        }

        /// <summary>
        /// Processes all incoming requests
        /// </summary>
        private void _RequestListener()
        {
            try
            {
                while (!StopRequested)
                {
                    if (ListenerSocket.Pending())
                    {
                        try
                        {
                            Log("Connection received");

                            // Accept the connection
                            Task<Socket> connectTask = ListenerSocket.AcceptSocketAsync();
                            connectTask.Wait();
                            Socket newConnection = connectTask.Result;

                            // Start a new connection
                            ProxyServerConnection proxyConnection = new ProxyServerConnection(newConnection, this);
                            proxyConnection.Start();

                            // Registering new connection
                            lock (SyncRoot)
                            {
                                Connections.Add(proxyConnection);
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

            Log("Stopping the server...");

            // Stop the server
            ListenerSocket.Stop();
            ListenerSocket = null;

            Log("Waiting for client connections to terminate...");

            // Wait for connections
            int connectionsLeft = short.MaxValue;
            while (connectionsLeft > 0)
            {
                lock (SyncRoot)
                {
                    // Check the amount of connections left
                    connectionsLeft = Connections.Count;
                }

                // Wait a bit
                Thread.Sleep(10);
            }

            Log("Server is stopped");
        }


        /// <summary>
        /// Write a string to the log
        /// </summary>
        internal void Log(string text, params object[] args)
        {
            if (EventLog != null)
            {
                EventLog.AppendFormat("[{0:O}]: ", DateTime.Now);
                EventLog.AppendFormat(text, args);
                EventLog.AppendLine();
            }
        }

        /// <summary>
        /// Return the ProxyServer log
        /// </summary>
        /// <returns></returns>
        public string GetServerEventLog()
        {
            EventLog.Append(s_logTrailer);
            EventLog.AppendLine();
            return EventLog.ToString();
        }

        /// <summary>
        /// Signals to all connections to stop copying between their streams
        /// </summary>
        public void PauseCopying()
        {
            PermitCopying.Reset();
        }

        /// <summary>
        /// Signals to all connections to continue copying between their streams
        /// </summary>
        public void ResumeCopying()
        {
            PermitCopying.Set();
        }

        /// <summary>
        /// Kills all currently open connections
        /// </summary>
        /// <param name="softKill">If true will perform a shutdown before closing, otherwise close will happen with lingering disabled</param>
        public void KillAllConnections(bool softKill = false)
        {
            lock (SyncRoot)
            {
                foreach (var connection in Connections)
                {
                    connection.Kill(softKill);
                }
            }
        }

        /// <summary>
        /// Creates a proxy server from the server in the given connection string using the default construction parameters and starts the proxy
        /// </summary>
        /// <param name="connectionString">Connection string to the server to proxy</param>
        /// <param name="newConnectionString">Connection string to the proxy server (using the same parameters as <paramref name="connectionString"/>)</param>
        /// <returns>The created and started proxy server</returns>
        public static ProxyServer CreateAndStartProxy(string connectionString, out string newConnectionString)
        {
            // Build builders
            SqlConnectionStringBuilder connStringbuilder = new SqlConnectionStringBuilder(connectionString);
            DataSourceBuilder dataSourceBuilder = new DataSourceBuilder(connStringbuilder.DataSource);

            // Setup proxy
            Task<System.Net.IPHostEntry> ipEntryTask = Dns.GetHostEntryAsync(dataSourceBuilder.ServerName);
            ipEntryTask.Wait();
            System.Net.IPHostEntry serverIpEntry = ipEntryTask.Result;

            ProxyServer proxy = new ProxyServer();
            proxy.RemoteEndpoint = new IPEndPoint(serverIpEntry.AddressList[0], dataSourceBuilder.Port ?? 1433);
            proxy.Start();

            // Switch connection over
            dataSourceBuilder.Protocol = "tcp";
            dataSourceBuilder.ServerName = "127.0.0.1";
            dataSourceBuilder.Port = proxy.LocalPort;
            connStringbuilder.DataSource = dataSourceBuilder.ToString();
            connStringbuilder.Remove("Network Library");

            newConnectionString = connStringbuilder.ToString();
            return proxy;
        }
    }

    /// <summary>
    /// This class maintains the tunnel between incoming connection and outgoing connection
    /// </summary>
    internal class ProxyServerConnection
    {
        /// <summary>
        /// This is a processing thread
        /// </summary>
        protected Thread ProcessorThread { get; set; }

        /// <summary>
        /// Returns the proxy server this connection belongs to
        /// </summary>
        public ProxyServer Server { get; set; }

        /// <summary>
        /// Incoming connection
        /// </summary>
        protected Socket IncomingConnection { get; set; }

        /// <summary>
        /// Outgoing connection
        /// </summary>
        protected Socket OutgoingConnection { get; set; }

        /// <summary>
        /// Standard constructor
        /// </summary>
        public ProxyServerConnection(Socket incomingConnection, ProxyServer server)
        {
            IncomingConnection = incomingConnection;
            Server = server;
        }

        /// <summary>
        /// Runs the processing thread
        /// </summary>
        public void Start()
        {
            ProcessorThread = new Thread(new ThreadStart(_ProcessorHandler));

            IPEndPoint incomingIPEndPoint = IncomingConnection.RemoteEndPoint as IPEndPoint;
            ProcessorThread.Name = string.Format("Proxy Server Connection {0} Thread", incomingIPEndPoint);

            ProcessorThread.Start();
        }

        /// <summary>
        /// Handles the bidirectional data transfers
        /// </summary>
        private void _ProcessorHandler()
        {
            try
            {
                Server.Log("Connecting to {0}...", Server.RemoteEndpoint);
                Server.Log("Remote end point address family {0}", Server.RemoteEndpoint.AddressFamily);

                // Establish outgoing connection to the proxy
                OutgoingConnection = new Socket(Server.RemoteEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Task connectTask = OutgoingConnection.ConnectAsync(Server.RemoteEndpoint.Address, Server.RemoteEndpoint.Port);
                connectTask.Wait(1000);

                // Writing connection information
                Server.Log("Connection established");

                // Obtain network streams
                NetworkStream outStream = new NetworkStream(OutgoingConnection, true);
                NetworkStream inStream = new NetworkStream(IncomingConnection, true);

                // Ensure copying isn't paused
                Server.PermitCopying.Wait();

                // Tunnel the traffic between two connections
                while (IncomingConnection.Connected && OutgoingConnection.Connected && !Server.StopRequested)
                {
                    bool DataAvailable = false;

                    // Check incoming buffer
                    if (inStream.DataAvailable)
                    {
                        DataAvailable = true;
                        CopyData(inStream, "client", outStream, "server", Server.SimulatedInDelay);
                    }


                    // Check outgoing buffer
                    if (outStream.DataAvailable)
                    {
                        DataAvailable = true;
                        CopyData(outStream, "server", inStream, "client", Server.SimulatedOutDelay);
                    }

                    // Pause
                    if (DataAvailable)
                    {
                        // Poll the sockets
                        if ((IncomingConnection.Poll(100, SelectMode.SelectRead) && !inStream.DataAvailable) ||
                            (OutgoingConnection.Poll(100, SelectMode.SelectRead) && !outStream.DataAvailable))
                            break;

                        Thread.Sleep(10);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }

                    // Ensure copying isn't paused
                    Server.PermitCopying.Wait();
                }
            }
            catch (Exception ex)
            {
                Server.Log(ex.ToString());
            }

            try
            {
                // Disconnect the client
                IncomingConnection.Dispose();
                OutgoingConnection.Dispose();
            }
            catch (Exception) { }

            // Logging disconnection message
            Server.Log("Connection closed");

            // Notify parent
            Server.NotifyClientDisconnection(this);
        }

        private void CopyData(NetworkStream readStream, string readStreamName, NetworkStream writeStream, string writeStreamName, bool simulateDelay)
        {
            // Note that the latency/bandwidth delay algorithm used here is a simple approximation used for test purposes

            Server.Log("Copying message from {0} to {1}", readStreamName, writeStreamName);

            // Read all available data from readStream
            var outBytes = new List<Tuple<byte[], int>>();
            while (readStream.DataAvailable)
            {
                byte[] buffer = new byte[Server.BufferSize];
                int numBytes = readStream.Read(buffer, 0, buffer.Length);
                outBytes.Add(new Tuple<byte[], int>(buffer, numBytes));
                Server.Log("\tRead {0} bytes from {1}", numBytes, readStreamName);
            }

            // Ensure copying isn't paused
            Server.PermitCopying.Wait();

            // Delay for latency
            if (simulateDelay)
            {
                Server.Log("\tSleeping for {0}", Server.SimulatedNetworkDelay);
                Thread.Sleep(Server.SimulatedNetworkDelay);
            }

            // Write all data to writeStream
            foreach (var b in outBytes)
            {
                writeStream.Write(b.Item1, 0, b.Item2);
                Server.Log("\tWrote {0} bytes to {1}", b.Item2, writeStreamName);

                // Ensure copying isn't paused
                Server.PermitCopying.Wait();

                // Delay for bandwidth
                if (simulateDelay)
                {
                    Server.Log("\tSleeping for {0}", Server.SimulatedPacketDelay);
                    Thread.Sleep(Server.SimulatedPacketDelay);
                }
            }
        }

        /// <summary>
        /// Kills this connection
        /// </summary>
        /// <param name="softKill">If true will perform a shutdown before closing, otherwise close will happen with lingering disabled</param>
        public void Kill(bool softKill)
        {
            if (softKill)
            {
                // Soft close, do shutdown first
                IncomingConnection.Shutdown(SocketShutdown.Both);
            }
            else
            {
                // Hard close - force no lingering
                IncomingConnection.LingerState = new LingerOption(true, 0);
            }

            IncomingConnection.Dispose();
            OutgoingConnection.Dispose();
        }
    }
}