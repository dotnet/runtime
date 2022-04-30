// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

#if TARGET_WINDOWS
using Microsoft.Win32;
#endif

using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal sealed unsafe class MsQuicApi
    {
        private static readonly Version MinWindowsVersion = new Version(10, 0, 20145, 1000);

        public SafeMsQuicRegistrationHandle Registration { get; }

        // This is workaround for a bug in ILTrimmer.
        // Without these DynamicDependency attributes, .ctor() will be removed from the safe handles.
        // Remove once fixed: https://github.com/mono/linker/issues/1660
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicRegistrationHandle))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicConfigurationHandle))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicListenerHandle))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicConnectionHandle))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicStreamHandle))]
        private MsQuicApi(NativeApi* vtable)
        {
            uint status;

            SetParamDelegate =
                new SetParamDelegate(new DelegateHelper(vtable->SetParam).SetParam);

            GetParamDelegate =
                new GetParamDelegate(new DelegateHelper(vtable->GetParam).GetParam);

            SetCallbackHandlerDelegate =
                new SetCallbackHandlerDelegate(new DelegateHelper(vtable->SetCallbackHandler).SetCallbackHandler);

            RegistrationOpenDelegate =
                new RegistrationOpenDelegate(new DelegateHelper(vtable->RegistrationOpen).RegistrationOpen);
            RegistrationCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<RegistrationCloseDelegate>(
                    vtable->RegistrationClose);

            ConfigurationOpenDelegate =
                new ConfigurationOpenDelegate(new DelegateHelper(vtable->ConfigurationOpen).ConfigurationOpen);
            ConfigurationCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<ConfigurationCloseDelegate>(
                    vtable->ConfigurationClose);
            ConfigurationLoadCredentialDelegate =
                new ConfigurationLoadCredentialDelegate(new DelegateHelper(vtable->ConfigurationLoadCredential).ConfigurationLoadCredential);

            ListenerOpenDelegate =
                new ListenerOpenDelegate(new DelegateHelper(vtable->ListenerOpen).ListenerOpen);
            ListenerCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<ListenerCloseDelegate>(
                    vtable->ListenerClose);
            ListenerStartDelegate =
                new ListenerStartDelegate(new DelegateHelper(vtable->ListenerStart).ListenerStart);
            ListenerStopDelegate =
                new ListenerStopDelegate(new DelegateHelper(vtable->ListenerStop).ListenerStop);

            ConnectionOpenDelegate =
                new ConnectionOpenDelegate(new DelegateHelper(vtable->ConnectionOpen).ConnectionOpen);
            ConnectionCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<ConnectionCloseDelegate>(
                    vtable->ConnectionClose);
            ConnectionSetConfigurationDelegate =
                new ConnectionSetConfigurationDelegate(new DelegateHelper(vtable->ConnectionSetConfiguration).ConnectionSetConfiguration);
            ConnectionShutdownDelegate =
                new ConnectionShutdownDelegate(new DelegateHelper(vtable->ConnectionShutdown).ConnectionShutdown);
            ConnectionStartDelegate =
                new ConnectionStartDelegate(new DelegateHelper(vtable->ConnectionStart).ConnectionStart);

            StreamOpenDelegate =
                new StreamOpenDelegate(new DelegateHelper(vtable->StreamOpen).StreamOpen);
            StreamCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<StreamCloseDelegate>(
                    vtable->StreamClose);
            StreamStartDelegate =
                new StreamStartDelegate(new DelegateHelper(vtable->StreamStart).StreamStart);
            StreamShutdownDelegate =
                new StreamShutdownDelegate(new DelegateHelper(vtable->StreamShutdown).StreamShutdown);
            StreamSendDelegate =
                new StreamSendDelegate(new DelegateHelper(vtable->StreamSend).StreamSend);
            StreamReceiveCompleteDelegate =
                new StreamReceiveCompleteDelegate(new DelegateHelper(vtable->StreamReceiveComplete).StreamReceiveComplete);
            StreamReceiveSetEnabledDelegate =
                new StreamReceiveSetEnabledDelegate(new DelegateHelper(vtable->StreamReceiveSetEnabled).StreamReceiveSetEnabled);

            var cfg = new RegistrationConfig
            {
                AppName = ".NET",
                ExecutionProfile = QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_LOW_LATENCY
            };

            status = RegistrationOpenDelegate(ref cfg, out SafeMsQuicRegistrationHandle handle);
            QuicExceptionHelpers.ThrowIfFailed(status, "RegistrationOpen failed.");

            Registration = handle;
        }

        internal static MsQuicApi Api { get; } = null!;

        internal static bool IsQuicSupported { get; }

        private const int MsQuicVersion = 2;

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
            if (NativeLibrary.TryLoad($"{Interop.Libraries.MsQuic}.{MsQuicVersion}", typeof(MsQuicApi).Assembly, DllImportSearchPath.AssemblyDirectory, out msQuicHandle) ||
                NativeLibrary.TryLoad(Interop.Libraries.MsQuic, typeof(MsQuicApi).Assembly, DllImportSearchPath.AssemblyDirectory, out msQuicHandle))
            {
                try
                {
                    if (NativeLibrary.TryGetExport(msQuicHandle, "MsQuicOpenVersion", out IntPtr msQuicOpenVersionAddress))
                    {
                        NativeApi* vtable;
                        delegate* unmanaged[Cdecl]<uint, NativeApi**, uint> msQuicOpenVersion =
                            (delegate* unmanaged[Cdecl]<uint, NativeApi**, uint>)msQuicOpenVersionAddress;
                        uint status = msQuicOpenVersion(MsQuicVersion, &vtable);
                        if (MsQuicStatusHelper.SuccessfulStatusCode(status))
                        {
                            IsQuicSupported = true;
                            Api = new MsQuicApi(vtable);
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

        // TODO: Consider updating all of these delegates to instead use function pointers.
        internal RegistrationOpenDelegate RegistrationOpenDelegate { get; }
        internal RegistrationCloseDelegate RegistrationCloseDelegate { get; }

        internal ConfigurationOpenDelegate ConfigurationOpenDelegate { get; }
        internal ConfigurationCloseDelegate ConfigurationCloseDelegate { get; }
        internal ConfigurationLoadCredentialDelegate ConfigurationLoadCredentialDelegate { get; }

        internal ListenerOpenDelegate ListenerOpenDelegate { get; }
        internal ListenerCloseDelegate ListenerCloseDelegate { get; }
        internal ListenerStartDelegate ListenerStartDelegate { get; }
        internal ListenerStopDelegate ListenerStopDelegate { get; }

        // TODO: missing SendResumptionTicket
        internal ConnectionOpenDelegate ConnectionOpenDelegate { get; }
        internal ConnectionCloseDelegate ConnectionCloseDelegate { get; }
        internal ConnectionShutdownDelegate ConnectionShutdownDelegate { get; }
        internal ConnectionStartDelegate ConnectionStartDelegate { get; }
        internal ConnectionSetConfigurationDelegate ConnectionSetConfigurationDelegate { get; }

        internal StreamOpenDelegate StreamOpenDelegate { get; }
        internal StreamCloseDelegate StreamCloseDelegate { get; }
        internal StreamStartDelegate StreamStartDelegate { get; }
        internal StreamShutdownDelegate StreamShutdownDelegate { get; }
        internal StreamSendDelegate StreamSendDelegate { get; }
        internal StreamReceiveCompleteDelegate StreamReceiveCompleteDelegate { get; }
        internal StreamReceiveSetEnabledDelegate StreamReceiveSetEnabledDelegate { get; }

        internal SetCallbackHandlerDelegate SetCallbackHandlerDelegate { get; }

        internal SetParamDelegate SetParamDelegate { get; }
        internal GetParamDelegate GetParamDelegate { get; }
    }
}
