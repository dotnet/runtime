// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    public delegate void PingCompletedEventHandler(object sender, PingCompletedEventArgs e);

    public class PingCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        private PingReply _reply;

        internal PingCompletedEventArgs(PingReply reply, Exception error, bool cancelled, object userToken) : base(error, cancelled, userToken)
        {
            _reply = reply;
        }

        public PingReply Reply { get { return _reply; } }
    }

    public class Ping : IDisposable
    {
        private const int MaxUdpPacket = 0xFFFF + 256; // Marshal.SizeOf(typeof(Icmp6EchoReply)) * 2 + ip header info;  
        private const int MaxBufferSize = 65500; // Artificial constraint due to win32 api limitations.
        private const int DefaultTimeout = 5000; // 5 seconds: same as ping.exe.
        private const int DefaultSendBufferSize = 32;  // Same as ping.exe.

        private byte[] _defaultSendBuffer = null;
        private bool _ipv6 = false;
        private bool _cancelled = false;
        private bool _disposeRequested = false;
        private object _lockObject = new object();
        private static bool s_socketInitialized;
        private static readonly object s_socketInitializationLock = new object();

        // Used by icmpsendecho APIs:
        internal ManualResetEvent pingEvent = null;
        private RegisteredWaitHandle _registeredWait = null;
        private SafeLocalAllocHandle _requestBuffer = null;
        private SafeLocalAllocHandle _replyBuffer = null;
        private int _sendSize = 0;  // Needed to determine what reply size is for ipv6 in callback.

        private SafeCloseIcmpHandle _handlePingV4 = null;
        private SafeCloseIcmpHandle _handlePingV6 = null;

        private AsyncOperation _asyncOp = null;
        private SendOrPostCallback _onPingCompletedDelegate;
        public event PingCompletedEventHandler PingCompleted;

        // For blocking in SendAsyncCancel():
        private ManualResetEvent _asyncFinished = null;

        private bool InAsyncCall
        {
            get
            {
                if (_asyncFinished == null)
                {
                    return false;
                }

                // Never blocks, just checks if a thread would block.
                return !_asyncFinished.WaitOne(0);
            }
            set
            {
                if (_asyncFinished == null)
                {
                    _asyncFinished = new ManualResetEvent(!value);
                }
                else if (value)
                {
                    _asyncFinished.Reset(); // Block
                }
                else
                {
                    _asyncFinished.Set(); // Clear
                }
            }
        }

        // Thread safety:
        private const int Free = 0;
        private const int InProgress = 1;
        private const int Disposed = 2;
        private int _status = Free;

        private void CheckStart(bool async)
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

            if (async)
            {
                InAsyncCall = true;
            }
        }

        private void Finish(bool async)
        {
            Debug.Assert(_status == InProgress, "Invalid status: " + _status);
            _status = Free;

            if (async)
            {
                InAsyncCall = false;
            }

            if (_disposeRequested)
            {
                InternalDispose();
            }
        }

        protected void OnPingCompleted(PingCompletedEventArgs e)
        {
            if (PingCompleted != null)
            {
                PingCompleted(this, e);
            }
        }

        private void PingCompletedWaitCallback(object operationState)
        {
            OnPingCompleted((PingCompletedEventArgs)operationState);
        }

        public Ping()
        {
            _onPingCompletedDelegate = new SendOrPostCallback(PingCompletedWaitCallback);
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

            if (_handlePingV4 != null)
            {
                _handlePingV4.Dispose();
                _handlePingV4 = null;
            }

            if (_handlePingV6 != null)
            {
                _handlePingV6.Dispose();
                _handlePingV6 = null;
            }

            UnregisterWaitHandle();

            if (pingEvent != null)
            {
                pingEvent.Dispose();
                pingEvent = null;
            }

            if (_replyBuffer != null)
            {
                _replyBuffer.Dispose();
                _replyBuffer = null;
            }

            if (_asyncFinished != null)
            {
                _asyncFinished.Dispose();
                _asyncFinished = null;
            }
        }

        private void UnregisterWaitHandle()
        {
            lock (_lockObject)
            {
                if (_registeredWait != null)
                {
                    _registeredWait.Unregister(null);
                    // If Unregister returns false, it is sufficient to nullify registeredWait
                    // and let its own finalizer clean up later.
                    _registeredWait = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                // Only on explicit dispose.  Otherwise, the GC can cleanup everything else.
                InternalDispose();
            }
        }

        // Private callback invoked when icmpsendecho APIs succeed.
        private static void PingCallback(object state, bool signaled)
        {
            Ping ping = (Ping)state;
            PingCompletedEventArgs eventArgs = null;
            bool cancelled = false;
            AsyncOperation asyncOp = null;
            SendOrPostCallback onPingCompletedDelegate = null;

            try
            {
                lock (ping._lockObject)
                {
                    cancelled = ping._cancelled;
                    asyncOp = ping._asyncOp;
                    onPingCompletedDelegate = ping._onPingCompletedDelegate;

                    if (!cancelled)
                    {
                        // Parse reply buffer.
                        SafeLocalAllocHandle buffer = ping._replyBuffer;

                        // Marshals and constructs new reply.
                        PingReply reply;

                        if (ping._ipv6)
                        {
                            Interop.IpHlpApi.Icmp6EchoReply icmp6Reply = Marshal.PtrToStructure<Interop.IpHlpApi.Icmp6EchoReply>(buffer.DangerousGetHandle());
                            reply = new PingReply(icmp6Reply, buffer.DangerousGetHandle(), ping._sendSize);
                        }
                        else
                        {
                            Interop.IpHlpApi.IcmpEchoReply icmpReply = Marshal.PtrToStructure<Interop.IpHlpApi.IcmpEchoReply>(buffer.DangerousGetHandle());
                            reply = new PingReply(icmpReply);
                        }

                        eventArgs = new PingCompletedEventArgs(reply, null, false, asyncOp.UserSuppliedState);
                    }
                    else
                    {
                        // Canceled.
                        eventArgs = new PingCompletedEventArgs(null, null, true, asyncOp.UserSuppliedState);
                    }
                }
            }
            catch (Exception e)
            {
                // In case of failure, create a failed event arg.
                PingException pe = new PingException(SR.net_ping, e);
                eventArgs = new PingCompletedEventArgs(null, pe, false, asyncOp.UserSuppliedState);
            }
            finally
            {
                ping.FreeUnmanagedStructures();
                ping.UnregisterWaitHandle();
                ping.Finish(true);
            }

            asyncOp.PostOperationCompleted(onPingCompletedDelegate, eventArgs);
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

            CheckStart(true);
            try
            {
                _cancelled = false;
                _asyncOp = AsyncOperationManager.CreateOperation(userToken);
                AsyncStateObject state = new AsyncStateObject(hostNameOrAddress, buffer, timeout, options, userToken);
                ThreadPool.QueueUserWorkItem(new WaitCallback(ContinueAsyncSend), state);
            }
            catch (Exception e)
            {
                Finish(true);
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

            // FxCop: need to snapshot the address here, so we're sure that it's not changed between the permission
            // check and the operation, and to be sure that IPAddress.ToString() is called and not some override.
            IPAddress addressSnapshot;
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                addressSnapshot = new IPAddress(address.GetAddressBytes());
            }
            else
            {
                addressSnapshot = new IPAddress(address.GetAddressBytes(), address.ScopeId);
            }

            CheckStart(true);
            try
            {
                _cancelled = false;
                _asyncOp = AsyncOperationManager.CreateOperation(userToken);
                InternalSend(addressSnapshot, buffer, timeout, options, true);
            }
            catch (Exception e)
            {
                Finish(true);
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

        internal class AsyncStateObject
        {
            internal AsyncStateObject(string hostName, byte[] buffer, int timeout, PingOptions options, object userToken)
            {
                HostName = hostName;
                Buffer = buffer;
                Timeout = timeout;
                Options = options;
                UserToken = userToken;
            }

            internal byte[] Buffer;
            internal string HostName;
            internal int Timeout;
            internal PingOptions Options;
            internal object UserToken;
        }

        private void ContinueAsyncSend(object state)
        {
            // FxCop: need to snapshot the address here, so we're sure that it's not changed between the permission
            // check and the operation, and to be sure that IPAddress.ToString() is called and not some override.
            Debug.Assert(_asyncOp != null, "Null AsyncOp?");

            AsyncStateObject stateObject = (AsyncStateObject)state;

            try
            {
                IPAddress addressSnapshot = Dns.GetHostAddressesAsync(stateObject.HostName).GetAwaiter().GetResult()[0];
                InternalSend(addressSnapshot, stateObject.Buffer, stateObject.Timeout, stateObject.Options, true);
            }
            catch (Exception e)
            {
                PingException pe = new PingException(SR.net_ping, e);
                PingCompletedEventArgs eventArgs = new PingCompletedEventArgs(null, pe, false, _asyncOp.UserSuppliedState);
                Finish(true);
                _asyncOp.PostOperationCompleted(_onPingCompletedDelegate, eventArgs);
            }
        }

        private PingReply InternalSend(IPAddress address, byte[] buffer, int timeout, PingOptions options, bool async)
        {
            _ipv6 = (address.AddressFamily == AddressFamily.InterNetworkV6) ? true : false;
            _sendSize = buffer.Length;

            // Get and cache correct handle.
            if (!_ipv6 && _handlePingV4 == null)
            {
                _handlePingV4 = Interop.IpHlpApi.IcmpCreateFile();
                if (_handlePingV4.IsInvalid)
                {
                    _handlePingV4 = null;
                    throw new Win32Exception(); // Gets last error.
                }
            }
            else if (_ipv6 && _handlePingV6 == null)
            {
                _handlePingV6 = Interop.IpHlpApi.Icmp6CreateFile();
                if (_handlePingV6.IsInvalid)
                {
                    _handlePingV6 = null;
                    throw new Win32Exception(); // Gets last error.
                }
            }

            var ipOptions = new Interop.IpHlpApi.IPOptions(options);

            if (_replyBuffer == null)
            {
                _replyBuffer = SafeLocalAllocHandle.LocalAlloc(MaxUdpPacket);
            }

            // Queue the event.
            int error;

            try
            {
                if (async)
                {
                    if (pingEvent == null)
                    {
                        pingEvent = new ManualResetEvent(false);
                    }
                    else
                    {
                        pingEvent.Reset();
                    }

                    _registeredWait = ThreadPool.RegisterWaitForSingleObject(pingEvent, new WaitOrTimerCallback(PingCallback), this, -1, true);
                }

                SetUnmanagedStructures(buffer);

                if (!_ipv6)
                {
                    if (async)
                    {
                        SafeWaitHandle pingEventSafeWaitHandle = pingEvent.GetSafeWaitHandle();
                        error = (int)Interop.IpHlpApi.IcmpSendEcho2(
                            _handlePingV4,
                            pingEventSafeWaitHandle,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            (uint)address.GetAddress(),
                            _requestBuffer,
                            (ushort)buffer.Length,
                            ref ipOptions,
                            _replyBuffer,
                            MaxUdpPacket,
                            (uint)timeout);
                    }
                    else
                    {
                        error = (int)Interop.IpHlpApi.IcmpSendEcho2(
                            _handlePingV4,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            (uint)address.GetAddress(),
                            _requestBuffer,
                            (ushort)buffer.Length,
                            ref ipOptions,
                            _replyBuffer,
                            MaxUdpPacket,
                            (uint)timeout);
                    }
                }
                else
                {
                    IPEndPoint ep = new IPEndPoint(address, 0);
                    Internals.SocketAddress remoteAddr = IPEndPointExtensions.Serialize(ep);
                    byte[] sourceAddr = new byte[28];
                    if (async)
                    {
                        SafeWaitHandle pingEventSafeWaitHandle = pingEvent.GetSafeWaitHandle();
                        error = (int)Interop.IpHlpApi.Icmp6SendEcho2(
                            _handlePingV6,
                            pingEventSafeWaitHandle,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            sourceAddr,
                            remoteAddr.Buffer,
                            _requestBuffer,
                            (ushort)buffer.Length,
                            ref ipOptions,
                            _replyBuffer,
                            MaxUdpPacket,
                            (uint)timeout);
                    }
                    else
                    {
                        error = (int)Interop.IpHlpApi.Icmp6SendEcho2(
                            _handlePingV6,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            sourceAddr,
                            remoteAddr.Buffer,
                            _requestBuffer,
                            (ushort)buffer.Length,
                            ref ipOptions,
                            _replyBuffer,
                            MaxUdpPacket,
                            (uint)timeout);
                    }
                }
            }
            catch
            {
                UnregisterWaitHandle();
                throw;
            }

            if (error == 0)
            {
                error = Marshal.GetLastWin32Error();

                // Only skip Async IO Pending error value.
                if (async && error == Interop.IpHlpApi.ERROR_IO_PENDING)
                {
                    // Expected async return value.
                    return null;
                }

                // Cleanup.
                FreeUnmanagedStructures();
                UnregisterWaitHandle();

                if (async // No IPStatus async errors.
                    || error < (int)IPStatus.DestinationNetworkUnreachable // Min
                    || error > (int)IPStatus.DestinationScopeMismatch) // Max or Out of IPStatus range.
                {
                    throw new Win32Exception(error);
                }

                return new PingReply((IPStatus)error); // Synchronous IPStatus errors.
            }

            if (async)
            {
                return null;
            }

            FreeUnmanagedStructures();

            PingReply reply;
            if (_ipv6)
            {
                Interop.IpHlpApi.Icmp6EchoReply icmp6Reply = Marshal.PtrToStructure<Interop.IpHlpApi.Icmp6EchoReply>(_replyBuffer.DangerousGetHandle());
                reply = new PingReply(icmp6Reply, _replyBuffer.DangerousGetHandle(), _sendSize);
            }
            else
            {
                Interop.IpHlpApi.IcmpEchoReply icmpReply = Marshal.PtrToStructure<Interop.IpHlpApi.IcmpEchoReply>(_replyBuffer.DangerousGetHandle());
                reply = new PingReply(icmpReply);
            }

            // IcmpEchoReply still has an unsafe IntPtr reference into replybuffer
            // and replybuffer was being freed prematurely by the GC, causing AccessViolationExceptions.
            GC.KeepAlive(_replyBuffer);

            return reply;
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

        // Copies _requestBuffer into unmanaged memory for async icmpsendecho APIs.
        private unsafe void SetUnmanagedStructures(byte[] buffer)
        {
            _requestBuffer = SafeLocalAllocHandle.LocalAlloc(buffer.Length);
            byte* dst = (byte*)_requestBuffer.DangerousGetHandle();
            for (int i = 0; i < buffer.Length; ++i)
            {
                dst[i] = buffer[i];
            }
        }

        // Releases the unmanaged memory after ping completion.
        private void FreeUnmanagedStructures()
        {
            if (_requestBuffer != null)
            {
                _requestBuffer.Dispose();
                _requestBuffer = null;
            }
        }

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

        internal static void InitializeSockets()
        {
            if (!Volatile.Read(ref s_socketInitialized))
            {
                lock (s_socketInitializationLock)
                {
                    if (!s_socketInitialized)
                    {
                        // TODO: Note for PAL implementation: this call is not required for *NIX and should be avoided during PAL design.

                        // Ensure that WSAStartup has been called once per process.  
                        // The System.Net.NameResolution contract is responsible with the initialization.
                        Dns.GetHostName();

                        // Cache some settings locally.
                        s_socketInitialized = true;
                    }
                }
            }
        }
    }
}
