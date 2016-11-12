// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    partial class TcpClient
    {
        // The Unix implementation is separate from the Windows implementation due to a fundamental
        // difference in socket support on the two platforms: on Unix once a socket has been used
        // in a failed connect, its state is undefined and it can no longer be used (Windows supports
        // this).  This then means that the instance Socket.Connect methods that take either multiple
        // addresses or a string host name that could be mapped to multiple addresses via DNS don't
        // work, because we can't try to individually connect to a second address after failing the
        // first.  That then impacts TcpClient, which exposes not only such Connect methods that
        // just delegate to the underlying Socket, but also the Socket itself.
        //
        // To address the most common cases, where the Socket isn't actually accessed directly by
        // a consumer of TcpClient, whereas on Windows TcpClient creates the socket during construction,
        // on Unix we create the socket on-demand, either when it's explicitly asked for via the Client
        // property or when the TcpClient.Connect methods are called.  In the former case, we have no
        // choice but to create a Socket, after which point we'll throw PlatformNotSupportedException
        // on any subsequent attempts to use Connect with multiple addresses.  In the latter case, though,
        // we can create a new Socket for each address we try, and only store as the "real" Client socket
        // the one that successfully connected.
        //
        // However, TcpClient also exposes some properties for common configuration of the Socket, and
        // if we simply forwarded those through Client, we'd frequently end up forcing the Socket's
        // creation before Connect is used, thwarting this whole scheme.  To address that, we maintain
        // shadow values for the relevant state.  When the properties are read, we read them initially
        // from a temporary socket created just for the purpose of getting the system's default. When
        // the properties are written, we store their values into these shadow properties instead of into
        // an actual socket (though we do also create a temporary socket and store the property onto it,
        // enabling errors to be caught earlier).  This is a relatively expensive, but it enables
        // TcpClient to be used in its recommended fashion and with its most popular APIs.

        // Shadow values for storing data set through TcpClient's properties.
        // We use a separate bool field to store whether the value has been set.
        // We don't use nullables, due to one of the properties being a reference type.

        
        private static readonly CancellationTokenSource s_canceledSource = CreateCanceledSource();
        private CancellationTokenSource _disposing;
        private ShadowOptions _shadowOptions; // shadow state used in public properties before the socket is created
        private int _connectRunning; // tracks whether a connect operation that could set _clientSocket is currently running

        private void InitializeClientSocket()
        {
            // Nop.  We want to lazily-allocate the socket.
        }

        private void DisposeCore()
        {
            // In case there's a concurrent ConnectAsync operation, we need to signal to that
            // operation that we're being disposed of, so that it can dispose of the current
            // temporary socket that hasn't yet been published as the official one.  If there's
            // already a cancellation source, just cancel it.  If there isn't, try to swap in
            // an already-canceled source so that we don't have to artificially create a new one
            // (since not all async connect operations require temporary sockets), but we may
            // lose that race condition, in which case we still need to dispose of whatever is
            // published.  It's fine to Cancel an already canceled cancellation source.
            if (Volatile.Read(ref _disposing) == null)
            {
                Interlocked.CompareExchange(ref _disposing, s_canceledSource, null);
            }
            _disposing.Cancel();
        }

        private Socket ClientCore
        {
            get
            {
                EnterClientLock();
                try
                {
                    // The Client socket is being explicitly accessed, so we're forced
                    // to create it if it doesn't exist.  Only do so if we haven't been disposed of,
                    // which nulls out the field.
                    if (_clientSocket == null && (_disposing == null || !_disposing.IsCancellationRequested))
                    {
                        // Create the socket, and transfer to it any of our shadow properties.
                        _clientSocket = CreateSocket();
                        ApplyInitializedOptionsToSocket(_clientSocket);
                    }
                    return _clientSocket;
                }
                finally
                {
                    ExitClientLock();
                }
            }
            set
            {
                EnterClientLock();
                try
                {
                    _clientSocket = value;
                    ClearInitializedValues();
                }
                finally
                {
                    ExitClientLock();
                }
            }
        }

        private void ApplyInitializedOptionsToSocket(Socket socket)
        {
            ShadowOptions so = _shadowOptions;
            if (so == null)
            {
                return;
            }

            // For each shadow property where we have an initialized value,
            // transfer that value to the provided socket.
            if (so._exclusiveAddressUseInitialized)
            {
                socket.ExclusiveAddressUse = so._exclusiveAddressUse != 0;
            }

            if (so._receiveBufferSizeInitialized)
            {
                socket.ReceiveBufferSize = so._receiveBufferSize;
            }

            if (so._sendBufferSizeInitialized)
            {
                socket.SendBufferSize = so._sendBufferSize;
            }

            if (so._receiveTimeoutInitialized)
            {
                socket.ReceiveTimeout = so._receiveTimeout;
            }

            if (so._sendTimeoutInitialized)
            {
                socket.SendTimeout = so._sendTimeout;
            }

            if (so._lingerStateInitialized)
            {
                socket.LingerState = so._lingerState;
            }

            if (so._noDelayInitialized)
            {
                socket.NoDelay = so._noDelay != 0;
            }
        }

        private void ClearInitializedValues()
        {
            ShadowOptions so = _shadowOptions;
            if (so != null)
            {
                // Clear the initialized fields for all of our shadow properties.
                so._exclusiveAddressUseInitialized =
                    so._receiveBufferSizeInitialized =
                    so._sendBufferSizeInitialized =
                    so._receiveTimeoutInitialized =
                    so._sendTimeoutInitialized =
                    so._lingerStateInitialized =
                    so._noDelayInitialized =
                    false;
            }
        }

        private int AvailableCore
        {
            get
            {
                // If we have a client socket, return its available value.
                // Otherwise, there isn't data available, so return 0.
                return _clientSocket?.Available ?? 0;
            }
        }

        private bool ConnectedCore
        {
            get
            {
                // If we have a client socket, return whether it's connected.
                // Otherwise as we don't have a socket, by definition it's not.
                return _clientSocket?.Connected ?? false;
            }
        }

        private Task ConnectAsyncCore(IPAddress address, int port)
        {
            return Client.ConnectAsync(address, port).ContinueWith((t, s) =>
            {
                var thisRef = (TcpClient)s;
                if (thisRef.Client == null)
                {
                    throw new ObjectDisposedException(thisRef.GetType().Name); // Dispose nulls out the client socket field.
                }
                t.GetAwaiter().GetResult(); // propagate any exception
                thisRef._active = true;
            }, this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private Task ConnectAsyncCore(string host, int port)
        {
            StartConnectCore(host, port);
            return ConnectAsyncCorePrivate(host, port, (s, a, p) => s.ConnectAsync(a, p));
        }

        private Task ConnectAsyncCore(IPAddress[] addresses, int port)
        {
            StartConnectCore(addresses, port);
            return ConnectCorePrivate(addresses, port, (s, a, p) => s.ConnectAsync(a, p));
        }

        private IAsyncResult BeginConnectCore(string host, int port, AsyncCallback requestCallback, object state) =>
            TaskToApm.Begin(ConnectAsyncCore(host, port), requestCallback, state);

        private IAsyncResult BeginConnectCore(IPAddress address, int port, AsyncCallback requestCallback, object state) =>
            TaskToApm.Begin(ConnectAsyncCore(address, port), requestCallback, state);

        private IAsyncResult BeginConnectCore(IPAddress[] addresses, int port, AsyncCallback requestCallback, object state) =>
            TaskToApm.Begin(ConnectAsyncCore(addresses, port), requestCallback, state);

        private void EndConnectCore(Socket socket, IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        private void StartConnectCore(string host, int port)
        {
            // Validate the args, similar to how Socket.Connect(string, int) would.
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            // If the client socket has already materialized, this API can't be used.
            if (_clientSocket != null)
            {
                throw new PlatformNotSupportedException(SR.net_sockets_connect_multiaddress_notsupported);
            }

            EnterClientLock();
        }

        private async Task ConnectAsyncCorePrivate(string host, int port, Func<Socket, IPAddress, int, Task> connect)
        {
            try
            {
                // Since Socket.Connect(host, port) won't work, get the addresses manually,
                // and then delegate to Connect(IPAddress[], int).
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
                await ConnectCorePrivate(addresses, port, connect).ConfigureAwait(false);
            }
            finally
            {
                ExitClientLock();
            }
        }

        private void StartConnectCore(IPAddress[] addresses, int port)
        {
            // Validate the args, similar to how Socket.Connect(IPAddress[], int) would.
            if (addresses == null)
            {
                throw new ArgumentNullException(nameof(addresses));
            }
            if (addresses.Length == 0)
            {
                throw new ArgumentException(SR.net_invalidAddressList, nameof(addresses));
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            // If the client socket has already materialized, this API can't be used.
            // Similarly if another operation to set the socket is already in progress.
            if (_clientSocket != null)
            {
                throw new PlatformNotSupportedException(SR.net_sockets_connect_multiaddress_notsupported);
            }

            EnterClientLock();
        }

        private async Task ConnectCorePrivate(IPAddress[] addresses, int port, Func<Socket, IPAddress, int, Task> connect)
        {
            try
            {
                // Make sure we've created a disposing cancellation source so that we get alerted
                // to a potentially concurrent disposal happening.
                if (Volatile.Read(ref _disposing) != null && _disposing.IsCancellationRequested)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
                Interlocked.CompareExchange(ref _disposing, new CancellationTokenSource(), null);

                // For each address, create a new socket (configured appropriately) and try to connect
                // to the endpoint.  If we're successful, set the newly connected socket as the client
                // socket, and we're done.  If we're unsuccessful, try the next address.
                ExceptionDispatchInfo lastException = null;
                foreach (IPAddress address in addresses)
                {
                    Socket s = CreateSocket();
                    try
                    {
                        // Configure the socket
                        ApplyInitializedOptionsToSocket(s);

                        // Register to dispose of the socket when the TcpClient is Dispose'd of.
                        // Some consumers use Dispose as a way to cancel a connect operation, as
                        // TcpClient.Dispose calls Socket.Dispose on the stored socket... but we've
                        // not stored the socket into the field yet, as doing so will publish it
                        // to be seen via the Client property.  Instead, we register to be notified
                        // when Dispose is called or has happened, and Dispose of the socket
                        using (_disposing.Token.Register(o => ((Socket)o).Dispose(), s))
                        {
                            await connect(s, address, port).ConfigureAwait(false);
                        }
                        _clientSocket = s;
                        _active = true;

                        if (_disposing.IsCancellationRequested)
                        {
                            s.Dispose();
                            _clientSocket = null;
                        }
                        return;
                    }
                    catch (Exception exc) when (!(exc is ObjectDisposedException))
                    {
                        s.Dispose();
                        lastException = ExceptionDispatchInfo.Capture(exc);
                    }
                }

                // None of the addresses worked.  Throw whatever exception we last captured, or a
                // new one if something went terribly wrong.

                if (lastException != null)
                {
                    lastException.Throw();
                }

                throw new ArgumentException(SR.net_invalidAddressList, nameof(addresses));
            }
            finally
            {
                ExitClientLock();
            }
        }

        private int ReceiveBufferSizeCore
        {
            get
            {
                ShadowOptions so = EnsureShadowValuesInitialized();
                return GetOption(ref so._receiveBufferSize, ref so._receiveBufferSizeInitialized, SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
            }
            set
            {
                ShadowOptions so = EnsureShadowValuesInitialized();
                SetOption(ref so._receiveBufferSize, ref so._receiveBufferSizeInitialized, SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, value);
            }
        }

        private int SendBufferSizeCore
        {
            get
            {
                ShadowOptions so = EnsureShadowValuesInitialized();
                return GetOption(ref so._sendBufferSize, ref so._sendBufferSizeInitialized, SocketOptionLevel.Socket, SocketOptionName.SendBuffer);
            }
            set
            {
                ShadowOptions so = EnsureShadowValuesInitialized();
                SetOption(ref so._sendBufferSize, ref so._sendBufferSizeInitialized, SocketOptionLevel.Socket, SocketOptionName.SendBuffer, value);
            }
        }

        private int ReceiveTimeoutCore
        {
            get
            {
                ShadowOptions so = EnsureShadowValuesInitialized();
                return GetOption(ref so._receiveTimeout, ref so._receiveTimeoutInitialized, SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout);
            }
            set
            {
                ShadowOptions so = EnsureShadowValuesInitialized();
                SetOption(ref so._receiveTimeout, ref so._receiveTimeoutInitialized, SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, value);
            }
        }

        private int SendTimeoutCore
        {
            get
            {
                ShadowOptions so = EnsureShadowValuesInitialized();
                return GetOption(ref so._sendTimeout, ref so._sendTimeoutInitialized, SocketOptionLevel.Socket, SocketOptionName.SendTimeout);
            }
            set
            {
                ShadowOptions so = EnsureShadowValuesInitialized();
                SetOption(ref so._sendTimeout, ref so._sendTimeoutInitialized, SocketOptionLevel.Socket, SocketOptionName.SendTimeout, value);
            }
        }

        private LingerOption LingerStateCore
        {
            get
            {
                ShadowOptions so = EnsureShadowValuesInitialized();
                return GetOption(ref so._lingerState, ref so._lingerStateInitialized, SocketOptionLevel.Socket, SocketOptionName.Linger);
            }
            set
            {
                ShadowOptions so = EnsureShadowValuesInitialized();
                SetOption(ref so._lingerState, ref so._lingerStateInitialized, SocketOptionLevel.Socket, SocketOptionName.Linger, value);
            }
        }

        private bool NoDelayCore
        {
            get
            {
                ShadowOptions so = EnsureShadowValuesInitialized();
                return GetOption(ref so._noDelay, ref so._noDelayInitialized, SocketOptionLevel.Tcp, SocketOptionName.NoDelay) != 0;
            }
            set
            {
                ShadowOptions so = EnsureShadowValuesInitialized();
                SetOption(ref so._noDelay, ref so._noDelayInitialized, SocketOptionLevel.Tcp, SocketOptionName.NoDelay, value ? 1 : 0);
            }
        }

        private bool ExclusiveAddressUseCore
        {
            get
            {
                ShadowOptions so = EnsureShadowValuesInitialized();
                return GetOption(ref so._exclusiveAddressUse, ref so._exclusiveAddressUseInitialized, SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse) != 0;
            }
            set
            {
                // Unlike the rest of the properties, we code this one explicitly, so as to take advantage of validation done by Socket's property.
                ShadowOptions so = EnsureShadowValuesInitialized();
                so._exclusiveAddressUse = value ? 1 : 0;
                so._exclusiveAddressUseInitialized = true;

                if (_clientSocket == null)
                {
                    using (Socket s = CreateSocket())
                    {
                        s.ExclusiveAddressUse = value;
                    }
                }
                else
                {
                    _clientSocket.ExclusiveAddressUse = value; // Use setter explicitly as it does additional validation beyond that done by SetOption
                }
            }
        }

        private ShadowOptions EnsureShadowValuesInitialized()
        {
            return _shadowOptions ?? (_shadowOptions = new ShadowOptions());
        }

        private T GetOption<T>(ref T location, ref bool initialized, SocketOptionLevel level, SocketOptionName name)
        {
            // Used in the getter for each shadow property.  

            // If we already have our client socket set up, just get its value for the option.  
            // Otherwise, return our shadow property.  If that shadow hasn't yet been initialized, 
            // first initialize it by creating a temporary socket from which we can obtain a default value.

            if (_clientSocket != null)
            {
                return (T)_clientSocket.GetSocketOption(level, name);
            }

            if (!initialized)
            {
                using (Socket s = CreateSocket())
                {
                    location = (T)s.GetSocketOption(level, name);
                }
                initialized = true;
            }

            return location;
        }

        private void SetOption<T>(ref T location, ref bool initialized, SocketOptionLevel level, SocketOptionName name, T value)
        {
            // Used in the setter for each shadow property.

            // Store the option on to the client socket.  If the client socket isn't yet set, we still want to set
            // the property onto a socket so we can get validation performed by the underlying system on the option being
            // set... so, albeit being a bit expensive, we create a temporary socket we can use for validation purposes.
            Socket s = _clientSocket ?? CreateSocket();
            try
            {
                if (typeof(T) == typeof(int))
                {
                    s.SetSocketOption(level, name, (int)(object)value);
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(LingerOption), $"Unexpected type: {typeof(T)}");
                    s.SetSocketOption(level, name, value);
                }
            }
            finally
            {
                if (s != _clientSocket)
                {
                    s.Dispose();
                }
            }

            // Then if it was successful, store it into the shadow.
            location = value;
            initialized = true;
        }

        private void EnterClientLock()
        {
            // TcpClient is not safe to be used concurrently.  But in case someone does access various members
            // while an async Connect operation is running that could asynchronously transition _clientSocket
            // from null to non-null, we have a simple lock... if it's taken when you try to take it, it throws
            // a PlatformNotSupportedException indicating the limitations of Connect.
            if (Interlocked.CompareExchange(ref _connectRunning, 1, 0) != 0)
            {
                throw new PlatformNotSupportedException(SR.net_sockets_connect_multiaddress_notsupported);
            }
        }

        private void ExitClientLock()
        {
            Volatile.Write(ref _connectRunning, 0);
        }

        private static CancellationTokenSource CreateCanceledSource()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            return cts;
        }

        private sealed class ShadowOptions
        {
            internal int _exclusiveAddressUse;
            internal bool _exclusiveAddressUseInitialized;

            internal int _receiveBufferSize;
            internal bool _receiveBufferSizeInitialized;

            internal int _sendBufferSize;
            internal bool _sendBufferSizeInitialized;

            internal int _receiveTimeout;
            internal bool _receiveTimeoutInitialized;

            internal int _sendTimeout;
            internal bool _sendTimeoutInitialized;

            internal LingerOption _lingerState;
            internal bool _lingerStateInitialized;

            internal int _noDelay;
            internal bool _noDelayInitialized;
        }
    }
}
