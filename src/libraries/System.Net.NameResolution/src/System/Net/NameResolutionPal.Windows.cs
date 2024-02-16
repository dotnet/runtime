// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Net
{
    internal static partial class NameResolutionPal
    {
        private static volatile int s_getAddrInfoExSupported;

        public static bool SupportsGetAddrInfoAsync
        {
            get
            {
                int supported = s_getAddrInfoExSupported;
                if (supported == 0)
                {
                    Initialize();
                    supported = s_getAddrInfoExSupported;
                }
                return supported == 1;

                static void Initialize()
                {
                    Interop.Winsock.EnsureInitialized();

                    IntPtr libHandle = Interop.Kernel32.LoadLibraryEx(Interop.Libraries.Ws2_32, IntPtr.Zero, Interop.Kernel32.LOAD_LIBRARY_SEARCH_SYSTEM32);
                    Debug.Assert(libHandle != IntPtr.Zero);

                    // We can't just check that 'GetAddrInfoEx' exists, because it existed before supporting overlapped.
                    // The existence of 'GetAddrInfoExCancel' indicates that overlapped is supported.
                    bool supported = NativeLibrary.TryGetExport(libHandle, Interop.Winsock.GetAddrInfoExCancelFunctionName, out _);
                    Interlocked.CompareExchange(ref s_getAddrInfoExSupported, supported ? 1 : -1, 0);
                }
            }
        }

        public static unsafe SocketError TryGetAddrInfo(string name, bool justAddresses, AddressFamily addressFamily, out string? hostName, out string[] aliases, out IPAddress[] addresses, out int nativeErrorCode)
        {
            Interop.Winsock.EnsureInitialized();

            aliases = Array.Empty<string>();

            var hints = new Interop.Winsock.AddressInfo { ai_family = addressFamily };
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
            Interop.Winsock.EnsureInitialized();

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
            Interop.Winsock.EnsureInitialized();

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

        public static unsafe Task? GetAddrInfoAsync(string hostName, bool justAddresses, AddressFamily family, CancellationToken cancellationToken)
        {
            Interop.Winsock.EnsureInitialized();

            GetAddrInfoExState? state = null;
            try
            {
                state = new GetAddrInfoExState(hostName, justAddresses);
            }
            catch
            {
                state?.Dispose();
                throw;
            }

            var hints = new Interop.Winsock.AddressInfoEx { ai_family = family };
            if (!justAddresses)
            {
                hints.ai_flags = AddressInfoHints.AI_CANONNAME;
            }

            GetAddrInfoExContext* context = state.Context;

            SocketError errorCode = (SocketError)Interop.Winsock.GetAddrInfoExW(
                hostName, null, Interop.Winsock.NS_ALL, IntPtr.Zero, &hints, &context->Result, IntPtr.Zero, &context->Overlapped, &GetAddressInfoExCallback, &context->CancelHandle);

            if (errorCode == SocketError.IOPending)
            {
                state.RegisterForCancellation(cancellationToken);
            }
            else if (errorCode == SocketError.TryAgain || (int)errorCode == Interop.Winsock.WSA_E_CANCELLED)
            {
                // WSATRY_AGAIN indicates possible problem with reachability according to docs.
                // However, if servers are really unreachable, we would still get IOPending here
                // and final result would be posted via overlapped IO.
                // synchronous failure here may signal issue when GetAddrInfoExW does not work from
                // impersonated context. Windows 8 and Server 2012 fail for same reason with different errorCode.
                state.Dispose();
                return null;
            }
            else
            {
                ProcessResult(errorCode, context);
            }

            return state.Task;
        }

        [UnmanagedCallersOnly]
        private static unsafe void GetAddressInfoExCallback(int error, int bytes, NativeOverlapped* overlapped)
        {
            // Can be casted directly to GetAddrInfoExContext* because the overlapped is its first field
            GetAddrInfoExContext* context = (GetAddrInfoExContext*)overlapped;

            ProcessResult((SocketError)error, context);
        }

        private static unsafe void ProcessResult(SocketError errorCode, GetAddrInfoExContext* context)
        {
            GetAddrInfoExState state = GetAddrInfoExState.FromHandleAndFree(context->QueryStateHandle);

            try
            {
                CancellationToken cancellationToken = state.UnregisterAndGetCancellationToken();

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
                    Exception ex = (errorCode == (SocketError)Interop.Winsock.WSA_E_CANCELLED && cancellationToken.IsCancellationRequested)
                        ? (Exception)new OperationCanceledException(cancellationToken)
                        : new SocketException((int)errorCode);
                    state.SetResult(ExceptionDispatchInfo.SetCurrentStackTrace(ex));
                }
            }
            finally
            {
                state.Dispose();
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

        // GetAddrInfoExState is a SafeHandle that manages the lifetime of GetAddrInfoExContext*
        // to make sure GetAddrInfoExCancel always takes a valid memory address regardless of the race
        // between cancellation and completion callbacks.
        private sealed unsafe class GetAddrInfoExState : SafeHandleZeroOrMinusOneIsInvalid, IThreadPoolWorkItem
        {
            private CancellationTokenRegistration _cancellationRegistration;

            private AsyncTaskMethodBuilder<IPHostEntry> IPHostEntryBuilder;
            private AsyncTaskMethodBuilder<IPAddress[]> IPAddressArrayBuilder;
            private object? _result;
            private volatile bool _completed;

            public GetAddrInfoExState(string hostName, bool justAddresses)
                : base(true)
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

                GetAddrInfoExContext* context = GetAddrInfoExContext.AllocateContext();
                context->QueryStateHandle = CreateHandle();
                SetHandle((IntPtr)context);
            }

            public string HostName { get; }

            public bool JustAddresses { get; }

            public Task Task => JustAddresses ? (Task)IPAddressArrayBuilder.Task : IPHostEntryBuilder.Task;

            internal GetAddrInfoExContext* Context => (GetAddrInfoExContext*)handle;

            public void RegisterForCancellation(CancellationToken cancellationToken)
            {
                if (!cancellationToken.CanBeCanceled) return;

                if (_completed)
                {
                    // The operation completed before registration could be done.
                    return;
                }

                _cancellationRegistration = cancellationToken.UnsafeRegister(static o =>
                {
                    var @this = (GetAddrInfoExState)o!;
                    if (@this._completed)
                    {
                        // Escape early and avoid ObjectDisposedException in DangerousAddRef
                        return;
                    }

                    bool needRelease = false;
                    try
                    {
                        @this.DangerousAddRef(ref needRelease);

                        // If DangerousAddRef didn't throw ODE, the handle should contain a valid pointer.
                        GetAddrInfoExContext* context = @this.Context;

                        // An outstanding operation will be completed with WSA_E_CANCELLED, and GetAddrInfoExCancel will return NO_ERROR.
                        // If this thread has lost the race between cancellation and completion, this will be a NOP
                        // with GetAddrInfoExCancel returning WSA_INVALID_HANDLE.
                        int cancelResult = Interop.Winsock.GetAddrInfoExCancel(&context->CancelHandle);
                        if (cancelResult != Interop.Winsock.WSA_INVALID_HANDLE && NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Info(@this, $"GetAddrInfoExCancel returned error {cancelResult}");
                        }
                    }
                    finally
                    {
                        if (needRelease)
                        {
                            @this.DangerousRelease();
                        }
                    }

                }, this);
            }

            public CancellationToken UnregisterAndGetCancellationToken()
            {
                _completed = true;

                // We should not wait for pending cancellation callbacks with CTR.Dispose(),
                // since we are in a completion routine and GetAddrInfoExCancel may get blocked until it's finished.
                _cancellationRegistration.Unregister();
                return _cancellationRegistration.Token;
            }

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

            public static GetAddrInfoExState FromHandleAndFree(IntPtr handle)
            {
                GCHandle gcHandle = GCHandle.FromIntPtr(handle);
                var state = (GetAddrInfoExState)gcHandle.Target!;
                gcHandle.Free();
                return state;
            }

            protected override bool ReleaseHandle()
            {
                GetAddrInfoExContext.FreeContext(Context);

                return true;
            }

            private IntPtr CreateHandle() => GCHandle.ToIntPtr(GCHandle.Alloc(this, GCHandleType.Normal));
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct GetAddrInfoExContext
        {
            public NativeOverlapped Overlapped;
            public Interop.Winsock.AddressInfoEx* Result;
            public IntPtr CancelHandle;
            public IntPtr QueryStateHandle;

            public static GetAddrInfoExContext* AllocateContext() => (GetAddrInfoExContext*)NativeMemory.AllocZeroed((nuint)sizeof(GetAddrInfoExContext));

            public static void FreeContext(GetAddrInfoExContext* context)
            {
                if (context->Result != null)
                {
                    Interop.Winsock.FreeAddrInfoExW(context->Result);
                }
                NativeMemory.Free(context);
            }
        }
    }
}
