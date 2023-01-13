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

    private static readonly Version MinMsQuicVersion = new Version(2, 1);

    private static readonly delegate* unmanaged[Cdecl]<uint, QUIC_API_TABLE**, int> MsQuicOpenVersion;
    private static readonly delegate* unmanaged[Cdecl]<QUIC_API_TABLE*, void> MsQuicClose;

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

    internal static string MsQuicLibraryVersion { get; } = "unknown";

    internal static bool UsesSChannelBackend { get; }

    internal static bool Tls13ServerMayBeDisabled { get; }
    internal static bool Tls13ClientMayBeDisabled { get; }

#pragma warning disable CA1810 // Initialize all static fields in 'MsQuicApi' when those fields are declared and remove the explicit static constructor
    static MsQuicApi()
    {
        if (!NativeLibrary.TryLoad($"{Interop.Libraries.MsQuic}.{MinMsQuicVersion.Major}", typeof(MsQuicApi).Assembly, DllImportSearchPath.AssemblyDirectory, out IntPtr msQuicHandle) &&
            !NativeLibrary.TryLoad(Interop.Libraries.MsQuic, typeof(MsQuicApi).Assembly, DllImportSearchPath.AssemblyDirectory, out msQuicHandle))
        {
            // MsQuic library not loaded
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(null, $"Unable to load MsQuic library version '{MinMsQuicVersion.Major}'.");
            }
            return;
        }

        MsQuicOpenVersion = (delegate* unmanaged[Cdecl]<uint, QUIC_API_TABLE**, int>)NativeLibrary.GetExport(msQuicHandle, nameof(MsQuicOpenVersion));
        MsQuicClose = (delegate* unmanaged[Cdecl]<QUIC_API_TABLE*, void>)NativeLibrary.GetExport(msQuicHandle, nameof(MsQuicClose));

        if (!TryOpenMsQuic(out QUIC_API_TABLE* apiTable, out _))
        {
            // Too low version of the library (likely pre-2.0)
            return;
        }

        try
        {
            // Check version
            int arraySize;
            uint paramSize;
            int status;

            arraySize = 4;
            paramSize = (uint)arraySize * sizeof(uint);
            uint* libVersion = stackalloc uint[arraySize];
            status = apiTable->GetParam(null, QUIC_PARAM_GLOBAL_LIBRARY_VERSION, &paramSize, libVersion);
            if (StatusFailed(status))
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(null, $"Cannot retrieve {nameof(QUIC_PARAM_GLOBAL_LIBRARY_VERSION)} from MsQuic library: '{status}'.");
                }
                return;
            }
            Version version = new Version((int)libVersion[0], (int)libVersion[1], (int)libVersion[2], (int)libVersion[3]);

            arraySize = 64;
            paramSize = (uint)arraySize * sizeof(sbyte);
            sbyte* libGitHash = stackalloc sbyte[arraySize];
            status = apiTable->GetParam(null, QUIC_PARAM_GLOBAL_LIBRARY_GIT_HASH, &paramSize, libGitHash);
            if (StatusFailed(status))
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(null, $"Cannot retrieve {nameof(QUIC_PARAM_GLOBAL_LIBRARY_GIT_HASH)} from MsQuic library: '{status}'.");
                }
                return;
            }
            string? gitHash = Marshal.PtrToStringUTF8((IntPtr)libGitHash);

            MsQuicLibraryVersion = $"{Interop.Libraries.MsQuic} version={version} commit={gitHash}";

            if (version < MinMsQuicVersion)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(null, $"Incompatible MsQuic library version '{version}', expecting higher than '{MinMsQuicVersion}'.");
                }
                return;
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(null, $"Loaded MsQuic library version '{version}', commit '{gitHash}'.");
            }

            // Assume SChannel is being used on windows and query for the actual provider from the library if querying is supported
            QUIC_TLS_PROVIDER provider = OperatingSystem.IsWindows() ? QUIC_TLS_PROVIDER.SCHANNEL : QUIC_TLS_PROVIDER.OPENSSL;
            paramSize = sizeof(QUIC_TLS_PROVIDER);
            apiTable->GetParam(null, QUIC_PARAM_GLOBAL_TLS_PROVIDER, &paramSize, &provider);
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
            MsQuicClose(apiTable);
        }
    }
#pragma warning restore CA1810

    private static MsQuicApi AllocateMsQuicApi()
    {
        Debug.Assert(IsQuicSupported);

        if (!TryOpenMsQuic(out QUIC_API_TABLE* apiTable, out int openStatus))
        {
            throw ThrowHelper.GetExceptionForMsQuicStatus(openStatus);
        }

        return new MsQuicApi(apiTable);
    }

    private static bool TryOpenMsQuic(out QUIC_API_TABLE* apiTable, out int openStatus)
    {
        Debug.Assert(MsQuicOpenVersion != null);

        QUIC_API_TABLE* table = null;
        openStatus = MsQuicOpenVersion((uint)MinMsQuicVersion.Major, &table);
        if (StatusFailed(openStatus))
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(null, $"MsQuicOpenVersion for version {MinMsQuicVersion.Major} returned {openStatus} status code.");
            }

            apiTable = null;
            return false;
        }

        apiTable = table;
        return true;
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
