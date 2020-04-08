// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal static partial class NameResolutionPal
    {
        private static readonly unsafe Interop.Sys.GetHostEntryForNameCallback s_getHostEntryForNameCallback = GetHostEntryForNameCallback;

        public static bool SupportsGetAddrInfoAsync { get; } = Interop.Sys.PlatformSupportsGetAddrInfoAsync();

        public static void EnsureSocketsAreInitialized() { } // No-op for Unix

        public static unsafe SocketError TryGetAddrInfo(string name, bool justAddresses, out string? hostName, out string[] aliases, out IPAddress[] addresses, out int nativeErrorCode)
        {
            if (name == "")
            {
                // To match documented behavior on Windows, if an empty string is passed in, use the local host's name.
                name = Dns.GetHostName();
            }

            Interop.Sys.HostEntry entry;
            int result = Interop.Sys.GetHostEntryForName(name, &entry);
            if (result != 0)
            {
                nativeErrorCode = result;
                hostName = name;
                aliases = Array.Empty<string>();
                addresses = Array.Empty<IPAddress>();
                return GetSocketErrorForNativeError(result);
            }

            ParseHostEntry(entry, justAddresses, out hostName, out aliases, out addresses);
            nativeErrorCode = 0;
            return SocketError.Success;
        }

        public static unsafe string? TryGetNameInfo(IPAddress addr, out SocketError socketError, out int nativeErrorCode)
        {
            byte* buffer = stackalloc byte[Interop.Sys.NI_MAXHOST + 1 /*for null*/];

            byte isIPv6;
            int rawAddressLength;
            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                isIPv6 = 0;
                rawAddressLength = IPAddressParserStatics.IPv4AddressBytes;
            }
            else
            {
                isIPv6 = 1;
                rawAddressLength = IPAddressParserStatics.IPv6AddressBytes;
            }

            byte* rawAddress = stackalloc byte[rawAddressLength];
            addr.TryWriteBytes(new Span<byte>(rawAddress, rawAddressLength), out int bytesWritten);
            Debug.Assert(bytesWritten == rawAddressLength);

            int error = Interop.Sys.GetNameInfo(
                rawAddress,
                (uint)rawAddressLength,
                isIPv6,
                buffer,
                Interop.Sys.NI_MAXHOST,
                null,
                0,
                Interop.Sys.GetNameInfoFlags.NI_NAMEREQD);

            socketError = GetSocketErrorForNativeError(error);
            nativeErrorCode = error;
            return socketError == SocketError.Success ? Marshal.PtrToStringAnsi((IntPtr)buffer) : null;
        }

        public static unsafe string GetHostName() => Interop.Sys.GetHostName();

        public static unsafe Task GetAddrInfoAsync(string hostName, bool justAddresses)
        {
            GetHostEntryForNameContext* context = GetHostEntryForNameContext.AllocateContext();

            GetHostEntryForNameState state;
            try
            {
                state = new GetHostEntryForNameState(hostName, justAddresses);
                context->State = state.CreateHandle();
            }
            catch
            {
                GetHostEntryForNameContext.FreeContext(context);
                throw;
            }

            int errorCode = Interop.Sys.GetHostEntryForNameAsync(hostName, &context->Result, s_getHostEntryForNameCallback);

            if (errorCode != 0)
            {
                ProcessResult(GetSocketErrorForNativeError(errorCode), context);
            }

            return state.Task;
        }

        private static unsafe void GetHostEntryForNameCallback(Interop.Sys.HostEntry* entry, int error)
        {
            // Can be casted directly to GetHostEntryForNameContext* because the HostEntry is its first field
            GetHostEntryForNameContext* context = (GetHostEntryForNameContext*)entry;

            ProcessResult(GetSocketErrorForNativeError(error), context);
        }

        private static unsafe void ProcessResult(SocketError errorCode, GetHostEntryForNameContext* context)
        {
            try
            {
                GetHostEntryForNameState state = GetHostEntryForNameState.FromHandleAndFree(context->State);

                if (errorCode == SocketError.Success)
                {
                    ParseHostEntry(context->Result, state.JustAddresses, out string? hostName, out string[] aliases, out IPAddress[] addresses);

                    state.SetResult(state.JustAddresses
                        ? (object)addresses
                        : new IPHostEntry
                        {
                            HostName = hostName ?? state.HostName,
                            Aliases = aliases,
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
                GetHostEntryForNameContext.FreeContext(context);
            }
        }

        private static SocketError GetSocketErrorForNativeError(int error)
        {
            switch (error)
            {
                case 0:
                    return SocketError.Success;
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_AGAIN:
                    return SocketError.TryAgain;
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_BADFLAGS:
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_BADARG:
                    return SocketError.InvalidArgument;
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_FAIL:
                    return SocketError.NoRecovery;
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_FAMILY:
                    return SocketError.AddressFamilyNotSupported;
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_NONAME:
                    return SocketError.HostNotFound;
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_MEMORY:
                    throw new OutOfMemoryException();
                default:
                    Debug.Fail("Unexpected error: " + error.ToString());
                    return SocketError.SocketError;
            }
        }

        private static unsafe void ParseHostEntry(Interop.Sys.HostEntry hostEntry, bool justAddresses, out string? hostName, out string[] aliases, out IPAddress[] addresses)
        {
            try
            {
                hostName = !justAddresses && hostEntry.CanonicalName != null
                    ? new string((sbyte*)hostEntry.CanonicalName)
                    : null;

                IPAddress[] localAddresses;
                if (hostEntry.IPAddressCount == 0)
                {
                    localAddresses = Array.Empty<IPAddress>();
                }
                else
                {
                    // getaddrinfo returns multiple entries per address, for each socket type (datagram, stream, etc.).
                    // Our callers expect just one entry for each address. So we need to deduplicate the results.
                    // It's important to keep the addresses in order, since they are returned in the order in which
                    // connections should be attempted.
                    //
                    // We assume that the list returned by getaddrinfo is relatively short; after all, the intent is that
                    // the caller may need to attempt to contact every address in the list before giving up on a connection
                    // attempt. So an O(N^2) algorithm should be fine here. Keep in mind that any "better" algorithm
                    // is likely to involve extra allocations, hashing, etc., and so will probably be more expensive than
                    // this one in the typical (short list) case.

                    var nativeAddresses = new Interop.Sys.IPAddress[hostEntry.IPAddressCount];
                    int nativeAddressCount = 0;

                    Interop.Sys.IPAddress* addressHandle = hostEntry.IPAddressList;
                    for (int i = 0; i < hostEntry.IPAddressCount; i++)
                    {
                        if (Array.IndexOf(nativeAddresses, addressHandle[i], 0, nativeAddressCount) == -1)
                        {
                            nativeAddresses[nativeAddressCount++] = addressHandle[i];
                        }
                    }

                    localAddresses = new IPAddress[nativeAddressCount];
                    for (int i = 0; i < nativeAddressCount; i++)
                    {
                        localAddresses[i] = nativeAddresses[i].GetIPAddress();
                    }
                }

                string[] localAliases = Array.Empty<string>();
                if (!justAddresses && hostEntry.Aliases != null)
                {
                    int numAliases = 0;
                    while (hostEntry.Aliases[numAliases] != null)
                    {
                        numAliases++;
                    }

                    if (numAliases > 0)
                    {
                        localAliases = new string[numAliases];
                        for (int i = 0; i < localAliases.Length; i++)
                        {
                            localAliases[i] = new string((sbyte*)hostEntry.Aliases[i]);
                        }
                    }
                }

                aliases = localAliases;
                addresses = localAddresses;
            }
            finally
            {
                Interop.Sys.FreeHostEntry(&hostEntry);
            }
        }

        private sealed class GetHostEntryForNameState : IThreadPoolWorkItem
        {
            private AsyncTaskMethodBuilder<IPAddress[]> _ipAddressArrayBuilder;
            private AsyncTaskMethodBuilder<IPHostEntry> _ipHostEntryBuilder;
            private object? _result;

            public GetHostEntryForNameState(string hostName, bool justAddresses)
            {
                HostName = hostName;
                JustAddresses = justAddresses;

                if (justAddresses)
                {
                    _ipAddressArrayBuilder = AsyncTaskMethodBuilder<IPAddress[]>.Create();
                    _ = _ipAddressArrayBuilder.Task;     // force initialization
                }
                else
                {
                    _ipHostEntryBuilder = AsyncTaskMethodBuilder<IPHostEntry>.Create();
                    _ = _ipHostEntryBuilder.Task;       // force initialization
                }
            }

            public string HostName { get; }
            public bool JustAddresses { get; }

            public Task Task => JustAddresses ? (Task)_ipAddressArrayBuilder.Task : _ipHostEntryBuilder.Task;

            public void SetResult(object result)
            {
                // Store the result and then queue this object to the thread pool to actually complete the Tasks, as we
                // want to avoid invoking continuations on the OS callback thread. Effectively we're manually
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
                        _ipAddressArrayBuilder.SetException(e);
                    }
                    else
                    {
                        _ipAddressArrayBuilder.SetResult((IPAddress[])_result!);
                    }
                }
                else
                {
                    if (_result is Exception e)
                    {
                        _ipHostEntryBuilder.SetException(e);
                    }
                    else
                    {
                        _ipHostEntryBuilder.SetResult((IPHostEntry)_result!);
                    }
                }
            }

            public IntPtr CreateHandle() => GCHandle.ToIntPtr(GCHandle.Alloc(this, GCHandleType.Normal));

            public static GetHostEntryForNameState FromHandleAndFree(IntPtr handle)
            {
                GCHandle gCHandle = GCHandle.FromIntPtr(handle);
                var state = (GetHostEntryForNameState)gCHandle.Target!;
                gCHandle.Free();
                return state;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct GetHostEntryForNameContext
        {
            public Interop.Sys.HostEntry Result;
            public IntPtr State;

            public static GetHostEntryForNameContext* AllocateContext()
            {
                var context = (GetHostEntryForNameContext*)Marshal.AllocHGlobal(sizeof(GetHostEntryForNameContext));
                *context = default;
                return context;
            }

            public static void FreeContext(GetHostEntryForNameContext* context) => Marshal.FreeHGlobal((IntPtr)context);
        }
    }
}
