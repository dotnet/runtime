// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using Microsoft.Win32.SafeHandles;
using SafeSslHandle = System.Net.SafeSslHandle;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        private static readonly IdnMapping s_idnMapping = new IdnMapping();

        // Read data from connection (or an instance delegate captured context) and write it to data
        // dataLength comes in as the capacity of data, goes out as bytes written.
        // Note: the true type of dataLength is `size_t*`, but on macOS that's most equal to `void**`
        internal unsafe delegate int SSLReadFunc(void* connection, byte* data, void** dataLength);

        // (In the C decl for this function data is "const byte*", justifying the second type).
        // Read *dataLength from data and write it to connection (or an instance delegate captured context),
        // and set *dataLength to the number of bytes actually transferred.
        internal unsafe delegate int SSLWriteFunc(void* connection, byte* data, void** dataLength);

        private static readonly SafeCreateHandle s_cfHttp2Str = CoreFoundation.CFStringCreateWithCString("h2");
        private static readonly SafeCreateHandle s_cfHttp11Str = CoreFoundation.CFStringCreateWithCString("http/1.1");

        private static readonly IntPtr[] s_cfAlpnHttp2Protocol = new IntPtr[] { s_cfHttp2Str.DangerousGetHandle() };
        private static readonly IntPtr[] s_cfAlpnHttp11Protocol = new IntPtr[] { s_cfHttp11Str.DangerousGetHandle() };
        private static readonly IntPtr[] s_cfAlpnHttp211Protocol = new IntPtr[] { s_cfHttp2Str.DangerousGetHandle(), s_cfHttp11Str.DangerousGetHandle() };

        private static readonly SafeCreateHandle s_cfAlpnHttp11Protocols = CoreFoundation.CFArrayCreate(s_cfAlpnHttp11Protocol, (UIntPtr)1);
        private static readonly SafeCreateHandle s_cfAlpnHttp2Protocols = CoreFoundation.CFArrayCreate(s_cfAlpnHttp2Protocol, (UIntPtr)1);
        private static readonly SafeCreateHandle s_cfAlpnHttp211Protocols = CoreFoundation.CFArrayCreate(s_cfAlpnHttp211Protocol, (UIntPtr)2);

        internal enum PAL_TlsHandshakeState
        {
            Unknown,
            Complete,
            WouldBlock,
            ServerAuthCompleted,
            ClientAuthCompleted,
            ClientCertRequested,
        }

        internal enum PAL_TlsIo
        {
            Unknown,
            Success,
            WouldBlock,
            ClosedGracefully,
            Renegotiate,
        }

        [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslCreateContext")]
        internal static partial System.Net.SafeSslHandle SslCreateContext(int isServer);

        [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslSetConnection")]
        internal static partial int SslSetConnection(
            SafeSslHandle sslHandle,
            IntPtr sslConnection);

        [LibraryImport(Interop.Libraries.AppleCryptoNative)]
        private static partial int AppleCryptoNative_SslSetMinProtocolVersion(
            SafeSslHandle sslHandle,
            SslProtocols minProtocolId);

        [LibraryImport(Interop.Libraries.AppleCryptoNative)]
        private static partial int AppleCryptoNative_SslSetMaxProtocolVersion(
            SafeSslHandle sslHandle,
            SslProtocols maxProtocolId);

        [LibraryImport(Interop.Libraries.AppleCryptoNative)]
        private static partial int AppleCryptoNative_SslCopyCertChain(
            SafeSslHandle sslHandle,
            out SafeX509ChainHandle pTrustOut,
            out int pOSStatus);

        [LibraryImport(Interop.Libraries.AppleCryptoNative)]
        private static partial int AppleCryptoNative_SslCopyCADistinguishedNames(
            SafeSslHandle sslHandle,
            out SafeCFArrayHandle pArrayOut,
            out int pOSStatus);

        [LibraryImport(Interop.Libraries.AppleCryptoNative)]
        private static partial int AppleCryptoNative_SslSetBreakOnServerAuth(
            SafeSslHandle sslHandle,
            int setBreak,
            out int pOSStatus);

        [LibraryImport(Interop.Libraries.AppleCryptoNative)]
        private static partial int AppleCryptoNative_SslSetBreakOnClientAuth(
            SafeSslHandle sslHandle,
            int setBreak,
            out int pOSStatus);

        [LibraryImport(Interop.Libraries.AppleCryptoNative)]
        private static partial int AppleCryptoNative_SslSetBreakOnCertRequested(
            SafeSslHandle sslHandle,
            int setBreak,
            out int pOSStatus);

        [LibraryImport(Interop.Libraries.AppleCryptoNative)]
        private static partial int AppleCryptoNative_SslSetCertificate(
            SafeSslHandle sslHandle,
            SafeCreateHandle cfCertRefs);

        [LibraryImport(Interop.Libraries.AppleCryptoNative, StringMarshalling = StringMarshalling.Utf8)]
        private static partial int AppleCryptoNative_SslSetTargetName(
            SafeSslHandle sslHandle,
            string targetName,
            int cbTargetName,
            out int osStatus);

        [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SSLSetALPNProtocols")]
        internal static partial int SSLSetALPNProtocols(SafeSslHandle ctx, SafeCreateHandle cfProtocolsRefs, out int osStatus);

        [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslGetAlpnSelected")]
        internal static partial int SslGetAlpnSelected(SafeSslHandle ssl, out SafeCFDataHandle protocol);

        [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslHandshake")]
        internal static partial PAL_TlsHandshakeState SslHandshake(SafeSslHandle sslHandle);

        [LibraryImport(Interop.Libraries.AppleCryptoNative)]
        private static partial int AppleCryptoNative_SslSetAcceptClientCert(SafeSslHandle sslHandle);

        [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslSetIoCallbacks")]
        internal static unsafe partial int SslSetIoCallbacks(
            SafeSslHandle sslHandle,
            delegate* unmanaged<IntPtr, byte*, void**, int> readCallback,
            delegate* unmanaged<IntPtr, byte*, void**, int> writeCallback);

        [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslWrite")]
        internal static unsafe partial PAL_TlsIo SslWrite(SafeSslHandle sslHandle, byte* writeFrom, int count, out int bytesWritten);

        [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslRead")]
        internal static unsafe partial PAL_TlsIo SslRead(SafeSslHandle sslHandle, byte* writeFrom, int count, out int bytesWritten);

        [LibraryImport(Interop.Libraries.AppleCryptoNative)]
        private static partial int AppleCryptoNative_SslIsHostnameMatch(
            SafeSslHandle handle,
            SafeCreateHandle cfHostname,
            SafeCFDateHandle cfValidTime,
            out int pOSStatus);

        [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslShutdown")]
        internal static partial int SslShutdown(SafeSslHandle sslHandle);

        [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslGetCipherSuite")]
        internal static partial int SslGetCipherSuite(SafeSslHandle sslHandle, out TlsCipherSuite cipherSuite);

        [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslGetProtocolVersion")]
        internal static partial int SslGetProtocolVersion(SafeSslHandle sslHandle, out SslProtocols protocol);

        [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslSetEnabledCipherSuites")]
        internal static unsafe partial int SslSetEnabledCipherSuites(SafeSslHandle sslHandle, uint* cipherSuites, int numCipherSuites);

        [LibraryImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslSetCertificateAuthorities")]
        internal static partial int SslSetCertificateAuthorities(SafeSslHandle sslHandle, SafeCreateHandle certificateOrArray, int replaceExisting);

        internal static unsafe void SslSetCertificateAuthorities(SafeSslHandle sslHandle, Span<IntPtr> certificates, bool replaceExisting)
        {
            using (SafeCreateHandle cfCertRefs = CoreFoundation.CFArrayCreate(certificates))
            {
                int osStatus = SslSetCertificateAuthorities(sslHandle, cfCertRefs, replaceExisting ? 1 : 0);

                if (osStatus != 0)
                {
                    throw CreateExceptionForOSStatus(osStatus);
                }
            }
        }

        internal static void SslSetAcceptClientCert(SafeSslHandle sslHandle)
        {
            int osStatus = AppleCryptoNative_SslSetAcceptClientCert(sslHandle);

            if (osStatus != 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }
        }

        internal static void SslSetMinProtocolVersion(SafeSslHandle sslHandle, SslProtocols minProtocolId)
        {
            int osStatus = AppleCryptoNative_SslSetMinProtocolVersion(sslHandle, minProtocolId);

            if (osStatus != 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }
        }

        internal static void SslSetMaxProtocolVersion(SafeSslHandle sslHandle, SslProtocols maxProtocolId)
        {
            int osStatus = AppleCryptoNative_SslSetMaxProtocolVersion(sslHandle, maxProtocolId);

            if (osStatus != 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }
        }

        internal static SafeX509ChainHandle SslCopyCertChain(SafeSslHandle sslHandle)
        {
            SafeX509ChainHandle chainHandle;
            int osStatus;
            int result = AppleCryptoNative_SslCopyCertChain(sslHandle, out chainHandle, out osStatus);

            if (result == 1)
            {
                return chainHandle;
            }

            chainHandle.Dispose();

            if (result == 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }

            Debug.Fail($"AppleCryptoNative_SslCopyCertChain returned {result}");
            throw new SslException();
        }

        internal static SafeCFArrayHandle SslCopyCADistinguishedNames(SafeSslHandle sslHandle)
        {
            SafeCFArrayHandle dnArray;
            int osStatus;
            int result = AppleCryptoNative_SslCopyCADistinguishedNames(sslHandle, out dnArray, out osStatus);

            if (result == 1)
            {
                return dnArray;
            }

            dnArray.Dispose();

            if (result == 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }

            Debug.Fail($"AppleCryptoNative_SslCopyCADistinguishedNames returned {result}");
            throw new SslException();
        }

        internal static void SslBreakOnServerAuth(SafeSslHandle sslHandle, bool setBreak)
        {
            int osStatus;
            int result = AppleCryptoNative_SslSetBreakOnServerAuth(sslHandle, setBreak ? 1 : 0, out osStatus);

            if (result == 1)
            {
                return;
            }

            if (result == 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }

            Debug.Fail($"AppleCryptoNative_SslSetBreakOnServerAuth returned {result}");
            throw new SslException();
        }

        internal static void SslBreakOnClientAuth(SafeSslHandle sslHandle, bool setBreak)
        {
            int osStatus;
            int result = AppleCryptoNative_SslSetBreakOnClientAuth(sslHandle, setBreak ? 1 : 0, out osStatus);

            if (result == 1)
            {
                return;
            }

            if (result == 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }

            Debug.Fail($"AppleCryptoNative_SslSetBreakOnClientAuth returned {result}");
            throw new SslException();
        }

        internal static void SslBreakOnCertRequested(SafeSslHandle sslHandle, bool setBreak)
        {
            int osStatus;
            int result = AppleCryptoNative_SslSetBreakOnCertRequested(sslHandle, setBreak ? 1 : 0, out osStatus);

            if (result == 1)
            {
                return;
            }

            if (result == 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }

            Debug.Fail($"AppleCryptoNative_SslSetBreakOnCertRequested returned {result}");
            throw new SslException();
        }

        internal static void SslSetCertificate(SafeSslHandle sslHandle, IntPtr[] certChainPtrs)
        {
            using (SafeCreateHandle cfCertRefs = CoreFoundation.CFArrayCreate(certChainPtrs, (UIntPtr)certChainPtrs.Length))
            {
                int osStatus = AppleCryptoNative_SslSetCertificate(sslHandle, cfCertRefs);

                if (osStatus != 0)
                {
                    throw CreateExceptionForOSStatus(osStatus);
                }
            }
        }

        internal static void SslSetTargetName(SafeSslHandle sslHandle, string targetName)
        {
            Debug.Assert(!string.IsNullOrEmpty(targetName));

            int osStatus;
            int cbTargetName = System.Text.Encoding.UTF8.GetByteCount(targetName);

            int result = AppleCryptoNative_SslSetTargetName(sslHandle, targetName, cbTargetName, out osStatus);

            if (result == 1)
            {
                return;
            }

            if (result == 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }

            Debug.Fail($"AppleCryptoNative_SslSetTargetName returned {result}");
            throw new SslException();
        }

        internal static unsafe void SslCtxSetAlpnProtos(SafeSslHandle ctx, List<SslApplicationProtocol> protocols)
        {
            SafeCreateHandle? cfProtocolsRefs = null;
            SafeCreateHandle[]? cfProtocolsArrayRef = null;
            try
            {
                if (protocols.Count == 1 && protocols[0] == SslApplicationProtocol.Http2)
                {
                    cfProtocolsRefs = s_cfAlpnHttp2Protocols;
                }
                else if (protocols.Count == 1 && protocols[0] == SslApplicationProtocol.Http11)
                {
                    cfProtocolsRefs = s_cfAlpnHttp11Protocols;
                }
                else if (protocols.Count == 2 && protocols[0] == SslApplicationProtocol.Http2 && protocols[1] == SslApplicationProtocol.Http11)
                {
                    cfProtocolsRefs = s_cfAlpnHttp211Protocols;
                }
                else
                {
                    // we did not match common case. This is more expensive path allocating Core Foundation objects.
                    cfProtocolsArrayRef = new SafeCreateHandle[protocols.Count];
                    IntPtr[] protocolsPtr = new System.IntPtr[protocols.Count];

                    for (int i = 0; i < protocols.Count; i++)
                    {
                        cfProtocolsArrayRef[i] = CoreFoundation.CFStringCreateWithCString(protocols[i].ToString());
                        protocolsPtr[i] = cfProtocolsArrayRef[i].DangerousGetHandle();
                    }

                    cfProtocolsRefs = CoreFoundation.CFArrayCreate(protocolsPtr, (UIntPtr)protocols.Count);
                }

                int osStatus;
                int result = SSLSetALPNProtocols(ctx, cfProtocolsRefs, out osStatus);
                if (result != 1)
                {
                    throw CreateExceptionForOSStatus(osStatus);
                }
            }
            finally
            {
                if (cfProtocolsArrayRef != null)
                {
                    for (int i = 0; i < cfProtocolsArrayRef.Length; i++)
                    {
                        cfProtocolsArrayRef[i]?.Dispose();
                    }

                    cfProtocolsRefs?.Dispose();
                }
            }
        }

        internal static byte[]? SslGetAlpnSelected(SafeSslHandle ssl)
        {
            SafeCFDataHandle protocol;

            if (SslGetAlpnSelected(ssl, out protocol) != 1 || protocol == null)
            {
                return null;
            }

            try
            {
                byte[] result = Interop.CoreFoundation.CFGetData(protocol);
                return result;
            }
            finally
            {
                protocol.Dispose();
            }
        }

        public static bool SslCheckHostnameMatch(SafeSslHandle handle, string hostName, DateTime notBefore, out int osStatus)
        {
            int result;
            // The IdnMapping converts Unicode input into the IDNA punycode sequence.
            // It also does host case normalization.  The bypass logic would be something
            // like "all characters being within [a-z0-9.-]+"
            //
            // The SSL Policy (SecPolicyCreateSSL) has been verified as not inherently supporting
            // IDNA as of macOS 10.12.1 (Sierra).  If it supports low-level IDNA at a later date,
            // this code could be removed.
            //
            // It was verified as supporting case invariant match as of 10.12.1 (Sierra).
            string matchName = string.IsNullOrEmpty(hostName) ? string.Empty : s_idnMapping.GetAscii(hostName);

            using (SafeCFDateHandle cfNotBefore = CoreFoundation.CFDateCreate(notBefore))
            using (SafeCreateHandle cfHostname = CoreFoundation.CFStringCreateWithCString(matchName))
            {
                result = AppleCryptoNative_SslIsHostnameMatch(handle, cfHostname, cfNotBefore, out osStatus);
            }

            switch (result)
            {
                case 0:
                    return false;
                case 1:
                    return true;
                default:
                    if (NetEventSource.Log.IsEnabled())
                        NetEventSource.Error(null, $"AppleCryptoNative_SslIsHostnameMatch returned '{result}' for '{hostName}'");
                    Debug.Fail($"AppleCryptoNative_SslIsHostnameMatch returned {result}");
                    throw new SslException();
            }
        }
    }
}

namespace System.Net
{
    internal sealed class SafeSslHandle : SafeHandle
    {
        public SafeSslHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        internal SafeSslHandle(IntPtr invalidHandleValue, bool ownsHandle)
            : base(invalidHandleValue, ownsHandle)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.CoreFoundation.CFRelease(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
