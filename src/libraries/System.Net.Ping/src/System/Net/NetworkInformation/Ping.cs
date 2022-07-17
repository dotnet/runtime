// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    public partial class Ping : Component
    {
        private const int DefaultSendBufferSize = 32;  // Same as ping.exe on Windows.
        private const int DefaultTimeout = 5000;       // 5 seconds: same as ping.exe on Windows.
        private const int MaxBufferSize = 65500;       // Artificial constraint due to win32 api limitations.

        private readonly ManualResetEventSlim _lockObject = new ManualResetEventSlim(initialState: true); // doubles as the ability to wait on the current operation
        private SendOrPostCallback? _onPingCompletedDelegate;
        private bool _disposeRequested;
        private byte[]? _defaultSendBuffer;
        private CancellationTokenSource? _timeoutOrCancellationSource;
        // Used to differentiate between timeout and cancellation when _timeoutOrCancellationSource triggers
        private bool _canceled;

        // Thread safety:
        private const int Free = 0;
        private const int InProgress = 1;
        private new const int Disposed = 2;
        private int _status = Free;

        public Ping()
        {
            // This class once inherited a finalizer. For backward compatibility it has one so that
            // any derived class that depends on it will see the behaviour expected. Since it is
            // not used by this class itself, suppress it immediately if this is not an instance
            // of a derived class it doesn't suffer the GC burden of finalization.
            if (GetType() == typeof(Ping))
            {
                GC.SuppressFinalize(this);
            }
        }

        private void CheckArgs(int timeout, byte[] buffer, PingOptions? options)
        {
            CheckDisposed();
            ArgumentNullException.ThrowIfNull(buffer);

            if (buffer.Length > MaxBufferSize)
            {
                throw new ArgumentException(SR.net_invalidPingBufferSize, nameof(buffer));
            }

            if (timeout < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }
        }

        private void CheckArgs(IPAddress address, int timeout, byte[] buffer, PingOptions? options)
        {
            CheckArgs(timeout, buffer, options);

            ArgumentNullException.ThrowIfNull(address);

            // Check if address family is installed.
            TestIsIpSupported(address);

            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                throw new ArgumentException(SR.net_invalid_ip_addr, nameof(address));
            }
        }

        private void CheckDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposeRequested, this);
        }

        private void CheckStart()
        {
            int currentStatus;
            lock (_lockObject)
            {
                currentStatus = _status;
                if (currentStatus == Free)
                {
                    _timeoutOrCancellationSource ??= new();
                    _canceled = false;
                    _status = InProgress;
                    _lockObject.Reset();
                    return;
                }
            }

            if (currentStatus == InProgress)
            {
                throw new InvalidOperationException(SR.net_inasync);
            }
            else
            {
                Debug.Assert(currentStatus == Disposed, $"Expected currentStatus == Disposed, got {currentStatus}");
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private static IPAddress GetAddressSnapshot(IPAddress address)
        {
            IPAddress addressSnapshot = address.AddressFamily == AddressFamily.InterNetwork ?
#pragma warning disable CS0618 // IPAddress.Address is obsoleted, but it's the most efficient way to get the Int32 IPv4 address
                new IPAddress(address.Address) :
#pragma warning restore CS0618
                new IPAddress(address.GetAddressBytes(), address.ScopeId);

            return addressSnapshot;
        }

        private void Finish()
        {
            lock (_lockObject)
            {
                Debug.Assert(_status == InProgress, $"Invalid status: {_status}");
                _status = Free;
                if (!_timeoutOrCancellationSource!.TryReset())
                {
                    _timeoutOrCancellationSource = null;
                }
                _lockObject.Set();
            }

            if (_disposeRequested)
            {
                InternalDispose();
            }
        }

        // Cancels pending async requests, closes the handles.
        private void InternalDispose()
        {
            _disposeRequested = true;

            lock (_lockObject)
            {
                if (_status != Free)
                {
                    // Already disposed, or Finish will call Dispose again once Free.
                    return;
                }
                _status = Disposed;
            }

            _timeoutOrCancellationSource?.Dispose();

            InternalDisposeCore();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Only on explicit dispose.  Otherwise, the GC can cleanup everything else.
                InternalDispose();
            }
        }

        public event PingCompletedEventHandler? PingCompleted;

        protected void OnPingCompleted(PingCompletedEventArgs e)
        {
            PingCompleted?.Invoke(this, e);
        }

        public PingReply Send(string hostNameOrAddress)
        {
            return Send(hostNameOrAddress, DefaultTimeout, DefaultSendBuffer);
        }

        public PingReply Send(string hostNameOrAddress, int timeout)
        {
            return Send(hostNameOrAddress, timeout, DefaultSendBuffer);
        }

        public PingReply Send(IPAddress address)
        {
            return Send(address, DefaultTimeout, DefaultSendBuffer);
        }

        public PingReply Send(IPAddress address, int timeout)
        {
            return Send(address, timeout, DefaultSendBuffer);
        }

        public PingReply Send(string hostNameOrAddress, int timeout, byte[] buffer)
        {
            return Send(hostNameOrAddress, timeout, buffer, null);
        }

        public PingReply Send(IPAddress address, int timeout, byte[] buffer)
        {
            return Send(address, timeout, buffer, null);
        }

        public PingReply Send(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions? options)
        {
            if (string.IsNullOrEmpty(hostNameOrAddress))
            {
                throw new ArgumentNullException(nameof(hostNameOrAddress));
            }

            if (IPAddress.TryParse(hostNameOrAddress, out IPAddress? address))
            {
                return Send(address, timeout, buffer, options);
            }

            CheckArgs(timeout, buffer, options);

            return GetAddressAndSend(hostNameOrAddress, timeout, buffer, options);
        }

        public PingReply Send(IPAddress address, int timeout, byte[] buffer, PingOptions? options)
        {
            CheckArgs(address, timeout, buffer, options);

            // Need to snapshot the address here, so we're sure that it's not changed between now
            // and the operation, and to be sure that IPAddress.ToString() is called and not some override.
            IPAddress addressSnapshot = GetAddressSnapshot(address);

            CheckStart();
            try
            {
                return SendPingCore(addressSnapshot, buffer, timeout, options);
            }
            catch (Exception e) when (e is not PlatformNotSupportedException)
            {
                throw new PingException(SR.net_ping, e);
            }
            finally
            {
                Finish();
            }
        }

        public PingReply Send(IPAddress address, TimeSpan timeout, byte[]? buffer = null, PingOptions? options = null) =>
            Send(address, ToTimeoutMilliseconds(timeout), buffer ?? DefaultSendBuffer, options);

        public PingReply Send(string hostNameOrAddress, TimeSpan timeout, byte[]? buffer = null,
            PingOptions? options = null) => Send(hostNameOrAddress, ToTimeoutMilliseconds(timeout), buffer ?? DefaultSendBuffer, options);

        public void SendAsync(string hostNameOrAddress, object? userToken)
        {
            SendAsync(hostNameOrAddress, DefaultTimeout, DefaultSendBuffer, userToken);
        }

        public void SendAsync(string hostNameOrAddress, int timeout, object? userToken)
        {
            SendAsync(hostNameOrAddress, timeout, DefaultSendBuffer, userToken);
        }

        public void SendAsync(IPAddress address, object? userToken)
        {
            SendAsync(address, DefaultTimeout, DefaultSendBuffer, userToken);
        }

        public void SendAsync(IPAddress address, int timeout, object? userToken)
        {
            SendAsync(address, timeout, DefaultSendBuffer, userToken);
        }

        public void SendAsync(string hostNameOrAddress, int timeout, byte[] buffer, object? userToken)
        {
            SendAsync(hostNameOrAddress, timeout, buffer, null, userToken);
        }

        public void SendAsync(IPAddress address, int timeout, byte[] buffer, object? userToken)
        {
            SendAsync(address, timeout, buffer, null, userToken);
        }

        public void SendAsync(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions? options, object? userToken)
        {
            TranslateTaskToEap(userToken, SendPingAsync(hostNameOrAddress, timeout, buffer, options));
        }

        public void SendAsync(IPAddress address, int timeout, byte[] buffer, PingOptions? options, object? userToken)
        {
            TranslateTaskToEap(userToken, SendPingAsync(address, timeout, buffer, options));
        }

        private void TranslateTaskToEap(object? userToken, Task<PingReply> pingTask)
        {
            pingTask.ContinueWith((t, state) =>
            {
                var asyncOp = (AsyncOperation)state!;
                var e = new PingCompletedEventArgs(t.IsCompletedSuccessfully ? t.Result : null, t.Exception, t.IsCanceled, asyncOp.UserSuppliedState);
                SendOrPostCallback callback = _onPingCompletedDelegate ??= new SendOrPostCallback(o => { OnPingCompleted((PingCompletedEventArgs)o!); });
                asyncOp.PostOperationCompleted(callback, e);
            }, AsyncOperationManager.CreateOperation(userToken), CancellationToken.None, TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public Task<PingReply> SendPingAsync(IPAddress address)
        {
            return SendPingAsync(address, DefaultTimeout, DefaultSendBuffer, null);
        }

        public Task<PingReply> SendPingAsync(string hostNameOrAddress)
        {
            return SendPingAsync(hostNameOrAddress, DefaultTimeout, DefaultSendBuffer, null);
        }

        public Task<PingReply> SendPingAsync(IPAddress address, int timeout)
        {
            return SendPingAsync(address, timeout, DefaultSendBuffer, null);
        }

        public Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout)
        {
            return SendPingAsync(hostNameOrAddress, timeout, DefaultSendBuffer, null);
        }

        public Task<PingReply> SendPingAsync(IPAddress address, int timeout, byte[] buffer)
        {
            return SendPingAsync(address, timeout, buffer, null);
        }

        public Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout, byte[] buffer)
        {
            return SendPingAsync(hostNameOrAddress, timeout, buffer, null);
        }

        public Task<PingReply> SendPingAsync(IPAddress address, int timeout, byte[] buffer, PingOptions? options)
        {
            return SendPingAsync(address, timeout, buffer, options, CancellationToken.None);
        }

        public Task<PingReply> SendPingAsync(IPAddress address, TimeSpan timeout, byte[]? buffer = null, PingOptions? options = null, CancellationToken cancellationToken = default)
        {
            return SendPingAsync(address, ToTimeoutMilliseconds(timeout), buffer ?? DefaultSendBuffer, options, cancellationToken);
        }

        private Task<PingReply> SendPingAsync(IPAddress address, int timeout, byte[] buffer, PingOptions? options, CancellationToken cancellationToken)
        {
            CheckArgs(address, timeout, buffer, options);

            return SendPingAsyncInternal(
                // Need to snapshot the address here, so we're sure that it's not changed between now
                // and the operation, and to be sure that IPAddress.ToString() is called and not some override.
                GetAddressSnapshot(address),
                static (address, cancellationToken) => new ValueTask<IPAddress>(address),
                timeout,
                buffer,
                options,
                cancellationToken);
        }

        public Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions? options)
        {
            return SendPingAsync(hostNameOrAddress, timeout, buffer, options, CancellationToken.None);
        }

        public Task<PingReply> SendPingAsync(string hostNameOrAddress, TimeSpan timeout, byte[]? buffer = null, PingOptions? options = null, CancellationToken cancellationToken = default)
        {
            return SendPingAsync(hostNameOrAddress, ToTimeoutMilliseconds(timeout), buffer ?? DefaultSendBuffer, options, cancellationToken);
        }

        private Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions? options, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(hostNameOrAddress))
            {
                throw new ArgumentNullException(nameof(hostNameOrAddress));
            }

            if (IPAddress.TryParse(hostNameOrAddress, out IPAddress? address))
            {
                return SendPingAsync(address, timeout, buffer, options, cancellationToken);
            }

            CheckArgs(timeout, buffer, options);

            return SendPingAsyncInternal(
                hostNameOrAddress,
                static async (hostName, cancellationToken) =>
                    (await Dns.GetHostAddressesAsync(hostName, cancellationToken).ConfigureAwait(false))[0],
                timeout,
                buffer,
                options,
                cancellationToken);
        }

        private static int ToTimeoutMilliseconds(TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }
            return (int)totalMilliseconds;
        }

        public void SendAsyncCancel()
        {
            lock (_lockObject)
            {
                if (!_lockObject.IsSet)
                {
                    SetCanceled();
                }
            }

            // As in the .NET Framework, synchronously wait for the in-flight operation to complete.
            // If there isn't one in flight, this event will already be set.
            _lockObject.Wait();
        }

        private void SetCanceled()
        {
            _canceled = true;
            _timeoutOrCancellationSource?.Cancel();
        }

        private PingReply GetAddressAndSend(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions? options)
        {
            CheckStart();
            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(hostNameOrAddress);
                return SendPingCore(addresses[0], buffer, timeout, options);
            }
            catch (Exception e) when (e is not PlatformNotSupportedException)
            {
                throw new PingException(SR.net_ping, e);
            }
            finally
            {
                Finish();
            }
        }

        private async Task<PingReply> SendPingAsyncInternal<TArg>(
            TArg getAddressArg,
            Func<TArg, CancellationToken, ValueTask<IPAddress>> getAddress,
            int timeout,
            byte[] buffer,
            PingOptions? options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CheckStart();
            try
            {
                using CancellationTokenRegistration _ = cancellationToken.Register(static state => ((Ping)state!).SetCanceled(), this);

                IPAddress address = await getAddress(getAddressArg, _timeoutOrCancellationSource!.Token).ConfigureAwait(false);

                _timeoutOrCancellationSource.CancelAfter(timeout);

                return await SendPingAsyncCore(address, buffer, timeout, options).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not PlatformNotSupportedException && !_canceled)
            {
                throw new PingException(SR.net_ping, e);
            }
            finally
            {
                Finish();
            }
        }

        // Tests if the current machine supports the given ip protocol family.
        private static void TestIsIpSupported(IPAddress ip)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork && !SocketProtocolSupportPal.OSSupportsIPv4)
            {
                throw new NotSupportedException(SR.net_ipv4_not_installed);
            }
            else if ((ip.AddressFamily == AddressFamily.InterNetworkV6 && !SocketProtocolSupportPal.OSSupportsIPv6))
            {
                throw new NotSupportedException(SR.net_ipv6_not_installed);
            }
        }

        partial void InternalDisposeCore();

        // Creates a default send buffer if a buffer wasn't specified.  This follows the ping.exe model.
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
