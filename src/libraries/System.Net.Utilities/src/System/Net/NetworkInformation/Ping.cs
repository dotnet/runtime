// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    public partial class Ping : IDisposable
    {
        private const int DefaultSendBufferSize = 32;  // Same as ping.exe on Windows.
        private const int DefaultTimeout = 5000;       // 5 seconds: same as ping.exe on Windows.
        private const int MaxBufferSize = 65500;       // Artificial constraint due to win32 api limitations.
        private const int MaxUdpPacket = 0xFFFF + 256; // Marshal.SizeOf(typeof(Icmp6EchoReply)) * 2 + ip header info;

        private bool _disposeRequested = false;
        private byte[] _defaultSendBuffer = null;

        private AsyncOperation _asyncOp = null;
        private SendOrPostCallback _onPingCompletedDelegate;

        public event PingCompletedEventHandler PingCompleted;

        // Thread safety:
        private const int Free = 0;
        private const int InProgress = 1;
        private const int Disposed = 2;
        private int _status = Free;

        private void CheckStart()
        {
            if (_disposeRequested)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            int currentStatus = Interlocked.CompareExchange(ref _status, InProgress, Free);
            if (currentStatus == InProgress)
            {
                throw new InvalidOperationException(SR.net_inasync);
            }
            else if (currentStatus == Disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private void Finish()
        {
            Debug.Assert(_status == InProgress, "Invalid status: " + _status);
            _status = Free;

            if (_disposeRequested)
            {
                InternalDispose();
            }
        }

        protected void OnPingCompleted(PingCompletedEventArgs e)
        {
            PingCompletedEventHandler handler = PingCompleted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public Ping()
        {
            _onPingCompletedDelegate = s => OnPingCompleted((PingCompletedEventArgs)s);
        }

        // Cancels pending async requests, closes the handles.
        private void InternalDispose()
        {
            _disposeRequested = true;

            if (Interlocked.CompareExchange(ref _status, Disposed, Free) != Free)
            {
                // Already disposed, or Finish will call Dispose again once Free.
                return;
            }

            InternalDisposeCore();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Only on explicit dispose.  Otherwise, the GC can cleanup everything else.
                InternalDispose();
            }
        }

        private void SendAsync(string hostNameOrAddress, object userToken)
        {
            SendAsync(hostNameOrAddress, DefaultTimeout, DefaultSendBuffer, userToken);
        }

        public void SendAsync(string hostNameOrAddress, int timeout, object userToken)
        {
            SendAsync(hostNameOrAddress, timeout, DefaultSendBuffer, userToken);
        }

        private void SendAsync(IPAddress address, object userToken)
        {
            SendAsync(address, DefaultTimeout, DefaultSendBuffer, userToken);
        }

        private void SendAsync(IPAddress address, int timeout, object userToken)
        {
            SendAsync(address, timeout, DefaultSendBuffer, userToken);
        }

        private void SendAsync(string hostNameOrAddress, int timeout, byte[] buffer, object userToken)
        {
            SendAsync(hostNameOrAddress, timeout, buffer, null, userToken);
        }

        private void SendAsync(IPAddress address, int timeout, byte[] buffer, object userToken)
        {
            SendAsync(address, timeout, buffer, null, userToken);
        }

        private void SendAsync(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions options, object userToken)
        {
            if (string.IsNullOrEmpty(hostNameOrAddress))
            {
                throw new ArgumentNullException("hostNameOrAddress");
            }

            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (buffer.Length > MaxBufferSize)
            {
                throw new ArgumentException(SR.net_invalidPingBufferSize, "buffer");
            }

            if (timeout < 0)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            IPAddress address;
            if (IPAddress.TryParse(hostNameOrAddress, out address))
            {
                SendAsync(address, timeout, buffer, options, userToken);
                return;
            }

            CheckStart();
            try
            {
                _asyncOp = AsyncOperationManager.CreateOperation(userToken);
                AsyncStateObject state = new AsyncStateObject(hostNameOrAddress, buffer, timeout, options, userToken);
                ThreadPool.QueueUserWorkItem(new WaitCallback(ContinueAsyncSend), state);
            }
            catch (Exception e)
            {
                Finish();
                throw new PingException(SR.net_ping, e);
            }
        }

        private void SendAsync(IPAddress address, int timeout, byte[] buffer, PingOptions options, object userToken)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (buffer.Length > MaxBufferSize)
            {
                throw new ArgumentException(SR.net_invalidPingBufferSize, "buffer");
            }

            if (timeout < 0)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            // Check if address family is installed.
            TestIsIpSupported(address);

            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                throw new ArgumentException(SR.net_invalid_ip_addr, "address");
            }

            // Need to snapshot the address here, so we're sure that it's not changed between now
            // and the operation, and to be sure that IPAddress.ToString() is called and not some override.
            IPAddress addressSnapshot = (address.AddressFamily == AddressFamily.InterNetwork) ?
                new IPAddress(address.GetAddressBytes()) :
                new IPAddress(address.GetAddressBytes(), address.ScopeId);

            CheckStart();
            try
            {
                _asyncOp = AsyncOperationManager.CreateOperation(userToken);
                InternalSendAsync(addressSnapshot, buffer, timeout, options);
            }
            catch (Exception e)
            {
                Finish();
                throw new PingException(SR.net_ping, e);
            }
        }

        public Task<PingReply> SendPingAsync(IPAddress address)
        {
            return SendPingAsyncCore(tcs => SendAsync(address, tcs));
        }

        public Task<PingReply> SendPingAsync(string hostNameOrAddress)
        {
            return SendPingAsyncCore(tcs => SendAsync(hostNameOrAddress, tcs));
        }

        public Task<PingReply> SendPingAsync(IPAddress address, int timeout)
        {
            return SendPingAsyncCore(tcs => SendAsync(address, timeout, tcs));
        }

        public Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout)
        {
            return SendPingAsyncCore(tcs => SendAsync(hostNameOrAddress, timeout, tcs));
        }

        public Task<PingReply> SendPingAsync(IPAddress address, int timeout, byte[] buffer)
        {
            return SendPingAsyncCore(tcs => SendAsync(address, timeout, buffer, tcs));
        }

        public Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout, byte[] buffer)
        {
            return SendPingAsyncCore(tcs => SendAsync(hostNameOrAddress, timeout, buffer, tcs));
        }

        public Task<PingReply> SendPingAsync(IPAddress address, int timeout, byte[] buffer, PingOptions options)
        {
            return SendPingAsyncCore(tcs => SendAsync(address, timeout, buffer, options, tcs));
        }

        public Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions options)
        {
            return SendPingAsyncCore(tcs => SendAsync(hostNameOrAddress, timeout, buffer, options, tcs));
        }

        private Task<PingReply> SendPingAsyncCore(Action<TaskCompletionSource<PingReply>> sendAsync)
        {
            // Create a TaskCompletionSource to represent the operation.
            var tcs = new TaskCompletionSource<PingReply>();

            // Register a handler that will transfer completion results to the TCS Task.
            PingCompletedEventHandler handler = null;
            handler = (sender, e) => HandleCompletion(tcs, e, handler);
            PingCompleted += handler;

            // Start the async operation.
            try { sendAsync(tcs); }
            catch
            {
                PingCompleted -= handler;
                throw;
            }

            // Return the task to represent the asynchronous operation.
            return tcs.Task;
        }

        private void HandleCompletion(TaskCompletionSource<PingReply> tcs, PingCompletedEventArgs e, PingCompletedEventHandler handler)
        {
            if (e.UserState == tcs)
            {
                try
                {
                    PingCompleted -= handler;
                }
                finally
                {
                    if (e.Error != null) tcs.TrySetException(e.Error);
                    else if (e.Cancelled) tcs.TrySetCanceled();
                    else tcs.TrySetResult(e.Reply);
                }
            }
        }

        internal sealed class AsyncStateObject
        {
            internal AsyncStateObject(string hostName, byte[] buffer, int timeout, PingOptions options, object userToken)
            {
                HostName = hostName;
                Buffer = buffer;
                Timeout = timeout;
                Options = options;
                UserToken = userToken;
            }

            internal readonly byte[] Buffer;
            internal readonly string HostName;
            internal readonly int Timeout;
            internal readonly PingOptions Options;
            internal readonly object UserToken;
        }

        private void ContinueAsyncSend(object state)
        {
            Debug.Assert(_asyncOp != null, "Null AsyncOp?");
            AsyncStateObject stateObject = (AsyncStateObject)state;

            try
            {
                IPAddress address = Dns.GetHostAddressesAsync(stateObject.HostName).GetAwaiter().GetResult()[0];
                InternalSendAsync(address, stateObject.Buffer, stateObject.Timeout, stateObject.Options);
            }
            catch (Exception e)
            {
                PingException pe = new PingException(SR.net_ping, e);
                PingCompletedEventArgs eventArgs = new PingCompletedEventArgs(null, pe, false, _asyncOp.UserSuppliedState);
                Finish();
                _asyncOp.PostOperationCompleted(_onPingCompletedDelegate, eventArgs);
            }
        }

        // Tests if the current machine supports the given ip protocol family.
        private void TestIsIpSupported(IPAddress ip)
        {
            InitializeSockets();

            if (ip.AddressFamily == AddressFamily.InterNetwork && !SocketProtocolSupportPal.OSSupportsIPv4)
            {
                throw new NotSupportedException(SR.net_ipv4_not_installed);
            }
            else if ((ip.AddressFamily == AddressFamily.InterNetworkV6 && !SocketProtocolSupportPal.OSSupportsIPv6))
            {
                throw new NotSupportedException(SR.net_ipv6_not_installed);
            }
        }

        static partial void InitializeSockets();

        // Creates a default send buffer if a buffer wasn't specified.  This follows the
        // ping.exe model.
        private byte[] DefaultSendBuffer
        {
            get
            {
                if (_defaultSendBuffer == null)
                {
                    _defaultSendBuffer = new byte[DefaultSendBufferSize];
                    for (int i = 0; i < DefaultSendBufferSize; i++)
                        _defaultSendBuffer[i] = (byte)((int)'a' + i % 23);
                }
                return _defaultSendBuffer;
            }
        }
    }
}
