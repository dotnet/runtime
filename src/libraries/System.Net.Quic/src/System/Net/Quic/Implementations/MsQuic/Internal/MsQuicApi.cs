// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal unsafe sealed class MsQuicApi
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
                Marshal.GetDelegateForFunctionPointer<SetParamDelegate>(
                    vtable->SetParam);

            GetParamDelegate =
                Marshal.GetDelegateForFunctionPointer<GetParamDelegate>(
                    vtable->GetParam);

            SetCallbackHandlerDelegate =
                Marshal.GetDelegateForFunctionPointer<SetCallbackHandlerDelegate>(
                    vtable->SetCallbackHandler);

            RegistrationOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<RegistrationOpenDelegate>(
                    vtable->RegistrationOpen);
            RegistrationCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<RegistrationCloseDelegate>(
                    vtable->RegistrationClose);

            ConfigurationOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<ConfigurationOpenDelegate>(
                    vtable->ConfigurationOpen);
            ConfigurationCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<ConfigurationCloseDelegate>(
                    vtable->ConfigurationClose);
            ConfigurationLoadCredentialDelegate =
                Marshal.GetDelegateForFunctionPointer<ConfigurationLoadCredentialDelegate>(
                    vtable->ConfigurationLoadCredential);

            ListenerOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<ListenerOpenDelegate>(
                    vtable->ListenerOpen);
            ListenerCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<ListenerCloseDelegate>(
                    vtable->ListenerClose);
            ListenerStartDelegate =
                Marshal.GetDelegateForFunctionPointer<ListenerStartDelegate>(
                    vtable->ListenerStart);
            ListenerStopDelegate =
                Marshal.GetDelegateForFunctionPointer<ListenerStopDelegate>(
                    vtable->ListenerStop);

            ConnectionOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<ConnectionOpenDelegate>(
                    vtable->ConnectionOpen);
            ConnectionCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<ConnectionCloseDelegate>(
                    vtable->ConnectionClose);
            ConnectionSetConfigurationDelegate =
                Marshal.GetDelegateForFunctionPointer<ConnectionSetConfigurationDelegate>(
                    vtable->ConnectionSetConfiguration);
            ConnectionShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<ConnectionShutdownDelegate>(
                    vtable->ConnectionShutdown);
            ConnectionStartDelegate =
                Marshal.GetDelegateForFunctionPointer<ConnectionStartDelegate>(
                    vtable->ConnectionStart);

            StreamOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<StreamOpenDelegate>(
                    vtable->StreamOpen);
            StreamCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<StreamCloseDelegate>(
                    vtable->StreamClose);
            StreamStartDelegate =
                Marshal.GetDelegateForFunctionPointer<StreamStartDelegate>(
                    vtable->StreamStart);
            StreamShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<StreamShutdownDelegate>(
                    vtable->StreamShutdown);
            StreamSendDelegate =
                Marshal.GetDelegateForFunctionPointer<StreamSendDelegate>(
                    vtable->StreamSend);
            StreamReceiveCompleteDelegate =
                Marshal.GetDelegateForFunctionPointer<StreamReceiveCompleteDelegate>(
                    vtable->StreamReceiveComplete);
            StreamReceiveSetEnabledDelegate =
                Marshal.GetDelegateForFunctionPointer<StreamReceiveSetEnabledDelegate>(
                    vtable->StreamReceiveSetEnabled);

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

        private const int MsQuicVersion = 1;

        static MsQuicApi()
        {
            if (!IsHttp3Enabled())
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(null, $"HTTP/3 and QUIC is not enabled, see 'System.Net.SocketsHttpHandler.Http3Support' AppContext switch.");
                }

                return;
            }

            if (OperatingSystem.IsWindows() && !IsWindowsVersionSupported())
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(null, $"Current Windows version ({Environment.OSVersion}) is not supported by QUIC. Minimal supported version is {MinWindowsVersion}");
                }

                return;
            }

            if (NativeLibrary.TryLoad(Interop.Libraries.MsQuic, typeof(MsQuicApi).Assembly, DllImportSearchPath.AssemblyDirectory, out IntPtr msQuicHandle))
            {
                try
                {
                    if (NativeLibrary.TryGetExport(msQuicHandle, "MsQuicOpenVersion", out IntPtr msQuicOpenVersionAddress))
                    {
                        delegate* unmanaged[Cdecl]<uint, out NativeApi*, uint> msQuicOpenVersion =
                            (delegate* unmanaged[Cdecl]<uint, out NativeApi*, uint>)msQuicOpenVersionAddress;
                        uint status = msQuicOpenVersion(MsQuicVersion, out NativeApi* vtable);
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

        // Note that this is copy-pasted from S.N.Http just to hide S.N.Quic behind the same AppContext switch
        // since this library is considered "private" for 6.0.
        // We should get rid of this once S.N.Quic API surface is officially exposed.
        private static bool IsHttp3Enabled()
        {
            bool value;

            // First check for the AppContext switch, giving it priority over the environment variable.
            if (AppContext.TryGetSwitch("System.Net.SocketsHttpHandler.Http3Support", out value))
            {
                return value;
            }

            // AppContext switch wasn't used. Check the environment variable.
            string? envVar = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP3SUPPORT");

            if (bool.TryParse(envVar, out value))
            {
                return value;
            }
            else if (uint.TryParse(envVar, out uint intVal))
            {
                return intVal != 0;
            }

            return false;
        }

        private static bool IsWindowsVersionSupported() => OperatingSystem.IsWindowsVersionAtLeast(MinWindowsVersion.Major,
            MinWindowsVersion.Minor, MinWindowsVersion.Build, MinWindowsVersion.Revision);

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
