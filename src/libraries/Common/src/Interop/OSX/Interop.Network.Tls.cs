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
            internal static unsafe partial bool Init(
                delegate* unmanaged<IntPtr, StatusUpdates, IntPtr, IntPtr, void> statusCallback,
                delegate* unmanaged<IntPtr, byte*, void**, int> writeCallback);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwCreateContext")]
            internal static partial SafeNwHandle CreateContext([MarshalAs(UnmanagedType.I4)] bool isServer);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwSetTlsOptions", StringMarshalling = StringMarshalling.Utf8)]
            private static partial void SetTlsOptions(SafeNwHandle connection, IntPtr state,
            string targetName, Span<byte> alpnBuffer, int alpnLength, SslProtocols minTlsProtocol, SslProtocols maxTlsProtocol);

            internal static void SetTlsOptions(SafeNwHandle nwHandle, IntPtr state, string targetName, List<SslApplicationProtocol>? applicationProtocols, SslProtocols minTlsVersion, SslProtocols maxTlsVersion)
            {
                int alpnLength = GetAlpnProtocolListSerializedLength(applicationProtocols);
                Span<byte> alpn = stackalloc byte[alpnLength];
                SerializeAlpnProtocolList(applicationProtocols, alpn);

                SetTlsOptions(nwHandle, state, targetName, alpn, alpnLength, minTlsVersion, maxTlsVersion);
            }

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwStartTlsHandshake")]
            internal static partial int StartTlsHandshake(SafeNwHandle connection, IntPtr state);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwProcessInputData")]
            internal static unsafe partial int ProcessInputData(SafeNwHandle connection, SafeNwHandle framer, byte* buffer, int bufferLength);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwSendToConnection")]
            internal static unsafe partial void SendToConnection(SafeNwHandle connection, IntPtr state, void* buffer, int bufferLength);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwReadFromConnection")]
            internal static partial void ReadFromConnection(SafeNwHandle connection, IntPtr state);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwCancelConnection")]
            internal static partial void CancelConnection(SafeNwHandle connection);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwGetConnectionInfo")]
            internal static unsafe partial int GetConnectionInfo(SafeNwHandle connection, out SslProtocols pProtocol, out TlsCipherSuite pCipherSuiteOut, ref byte* negotiatedAlpn, out int negotiatedAlpnLength);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwCopyCertChain")]
            internal static partial void CopyCertChain(SafeNwHandle connection, out SafeCFArrayHandle certificates, out int count);

            internal static int GetAlpnProtocolListSerializedLength(List<SslApplicationProtocol>? applicationProtocols)
            {
                if (applicationProtocols is null)
                {
                    return 0;
                }

                int protocolSize = 0;

                foreach (SslApplicationProtocol protocol in applicationProtocols)
                {
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
            DebugLog = 200,
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
    internal sealed class SafeNwHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeNwHandle() : base(ownsHandle: true) { }

        public SafeNwHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            SetHandle(IntPtr.Zero);
            return true;
        }
    }
}
