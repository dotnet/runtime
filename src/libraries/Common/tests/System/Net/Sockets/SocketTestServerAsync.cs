// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading;

namespace System.Net.Sockets.Tests
{
    // Code taken from https://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs.aspx

    // Implements the connection logic for the socket server.
    // After accepting a connection, all data read from the client
    // is sent back to the client. The read and echo back to the client pattern
    // is continued until the client disconnects.
    public class SocketTestServerAsync : SocketTestServer
    {
        private const int OpsToPreAlloc = 2;  // Read, write (don't alloc buffer space for accepts).

        private VerboseTestLogging _log;

        private int _maxNumConnections;       // The maximum number of connections the sample is designed to handle simultaneously.
        private int _receiveBufferSize;       // Buffer size to use for each socket I/O operation.
        private BufferManager _bufferManager; // Represents a large reusable set of buffers for all socket operations.
        private Socket _listenSocket;          // The socket used to listen for incoming connection requests.
        private SocketAsyncEventArgsPool _readWritePool; // Pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations.
        private int _totalBytesRead;          // Counter of the total # bytes received by the server.
        private int _numConnectedSockets;     // The total number of clients connected to the server.
        private SemaphoreSlim _maxNumberAcceptedClientsSemaphore;
        private int _acceptRetryCount = 10;
        private ProtocolType _protocolType;

        private object _listenSocketLock = new object();

        protected sealed override int Port { get { return ((IPEndPoint)_listenSocket.LocalEndPoint).Port; } }
        public sealed override EndPoint EndPoint { get { return _listenSocket.LocalEndPoint; } }

        public SocketTestServerAsync(int numConnections, int receiveBufferSize, EndPoint localEndPoint, ProtocolType protocolType = ProtocolType.Tcp)
        {
            _log = VerboseTestLogging.GetInstance();
            _totalBytesRead = 0;
            _numConnectedSockets = 0;
            _maxNumConnections = numConnections;
            _receiveBufferSize = receiveBufferSize;

            // Allocate buffers such that the maximum number of sockets can have one outstanding read and
            // write posted to the socket simultaneously.
            _bufferManager = new BufferManager(receiveBufferSize * numConnections * OpsToPreAlloc,
                receiveBufferSize);

            _readWritePool = new SocketAsyncEventArgsPool(numConnections);
            _maxNumberAcceptedClientsSemaphore = new SemaphoreSlim(numConnections, numConnections);
            _protocolType = protocolType;
            Init();
            Start(localEndPoint);
        }

        protected override void Dispose(bool disposing)
        {
            _log.WriteLine(this.GetHashCode() + " Dispose (_numConnectedSockets={0})", _numConnectedSockets);
            if (disposing && (_listenSocket != null))
            {
                lock (_listenSocketLock)
                {
                    if (_listenSocket != null)
                    {
                        _listenSocket.Dispose();
                        _listenSocket = null;
                    }
                }
            }
        }

        // Initializes the server by preallocating reusable buffers and
        // context objects.  These objects do not need to be preallocated
        // or reused, but it is done this way to illustrate how the API can
        // easily be used to create reusable objects to increase server performance.
        //
        private void Init()
        {
            // Allocates one large byte buffer which all I/O operations use a piece of.  This guards
            // against memory fragmentation.
            _bufferManager.InitBuffer();

            // Pre-allocate pool of SocketAsyncEventArgs objects.
            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < _maxNumConnections; i++)
            {
                // Pre-allocate a set of reusable SocketAsyncEventArgs.
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                readWriteEventArg.UserToken = new AsyncUserToken();

                // Assign a byte buffer from the buffer pool to the SocketAsyncEventArg object.
                _bufferManager.SetBuffer(readWriteEventArg);

                // Add SocketAsyncEventArg to the pool.
                _readWritePool.Push(readWriteEventArg);
            }
        }

        // Starts the server such that it is listening for
        // incoming connection requests.
        //
        // <param name="localEndPoint">The endpoint which the server will listen
        // for connection requests on</param>
        private void Start(EndPoint localEndPoint)
        {
            // Create the socket which listens for incoming connections.
            _listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, _protocolType);
            _listenSocket.Bind(localEndPoint);

            // Start the server with a listen backlog of 100 connections.
            _listenSocket.Listen(100);

            // Post accepts on the listening socket.
            StartAccept(null);
        }

        // Begins an operation to accept a connection request from the client
        //
        // <param name="acceptEventArg">The context object to use when issuing
        // the accept operation on the server's listening socket</param>
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // Socket must be cleared since the context object is being reused.
                acceptEventArg.AcceptSocket = null;
            }

            _log.WriteLine(this.GetHashCode() + " StartAccept(_numConnectedSockets={0})", _numConnectedSockets);
            if (!_maxNumberAcceptedClientsSemaphore.Wait(TestSettings.PassingTestTimeout))
            {
                throw new TimeoutException("Timeout waiting for client connection.");
            }

            if (_listenSocket == null)
            {
                return;
            }

            bool willRaiseEvent = false;

            lock (_listenSocketLock)
            {
                if (_listenSocket != null)
                {
                    willRaiseEvent = _listenSocket.AcceptAsync(acceptEventArg);
                }
                else
                {
                    return;
                }
            }

            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        // This method is the callback method associated with Socket.AcceptAsync
        // operations and is invoked when an accept operation is complete.
        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                _log.WriteLine(this.GetHashCode() + " ProcessAccept(_numConnectedSockets={0}): {1}", _numConnectedSockets, e.SocketError);

                // If possible, retry the accept after a short wait.
                if (e.SocketError == SocketError.OperationAborted || e.SocketError == SocketError.Interrupted)
                {
                    return;
                }

                if (Interlocked.Decrement(ref _acceptRetryCount) <= 0)
                {
                    throw new InvalidOperationException("accept retry limit exceeded.");
                }

                Thread.Sleep(500);
            }
            else
            {
                Interlocked.Increment(ref _numConnectedSockets);
                _log.WriteLine(this.GetHashCode() + " ProcessAccept(_numConnectedSockets={0})", _numConnectedSockets);

                // Get the socket for the accepted client connection and put it into the ReadEventArg object user token.
                SocketAsyncEventArgs readEventArgs = _readWritePool.Pop();

                NotifyAccepted(e.AcceptSocket);
                ((AsyncUserToken)readEventArgs.UserToken).Socket = e.AcceptSocket;

                // As soon as the client is connected, post a receive to the connection.
                bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);
                if (!willRaiseEvent)
                {
                    _log.WriteLine(this.GetHashCode() + " ProcessAccept -> ProcessReceive");
                    IO_Completed(null, readEventArgs);
                }
            }

            // Accept the next connection request.
            StartAccept(e);
        }

        // This method is called whenever a receive or send operation is completed on a socket.
        //
        // <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    if (ProcessReceive(e))
                    {
                        return; // resume in callback
                    }
                    break;
                case SocketAsyncOperation.Send:
                    // Fall through to loop below
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }

            // Loop until some operation pends
            while (true)
            {
                if (ProcessSend(e))
                {
                    return;
                }

                if (ProcessReceive(e))
                {
                    return;
                }
            }
        }

        // This method is invoked when an asynchronous receive operation completes.
        // If the remote host closed the connection, then the socket is closed.
        // If data was received then the data is echoed back to the client.
        private bool ProcessReceive(SocketAsyncEventArgs e)
        {
            _log.WriteLine(
                this.GetHashCode() + " ProcessReceive(bytesTransferred={0}, SocketError={1}, _numConnectedSockets={2})",
                e.BytesTransferred,
                e.SocketError,
                _numConnectedSockets);

            // Check if the remote host closed the connection.
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                // Increment the count of the total bytes receive by the server.
                Interlocked.Add(ref _totalBytesRead, e.BytesTransferred);
                _log.WriteLine(this.GetHashCode() + " The server has read a total of {0} bytes", _totalBytesRead);

                // Echo the data received back to the client.
                e.SetBuffer(e.Offset, e.BytesTransferred);
                return token.Socket.SendAsync(e);
            }
            else
            {
                CloseClientSocket(e);
                return true;        // meaning, no callback will happen
            }
        }

        // This method is invoked when an asynchronous send operation completes.
        // The method issues another receive on the socket to read any additional
        // data sent from the client.
        //
        // <param name="e"></param>
        private bool ProcessSend(SocketAsyncEventArgs e)
        {
            _log.WriteLine(
                this.GetHashCode() + " ProcessSend(SocketError={0}, _numConnectedSockets={1})",
                e.SocketError,
                _numConnectedSockets);

            if (e.SocketError == SocketError.Success)
            {
                // Done echoing data back to the client.
                AsyncUserToken token = (AsyncUserToken)e.UserToken;

                // Read the next block of data send from the client.
                bool willRaiseEvent = token.Socket.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    _log.WriteLine(this.GetHashCode() + " ProcessSend -> ProcessReceive");
                }
                return willRaiseEvent;
            }
            else
            {
                _log.WriteLine(this.GetHashCode() + " ProcessSend -> CloseClientSocket");
                CloseClientSocket(e);
                return true;        // meaning, no callback will happen
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;

            _log.WriteLine(
                this.GetHashCode() + " CloseClientSocket(_numConnectedSockets={0}, SocketError={1})",
                _numConnectedSockets,
                e.SocketError);

            // Close the socket associated with the client.
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                // Throws if client process has already closed.
                _log.WriteLine(
                    this.GetHashCode() + " CloseClientSocket Exception:{0}",
                    ex);
            }
            token.Socket.Dispose();

            // Decrement the counter keeping track of the total number of clients connected to the server.
            Interlocked.Decrement(ref _numConnectedSockets);
            _maxNumberAcceptedClientsSemaphore.Release();
            _log.WriteLine(
                this.GetHashCode() + " CloseClientSocket(_numConnectedSockets={0})",
                _numConnectedSockets);

            // Free the SocketAsyncEventArg so they can be reused by another client.
            _readWritePool.Push(e);
        }
    }

    internal class AsyncUserToken
    {
        public Socket Socket { get; set; }
    }

    // Represents a collection of reusable SocketAsyncEventArgs objects.
    internal class SocketAsyncEventArgsPool
    {
        private Stack<SocketAsyncEventArgs> _pool;

        // Initializes the object pool to the specified size.
        //
        // The "capacity" parameter is the maximum number of
        // SocketAsyncEventArgs objects the pool can hold.
        public SocketAsyncEventArgsPool(int capacity)
        {
            _pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        // Add a SocketAsyncEventArg instance to the pool.
        //
        // The "item" parameter is the SocketAsyncEventArgs instance
        // to add to the pool.
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) { throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); }
            lock (_pool)
            {
                _pool.Push(item);
            }
        }

        // Removes a SocketAsyncEventArgs instance from the pool
        // and returns the object removed from the pool.
        public SocketAsyncEventArgs Pop()
        {
            lock (_pool)
            {
                return _pool.Pop();
            }
        }

        // The number of SocketAsyncEventArgs instances in the pool.
        public int Count
        {
            get { return _pool.Count; }
        }
    }

    // This class creates a single large buffer which can be divided up
    // and assigned to SocketAsyncEventArgs objects for use with each
    // socket I/O operation.
    //
    // This enables buffers to be easily reused and guards against
    // fragmenting heap memory.
    //
    // The operations exposed on the BufferManager class are not thread safe.
    internal class BufferManager
    {
        private readonly int _numBytes;  // The total number of bytes controlled by the buffer pool.
        private readonly Stack<int> _freeIndexPool;
        private readonly int _bufferSize;
        private byte[] _buffer; // The underlying byte array maintained by the Buffer Manager.
        private int _currentIndex;

        public BufferManager(int totalBytes, int bufferSize)
        {
            _numBytes = totalBytes;
            _currentIndex = 0;
            _bufferSize = bufferSize;
            _freeIndexPool = new Stack<int>();
        }

        // Allocates buffer space used by the buffer pool.
        public void InitBuffer()
        {
            // Create one big large buffer and divide that
            // out to each SocketAsyncEventArg object.
            _buffer = new byte[_numBytes];
        }

        // Assigns a buffer from the buffer pool to the
        // specified SocketAsyncEventArgs object.
        //
        // <returns>true if the buffer was successfully set, else false</returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (_freeIndexPool.Count > 0)
            {
                args.SetBuffer(_buffer, _freeIndexPool.Pop(), _bufferSize);
            }
            else
            {
                if ((_numBytes - _bufferSize) < _currentIndex)
                {
                    return false;
                }
                args.SetBuffer(_buffer, _currentIndex, _bufferSize);
                _currentIndex += _bufferSize;
            }
            return true;
        }

        // Removes the buffer from a SocketAsyncEventArg object.
        // This frees the buffer back to the buffer pool.
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            _freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
