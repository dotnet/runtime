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
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_Init")]
            [return: MarshalAs(UnmanagedType.I4)]
            internal static unsafe partial bool Init(
                delegate* unmanaged<IntPtr, StatusUpdates, IntPtr, IntPtr, NetworkFrameworkError*, void> statusCallback,
                delegate* unmanaged<IntPtr, byte*, ulong, void> writeCallback,
                delegate* unmanaged<IntPtr, IntPtr, IntPtr> challengeCallback);

            // Create a new connection context
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwConnectionCreate", StringMarshalling = StringMarshalling.Utf8)]
            internal static unsafe partial SafeNwHandle NwConnectionCreate([MarshalAs(UnmanagedType.I4)] bool isServer, IntPtr context, string targetName, byte* alpnBuffer, int alpnLength, SslProtocols minTlsProtocol, SslProtocols maxTlsProtocol, uint* cipherSuites, int cipherSuitesLength);

            // Start the TLS handshake, notifications are received via the status callback (potentially from a different thread).
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwConnectionStart")]
            internal static partial int NwConnectionStart(SafeNwHandle connection, IntPtr context);

            // takes encrypted input from underlying stream and feed it to the connection.
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwFramerDeliverInput")]
            internal static unsafe partial int NwFramerDeliverInput(SafeNwHandle framer, IntPtr context, byte* buffer, int bufferLength, delegate* unmanaged<IntPtr, NetworkFrameworkError*, void> completionCallback);

            // sends plaintext data to the connection.
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwConnectionSend")]
            internal static unsafe partial void NwConnectionSend(SafeNwHandle connection, IntPtr context, void* buffer, int bufferLength, delegate* unmanaged<IntPtr, NetworkFrameworkError*, void> completionCallback);

            // read plaintext data from the connection.
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwConnectionReceive")]
            internal static unsafe partial void NwConnectionReceive(SafeNwHandle connection, IntPtr context, int length, delegate* unmanaged<IntPtr, NetworkFrameworkError*, byte*, int, void> readCompletionCallback);

            // starts connection cleanup
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_NwConnectionCancel")]
            internal static partial void NwConnectionCancel(SafeNwHandle connection);

            // gets TLS connection information
            [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_GetConnectionInfo")]
            internal static unsafe partial int GetConnectionInfo(SafeNwHandle connection, IntPtr context, out SslProtocols pProtocol, out TlsCipherSuite pCipherSuiteOut, byte* negotiatedAlpn, ref int negotiatedAlpnLength);
        }

        // Status enumeration for Network Framework TLS operations
        internal enum StatusUpdates
        {
            UnknownError = 0,
            FramerStart = 1,
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
