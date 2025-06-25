// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    // TLS 1.3 specific Network Framework implementation for macOS
    internal static partial class NetworkFramework
    {
        internal static partial class Tls
        {
            // Core TLS functions for Network Framework integration
            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwInit")]
            [return: MarshalAs(UnmanagedType.I4)]
            internal static unsafe partial bool Init(delegate* unmanaged<IntPtr, StatusUpdates, IntPtr, IntPtr, void> statusCallback,
                                                   delegate* unmanaged<IntPtr, byte*, void**, int> readCallback,
                                                   delegate* unmanaged<IntPtr, byte*, void**, int> writeCallback);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwCreateContext")]
            internal static partial SafeNetworkFrameworkHandle CreateContext([MarshalAs(UnmanagedType.I4)] bool isServer);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwSetTlsOptions", StringMarshalling = StringMarshalling.Utf8)]
            private static partial int SetTlsOptions(SafeNetworkFrameworkHandle connection, IntPtr gcHandle,
                                                            string targetName, Span<byte> alpnBuffer, int alpnLength,
                                                            SslProtocols minTlsProtocol, SslProtocols maxTlsProtocol);

            internal static int SetTlsOptions(SafeNetworkFrameworkHandle nwHandle, IntPtr gcHandle, string targetName, List<SslApplicationProtocol>? applicationProtocols, SslProtocols minTlsVersion, SslProtocols maxTlsVersion)
            {
                int alpnLength = GetAlpnProtocolListSerializedLength(applicationProtocols);
                Span<byte> alpn = alpnLength <= 256 ? stackalloc byte[256].Slice(0, alpnLength) : new byte[alpnLength];
                SerializeAlpnProtocolList(applicationProtocols, alpn);

                return SetTlsOptions(nwHandle, gcHandle, targetName, alpn, alpnLength, minTlsVersion, maxTlsVersion);
            }

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwStartTlsHandshake")]
            internal static partial int StartTlsHandshake(SafeNetworkFrameworkHandle connection, IntPtr gcHandle);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwProcessInputData")]
            internal static unsafe partial int ProcessInputData(SafeNetworkFrameworkHandle connection,
                                                               SafeNetworkFrameworkHandle framer,
                                                               byte* buffer, int bufferLength);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwSendToConnection")]
            internal static unsafe partial int SendToConnection(SafeNetworkFrameworkHandle connection, IntPtr gcHandle,
                                                               void* buffer, int bufferLength);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwReadFromConnection")]
            internal static partial int ReadFromConnection(SafeNetworkFrameworkHandle connection, IntPtr gcHandle);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwCancelConnection")]
            internal static partial int CancelConnection(SafeNetworkFrameworkHandle connection);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwGetConnectionInfo")]
            internal static unsafe partial int GetConnectionInfo(SafeNetworkFrameworkHandle connection,
                                                               out SslProtocols pProtocol, out TlsCipherSuite pCipherSuiteOut,
                                                               ref void* negotiatedAlpn, out uint alpnLength);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwCopyCertChain")]
            internal static partial int CopyCertChain(SafeNetworkFrameworkHandle connection,
                                                             out SafeCFArrayHandle certificates,
                                                             out int count);

            internal static int GetAlpnProtocolListSerializedLength(List<SslApplicationProtocol>? applicationProtocols)
            {
                if (applicationProtocols is null)
                {
                    return 0;
                }

                int protocolSize = 0;

                foreach (SslApplicationProtocol protocol in applicationProtocols)
                {
                    if (protocol.Protocol.Length == 0 || protocol.Protocol.Length > byte.MaxValue)
                    {
                        throw new ArgumentException(SR.net_ssl_app_protocols_invalid, nameof(applicationProtocols));
                    }

                    protocolSize += protocol.Protocol.Length + 2;
                }

                return protocolSize;
            }

            private static void SerializeAlpnProtocolList(List<SslApplicationProtocol>? applicationProtocols, Span<byte> buffer)
            {
                if (applicationProtocols is null)
                {
                    return;
                }

                Debug.Assert(GetAlpnProtocolListSerializedLength(applicationProtocols) == buffer.Length);

                int offset = 0;
                foreach (SslApplicationProtocol protocol in applicationProtocols)
                {
                    buffer[offset++] = (byte)protocol.Protocol.Length;
                    protocol.Protocol.Span.CopyTo(buffer.Slice(offset));
                    offset += protocol.Protocol.Length;
                    buffer[offset++] = 0;
                }
            }
        }
        // Status enumeration for Network Framework TLS operations
        internal enum StatusUpdates
        {
            UnknownError = 0,
            FramerStart = 1,
            FramerStop = 2,
            HandshakeFinished = 3,
            HandshakeFailed = 4,
            ConnectionReadFinished = 100,
            ConnectionWriteFinished = 101,
            ConnectionWriteFailed = 102,
            ConnectionCancelled = 103,
        }

        internal enum OSStatus
        {
            NoError = 0,
            ReadError = -19,
            WriteError = -20,
            EOFError = -39,
            SecUserCanceled = -128,
            WouldBlock = -9803
        }
    }

    // Safe handle classes for Network Framework TLS resources
    internal sealed class SafeNetworkFrameworkHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeNetworkFrameworkHandle() : base(ownsHandle: true) { }

        public SafeNetworkFrameworkHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(NetworkFramework.Retain(handle));
        }

        protected override bool ReleaseHandle()
        {
            NetworkFramework.Release(handle);
            return true;
        }
    }
}
