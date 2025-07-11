// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
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
            // Initialize internal shim for NetworkFramework integration
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwInit")]
            [return: MarshalAs(UnmanagedType.I4)]
            internal static unsafe partial bool Init(
                delegate* unmanaged<IntPtr, StatusUpdates, IntPtr, IntPtr, NetworkFrameworkError*, void> statusCallback,
                delegate* unmanaged<IntPtr, byte*, void**, int> writeCallback,
                delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr> challengeCallback);

            // Create a new connection context
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwCreateContext")]
            internal static partial SafeNwHandle CreateContext([MarshalAs(UnmanagedType.I4)] bool isServer);

            // Set TLS options for a connection
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwSetTlsOptions", StringMarshalling = StringMarshalling.Utf8)]
            private static unsafe partial void SetTlsOptions(SafeNwHandle connection, IntPtr state,
            string targetName, byte* alpnBuffer, int alpnLength, SslProtocols minTlsProtocol, SslProtocols maxTlsProtocol, uint* cipherSuites, int cipherSuitesLength);

            internal static void SetTlsOptions(SafeNwHandle nwHandle, IntPtr state, SslAuthenticationOptions options)
            {
                int alpnLength = GetAlpnProtocolListSerializedLength(options.ApplicationProtocols);

                SslProtocols minProtocol = SslProtocols.None;
                SslProtocols maxProtocol = SslProtocols.None;

                if (options.EnabledSslProtocols != SslProtocols.None)
                {
                    (minProtocol, maxProtocol) = GetMinMaxProtocols(options.EnabledSslProtocols);
                }

                byte[]? alpnBuffer = null;
                try
                {
                    const int StackAllocThreshold = 256;
                    Span<byte> alpn = alpnLength == 0
                        ? Span<byte>.Empty
                        : alpnLength <= StackAllocThreshold
                            ? stackalloc byte[StackAllocThreshold]
                            : (alpnBuffer = ArrayPool<byte>.Shared.Rent(alpnLength));

                    if (alpnLength > 0)
                    {
                        SerializeAlpnProtocolList(options.ApplicationProtocols!, alpn.Slice(0, alpnLength));
                    }

                    Span<uint> ciphers = options.CipherSuitesPolicy is null
                       ? Span<uint>.Empty
                       : options.CipherSuitesPolicy.Pal.TlsCipherSuites;

                    string idnHost = TargetHostNameHelper.NormalizeHostName(options.TargetHost);

                    unsafe
                    {
                        fixed (byte* alpnPtr = alpn)
                        fixed (uint* ciphersPtr = ciphers)
                        {
                            SetTlsOptions(nwHandle, state, idnHost, alpnPtr, alpnLength, minProtocol, maxProtocol, ciphersPtr, ciphers.Length);
                        }
                    }
                }
                finally
                {
                    if (alpnBuffer != null)
                    {
                        ArrayPool<byte>.Shared.Return(alpnBuffer);
                    }
                }

                //
                // Native API accepts only a single ALPN protocol at a time
                // (null-terminated string). We serialize all used app protocols
                // into a single buffer in the format <len><protocol><0>
                //

                static int GetAlpnProtocolListSerializedLength(List<SslApplicationProtocol>? applicationProtocols)
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

                static void SerializeAlpnProtocolList(List<SslApplicationProtocol> applicationProtocols, Span<byte> buffer)
                {
                    Debug.Assert(GetAlpnProtocolListSerializedLength(applicationProtocols) == buffer.Length);

                    int offset = 0;

                    foreach (SslApplicationProtocol protocol in applicationProtocols)
                    {
                        buffer[offset] = (byte)protocol.Protocol.Length; // preffix len
                        protocol.Protocol.Span.CopyTo(buffer.Slice(offset + 1)); // ALPN
                        buffer[offset + protocol.Protocol.Length + 1] = 0; // null-terminator

                        offset += protocol.Protocol.Length + 2;
                    }
                }

                static (SslProtocols, SslProtocols) GetMinMaxProtocols(SslProtocols protocols)
                {
                    (int minIndex, int maxIndex) = protocols.ValidateContiguous(OrderedSslProtocols);
                    SslProtocols minProtocolId = OrderedSslProtocols[minIndex];
                    SslProtocols maxProtocolId = OrderedSslProtocols[maxIndex];

                    return (minProtocolId, maxProtocolId);
                }

            }

            private static ReadOnlySpan<SslProtocols> OrderedSslProtocols =>
            [
#pragma warning disable 0618
                SslProtocols.Ssl2,
                SslProtocols.Ssl3,
#pragma warning restore 0618
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                SslProtocols.Tls,
                SslProtocols.Tls11,
#pragma warning restore SYSLIB0039
                SslProtocols.Tls12,
                SslProtocols.Tls13
            ];

            // Start the TLS handshake, notifications are received via the status callback (potentially from a different thread).
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwStartTlsHandshake")]
            internal static partial int StartTlsHandshake(SafeNwHandle connection, IntPtr state);

            // takes encrypted input from underlying stream and feed it to the connection.
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwProcessInputData")]
            internal static unsafe partial int ProcessInputData(SafeNwHandle connection, SafeNwHandle framer, byte* buffer, int bufferLength, IntPtr context, delegate* unmanaged<IntPtr, NetworkFrameworkError*, void> completionCallback);

            // sends plaintext data to the connection.
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwSendToConnection")]
            internal static unsafe partial void SendToConnection(SafeNwHandle connection, IntPtr state, void* buffer, int bufferLength, IntPtr context, delegate* unmanaged<IntPtr, NetworkFrameworkError*, void> completionCallback);

            // read plaintext data from the connection.
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwReadFromConnection")]
            internal static unsafe partial void ReadFromConnection(SafeNwHandle connection, IntPtr state, int length, IntPtr context, delegate* unmanaged<IntPtr, NetworkFrameworkError*, byte*, int, void> readCompletionCallback);

            // starts connection cleanup
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwCancelConnection")]
            internal static partial void CancelConnection(SafeNwHandle connection);

            // gets TLS connection information
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwGetConnectionInfo")]
            internal static unsafe partial int GetConnectionInfo(SafeNwHandle connection, out SslProtocols pProtocol, out TlsCipherSuite pCipherSuiteOut, byte* negotiatedAlpn, ref int negotiatedAlpnLength);

            // copies the certificate chain from the connection
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwCopyCertChain")]
            internal static partial void CopyCertChain(SafeNwHandle connection, out SafeCFArrayHandle certificates, out int count);

        }

        // Status enumeration for Network Framework TLS operations
        internal enum StatusUpdates
        {
            UnknownError = 0,
            FramerStart = 1,
            FramerStop = 2,
            HandshakeFinished = 3,
            ConnectionFailed = 4,
            ConnectionCancelled = 103,
            CertificateAvailable = 104,
            DebugLog = 200,
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
            NetworkFramework.Release(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }
    }
}
