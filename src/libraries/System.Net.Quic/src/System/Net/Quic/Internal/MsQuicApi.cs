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

internal sealed unsafe class MsQuicApi
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

    internal static MsQuicApi Api { get; } = null!;

    internal static bool IsQuicSupported { get; }

    internal static bool UsesSChannelBackend { get; }

    internal static bool Tls13ServerMayBeDisabled { get; }
    internal static bool Tls13ClientMayBeDisabled { get; }

    static MsQuicApi()
    {
        IntPtr msQuicHandle;
        if (!NativeLibrary.TryLoad($"{Interop.Libraries.MsQuic}.{MsQuicVersion.Major}", typeof(MsQuicApi).Assembly, DllImportSearchPath.AssemblyDirectory, out msQuicHandle) &&
            !NativeLibrary.TryLoad(Interop.Libraries.MsQuic, typeof(MsQuicApi).Assembly, DllImportSearchPath.AssemblyDirectory, out msQuicHandle))
        {
            return;
        }

        try
        {
            if (!NativeLibrary.TryGetExport(msQuicHandle, "MsQuicOpenVersion", out IntPtr msQuicOpenVersionAddress))
            {
                return;
            }

            QUIC_API_TABLE* apiTable = null;
            delegate* unmanaged[Cdecl]<uint, QUIC_API_TABLE**, int> msQuicOpenVersion = (delegate* unmanaged[Cdecl]<uint, QUIC_API_TABLE**, int>)msQuicOpenVersionAddress;
            if (StatusFailed(msQuicOpenVersion((uint)MsQuicVersion.Major, &apiTable)))
            {
                return;
            }

            try
            {
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

                // Assume SChannel is being used on windows and query for the actual provider from the library
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

                Api = new MsQuicApi(apiTable);
                IsQuicSupported = true;
            }
            finally
            {
                if (!IsQuicSupported && NativeLibrary.TryGetExport(msQuicHandle, "MsQuicClose", out IntPtr msQuicClose))
                {
                    // Gracefully close the API table
                    ((delegate* unmanaged[Cdecl]<QUIC_API_TABLE*, void>)msQuicClose)(apiTable);
                }
            }

        }
        finally
        {
            if (!IsQuicSupported)
            {
                NativeLibrary.Free(msQuicHandle);
            }
        }
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

    public void SetContext(MsQuicSafeHandle handle, void* context)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            ApiTable->SetContext(handle.QuicHandle, context);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public void* GetContext(MsQuicSafeHandle handle)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            return ApiTable->GetContext(handle.QuicHandle);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public void SetCallbackHandler(MsQuicSafeHandle handle, void* callback, void* context)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            ApiTable->SetCallbackHandler(handle.QuicHandle, callback, context);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public int SetParam(MsQuicSafeHandle handle, uint param, uint bufferLength, void* buffer)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            return ApiTable->SetParam(handle.QuicHandle, param, bufferLength, buffer);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public int GetParam(MsQuicSafeHandle handle, uint param, uint* bufferLength, void* buffer)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            return ApiTable->GetParam(handle.QuicHandle, param, bufferLength, buffer);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public void RegistrationShutdown(MsQuicSafeHandle handle, QUIC_CONNECTION_SHUTDOWN_FLAGS flags, ulong code)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            ApiTable->RegistrationShutdown(handle.QuicHandle, flags, code);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public int ConfigurationOpen(MsQuicSafeHandle regHandle, QUIC_BUFFER* alpnBuffers, uint alpnBuffersCount, QUIC_SETTINGS* settings, uint settingsSize, void* context, QUIC_HANDLE** configuration)
    {
        bool success = false;
        try
        {
            regHandle.DangerousAddRef(ref success);
            Debug.Assert(success || regHandle.IsInvalid);
            ObjectDisposedException.ThrowIf(regHandle.IsInvalid, regHandle);
            return ApiTable->ConfigurationOpen(regHandle.QuicHandle, alpnBuffers, alpnBuffersCount, settings, settingsSize, context, configuration);
        }
        finally
        {
            if (success)
            {
                regHandle.DangerousRelease();
            }
        }
    }

    public int ConfigurationLoadCredential(MsQuicSafeHandle handle, QUIC_CREDENTIAL_CONFIG* config)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            return ApiTable->ConfigurationLoadCredential(handle.QuicHandle, config);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public int ListenerOpen(MsQuicSafeHandle handle, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_LISTENER_EVENT*, int> callback, void* context, QUIC_HANDLE** listener)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            return ApiTable->ListenerOpen(handle.QuicHandle, callback, context, listener);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public int ListenerStart(MsQuicSafeHandle handle, QUIC_BUFFER* alpnBuffers, uint alpnBuffersCount, QuicAddr* localAddress)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            return ApiTable->ListenerStart(handle.QuicHandle, alpnBuffers, alpnBuffersCount, localAddress);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public void ListenerStop(MsQuicSafeHandle handle)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            ApiTable->ListenerStop(handle.QuicHandle);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public int ConnectionOpen(MsQuicSafeHandle regHandle, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int> callback, void* context, QUIC_HANDLE** connection)
    {
        bool success = false;
        try
        {
            regHandle.DangerousAddRef(ref success);
            Debug.Assert(success || regHandle.IsInvalid);
            ObjectDisposedException.ThrowIf(regHandle.IsInvalid, regHandle);
            return ApiTable->ConnectionOpen(regHandle.QuicHandle, callback, context, connection);
        }
        finally
        {
            if (success)
            {
                regHandle.DangerousRelease();
            }
        }
    }

    public void ConnectionShutdown(MsQuicSafeHandle handle, QUIC_CONNECTION_SHUTDOWN_FLAGS flags, ulong code)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            ApiTable->ConnectionShutdown(handle.QuicHandle, flags, code);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public int ConnectionStart(MsQuicSafeHandle handle, MsQuicSafeHandle configHandle, ushort family, sbyte* serverName, ushort serverPort)
    {
        bool success = false;
        bool configSuccess = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            configHandle.DangerousAddRef(ref configSuccess);
            Debug.Assert(configSuccess || configHandle.IsInvalid);
            ObjectDisposedException.ThrowIf(configHandle.IsInvalid, configHandle);

            return ApiTable->ConnectionStart(handle.QuicHandle, configHandle.QuicHandle, family, serverName, serverPort);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
            if (configSuccess)
            {
                configHandle.DangerousRelease();
            }
        }
    }

    public int ConnectionSetConfiguration(MsQuicSafeHandle handle, MsQuicSafeHandle configHandle)
    {
        bool success = false;
        bool configSuccess = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            configHandle.DangerousAddRef(ref configSuccess);
            Debug.Assert(configSuccess || configHandle.IsInvalid);
            ObjectDisposedException.ThrowIf(configHandle.IsInvalid, configHandle);

            return ApiTable->ConnectionSetConfiguration(handle.QuicHandle, configHandle.QuicHandle);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
            if (configSuccess)
            {
                configHandle.DangerousRelease();
            }
        }
    }

    public int StreamOpen(MsQuicSafeHandle connHandle, QUIC_STREAM_OPEN_FLAGS flags, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int> callback, void* context, QUIC_HANDLE** stream)
    {
        bool success = false;
        try
        {
            connHandle.DangerousAddRef(ref success);
            Debug.Assert(success || connHandle.IsInvalid);
            ObjectDisposedException.ThrowIf(connHandle.IsInvalid, connHandle);
            return ApiTable->StreamOpen(connHandle.QuicHandle, flags, callback, context, stream);
        }
        finally
        {
            if (success)
            {
                connHandle.DangerousRelease();
            }
        }
    }

    public int StreamStart(MsQuicSafeHandle handle, QUIC_STREAM_START_FLAGS flags)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            return ApiTable->StreamStart(handle.QuicHandle, flags);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public int StreamShutdown(MsQuicSafeHandle handle, QUIC_STREAM_SHUTDOWN_FLAGS flags, ulong code)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            return ApiTable->StreamShutdown(handle.QuicHandle, flags, code);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public int StreamSend(MsQuicSafeHandle handle, QUIC_BUFFER* buffers, uint buffersCount, QUIC_SEND_FLAGS flags, void* context)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            return ApiTable->StreamSend(handle.QuicHandle, buffers, buffersCount, flags, context);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public void StreamReceiveComplete(MsQuicSafeHandle handle, ulong length)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            ApiTable->StreamReceiveComplete(handle.QuicHandle, length);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public int StreamReceiveSetEnabled(MsQuicSafeHandle handle, byte enabled)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success || handle.IsInvalid);
            ObjectDisposedException.ThrowIf(handle.IsInvalid, handle);
            return ApiTable->StreamReceiveSetEnabled(handle.QuicHandle, enabled);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }
}
