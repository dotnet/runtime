using System;
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
            internal static unsafe partial int Init(delegate* unmanaged<nuint, PAL_NwStatusUpdates, nuint, nuint, void> statusCallback,
                                                   delegate* unmanaged<void*, byte*, nuint*, int> readCallback,
                                                   delegate* unmanaged<void*, byte*, nuint, int> writeCallback);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwCreateContext")]
            internal static partial SafeNetworkFrameworkHandle CreateContext(bool isServer);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwSetTlsOptions")]
            internal static unsafe partial int SetTlsOptions(SafeNetworkFrameworkHandle connection, nuint gcHandle, 
                                                            string targetName, byte* alpnBuffer, int alpnLength, 
                                                            SslProtocols minTlsProtocol, SslProtocols maxTlsProtocol);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwStartTlsHandshake")]
            internal static partial int StartTlsHandshake(SafeNetworkFrameworkHandle connection, nuint gcHandle);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwProcessInputData")]
            internal static unsafe partial int ProcessInputData(SafeNetworkFrameworkHandle connection, 
                                                               SafeNetworkFrameworkHandle framer, 
                                                               byte* buffer, int bufferLength);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwSendToConnection")]
            internal static unsafe partial int SendToConnection(SafeNetworkFrameworkHandle connection, nuint gcHandle,
                                                               byte* buffer, int bufferLength);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwReadFromConnection")]
            internal static partial int ReadFromConnection(SafeNetworkFrameworkHandle connection, nuint gcHandle);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwCancelConnection")]
            internal static partial int CancelConnection(SafeNetworkFrameworkHandle connection);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwGetConnectionInfo")]
            internal static unsafe partial int GetConnectionInfo(SafeNetworkFrameworkHandle connection, 
                                                               SslProtocols* pProtocol, ushort* pCipherSuiteOut, 
                                                               byte** negotiatedAlpn, uint* alpnLength);

            [LibraryImport(Interop.Libraries.AppleNetworkNative, EntryPoint = "AppleNetNative_NwCopyCertChain")]
            internal static unsafe partial int CopyCertChain(SafeNetworkFrameworkHandle connection, 
                                                           void** certificates, int* certificateCount);
        }
    }

    // Status enumeration for Network Framework TLS operations
    internal enum PAL_NwStatusUpdates
    {
        UnknownError = 0,
        FramerStart = 1,
        HandshakeFinished = 2,
        HandshakeFailed = 3,
        ConnectionReadFinished = 4,
        ConnectionWriteFinished = 5,
        ConnectionWriteFailed = 6,
        ConnectionError = 7,
        ConnectionCancelled = 8,
    }

    // Safe handle classes for Network Framework TLS resources
    internal sealed class SafeNetworkFrameworkHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeNetworkFrameworkHandle() : base(true) { }

        protected override bool ReleaseHandle()
        {
            // Native cleanup will be handled by Network Framework
            return true;
        }
    }
}