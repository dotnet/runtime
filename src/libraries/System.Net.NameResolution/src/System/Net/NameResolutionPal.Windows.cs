// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Net
{
    internal static partial class NameResolutionPal
    {
        private static volatile bool s_initialized;
        private static readonly object s_initializedLock = new object();

        private static readonly unsafe Interop.Winsock.LPLOOKUPSERVICE_COMPLETION_ROUTINE s_getAddrInfoExCallback = GetAddressInfoExCallback;
        private static bool s_getAddrInfoExSupported;

        public static void EnsureSocketsAreInitialized()
        {
            if (!s_initialized)
            {
                InitializeSockets();
            }

            static void InitializeSockets()
            {
                lock (s_initializedLock)
                {
                    if (!s_initialized)
                    {
                        SocketError errorCode = Interop.Winsock.WSAStartup();
                        if (errorCode != SocketError.Success)
                        {
                            // WSAStartup does not set LastWin32Error
                            throw new SocketException((int)errorCode);
                        }

                        s_getAddrInfoExSupported = GetAddrInfoExSupportsOverlapped();
                        s_initialized = true;
                    }
                }
            }
        }

        public static bool SupportsGetAddrInfoAsync
        {
            get
            {
                EnsureSocketsAreInitialized();
                return s_getAddrInfoExSupported;
            }
        }

        public static unsafe SocketError TryGetAddrInfo(string name, bool justAddresses, out string? hostName, out string[] aliases, out IPAddress[] addresses, out int nativeErrorCode)
        {
            aliases = Array.Empty<string>();

            var hints = new Interop.Winsock.AddressInfo { ai_family = AddressFamily.Unspecified }; // Gets all address families
            if (!justAddresses)
            {
                hints.ai_flags = AddressInfoHints.AI_CANONNAME;
            }

            Interop.Winsock.AddressInfo* result = null;
            try
            {
                SocketError errorCode = (SocketError)Interop.Winsock.GetAddrInfoW(name, null, &hints, &result);
                if (errorCode != SocketError.Success)
                {
                    nativeErrorCode = (int)errorCode;
                    hostName = name;
                    addresses = Array.Empty<IPAddress>();
                    return errorCode;
                }

                addresses = ParseAddressInfo(result, justAddresses, out hostName);
                nativeErrorCode = 0;
                return SocketError.Success;
            }
            finally
            {
                if (result != null)
                {
                    Interop.Winsock.FreeAddrInfoW(result);
                }
            }
        }

        public static unsafe string? TryGetNameInfo(IPAddress addr, out SocketError errorCode, out int nativeErrorCode)
        {
            SocketAddress address = new IPEndPoint(addr, 0).Serialize();
            Span<byte> addressBuffer = address.Size <= 64 ? stackalloc byte[64] : new byte[address.Size];
            for (int i = 0; i < address.Size; i++)
            {
                addressBuffer[i] = address[i];
            }

            const int NI_MAXHOST = 1025;
            char* hostname = stackalloc char[NI_MAXHOST];

            fixed (byte* addressBufferPtr = addressBuffer)
            {
                errorCode = Interop.Winsock.GetNameInfoW(
                    addressBufferPtr,
                    address.Size,
                    hostname,
                    NI_MAXHOST,
                    null, // We don't want a service name
                    0, // so no need for buffer or length
                    (int)Interop.Winsock.NameInfoFlags.NI_NAMEREQD);
            }

            if (errorCode == SocketError.Success)
            {
                nativeErrorCode = 0;
                return new string(hostname);
            }

            nativeErrorCode = (int)errorCode;
            return null;
        }

        public static unsafe string GetHostName()
        {
            // We do not cache the result in case the hostname changes.

            const int HostNameBufferLength = 256;
            byte* buffer = stackalloc byte[HostNameBufferLength];
            SocketError result = Interop.Winsock.gethostname(buffer, HostNameBufferLength);

            if (result != SocketError.Success)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, $"GetHostName failed with {result}");
                throw new SocketException();
            }

            return new string((sbyte*)buffer);
        }

        public static unsafe Task GetAddrInfoAsync(string hostName, bool justAddresses)
        {
            GetAddrInfoExContext* context = GetAddrInfoExContext.AllocateContext();

            GetAddrInfoExState state;
            try
            {
                state = new GetAddrInfoExState(hostName, justAddresses);
                context->QueryStateHandle = state.CreateHandle();
            }
            catch
            {
                GetAddrInfoExContext.FreeContext(context);
                throw;
            }

            var hints = new Interop.Winsock.AddressInfoEx { ai_family = AddressFamily.Unspecified }; // Gets all address families
            if (!justAddresses)
            {
                hints.ai_flags = AddressInfoHints.AI_CANONNAME;
            }

            SocketError errorCode = (SocketError)Interop.Winsock.GetAddrInfoExW(
                hostName, null, Interop.Winsock.NS_ALL, IntPtr.Zero, &hints, &context->Result, IntPtr.Zero, &context->Overlapped, s_getAddrInfoExCallback, &context->CancelHandle);

            if (errorCode != SocketError.IOPending)
            {
                ProcessResult(errorCode, context);
            }

            return state.Task;
        }

        private static unsafe void GetAddressInfoExCallback(int error, int bytes, NativeOverlapped* overlapped)
        {
            // Can be casted directly to GetAddrInfoExContext* because the overlapped is its first field
            GetAddrInfoExContext* context = (GetAddrInfoExContext*)overlapped;

            ProcessResult((SocketError)error, context);
        }

        private static unsafe void ProcessResult(SocketError errorCode, GetAddrInfoExContext* context)
        {
            try
            {
                GetAddrInfoExState state = GetAddrInfoExState.FromHandleAndFree(context->QueryStateHandle);

                if (errorCode == SocketError.Success)
                {
                    IPAddress[] addresses = ParseAddressInfoEx(context->Result, state.JustAddresses, out string? hostName);
                    state.SetResult(state.JustAddresses ? (object)
                        addresses :
                        new IPHostEntry
                        {
                            HostName = hostName ?? state.HostName,
                            Aliases = Array.Empty<string>(),
                            AddressList = addresses
                        });
                }
                else
                {
                    state.SetResult(ExceptionDispatchInfo.SetCurrentStackTrace(new SocketException((int)errorCode)));
                }
            }
            finally
            {
                GetAddrInfoExContext.FreeContext(context);
            }
        }

        private static unsafe IPAddress[] ParseAddressInfo(Interop.Winsock.AddressInfo* addressInfoPtr, bool justAddresses, out string? hostName)
        {
            Debug.Assert(addressInfoPtr != null);

            // Count how many results we have.
            int addressCount = 0;
            for (Interop.Winsock.AddressInfo* result = addressInfoPtr; result != null; result = result->ai_next)
            {
                int addressLength = (int)result->ai_addrlen;

                if (result->ai_family == AddressFamily.InterNetwork)
                {
                    if (addressLength == SocketAddressPal.IPv4AddressSize)
                    {
                        addressCount++;
                    }
                }
                else if (SocketProtocolSupportPal.OSSupportsIPv6 && result->ai_family == AddressFamily.InterNetworkV6)
                {
                    if (addressLength == SocketAddressPal.IPv6AddressSize)
                    {
                        addressCount++;
                    }
                }
            }

            // Store them into the array.
            var addresses = new IPAddress[addressCount];
            addressCount = 0;
            string? canonicalName = justAddresses ? "NONNULLSENTINEL" : null;
            for (Interop.Winsock.AddressInfo* result = addressInfoPtr; result != null; result = result->ai_next)
            {
                if (canonicalName == null && result->ai_canonname != null)
                {
                    canonicalName = Marshal.PtrToStringUni((IntPtr)result->ai_canonname);
                }

                int addressLength = (int)result->ai_addrlen;
                var socketAddress = new ReadOnlySpan<byte>(result->ai_addr, addressLength);

                if (result->ai_family == AddressFamily.InterNetwork)
                {
                    if (addressLength == SocketAddressPal.IPv4AddressSize)
                    {
                        addresses[addressCount++] = CreateIPv4Address(socketAddress);
                    }
                }
                else if (SocketProtocolSupportPal.OSSupportsIPv6 && result->ai_family == AddressFamily.InterNetworkV6)
                {
                    if (addressLength == SocketAddressPal.IPv6AddressSize)
                    {
                        addresses[addressCount++] = CreateIPv6Address(socketAddress);
                    }
                }
            }

            hostName = justAddresses ? null : canonicalName;
            return addresses;
        }

        private static unsafe IPAddress[] ParseAddressInfoEx(Interop.Winsock.AddressInfoEx* addressInfoExPtr, bool justAddresses, out string? hostName)
        {
            Debug.Assert(addressInfoExPtr != null);

            // First count how many address results we have.
            int addressCount = 0;
            for (Interop.Winsock.AddressInfoEx* result = addressInfoExPtr; result != null; result = result->ai_next)
            {
                int addressLength = (int)result->ai_addrlen;

                if (result->ai_family == AddressFamily.InterNetwork)
                {
                    if (addressLength == SocketAddressPal.IPv4AddressSize)
                    {
                        addressCount++;
                    }
                }
                else if (SocketProtocolSupportPal.OSSupportsIPv6 && result->ai_family == AddressFamily.InterNetworkV6)
                {
                    if (addressLength == SocketAddressPal.IPv6AddressSize)
                    {
                        addressCount++;
                    }
                }
            }

            // Then store them into an array.
            var addresses = new IPAddress[addressCount];
            addressCount = 0;
            string? canonicalName = justAddresses ? "NONNULLSENTINEL" : null;
            for (Interop.Winsock.AddressInfoEx* result = addressInfoExPtr; result != null; result = result->ai_next)
            {
                if (canonicalName == null && result->ai_canonname != IntPtr.Zero)
                {
                    canonicalName = Marshal.PtrToStringUni(result->ai_canonname);
                }

                int addressLength = (int)result->ai_addrlen;
                var socketAddress = new ReadOnlySpan<byte>(result->ai_addr, addressLength);

                if (result->ai_family == AddressFamily.InterNetwork)
                {
                    if (addressLength == SocketAddressPal.IPv4AddressSize)
                    {
                        addresses[addressCount++] = CreateIPv4Address(socketAddress);
                    }
                }
                else if (SocketProtocolSupportPal.OSSupportsIPv6 && result->ai_family == AddressFamily.InterNetworkV6)
                {
                    if (addressLength == SocketAddressPal.IPv6AddressSize)
                    {
                        addresses[addressCount++] = CreateIPv6Address(socketAddress);
                    }
                }
            }

            // Return the parsed host name (if we got one) and addresses.
            hostName = justAddresses ? null : canonicalName;
            return addresses;
        }

        private static unsafe IPAddress CreateIPv4Address(ReadOnlySpan<byte> socketAddress)
        {
            long address = (long)SocketAddressPal.GetIPv4Address(socketAddress) & 0x0FFFFFFFF;
            return new IPAddress(address);
        }

        private static unsafe IPAddress CreateIPv6Address(ReadOnlySpan<byte> socketAddress)
        {
            Span<byte> address = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
            SocketAddressPal.GetIPv6Address(socketAddress, address, out uint scope);
            return new IPAddress(address, scope);
        }

        private sealed class GetAddrInfoExState : IThreadPoolWorkItem
        {
            private AsyncTaskMethodBuilder<IPHostEntry> IPHostEntryBuilder;
            private AsyncTaskMethodBuilder<IPAddress[]> IPAddressArrayBuilder;
            private object? _result;

            public GetAddrInfoExState(string hostName, bool justAddresses)
            {
                HostName = hostName;
                JustAddresses = justAddresses;
                if (justAddresses)
                {
                    IPAddressArrayBuilder = AsyncTaskMethodBuilder<IPAddress[]>.Create();
                    _ = IPAddressArrayBuilder.Task; // force initialization
                }
                else
                {
                    IPHostEntryBuilder = AsyncTaskMethodBuilder<IPHostEntry>.Create();
                    _ = IPHostEntryBuilder.Task; // force initialization
                }
            }

            public string HostName { get; }

            public bool JustAddresses { get; }

            public Task Task => JustAddresses ? (Task)IPAddressArrayBuilder.Task : IPHostEntryBuilder.Task;

            public void SetResult(object result)
            {
                // Store the result and then queue this object to the thread pool to actually complete the Tasks, as we
                // want to avoid invoking continuations on the Windows callback thread. Effectively we're manually
                // implementing TaskCreationOptions.RunContinuationsAsynchronously, which we can't use because we're
                // using AsyncTaskMethodBuilder, which we're using in order to create either a strongly-typed Task<IPHostEntry>
                // or Task<IPAddress[]> without allocating additional objects.
                Debug.Assert(result is Exception || result is IPAddress[] || result is IPHostEntry);
                _result = result;
                ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
            }

            void IThreadPoolWorkItem.Execute()
            {
                if (JustAddresses)
                {
                    if (_result is Exception e)
                    {
                        IPAddressArrayBuilder.SetException(e);
                    }
                    else
                    {
                        IPAddressArrayBuilder.SetResult((IPAddress[])_result!);
                    }
                }
                else
                {
                    if (_result is Exception e)
                    {
                        IPHostEntryBuilder.SetException(e);
                    }
                    else
                    {
                        IPHostEntryBuilder.SetResult((IPHostEntry)_result!);
                    }
                }
            }

            public IntPtr CreateHandle() => GCHandle.ToIntPtr(GCHandle.Alloc(this, GCHandleType.Normal));

            public static GetAddrInfoExState FromHandleAndFree(IntPtr handle)
            {
                GCHandle gcHandle = GCHandle.FromIntPtr(handle);
                var state = (GetAddrInfoExState)gcHandle.Target!;
                gcHandle.Free();
                return state;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct GetAddrInfoExContext
        {
            public NativeOverlapped Overlapped;
            public Interop.Winsock.AddressInfoEx* Result;
            public IntPtr CancelHandle;
            public IntPtr QueryStateHandle;

            public static GetAddrInfoExContext* AllocateContext()
            {
                var context = (GetAddrInfoExContext*)Marshal.AllocHGlobal(sizeof(GetAddrInfoExContext));
                *context = default;
                return context;
            }

            public static void FreeContext(GetAddrInfoExContext* context)
            {
                if (context->Result != null)
                {
                    Interop.Winsock.FreeAddrInfoExW(context->Result);
                }

                Marshal.FreeHGlobal((IntPtr)context);
            }
        }
    }
}
