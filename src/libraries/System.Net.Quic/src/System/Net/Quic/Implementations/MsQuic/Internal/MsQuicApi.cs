// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Quic;

using static Microsoft.Quic.MsQuic;

#if TARGET_WINDOWS
using Microsoft.Win32;
#endif

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal sealed unsafe class MsQuicApi
    {
        private static readonly Version MinWindowsVersion = new Version(10, 0, 20145, 1000);

        private static readonly Version MsQuicVersion = new Version(2, 0);

        public SafeMsQuicRegistrationHandle Registration { get; }

        public QUIC_API_TABLE* ApiTable { get; }

        // This is workaround for a bug in ILTrimmer.
        // Without these DynamicDependency attributes, .ctor() will be removed from the safe handles.
        // Remove once fixed: https://github.com/mono/linker/issues/1660
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicRegistrationHandle))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicConfigurationHandle))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicListenerHandle))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicConnectionHandle))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicStreamHandle))]
        private MsQuicApi(QUIC_API_TABLE* apiTable)
        {
            ApiTable = apiTable;

            fixed (byte* pAppName = "System.Net.Quic"u8)
            {
                var cfg = new QUIC_REGISTRATION_CONFIG {
                    AppName = (sbyte*)pAppName,
                    ExecutionProfile = QUIC_EXECUTION_PROFILE.LOW_LATENCY
                };

                QUIC_HANDLE* handle;
                ThrowIfFailure(ApiTable->RegistrationOpen(&cfg, &handle), "RegistrationOpen failed");

                Registration = new SafeMsQuicRegistrationHandle(handle);
            }
        }

        internal static MsQuicApi Api { get; } = null!;

        internal static bool IsQuicSupported { get; }

        internal static bool Tls13MayBeDisabled { get; }

        static MsQuicApi()
        {
            if (OperatingSystem.IsWindows())
            {
                if (!IsWindowsVersionSupported())
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Info(null, $"Current Windows version ({Environment.OSVersion}) is not supported by QUIC. Minimal supported version is {MinWindowsVersion}");
                    }

                    return;
                }

                Tls13MayBeDisabled = IsTls13Disabled();
            }

            IntPtr msQuicHandle;
            if (NativeLibrary.TryLoad($"{Interop.Libraries.MsQuic}.{MsQuicVersion.Major}", typeof(MsQuicApi).Assembly, DllImportSearchPath.AssemblyDirectory, out msQuicHandle) ||
                NativeLibrary.TryLoad(Interop.Libraries.MsQuic, typeof(MsQuicApi).Assembly, DllImportSearchPath.AssemblyDirectory, out msQuicHandle))
            {
                try
                {
                    if (NativeLibrary.TryGetExport(msQuicHandle, "MsQuicOpenVersion", out IntPtr msQuicOpenVersionAddress))
                    {
                        QUIC_API_TABLE* apiTable;
                        delegate* unmanaged[Cdecl]<uint, QUIC_API_TABLE**, int> msQuicOpenVersion = (delegate* unmanaged[Cdecl]<uint, QUIC_API_TABLE**, int>)msQuicOpenVersionAddress;
                        if (StatusSucceeded(msQuicOpenVersion((uint)MsQuicVersion.Major, &apiTable)))
                        {
                            int arraySize = 4;
                            uint* libVersion = stackalloc uint[arraySize];
                            uint size = (uint)arraySize * sizeof(uint);
                            if (StatusSucceeded(apiTable->GetParam(null, QUIC_PARAM_GLOBAL_LIBRARY_VERSION, &size, libVersion)))
                            {
                                var version = new Version((int)libVersion[0], (int)libVersion[1], (int)libVersion[2], (int)libVersion[3]);
                                if (version >= MsQuicVersion)
                                {
                                    Api = new MsQuicApi(apiTable);
                                    IsQuicSupported = true;
                                }
                                else
                                {
                                    if (NetEventSource.Log.IsEnabled())
                                    {
                                        NetEventSource.Info(null, $"Incompatible MsQuic library version '{version}', expecting '{MsQuicVersion}'");
                                    }
                                }
                            }
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
        }

        private static bool IsWindowsVersionSupported() => OperatingSystem.IsWindowsVersionAtLeast(MinWindowsVersion.Major,
            MinWindowsVersion.Minor, MinWindowsVersion.Build, MinWindowsVersion.Revision);

        private static bool IsTls13Disabled()
        {
#if TARGET_WINDOWS
            string[] SChannelTLS13RegKeys = {
                @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3\Client",
                @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3\Server"
            };

            foreach (var key in SChannelTLS13RegKeys)
            {
                using var regKey = Registry.LocalMachine.OpenSubKey(key);

                if (regKey is null) return false;

                if (regKey.GetValue("Enabled") is int enabled && enabled == 0)
                {
                    return true;
                }
            }
#endif
            return false;
        }
    }
}
