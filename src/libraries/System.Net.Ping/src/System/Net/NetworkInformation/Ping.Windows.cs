// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    public partial class Ping
    {
        private const int MaxUdpPacket = 0xFFFF + 256; // Marshal.SizeOf(typeof(Icmp6EchoReply)) * 2 + ip header info;

        private static readonly SafeWaitHandle s_nullSafeWaitHandle = new SafeWaitHandle(IntPtr.Zero, true);

        private int _sendSize;  // Needed to determine what the reply size is for ipv6 in callback.
        private bool _ipv6;
        private ManualResetEvent? _pingEvent;
        private RegisteredWaitHandle? _registeredWait;
        private SafeLocalAllocHandle? _requestBuffer;
        private SafeLocalAllocHandle? _replyBuffer;
        private Interop.IpHlpApi.SafeCloseIcmpHandle? _handlePingV4;
        private Interop.IpHlpApi.SafeCloseIcmpHandle? _handlePingV6;
        private TaskCompletionSource<PingReply>? _taskCompletionSource;

        private PingReply SendPingCore(IPAddress address, byte[] buffer, int timeout, PingOptions? options)
        {
            // Since isAsync == false, DoSendPingCore will execute synchronously and return a completed
            // Task - so no blocking here
            return DoSendPingCore(address, buffer, timeout, options, isAsync: false).GetAwaiter().GetResult();
        }

        private Task<PingReply> SendPingAsyncCore(IPAddress address, byte[] buffer, int timeout, PingOptions? options)
        {
            // Since isAsync == true, DoSendPingCore will execute asynchronously and return an active Task
            return DoSendPingCore(address, buffer, timeout, options, isAsync: true);
        }

        // Any exceptions that escape synchronously will be caught by the caller and wrapped in a PingException.
        // We do not need to or want to capture such exceptions into the returned task.
        private Task<PingReply> DoSendPingCore(IPAddress address, byte[] buffer, int timeout, PingOptions? options, bool isAsync)
        {
            TaskCompletionSource<PingReply>? tcs = null;
            if (isAsync)
            {
                _taskCompletionSource = tcs = new TaskCompletionSource<PingReply>();
            }

            _ipv6 = (address.AddressFamily == AddressFamily.InterNetworkV6);
            _sendSize = buffer.Length;

            // Cache correct handle.
            InitialiseIcmpHandle();

            if (_replyBuffer == null)
            {
                _replyBuffer = SafeLocalAllocHandle.LocalAlloc(MaxUdpPacket);
            }

            int error;
            try
            {
                if (isAsync)
                {
                    RegisterWaitHandle();
                }

                SetUnmanagedStructures(buffer);

                error = SendEcho(address, buffer, timeout, options, isAsync);
            }
            catch
            {
                Cleanup(isAsync);
                throw;
            }

            if (error == 0)
            {
                error = Marshal.GetLastPInvokeError();

                // Only skip Async IO Pending error value.
                if (!isAsync || error != Interop.IpHlpApi.ERROR_IO_PENDING)
                {
                    Cleanup(isAsync);

                    IPStatus status = GetStatusFromCode(error);
                    return Task.FromResult(new PingReply(address, default, status, default, Array.Empty<byte>()));
                }
            }

            if (tcs != null)
            {
                return tcs.Task;
            }

            Cleanup(isAsync);
            return Task.FromResult(CreatePingReply());
        }

        private void RegisterWaitHandle()
        {
            if (_pingEvent == null)
            {
                _pingEvent = new ManualResetEvent(false);
            }
            else
            {
                _pingEvent.Reset();
            }

            _registeredWait = ThreadPool.RegisterWaitForSingleObject(_pingEvent, (state, _) => ((Ping)state!).PingCallback(), this, -1, true);
        }

        private void UnregisterWaitHandle()
        {
            lock (_lockObject)
            {
                if (_registeredWait != null)
                {
                    // If Unregister returns false, it is sufficient to nullify registeredWait
                    // and let its own finalizer clean up later.
                    _registeredWait.Unregister(null);
                    _registeredWait = null;
                }
            }
        }

        private SafeWaitHandle GetWaitHandle(bool async)
        {
            if (async)
            {
                return _pingEvent!.GetSafeWaitHandle();
            }

            return s_nullSafeWaitHandle;
        }

        private void InitialiseIcmpHandle()
        {
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
        }

        private int SendEcho(IPAddress address, byte[] buffer, int timeout, PingOptions? options, bool isAsync)
        {
            Interop.IpHlpApi.IPOptions ipOptions = new Interop.IpHlpApi.IPOptions(options);
            if (!_ipv6)
            {
                return (int)Interop.IpHlpApi.IcmpSendEcho2(
                    _handlePingV4!,
                    GetWaitHandle(isAsync),
                    IntPtr.Zero,
                    IntPtr.Zero,
#pragma warning disable CS0618 // Address is marked obsolete
                    (uint)address.Address,
#pragma warning restore CS0618
                    _requestBuffer!,
                    (ushort)buffer.Length,
                    ref ipOptions,
                    _replyBuffer!,
                    MaxUdpPacket,
                    (uint)timeout);
            }

            IPEndPoint ep = new IPEndPoint(address, 0);
            Internals.SocketAddress remoteAddr = IPEndPointExtensions.Serialize(ep);
            byte[] sourceAddr = new byte[28];

            return (int)Interop.IpHlpApi.Icmp6SendEcho2(
                _handlePingV6!,
                GetWaitHandle(isAsync),
                IntPtr.Zero,
                IntPtr.Zero,
                sourceAddr,
                remoteAddr.Buffer,
                _requestBuffer!,
                (ushort)buffer.Length,
                ref ipOptions,
                _replyBuffer!,
                MaxUdpPacket,
                (uint)timeout);
        }

        private PingReply CreatePingReply()
        {
            SafeLocalAllocHandle buffer = _replyBuffer!;

            // Marshals and constructs new reply.
            if (_ipv6)
            {
                Interop.IpHlpApi.Icmp6EchoReply icmp6Reply = Marshal.PtrToStructure<Interop.IpHlpApi.Icmp6EchoReply>(buffer.DangerousGetHandle());
                return CreatePingReplyFromIcmp6EchoReply(icmp6Reply, buffer.DangerousGetHandle(), _sendSize);
            }

            Interop.IpHlpApi.IcmpEchoReply icmpReply = Marshal.PtrToStructure<Interop.IpHlpApi.IcmpEchoReply>(buffer.DangerousGetHandle());
            return CreatePingReplyFromIcmpEchoReply(icmpReply);
        }

        private void Cleanup(bool isAsync)
        {
            FreeUnmanagedStructures();

            if (isAsync)
            {
                UnregisterWaitHandle();
            }
        }

        partial void InternalDisposeCore()
        {
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

            if (_pingEvent != null)
            {
                _pingEvent.Dispose();
                _pingEvent = null;
            }

            if (_replyBuffer != null)
            {
                _replyBuffer.Dispose();
                _replyBuffer = null;
            }
        }

        // Private callback invoked when icmpsendecho APIs succeed.
        private void PingCallback()
        {
            TaskCompletionSource<PingReply>? tcs = _taskCompletionSource;
            _taskCompletionSource = null;
            Debug.Assert(tcs != null);

            PingReply? reply = null;
            Exception? error = null;
            bool canceled = false;

            try
            {
                lock (_lockObject)
                {
                    canceled = _canceled;

                    reply = CreatePingReply();
                }
            }
            catch (Exception e)
            {
                // In case of failure, create a failed event arg.
                error = new PingException(SR.net_ping, e);
            }
            finally
            {
                Cleanup(isAsync: true);
            }

            // Once we've called Finish, complete the task
            if (canceled)
            {
                tcs.SetCanceled();
            }
            else if (reply != null)
            {
                tcs.SetResult(reply);
            }
            else
            {
                Debug.Assert(error != null);
                tcs.SetException(error);
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

        private static IPStatus GetStatusFromCode(int statusCode)
        {
            // Caveat lector: IcmpSendEcho2 doesn't allow us to know for sure if an error code
            // is an IP Status code or a general win32 error code. This assumes everything under
            // the base is a win32 error, and everything beyond is an IPStatus.
            if (statusCode != 0 && statusCode < Interop.IpHlpApi.IP_STATUS_BASE)
            {
                throw new Win32Exception(statusCode);
            }

            return (IPStatus)statusCode;
        }

        private static PingReply CreatePingReplyFromIcmpEchoReply(Interop.IpHlpApi.IcmpEchoReply reply)
        {
            const int DontFragmentFlag = 2;

            IPAddress address = new IPAddress(reply.address);
            IPStatus ipStatus = GetStatusFromCode((int)reply.status);

            long rtt;
            PingOptions? options;
            byte[] buffer;

            if (ipStatus == IPStatus.Success)
            {
                // Only copy the data if we succeed w/ the ping operation.
                rtt = reply.roundTripTime;
                options = new PingOptions(reply.options.ttl, (reply.options.flags & DontFragmentFlag) > 0);
                buffer = new byte[reply.dataSize];
                Marshal.Copy(reply.data, buffer, 0, reply.dataSize);
            }
            else
            {
                rtt = 0;
                options = null;
                buffer = Array.Empty<byte>();
            }

            return new PingReply(address, options, ipStatus, rtt, buffer);
        }

        private static PingReply CreatePingReplyFromIcmp6EchoReply(Interop.IpHlpApi.Icmp6EchoReply reply, IntPtr dataPtr, int sendSize)
        {
            IPAddress address = new IPAddress(reply.Address.Address, reply.Address.ScopeID);
            IPStatus ipStatus = GetStatusFromCode((int)reply.Status);

            long rtt;
            byte[] buffer;

            if (ipStatus == IPStatus.Success)
            {
                // Only copy the data if we succeed w/ the ping operation.
                rtt = reply.RoundTripTime;
                buffer = new byte[sendSize];
                Marshal.Copy(dataPtr + 36, buffer, 0, sendSize);
            }
            else
            {
                rtt = 0;
                buffer = Array.Empty<byte>();
            }

            return new PingReply(address, null, ipStatus, rtt, buffer);
        }
    }
}
