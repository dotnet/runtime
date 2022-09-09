// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Quic;

using static Microsoft.Quic.MsQuic;

#if TARGET_WINDOWS
using Microsoft.Win32;
#endif

namespace System.Net.Quic;

internal sealed unsafe partial class MsQuicApi
{
    private static readonly Version MinWindowsVersion = new Version(10, 0, 20145, 1000);

    private static readonly Version MsQuicVersion = new Version(2, 1);

    public MsQuicSafeHandle Registration { get; }

    public QUIC_API_TABLE* ApiTable { get; }

    // This is workaround for a bug in ILTrimmer.
    // Without these DynamicDependency attributes, .ctor() will be removed from the safe handles.
    // Remove once fixed: https://github.com/mono/linker/issues/1660
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(MsQuicSafeHandle))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(MsQuicContextSafeHandle))]
    private MsQuicApi(QUIC_API_TABLE* apiTable)
    {
        ApiTable = apiTable;

        fixed (byte* pAppName = "System.Net.Quic"u8)
        {
            var cfg = new QUIC_REGISTRATION_CONFIG
            {
                AppName = (sbyte*)pAppName,
                ExecutionProfile = QUIC_EXECUTION_PROFILE.LOW_LATENCY
            };

            QUIC_HANDLE* handle;
            ThrowHelper.ThrowIfMsQuicError(ApiTable->RegistrationOpen(&cfg, &handle), "RegistrationOpen failed");

            Registration = new MsQuicSafeHandle(handle, apiTable->RegistrationClose, SafeHandleType.Registration);
        }
    }

    private static readonly Lazy<MsQuicApi> _api = new Lazy<MsQuicApi>(AllocateMsQuicApi);
    internal static MsQuicApi Api => _api.Value;

    internal static bool IsQuicSupported { get; }

    internal static bool UsesSChannelBackend { get; }

    internal static bool Tls13ServerMayBeDisabled { get; }
    internal static bool Tls13ClientMayBeDisabled { get; }

    static MsQuicApi()
    {
        if (!TryLoadMsQuic(out IntPtr msQuicHandle))
        {
            return;
        }

        try
        {
            if (!TryOpenMsQuic(msQuicHandle, out QUIC_API_TABLE* apiTable, out _))
            {
                return;
            }

            try
            {
                // Check version
                int arraySize = 4;
                uint* libVersion = stackalloc uint[arraySize];
                uint size = (uint)arraySize * sizeof(uint);
                if (StatusFailed(apiTable->GetParam(null, QUIC_PARAM_GLOBAL_LIBRARY_VERSION, &size, libVersion)))
                {
                    return;
                }

                var version = new Version((int)libVersion[0], (int)libVersion[1], (int)libVersion[2], (int)libVersion[3]);
                if (version < MsQuicVersion)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Info(null, $"Incompatible MsQuic library version '{version}', expecting '{MsQuicVersion}'");
                    }
                    return;
                }

                // Assume SChannel is being used on windows and query for the actual provider from the library if querying is supported
                QUIC_TLS_PROVIDER provider = OperatingSystem.IsWindows() ? QUIC_TLS_PROVIDER.SCHANNEL : QUIC_TLS_PROVIDER.OPENSSL;
                size = sizeof(QUIC_TLS_PROVIDER);
                apiTable->GetParam(null, QUIC_PARAM_GLOBAL_TLS_PROVIDER, &size, &provider);
                UsesSChannelBackend = provider == QUIC_TLS_PROVIDER.SCHANNEL;

                if (UsesSChannelBackend)
                {
                    // Implies windows platform, check TLS1.3 availability
                    if (!IsWindowsVersionSupported())
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Info(null, $"Current Windows version ({Environment.OSVersion}) is not supported by QUIC. Minimal supported version is {MinWindowsVersion}");
                        }

                        return;
                    }

                    Tls13ServerMayBeDisabled = IsTls13Disabled(isServer: true);
                    Tls13ClientMayBeDisabled = IsTls13Disabled(isServer: false);
                }

                IsQuicSupported = true;
            }
            finally
            {
                // Gracefully close the API table to free resources. The API table will be allocated lazily again if needed
                bool closed = TryCloseMsQuic(msQuicHandle, apiTable);
                Debug.Assert(closed, "Failed to close MsQuic");
            }
        }
        finally
        {
            // Unload the library, we will load it again when we actually use QUIC
            NativeLibrary.Free(msQuicHandle);
        }
    }

    private static MsQuicApi AllocateMsQuicApi()
    {
        Debug.Assert(IsQuicSupported);

        int openStatus = MsQuic.QUIC_STATUS_INTERNAL_ERROR;

        if (TryLoadMsQuic(out IntPtr msQuicHandle) &&
            TryOpenMsQuic(msQuicHandle, out QUIC_API_TABLE* apiTable, out openStatus))
        {
            return new MsQuicApi(apiTable);
        }

        ThrowHelper.ThrowIfMsQuicError(openStatus);

        // this should unreachable as TryOpenMsQuic returns non-success status on failure
        throw new Exception("Failed to create MsQuicApi instance");
    }

    private static bool TryLoadMsQuic(out IntPtr msQuicHandle) =>
        NativeLibrary.TryLoad($"{Interop.Libraries.MsQuic}.{MsQuicVersion.Major}", typeof(MsQuicApi).Assembly, DllImportSearchPath.AssemblyDirectory, out msQuicHandle) ||
        NativeLibrary.TryLoad(Interop.Libraries.MsQuic, typeof(MsQuicApi).Assembly, DllImportSearchPath.AssemblyDirectory, out msQuicHandle);

    private static bool TryOpenMsQuic(IntPtr msQuicHandle, out QUIC_API_TABLE* apiTable, out int openStatus)
    {
        apiTable = null;
        if (!NativeLibrary.TryGetExport(msQuicHandle, "MsQuicOpenVersion", out IntPtr msQuicOpenVersionAddress))
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(null, "Failed to get MsQuicOpenVersion export in msquic library.");
            }

            openStatus = MsQuic.QUIC_STATUS_NOT_FOUND;
            return false;
        }

        QUIC_API_TABLE* table = null;
        delegate* unmanaged[Cdecl]<uint, QUIC_API_TABLE**, int> msQuicOpenVersion = (delegate* unmanaged[Cdecl]<uint, QUIC_API_TABLE**, int>)msQuicOpenVersionAddress;
        openStatus = msQuicOpenVersion((uint)MsQuicVersion.Major, &table);
        if (StatusFailed(openStatus))
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(null, $"MsQuicOpenVersion returned {openStatus} status code.");
            }

            return false;
        }

        apiTable = table;
        return true;
    }

    private static bool TryCloseMsQuic(IntPtr msQuicHandle, QUIC_API_TABLE* apiTable)
    {
        if (NativeLibrary.TryGetExport(msQuicHandle, "MsQuicClose", out IntPtr msQuicClose))
        {
            ((delegate* unmanaged[Cdecl]<QUIC_API_TABLE*, void>)msQuicClose)(apiTable);
            return true;
        }

        return false;
    }

    private static bool IsWindowsVersionSupported() => OperatingSystem.IsWindowsVersionAtLeast(MinWindowsVersion.Major,
        MinWindowsVersion.Minor, MinWindowsVersion.Build, MinWindowsVersion.Revision);

    private static bool IsTls13Disabled(bool isServer)
    {
#if TARGET_WINDOWS
        string SChannelTls13RegistryKey = isServer
            ? @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3\Server"
            : @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3\Client";

        using var regKey = Registry.LocalMachine.OpenSubKey(SChannelTls13RegistryKey);

        if (regKey is null)
        {
            return false;
        }

        if (regKey.GetValue("Enabled") is int enabled && enabled == 0)
        {
            return true;
        }

        if (regKey.GetValue("DisabledByDefault") is int disabled && disabled == 1)
        {
            return true;
        }
#endif
        return false;
    }
}
