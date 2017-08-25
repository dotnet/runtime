// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private static readonly object s_socketInitializationLock = new object();
        private static bool s_socketInitialized;

        private int _sendSize = 0;  // Needed to determine what the reply size is for ipv6 in callback.
        private bool _ipv6 = false;
        private ManualResetEvent _pingEvent;
        private RegisteredWaitHandle _registeredWait;
        private SafeLocalAllocHandle _requestBuffer;
        private SafeLocalAllocHandle _replyBuffer;
        private Interop.IpHlpApi.SafeCloseIcmpHandle _handlePingV4;
        private Interop.IpHlpApi.SafeCloseIcmpHandle _handlePingV6;
        private TaskCompletionSource<PingReply> _taskCompletionSource;
        
        // Any exceptions that escape synchronously will be caught by the caller and wrapped in a PingException.
        // We do not need to or want to capture such exceptions into the returned task.
        private Task<PingReply> SendPingAsyncCore(IPAddress address, byte[] buffer, int timeout, PingOptions options)
        {
            var tcs = new TaskCompletionSource<PingReply>();
            _taskCompletionSource = tcs;

            _ipv6 = (address.AddressFamily == AddressFamily.InterNetworkV6);
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
                if (_pingEvent == null)
                {
                    _pingEvent = new ManualResetEvent(false);
                }
                else
                {
                    _pingEvent.Reset();
                }

                _registeredWait = ThreadPool.RegisterWaitForSingleObject(_pingEvent, (state, _) => ((Ping)state).PingCallback(), this, -1, true);

                SetUnmanagedStructures(buffer);

                if (!_ipv6)
                {
                    SafeWaitHandle pingEventSafeWaitHandle = _pingEvent.GetSafeWaitHandle();
                    error = (int)Interop.IpHlpApi.IcmpSendEcho2(
                        _handlePingV4,
                        pingEventSafeWaitHandle,
                        IntPtr.Zero,
                        IntPtr.Zero,
#pragma warning disable CS0618 // Address is marked obsolete
                        (uint)address.Address,
#pragma warning restore CS0618
                        _requestBuffer,
                        (ushort)buffer.Length,
                        ref ipOptions,
                        _replyBuffer,
                        MaxUdpPacket,
                        (uint)timeout);
                }
                else
                {
                    IPEndPoint ep = new IPEndPoint(address, 0);
                    Internals.SocketAddress remoteAddr = IPEndPointExtensions.Serialize(ep);
                    byte[] sourceAddr = new byte[28];

                    SafeWaitHandle pingEventSafeWaitHandle = _pingEvent.GetSafeWaitHandle();
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
                if (error != Interop.IpHlpApi.ERROR_IO_PENDING)
                {
                    // Cleanup.
                    FreeUnmanagedStructures();
                    UnregisterWaitHandle();

                    throw new Win32Exception(error);
                }
            }

            return tcs.Task;
        }

        /*private*/ partial void InternalDisposeCore()
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

        // Private callback invoked when icmpsendecho APIs succeed.
        private void PingCallback()
        {
            TaskCompletionSource<PingReply> tcs = _taskCompletionSource;
            _taskCompletionSource = null;

            PingReply reply = null;
            Exception error = null;
            bool canceled = false;

            try
            {
                lock (_lockObject)
                {
                    canceled = _canceled;

                    // Parse reply buffer.
                    SafeLocalAllocHandle buffer = _replyBuffer;

                    // Marshals and constructs new reply.
                    if (_ipv6)
                    {
                        Interop.IpHlpApi.Icmp6EchoReply icmp6Reply = Marshal.PtrToStructure<Interop.IpHlpApi.Icmp6EchoReply>(buffer.DangerousGetHandle());
                        reply = CreatePingReplyFromIcmp6EchoReply(icmp6Reply, buffer.DangerousGetHandle(), _sendSize);
                    }
                    else
                    {
                        Interop.IpHlpApi.IcmpEchoReply icmpReply = Marshal.PtrToStructure<Interop.IpHlpApi.IcmpEchoReply>(buffer.DangerousGetHandle());
                        reply = CreatePingReplyFromIcmpEchoReply(icmpReply);
                    }
                }
            }
            catch (Exception e)
            {
                // In case of failure, create a failed event arg.
                error = new PingException(SR.net_ping, e);
            }
            finally
            {
                FreeUnmanagedStructures();
                UnregisterWaitHandle();
                Finish();
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

        private static PingReply CreatePingReplyFromIcmpEchoReply(Interop.IpHlpApi.IcmpEchoReply reply)
        {
            const int DontFragmentFlag = 2;

            IPAddress address = new IPAddress(reply.address);
            IPStatus ipStatus = (IPStatus)reply.status; // The icmpsendecho IP status codes.

            long rtt;
            PingOptions options;
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
                rtt = default(long);
                options = default(PingOptions);
                buffer = Array.Empty<byte>();
            }

            return new PingReply(address, options, ipStatus, rtt, buffer);
        }

        private static PingReply CreatePingReplyFromIcmp6EchoReply(Interop.IpHlpApi.Icmp6EchoReply reply, IntPtr dataPtr, int sendSize)
        {
            IPAddress address = new IPAddress(reply.Address.Address, reply.Address.ScopeID);
            IPStatus ipStatus = (IPStatus)reply.Status; // The icmpsendecho IP status codes.

            long rtt;
            byte[] buffer;

            if (ipStatus == IPStatus.Success)
            {
                // Only copy the data if we succeed w/ the ping operation.
                rtt = reply.RoundTripTime;
                buffer = new byte[sendSize];
                Marshal.Copy(IntPtrHelper.Add(dataPtr, 36), buffer, 0, sendSize);
            }
            else
            {
                rtt = default(long);
                buffer = Array.Empty<byte>();
            }

            return new PingReply(address, default(PingOptions), ipStatus, rtt, buffer);
        }

        /*private*/ static partial void InitializeSockets()
        {
            if (!Volatile.Read(ref s_socketInitialized))
            {
                lock (s_socketInitializationLock)
                {
                    if (!s_socketInitialized)
                    {
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
