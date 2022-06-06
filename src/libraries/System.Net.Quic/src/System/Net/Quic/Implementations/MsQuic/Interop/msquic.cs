#pragma warning disable IDE0073
//
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
//
#pragma warning restore IDE0073

using System;
using System.Diagnostics;
using System.Net.Quic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NETSTANDARD
using OperatingSystem = Microsoft.Quic.Polyfill.OperatingSystem;
#else
using OperatingSystem = System.OperatingSystem;
#endif

namespace Microsoft.Quic
{
    internal unsafe partial struct QUIC_BUFFER
    {
        public Span<byte> Span => new(Buffer, (int)Length);
    }

    internal partial class MsQuic
    {
        public static unsafe QUIC_API_TABLE* Open()
        {
            QUIC_API_TABLE* ApiTable;
            int Status = MsQuicOpenVersion(2, (void**)&ApiTable);
            ThrowIfFailure(Status);
            return ApiTable;
        }

        public static unsafe void Close(QUIC_API_TABLE* ApiTable)
        {
            MsQuicClose(ApiTable);
        }

        public static void ThrowIfFailure(int status, string? message = null)
        {
            if (StatusFailed(status))
            {
                // TODO make custom exception, and maybe throw helpers
                throw new MsQuicException(status, message);
            }
        }

        public static bool StatusSucceeded(int status)
        {
            if (OperatingSystem.IsWindows())
            {
                return status >= 0;
            }
            else
            {
                return status <= 0;
            }
        }

        public static bool StatusFailed(int status)
        {
            if (OperatingSystem.IsWindows())
            {
                return status < 0;
            }
            else
            {
                return status > 0;
            }
        }

        public static int QUIC_STATUS_SUCCESS => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_SUCCESS : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_SUCCESS : MsQuic_MacOS.QUIC_STATUS_SUCCESS;
        public static int QUIC_STATUS_PENDING => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_PENDING : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_PENDING : MsQuic_Linux.QUIC_STATUS_PENDING;
        public static int QUIC_STATUS_CONTINUE => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_CONTINUE : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_CONTINUE : MsQuic_Linux.QUIC_STATUS_CONTINUE;
        public static int QUIC_STATUS_OUT_OF_MEMORY => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_OUT_OF_MEMORY : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_OUT_OF_MEMORY : MsQuic_Linux.QUIC_STATUS_OUT_OF_MEMORY;
        public static int QUIC_STATUS_INVALID_PARAMETER => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_INVALID_PARAMETER : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_INVALID_PARAMETER : MsQuic_Linux.QUIC_STATUS_INVALID_PARAMETER;
        public static int QUIC_STATUS_INVALID_STATE => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_INVALID_STATE : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_INVALID_STATE : MsQuic_Linux.QUIC_STATUS_INVALID_STATE;
        public static int QUIC_STATUS_NOT_SUPPORTED => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_NOT_SUPPORTED : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_NOT_SUPPORTED : MsQuic_Linux.QUIC_STATUS_NOT_SUPPORTED;
        public static int QUIC_STATUS_NOT_FOUND => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_NOT_FOUND : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_NOT_FOUND : MsQuic_Linux.QUIC_STATUS_NOT_FOUND;
        public static int QUIC_STATUS_BUFFER_TOO_SMALL => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_BUFFER_TOO_SMALL : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_BUFFER_TOO_SMALL : MsQuic_Linux.QUIC_STATUS_BUFFER_TOO_SMALL;
        public static int QUIC_STATUS_HANDSHAKE_FAILURE => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_HANDSHAKE_FAILURE : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_HANDSHAKE_FAILURE : MsQuic_Linux.QUIC_STATUS_HANDSHAKE_FAILURE;
        public static int QUIC_STATUS_ABORTED => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_ABORTED : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_ABORTED : MsQuic_Linux.QUIC_STATUS_ABORTED;
        public static int QUIC_STATUS_ADDRESS_IN_USE => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_ADDRESS_IN_USE : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_ADDRESS_IN_USE : MsQuic_Linux.QUIC_STATUS_ADDRESS_IN_USE;
        public static int QUIC_STATUS_CONNECTION_TIMEOUT => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_CONNECTION_TIMEOUT : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_CONNECTION_TIMEOUT : MsQuic_Linux.QUIC_STATUS_CONNECTION_TIMEOUT;
        public static int QUIC_STATUS_CONNECTION_IDLE => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_CONNECTION_IDLE : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_CONNECTION_IDLE : MsQuic_Linux.QUIC_STATUS_CONNECTION_IDLE;
        public static int QUIC_STATUS_UNREACHABLE => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_UNREACHABLE : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_UNREACHABLE : MsQuic_Linux.QUIC_STATUS_UNREACHABLE;
        public static int QUIC_STATUS_INTERNAL_ERROR => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_INTERNAL_ERROR : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_INTERNAL_ERROR : MsQuic_Linux.QUIC_STATUS_INTERNAL_ERROR;
        public static int QUIC_STATUS_CONNECTION_REFUSED => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_CONNECTION_REFUSED : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_CONNECTION_REFUSED : MsQuic_Linux.QUIC_STATUS_CONNECTION_REFUSED;
        public static int QUIC_STATUS_PROTOCOL_ERROR => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_PROTOCOL_ERROR : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_PROTOCOL_ERROR : MsQuic_Linux.QUIC_STATUS_PROTOCOL_ERROR;
        public static int QUIC_STATUS_VER_NEG_ERROR => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_VER_NEG_ERROR : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_VER_NEG_ERROR : MsQuic_Linux.QUIC_STATUS_VER_NEG_ERROR;
        public static int QUIC_STATUS_TLS_ERROR => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_TLS_ERROR : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_TLS_ERROR : MsQuic_Linux.QUIC_STATUS_TLS_ERROR;
        public static int QUIC_STATUS_USER_CANCELED => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_USER_CANCELED : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_USER_CANCELED : MsQuic_Linux.QUIC_STATUS_USER_CANCELED;
        public static int QUIC_STATUS_ALPN_NEG_FAILURE => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_ALPN_NEG_FAILURE : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_ALPN_NEG_FAILURE : MsQuic_Linux.QUIC_STATUS_ALPN_NEG_FAILURE;
        public static int QUIC_STATUS_STREAM_LIMIT_REACHED => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_STREAM_LIMIT_REACHED : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_STREAM_LIMIT_REACHED : MsQuic_Linux.QUIC_STATUS_STREAM_LIMIT_REACHED;
        public static int QUIC_STATUS_CLOSE_NOTIFY => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_CLOSE_NOTIFY : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_CLOSE_NOTIFY : MsQuic_Linux.QUIC_STATUS_CLOSE_NOTIFY;
        public static int QUIC_STATUS_BAD_CERTIFICATE => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_BAD_CERTIFICATE : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_BAD_CERTIFICATE : MsQuic_Linux.QUIC_STATUS_BAD_CERTIFICATE;
        public static int QUIC_STATUS_UNSUPPORTED_CERTIFICATE => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_UNSUPPORTED_CERTIFICATE : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_UNSUPPORTED_CERTIFICATE : MsQuic_Linux.QUIC_STATUS_UNSUPPORTED_CERTIFICATE;
        public static int QUIC_STATUS_REVOKED_CERTIFICATE => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_REVOKED_CERTIFICATE : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_REVOKED_CERTIFICATE : MsQuic_Linux.QUIC_STATUS_REVOKED_CERTIFICATE;
        public static int QUIC_STATUS_EXPIRED_CERTIFICATE => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_EXPIRED_CERTIFICATE : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_EXPIRED_CERTIFICATE : MsQuic_Linux.QUIC_STATUS_EXPIRED_CERTIFICATE;
        public static int QUIC_STATUS_UNKNOWN_CERTIFICATE => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_UNKNOWN_CERTIFICATE : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_UNKNOWN_CERTIFICATE : MsQuic_Linux.QUIC_STATUS_UNKNOWN_CERTIFICATE;
        public static int QUIC_STATUS_CERT_EXPIRED => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_CERT_EXPIRED : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_CERT_EXPIRED : MsQuic_Linux.QUIC_STATUS_CERT_EXPIRED;
        public static int QUIC_STATUS_CERT_UNTRUSTED_ROOT => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_STATUS_CERT_UNTRUSTED_ROOT : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_STATUS_CERT_UNTRUSTED_ROOT : MsQuic_Linux.QUIC_STATUS_CERT_UNTRUSTED_ROOT;

        public static int QUIC_ADDRESS_FAMILY_UNSPEC => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_ADDRESS_FAMILY_UNSPEC : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_ADDRESS_FAMILY_UNSPEC : MsQuic_Linux.QUIC_ADDRESS_FAMILY_UNSPEC;
        public static int QUIC_ADDRESS_FAMILY_INET => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_ADDRESS_FAMILY_INET : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_ADDRESS_FAMILY_INET : MsQuic_Linux.QUIC_ADDRESS_FAMILY_INET;
        public static int QUIC_ADDRESS_FAMILY_INET6 => OperatingSystem.IsWindows() ? MsQuic_Windows.QUIC_ADDRESS_FAMILY_INET6 : (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) ? MsQuic_Linux.QUIC_ADDRESS_FAMILY_INET6 : MsQuic_Linux.QUIC_ADDRESS_FAMILY_INET6;
    }

    /// <summary>Defines the type of a member as it was used in the native signature.</summary>
    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = true)]
    [Conditional("DEBUG")]
    internal sealed class NativeTypeNameAttribute : Attribute
    {
        private readonly string _name;

        /// <summary>Initializes a new instance of the <see cref="NativeTypeNameAttribute" /> class.</summary>
        /// <param name="name">The name of the type that was used in the native signature.</param>
        public NativeTypeNameAttribute(string name)
        {
            _name = name;
        }

        /// <summary>Gets the name of the type that was used in the native signature.</summary>
        public string Name => _name;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct QuicAddrFamilyAndLen
    {
        [FieldOffset(0)]
        public ushort sin_family;
        [FieldOffset(0)]
        public byte sin_len;
        [FieldOffset(1)]
        public byte sin_family_bsd;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct QuicAddrIn
    {
        public QuicAddrFamilyAndLen sin_family;
        public ushort sin_port;
        public fixed byte sin_addr[4];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct QuicAddrIn6
    {
        public QuicAddrFamilyAndLen sin6_family;
        public ushort sin6_port;
        public uint sin6_flowinfo;
        public fixed byte sin6_addr[16];
        public uint sin6_scope_id;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct QuicAddr
    {
        [FieldOffset(0)]
        public QuicAddrIn Ipv4;
        [FieldOffset(0)]
        public QuicAddrIn6 Ipv6;
        [FieldOffset(0)]
        public QuicAddrFamilyAndLen FamilyLen;

        public static bool SockaddrHasLength => OperatingSystem.IsFreeBSD() || OperatingSystem.IsIOS() || OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsTvOS() || OperatingSystem.IsWatchOS();

        public int Family
        {
            get
            {
                if (SockaddrHasLength)
                {
                    return FamilyLen.sin_family_bsd;
                }
                else
                {
                    return FamilyLen.sin_family;
                }
            }
            set
            {
                if (SockaddrHasLength)
                {
                    FamilyLen.sin_family_bsd = (byte)value;
                }
                else
                {
                    FamilyLen.sin_family = (ushort)value;
                }
            }
        }
    }

}
