// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using ProtocolFamily = System.Net.Internals.ProtocolFamily;

namespace System.Net
{
    internal static partial class NameResolutionPal
    {
        //
        // used by GetHostName() to preallocate a buffer for the call to gethostname.
        //
        private const int HostNameBufferLength = 256;

        private static bool s_initialized;
        private static readonly object s_initializedLock = new object();

        private static readonly unsafe Interop.Winsock.LPLOOKUPSERVICE_COMPLETION_ROUTINE s_getAddrInfoExCallback = GetAddressInfoExCallback;
        private static bool s_getAddrInfoExSupported;

        public static bool SupportsGetAddrInfoAsync
        {
            get
            {
                EnsureSocketsAreInitialized();
                return s_getAddrInfoExSupported;
            }
        }

        /*++

        Routine Description:

            Takes a native pointer (expressed as an int) to a hostent structure,
            and converts the information in their to an IPHostEntry class. This
            involves walking through an array of native pointers, and a temporary
            ArrayList object is used in doing this.

        Arguments:

            nativePointer   - Native pointer to hostent structure.



        Return Value:

            An IPHostEntry structure.

        --*/
        private static IPHostEntry NativeToHostEntry(IntPtr nativePointer)
        {
            //
            // marshal pointer to struct
            //

            hostent Host = Marshal.PtrToStructure<hostent>(nativePointer);
            IPHostEntry HostEntry = new IPHostEntry();

            if (Host.h_name != IntPtr.Zero)
            {
                HostEntry.HostName = Marshal.PtrToStringAnsi(Host.h_name);
                if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"HostEntry.HostName: {HostEntry.HostName}");
            }

            // decode h_addr_list to ArrayList of IP addresses.
            // The h_addr_list field is really a pointer to an array of pointers
            // to IP addresses. Loop through the array, and while the pointer
            // isn't NULL read the IP address, convert it to an IPAddress class,
            // and add it to the list.

            var TempIPAddressList = new List<IPAddress>();
            int IPAddressToAdd;
            string AliasToAdd;
            IntPtr currentArrayElement;

            //
            // get the first pointer in the array
            //
            currentArrayElement = Host.h_addr_list;
            nativePointer = Marshal.ReadIntPtr(currentArrayElement);

            while (nativePointer != IntPtr.Zero)
            {
                //
                // if it's not null it points to an IPAddress,
                // read it...
                //
                IPAddressToAdd = Marshal.ReadInt32(nativePointer);
#if BIGENDIAN
                // IP addresses from native code are always a byte array
                // converted to int.  We need to convert the address into
                // a uniform integer value.
                IPAddressToAdd = (int)(((uint)IPAddressToAdd << 24) |
                                        (((uint)IPAddressToAdd & 0x0000FF00) << 8) |
                                        (((uint)IPAddressToAdd >> 8) & 0x0000FF00) |
                                        ((uint)IPAddressToAdd >> 24));
#endif

                if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"currentArrayElement:{currentArrayElement} nativePointer:{nativePointer} IPAddressToAdd:{IPAddressToAdd}");

                //
                // ...and add it to the list
                //
                TempIPAddressList.Add(new IPAddress((long)IPAddressToAdd & 0x0FFFFFFFF));

                //
                // now get the next pointer in the array and start over
                //
                currentArrayElement = currentArrayElement + IntPtr.Size;
                nativePointer = Marshal.ReadIntPtr(currentArrayElement);
            }

            HostEntry.AddressList = TempIPAddressList.ToArray();

            //
            // Now do the same thing for the aliases.
            //

            var TempAliasList = new List<string>();

            currentArrayElement = Host.h_aliases;
            nativePointer = Marshal.ReadIntPtr(currentArrayElement);

            while (nativePointer != IntPtr.Zero)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"currentArrayElement:{currentArrayElement} nativePointer:{nativePointer}");

                //
                // if it's not null it points to an Alias,
                // read it...
                //
                AliasToAdd = Marshal.PtrToStringAnsi(nativePointer);

                //
                // ...and add it to the list
                //
                TempAliasList.Add(AliasToAdd);

                //
                // now get the next pointer in the array and start over
                //
                currentArrayElement = currentArrayElement + IntPtr.Size;
                nativePointer = Marshal.ReadIntPtr(currentArrayElement);
            }

            HostEntry.Aliases = TempAliasList.ToArray();

            return HostEntry;
        } // NativeToHostEntry

        public static unsafe SocketError TryGetAddrInfo(string name, out IPHostEntry hostinfo, out int nativeErrorCode)
        {
            //
            // Use SocketException here to show operation not supported
            // if, by some nefarious means, this method is called on an
            // unsupported platform.
            //
            SafeFreeAddrInfo root = null;
            var addresses = new List<IPAddress>();
            string canonicalname = null;

            AddressInfo hints = new AddressInfo();
            hints.ai_flags = AddressInfoHints.AI_CANONNAME;
            hints.ai_family = AddressFamily.Unspecified;   // gets all address families

            nativeErrorCode = 0;

            //
            // Use try / finally so we always get a shot at freeaddrinfo
            //
            try
            {
                SocketError errorCode = (SocketError)SafeFreeAddrInfo.GetAddrInfo(name, null, ref hints, out root);
                if (errorCode != SocketError.Success)
                { // Should not throw, return mostly blank hostentry
                    hostinfo = NameResolutionUtilities.GetUnresolvedAnswer(name);
                    return errorCode;
                }

                AddressInfo* pAddressInfo = (AddressInfo*)root.DangerousGetHandle();
                //
                // Process the results
                //
                while (pAddressInfo != null)
                {
                    //
                    // Retrieve the canonical name for the host - only appears in the first AddressInfo
                    // entry in the returned array.
                    //
                    if (canonicalname == null && pAddressInfo->ai_canonname != null)
                    {
                        canonicalname = Marshal.PtrToStringUni((IntPtr)pAddressInfo->ai_canonname);
                    }
                    //
                    // Only process IPv4 or IPv6 Addresses. Note that it's unlikely that we'll
                    // ever get any other address families, but better to be safe than sorry.
                    // We also filter based on whether IPv6 is supported on the current
                    // platform / machine.
                    //
                    var socketAddress = new ReadOnlySpan<byte>(pAddressInfo->ai_addr, pAddressInfo->ai_addrlen);

                    if (pAddressInfo->ai_family == AddressFamily.InterNetwork)
                    {
                        if (socketAddress.Length == SocketAddressPal.IPv4AddressSize)
                            addresses.Add(CreateIPv4Address(socketAddress));
                    }
                    else if (pAddressInfo->ai_family == AddressFamily.InterNetworkV6 && SocketProtocolSupportPal.OSSupportsIPv6)
                    {
                        if (socketAddress.Length == SocketAddressPal.IPv6AddressSize)
                            addresses.Add(CreateIPv6Address(socketAddress));
                    }
                    //
                    // Next addressinfo entry
                    //
                    pAddressInfo = pAddressInfo->ai_next;
                }
            }
            finally
            {
                if (root != null)
                {
                    root.Dispose();
                }
            }

            //
            // Finally, put together the IPHostEntry
            //
            hostinfo = new IPHostEntry();

            hostinfo.HostName = canonicalname != null ? canonicalname : name;
            hostinfo.Aliases = Array.Empty<string>();
            hostinfo.AddressList = addresses.ToArray();

            return SocketError.Success;
        }

        public static unsafe string TryGetNameInfo(IPAddress addr, out SocketError errorCode, out int nativeErrorCode)
        {
            SocketAddress address = new IPEndPoint(addr, 0).Serialize();
            Span<byte> addressBuffer = address.Size <= 64 ? stackalloc byte[64] : new byte[address.Size];
            for (int i = 0; i < address.Size; i++)
            {
                addressBuffer[i] = address[i];
            }

            const int NI_MAXHOST = 1025;
            char* hostname = stackalloc char[NI_MAXHOST];

            nativeErrorCode = 0;
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

            return errorCode == SocketError.Success ? new string(hostname) : null;
        }

        public static unsafe string GetHostName()
        {
            //
            // note that we could cache the result ourselves since you
            // wouldn't expect the hostname of the machine to change during
            // execution, but this might still happen and we would want to
            // react to that change.
            //

            byte* buffer = stackalloc byte[HostNameBufferLength];
            if (Interop.Winsock.gethostname(buffer, HostNameBufferLength) != SocketError.Success)
            {
                throw new SocketException();
            }
            return new string((sbyte*)buffer);
        }

        public static void EnsureSocketsAreInitialized()
        {
            if (!Volatile.Read(ref s_initialized))
            {
                lock (s_initializedLock)
                {
                    if (!s_initialized)
                    {
                        SocketError errorCode = Interop.Winsock.WSAStartup();

                        if (errorCode != SocketError.Success)
                        {
                            //
                            // failed to initialize, throw
                            //
                            // WSAStartup does not set LastWin32Error
                            throw new SocketException((int)errorCode);
                        }

                        s_getAddrInfoExSupported = GetAddrInfoExSupportsOverlapped();

                        Volatile.Write(ref s_initialized, true);
                    }
                }
            }
        }

        public static unsafe void GetAddrInfoAsync(DnsResolveAsyncResult asyncResult)
        {
            GetAddrInfoExContext* context = GetAddrInfoExContext.AllocateContext();

            try
            {
                var state = new GetAddrInfoExState(asyncResult);
                context->QueryStateHandle = state.CreateHandle();
            }
            catch
            {
                GetAddrInfoExContext.FreeContext(context);
                throw;
            }

            AddressInfoEx hints = new AddressInfoEx();
            hints.ai_flags = AddressInfoHints.AI_CANONNAME;
            hints.ai_family = AddressFamily.Unspecified; // Gets all address families

            SocketError errorCode =
                (SocketError)Interop.Winsock.GetAddrInfoExW(asyncResult.HostName, null, 0 /* NS_ALL*/, IntPtr.Zero, ref hints, out context->Result, IntPtr.Zero, ref context->Overlapped, s_getAddrInfoExCallback, out context->CancelHandle);

            if (errorCode != SocketError.IOPending)
                ProcessResult(errorCode, context);
        }

        private static unsafe void GetAddressInfoExCallback([In] int error, [In] int bytes, [In] NativeOverlapped* overlapped)
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

                if (errorCode != SocketError.Success)
                {
                    state.CompleteAsyncResult(new SocketException((int)errorCode));
                    return;
                }

                AddressInfoEx* result = context->Result;
                string canonicalName = null;

                List<IPAddress> addresses = new List<IPAddress>();

                while (result != null)
                {
                    if (canonicalName == null && result->ai_canonname != IntPtr.Zero)
                        canonicalName = Marshal.PtrToStringUni(result->ai_canonname);

                    var socketAddress = new ReadOnlySpan<byte>(result->ai_addr, result->ai_addrlen);

                    if (result->ai_family == AddressFamily.InterNetwork)
                    {
                        if (socketAddress.Length == SocketAddressPal.IPv4AddressSize)
                            addresses.Add(CreateIPv4Address(socketAddress));
                    }
                    else if (SocketProtocolSupportPal.OSSupportsIPv6 && result->ai_family == AddressFamily.InterNetworkV6)
                    {
                        if (socketAddress.Length == SocketAddressPal.IPv6AddressSize)
                            addresses.Add(CreateIPv6Address(socketAddress));
                    }

                    result = result->ai_next;
                }

                if (canonicalName == null)
                    canonicalName = state.HostName;

                state.CompleteAsyncResult(new IPHostEntry
                {
                    HostName = canonicalName,
                    Aliases = Array.Empty<string>(),
                    AddressList = addresses.ToArray()
                });
            }
            finally
            {
                GetAddrInfoExContext.FreeContext(context);
            }
        }

        private static unsafe IPAddress CreateIPv4Address(ReadOnlySpan<byte> socketAddress)
        {
            long address = (long)SocketAddressPal.GetIPv4Address(socketAddress) & 0x0FFFFFFFF;
            return new IPAddress(address);
        }

        private static unsafe IPAddress CreateIPv6Address(ReadOnlySpan<byte> socketAddress)
        {
            Span<byte> address = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
            uint scope;
            SocketAddressPal.GetIPv6Address(socketAddress, address, out scope);

            return new IPAddress(address, (long)scope);
        }

        #region GetAddrInfoAsync Helper Classes

        //
        // Warning: If this ever ported to NETFX, AppDomain unloads needs to be handled
        // to protect against AppDomainUnloadException if there are pending operations.
        //

        private sealed class GetAddrInfoExState
        {
            private DnsResolveAsyncResult _asyncResult;
            private object _result;

            public string HostName => _asyncResult.HostName;

            public GetAddrInfoExState(DnsResolveAsyncResult asyncResult)
            {
                _asyncResult = asyncResult;
            }

            public void CompleteAsyncResult(object o)
            {
                // We don't want to expose the GetAddrInfoEx callback thread to user code.
                // The callback occurs in a native windows thread pool.

                _result = o;

                Task.Factory.StartNew(s =>
                {
                    var self = (GetAddrInfoExState)s;
                    self._asyncResult.InvokeCallback(self._result);
                }, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }

            public IntPtr CreateHandle()
            {
                GCHandle handle = GCHandle.Alloc(this, GCHandleType.Normal);
                return GCHandle.ToIntPtr(handle);
            }

            public static GetAddrInfoExState FromHandleAndFree(IntPtr handle)
            {
                GCHandle gcHandle = GCHandle.FromIntPtr(handle);
                var state = (GetAddrInfoExState)gcHandle.Target;
                gcHandle.Free();

                return state;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct GetAddrInfoExContext
        {
            public NativeOverlapped Overlapped;
            public AddressInfoEx* Result;
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
                    Interop.Winsock.FreeAddrInfoExW(context->Result);

                Marshal.FreeHGlobal((IntPtr)context);
            }
        }

        #endregion
    }
}
