// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using Microsoft.Win32.SafeHandles;
using SafeSslHandle = System.Net.SafeSslHandle;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        // Read data from connection (or an instance delegate captured context) and write it to data
        // dataLength comes in as the capacity of data, goes out as bytes written.
        // Note: the true type of dataLength is `size_t*`, but on macOS that's most equal to `void**`
        internal unsafe delegate int SSLReadFunc(void* connection, byte* data, void** dataLength);

        // (In the C decl for this function data is "const byte*", justifying the second type).
        // Read *dataLength from data and write it to connection (or an instance delegate captured context),
        // and set *dataLength to the number of bytes actually transferred.
        internal unsafe delegate int SSLWriteFunc(void* connection, byte* data, void** dataLength);

        internal enum PAL_TlsHandshakeState
        {
            Unknown,
            Complete,
            WouldBlock,
            ServerAuthCompleted,
            ClientAuthCompleted,
        }

        internal enum PAL_TlsIo
        {
            Unknown,
            Success,
            WouldBlock,
            ClosedGracefully,
            Renegotiate,
        }

        // These come from the various SSL/TLS RFCs.
        internal enum TlsCipherSuite
        {
            TLS_NULL_WITH_NULL_NULL = 0x0000,
            SSL_NULL_WITH_NULL_NULL = 0x0000,

            TLS_RSA_WITH_NULL_MD5 = 0x0001,
            SSL_RSA_WITH_NULL_MD5 = 0x0001,

            TLS_RSA_WITH_NULL_SHA = 0x0002,
            SSL_RSA_WITH_NULL_SHA = 0x0002,

            SSL_RSA_EXPORT_WITH_RC4_40_MD5 = 0x0003,

            TLS_RSA_WITH_RC4_128_MD5 = 0x0004,
            SSL_RSA_WITH_RC4_128_MD5 = 0x0004,

            TLS_RSA_WITH_RC4_128_SHA = 0x0005,
            SSL_RSA_WITH_RC4_128_SHA = 0x0005,

            SSL_RSA_EXPORT_WITH_RC2_CBC_40_MD5 = 0x0006,

            //Windows has no value for IDEA, so .NET Framework didn't get one.
            //SSL_RSA_WITH_IDEA_CBC_SHA = 0x0007,

            SSL_RSA_EXPORT_WITH_DES40_CBC_SHA = 0x0008,

            SSL_RSA_WITH_DES_CBC_SHA = 0x0009,

            TLS_RSA_WITH_3DES_EDE_CBC_SHA = 0x000A,
            SSL_RSA_WITH_3DES_EDE_CBC_SHA = 0x000A,

            SSL_DH_DSS_EXPORT_WITH_DES40_CBC_SHA = 0x000B,

            SSL_DH_DSS_WITH_DES_CBC_SHA = 0x000C,

            TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA = 0x000D,
            SSL_DH_DSS_WITH_3DES_EDE_CBC_SHA = 0x000D,

            SSL_DH_RSA_EXPORT_WITH_DES40_CBC_SHA = 0x000E,

            SSL_DH_RSA_WITH_DES_CBC_SHA = 0x000F,

            TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA = 0x0010,
            SSL_DH_RSA_WITH_3DES_EDE_CBC_SHA = 0x0010,

            SSL_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA = 0x0011,

            SSL_DHE_DSS_WITH_DES_CBC_SHA = 0x0012,

            TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA = 0x0013,
            SSL_DHE_DSS_WITH_3DES_EDE_CBC_SHA = 0x0013,

            SSL_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA = 0x0014,

            SSL_DHE_RSA_WITH_DES_CBC_SHA = 0x0015,

            TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA = 0x0016,
            SSL_DHE_RSA_WITH_3DES_EDE_CBC_SHA = 0x0016,

            SSL_DH_anon_EXPORT_WITH_RC4_40_MD5 = 0x0017,

            TLS_DH_anon_WITH_RC4_128_MD5 = 0x0018,
            SSL_DH_anon_WITH_RC4_128_MD5 = 0x0018,

            SSL_DH_anon_EXPORT_WITH_DES40_CBC_SHA = 0x0019,

            SSL_DH_anon_WITH_DES_CBC_SHA = 0x001A,

            TLS_DH_anon_WITH_3DES_EDE_CBC_SHA = 0x001B,
            SSL_DH_anon_WITH_3DES_EDE_CBC_SHA = 0x001B,

            // Windows doesn't support FORTEZZA_DMS, so unclear what value to use.
            //SSL_FORTEZZA_DMS_WITH_NULL_SHA = 0x001C,

            // Windows doesn't support FORTEZZA_DMS, so unclear what value to use.
            //SSL_FORTEZZA_DMS_WITH_FORTEZZA_CBC_SHA = 0x001D,

            TLS_PSK_WITH_NULL_SHA = 0x002C,

            TLS_DHE_PSK_WITH_NULL_SHA = 0x002D,

            TLS_RSA_PSK_WITH_NULL_SHA = 0x002E,

            TLS_RSA_WITH_AES_128_CBC_SHA = 0x002F,

            TLS_DH_DSS_WITH_AES_128_CBC_SHA = 0x0030,

            TLS_DH_RSA_WITH_AES_128_CBC_SHA = 0x0031,

            TLS_DHE_DSS_WITH_AES_128_CBC_SHA = 0x0032,

            TLS_DHE_RSA_WITH_AES_128_CBC_SHA = 0x0033,

            TLS_DH_anon_WITH_AES_128_CBC_SHA = 0x0034,

            TLS_RSA_WITH_AES_256_CBC_SHA = 0x0035,

            TLS_DH_DSS_WITH_AES_256_CBC_SHA = 0x0036,

            TLS_DH_RSA_WITH_AES_256_CBC_SHA = 0x0037,

            TLS_DHE_DSS_WITH_AES_256_CBC_SHA = 0x0038,

            TLS_DHE_RSA_WITH_AES_256_CBC_SHA = 0x0039,

            TLS_DH_anon_WITH_AES_256_CBC_SHA = 0x003A,

            TLS_RSA_WITH_NULL_SHA256 = 0x003B,

            TLS_RSA_WITH_AES_128_CBC_SHA256 = 0x003C,

            TLS_RSA_WITH_AES_256_CBC_SHA256 = 0x003D,

            TLS_DH_DSS_WITH_AES_128_CBC_SHA256 = 0x003E,

            TLS_DH_RSA_WITH_AES_128_CBC_SHA256 = 0x003F,

            TLS_DHE_DSS_WITH_AES_128_CBC_SHA256 = 0x0040,

            TLS_DHE_RSA_WITH_AES_128_CBC_SHA256 = 0x0067,

            TLS_DH_DSS_WITH_AES_256_CBC_SHA256 = 0x0068,

            TLS_DH_RSA_WITH_AES_256_CBC_SHA256 = 0x0069,

            TLS_DHE_DSS_WITH_AES_256_CBC_SHA256 = 0x006A,

            TLS_DHE_RSA_WITH_AES_256_CBC_SHA256 = 0x006B,

            TLS_DH_anon_WITH_AES_128_CBC_SHA256 = 0x006C,

            TLS_DH_anon_WITH_AES_256_CBC_SHA256 = 0x006D,

            TLS_PSK_WITH_RC4_128_SHA = 0x008A,

            TLS_PSK_WITH_3DES_EDE_CBC_SHA = 0x008B,

            TLS_PSK_WITH_AES_128_CBC_SHA = 0x008C,

            TLS_PSK_WITH_AES_256_CBC_SHA = 0x008D,

            TLS_DHE_PSK_WITH_RC4_128_SHA = 0x008E,

            TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA = 0x008F,

            TLS_DHE_PSK_WITH_AES_128_CBC_SHA = 0x0090,

            TLS_DHE_PSK_WITH_AES_256_CBC_SHA = 0x0091,

            TLS_RSA_PSK_WITH_RC4_128_SHA = 0x0092,

            TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA = 0x0093,

            TLS_RSA_PSK_WITH_AES_128_CBC_SHA = 0x0094,

            TLS_RSA_PSK_WITH_AES_256_CBC_SHA = 0x0095,

            TLS_RSA_WITH_AES_128_GCM_SHA256 = 0x009C,

            TLS_RSA_WITH_AES_256_GCM_SHA384 = 0x009D,

            TLS_DHE_RSA_WITH_AES_128_GCM_SHA256 = 0x009E,

            TLS_DHE_RSA_WITH_AES_256_GCM_SHA384 = 0x009F,

            TLS_DH_RSA_WITH_AES_128_GCM_SHA256 = 0x00A0,

            TLS_DH_RSA_WITH_AES_256_GCM_SHA384 = 0x00A1,

            TLS_DHE_DSS_WITH_AES_128_GCM_SHA256 = 0x00A2,

            TLS_DHE_DSS_WITH_AES_256_GCM_SHA384 = 0x00A3,

            TLS_DH_DSS_WITH_AES_128_GCM_SHA256 = 0x00A4,

            TLS_DH_DSS_WITH_AES_256_GCM_SHA384 = 0x00A5,

            TLS_DH_anon_WITH_AES_128_GCM_SHA256 = 0x00A6,

            TLS_DH_anon_WITH_AES_256_GCM_SHA384 = 0x00A7,

            TLS_PSK_WITH_AES_128_GCM_SHA256 = 0x00A8,

            TLS_PSK_WITH_AES_256_GCM_SHA384 = 0x00A9,

            TLS_DHE_PSK_WITH_AES_128_GCM_SHA256 = 0x00AA,

            TLS_DHE_PSK_WITH_AES_256_GCM_SHA384 = 0x00AB,

            TLS_RSA_PSK_WITH_AES_128_GCM_SHA256 = 0x00AC,

            TLS_RSA_PSK_WITH_AES_256_GCM_SHA384 = 0x00AD,

            TLS_PSK_WITH_AES_128_CBC_SHA256 = 0x00AE,

            TLS_PSK_WITH_AES_256_CBC_SHA384 = 0x00AF,

            TLS_PSK_WITH_NULL_SHA256 = 0x00B0,

            TLS_PSK_WITH_NULL_SHA384 = 0x00B1,

            TLS_DHE_PSK_WITH_AES_128_CBC_SHA256 = 0x00B2,

            TLS_DHE_PSK_WITH_AES_256_CBC_SHA384 = 0x00B3,

            TLS_DHE_PSK_WITH_NULL_SHA256 = 0x00B4,

            TLS_DHE_PSK_WITH_NULL_SHA384 = 0x00B5,

            TLS_RSA_PSK_WITH_AES_128_CBC_SHA256 = 0x00B6,

            TLS_RSA_PSK_WITH_AES_256_CBC_SHA384 = 0x00B7,

            TLS_RSA_PSK_WITH_NULL_SHA256 = 0x00B8,

            TLS_RSA_PSK_WITH_NULL_SHA384 = 0x00B9,

            // Not a real CipherSuite
            //TLS_EMPTY_RENEGOTIATION_INFO_SCSV = 0x00FF,

            TLS_ECDH_ECDSA_WITH_NULL_SHA = 0xC001,

            TLS_ECDH_ECDSA_WITH_RC4_128_SHA = 0xC002,

            TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA = 0xC003,

            TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA = 0xC004,

            TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA = 0xC005,

            TLS_ECDHE_ECDSA_WITH_NULL_SHA = 0xC006,

            TLS_ECDHE_ECDSA_WITH_RC4_128_SHA = 0xC007,

            TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA = 0xC008,

            TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA = 0xC009,

            TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA = 0xC00A,

            TLS_ECDH_RSA_WITH_NULL_SHA = 0xC00B,

            TLS_ECDH_RSA_WITH_RC4_128_SHA = 0xC00C,

            TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA = 0xC00D,

            TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA = 0xC013,

            TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA = 0xC014,

            TLS_ECDH_anon_WITH_NULL_SHA = 0xC015,

            TLS_ECDH_anon_WITH_RC4_128_SHA = 0xC016,

            TLS_ECDH_anon_WITH_3DES_EDE_CBC_SHA = 0xC017,

            TLS_ECDH_anon_WITH_AES_128_CBC_SHA = 0xC018,

            TLS_ECDH_anon_WITH_AES_256_CBC_SHA = 0xC019,

            TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256 = 0xC023,

            TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384 = 0xC024,

            TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256 = 0xC025,

            TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384 = 0xC026,

            TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256 = 0xC027,

            TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384 = 0xC028,

            TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256 = 0xC029,

            TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384 = 0xC02A,

            TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256 = 0xC02B,

            TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384 = 0xC02C,

            TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256 = 0xC02D,

            TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384 = 0xC02E,

            TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256 = 0xC02F,

            TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384 = 0xC030,

            TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256 = 0xC031,

            TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384 = 0xC032,
        }

        [DllImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslCreateContext")]
        internal static extern System.Net.SafeSslHandle SslCreateContext(int isServer);

        [DllImport(Interop.Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SslSetMinProtocolVersion(
            SafeSslHandle sslHandle,
            SslProtocols minProtocolId);

        [DllImport(Interop.Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SslSetMaxProtocolVersion(
            SafeSslHandle sslHandle,
            SslProtocols maxProtocolId);

        [DllImport(Interop.Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SslCopyCertChain(
            SafeSslHandle sslHandle,
            out SafeX509ChainHandle pTrustOut,
            out int pOSStatus);

        [DllImport(Interop.Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SslCopyCADistinguishedNames(
            SafeSslHandle sslHandle,
            out SafeCFArrayHandle pArrayOut,
            out int pOSStatus);

        [DllImport(Interop.Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SslSetBreakOnServerAuth(
            SafeSslHandle sslHandle,
            int setBreak,
            out int pOSStatus);

        [DllImport(Interop.Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SslSetBreakOnClientAuth(
            SafeSslHandle sslHandle,
            int setBreak,
            out int pOSStatus);

        [DllImport(Interop.Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SslSetCertificate(
            SafeSslHandle sslHandle,
            SafeCreateHandle cfCertRefs);

        [DllImport(Interop.Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SslSetTargetName(
            SafeSslHandle sslHandle,
            string targetName,
            int cbTargetName,
            out int osStatus);

        [DllImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslHandshake")]
        internal static extern PAL_TlsHandshakeState SslHandshake(SafeSslHandle sslHandle);

        [DllImport(Interop.Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SslSetAcceptClientCert(SafeSslHandle sslHandle);

        [DllImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslSetIoCallbacks")]
        internal static extern int SslSetIoCallbacks(
            SafeSslHandle sslHandle,
            SSLReadFunc readCallback,
            SSLWriteFunc writeCallback);

        [DllImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslWrite")]
        internal static extern unsafe PAL_TlsIo SslWrite(SafeSslHandle sslHandle, byte* writeFrom, int count, out int bytesWritten);

        [DllImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslRead")]
        internal static extern unsafe PAL_TlsIo SslRead(SafeSslHandle sslHandle, byte* writeFrom, int count, out int bytesWritten);

        [DllImport(Interop.Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SslIsHostnameMatch(
            SafeSslHandle handle,
            SafeCreateHandle cfHostname,
            SafeCFDateHandle cfValidTime);

        [DllImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslShutdown")]
        internal static extern int SslShutdown(SafeSslHandle sslHandle);

        [DllImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslGetCipherSuite")]
        internal static extern int SslGetCipherSuite(SafeSslHandle sslHandle, out TlsCipherSuite cipherSuite);

        [DllImport(Interop.Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SslGetProtocolVersion")]
        internal static extern int SslGetProtocolVersion(SafeSslHandle sslHandle, out SslProtocols protocol);

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

        public static bool SslCheckHostnameMatch(SafeSslHandle handle, string hostName, DateTime notBefore)
        {
            int result;
            // The IdnMapping converts Unicode input into the IDNA punycode sequence.
            // It also does host case normalization.  The bypass logic would be something
            // like "all characters being within [a-z0-9.-]+"
            // Since it's not documented as being thread safe, create a new one each time.
            //
            // The SSL Policy (SecPolicyCreateSSL) has been verified as not inherently supporting
            // IDNA as of macOS 10.12.1 (Sierra).  If it supports low-level IDNA at a later date,
            // this code could be removed.
            //
            // It was verified as supporting case invariant match as of 10.12.1 (Sierra).
            string matchName = new System.Globalization.IdnMapping().GetAscii(hostName);

            using (SafeCFDateHandle cfNotBefore = CoreFoundation.CFDateCreate(notBefore))
            using (SafeCreateHandle cfHostname = CoreFoundation.CFStringCreateWithCString(matchName))
            {
                result = AppleCryptoNative_SslIsHostnameMatch(handle, cfHostname, cfNotBefore);
            }

            switch (result)
            {
                case 0:
                    return false;
                case 1:
                    return true;
                default:
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
        internal SafeSslHandle()
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
