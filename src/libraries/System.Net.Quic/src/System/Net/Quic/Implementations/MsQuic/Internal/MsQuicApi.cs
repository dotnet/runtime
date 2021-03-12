// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal unsafe sealed class MsQuicApi
    {
        private static MsQuicApi? _api;

        public SafeMsQuicRegistrationHandle Registration { get; }

        // This is workaround for a bug in ILTrimmer.
        // Withough these DynamicDependency attributes, .ctor() will be removed from the safe handles.
        // Remove once fixed: https://github.com/mono/linker/issues/1660
        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(SafeMsQuicRegistrationHandle))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(SafeMsQuicConfigurationHandle))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(SafeMsQuicListenerHandle))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(SafeMsQuicConnectionHandle))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(SafeMsQuicStreamHandle))]
        private unsafe MsQuicApi()
        {
            MsQuicNativeMethods.NativeApi* vtable;
            uint status;

            try
            {
                status = Interop.MsQuic.MsQuicOpen(out vtable);
                if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
                {
                    throw new NotSupportedException(SR.net_quic_notsupported);
                }
            }
            catch (DllNotFoundException)
            {
                throw new NotSupportedException(SR.net_quic_notsupported);
            }

            SetParamDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SetParamDelegate>(
                    vtable->SetParam);

            GetParamDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.GetParamDelegate>(
                    vtable->GetParam);

            SetCallbackHandlerDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SetCallbackHandlerDelegate>(
                    vtable->SetCallbackHandler);

            RegistrationOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.RegistrationOpenDelegate>(
                    vtable->RegistrationOpen);
            RegistrationCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.RegistrationCloseDelegate>(
                    vtable->RegistrationClose);

            ConfigurationOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConfigurationOpenDelegate>(
                    vtable->ConfigurationOpen);
            ConfigurationCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConfigurationCloseDelegate>(
                    vtable->ConfigurationClose);
            ConfigurationLoadCredentialDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConfigurationLoadCredentialDelegate>(
                    vtable->ConfigurationLoadCredential);

            ListenerOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerOpenDelegate>(
                    vtable->ListenerOpen);
            ListenerCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerCloseDelegate>(
                    vtable->ListenerClose);
            ListenerStartDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerStartDelegate>(
                    vtable->ListenerStart);
            ListenerStopDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerStopDelegate>(
                    vtable->ListenerStop);

            ConnectionOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionOpenDelegate>(
                    vtable->ConnectionOpen);
            ConnectionCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionCloseDelegate>(
                    vtable->ConnectionClose);
            ConnectionSetConfigurationDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionSetConfigurationDelegate>(
                    vtable->ConnectionSetConfiguration);
            ConnectionShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionShutdownDelegate>(
                    vtable->ConnectionShutdown);
            ConnectionStartDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionStartDelegate>(
                    vtable->ConnectionStart);

            StreamOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamOpenDelegate>(
                    vtable->StreamOpen);
            StreamCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamCloseDelegate>(
                    vtable->StreamClose);
            StreamStartDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamStartDelegate>(
                    vtable->StreamStart);
            StreamShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamShutdownDelegate>(
                    vtable->StreamShutdown);
            StreamSendDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamSendDelegate>(
                    vtable->StreamSend);
            StreamReceiveCompleteDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamReceiveCompleteDelegate>(
                    vtable->StreamReceiveComplete);
            StreamReceiveSetEnabledDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamReceiveSetEnabledDelegate>(
                    vtable->StreamReceiveSetEnabled);

            byte* appName = stackalloc byte[5];
            appName[0] = (byte)'.';
            appName[1] = (byte)'N';
            appName[2] = (byte)'E';
            appName[3] = (byte)'T';
            appName[4] = 0;

            var cfg = new RegistrationConfig
            {
                AppName = appName,
                ExecutionProfile = (uint)QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_LOW_LATENCY
            };

            status = RegistrationOpenDelegate(ref cfg, out SafeMsQuicRegistrationHandle handle);
            QuicExceptionHelpers.ThrowIfFailed(status, "RegistrationOpen failed.");

            Registration = handle;
        }

        internal static MsQuicApi Api => _api ??= new MsQuicApi();

        internal static bool IsQuicSupported => true;

        //static MsQuicApi()
        //{
        //    // MsQuicOpen will succeed even if the platform will not support it. It will then fail with unspecified
        //    // platform-specific errors in subsequent callbacks. For now, check for the minimum build we've tested it on.

        //    // TODO:
        //    // - Hopefully, MsQuicOpen will perform this check for us and give us a consistent error code.
        //    // - Otherwise, dial this in to reflect actual minimum requirements and add some sort of platform
        //    //   error code mapping when creating exceptions.

        //    // TODO: try to initialize TLS 1.3 in SslStream.

        //    try
        //    {
        //        Api = new MsQuicApi();
        //        IsQuicSupported = true;
        //    }
        //    catch (NotSupportedException)
        //    {
        //        IsQuicSupported = false;
        //    }
        //}

        internal MsQuicNativeMethods.RegistrationOpenDelegate RegistrationOpenDelegate { get; }
        internal MsQuicNativeMethods.RegistrationCloseDelegate RegistrationCloseDelegate { get; }

        internal MsQuicNativeMethods.ConfigurationOpenDelegate ConfigurationOpenDelegate { get; }
        internal MsQuicNativeMethods.ConfigurationCloseDelegate ConfigurationCloseDelegate { get; }
        internal MsQuicNativeMethods.ConfigurationLoadCredentialDelegate ConfigurationLoadCredentialDelegate { get; }

        internal MsQuicNativeMethods.ListenerOpenDelegate ListenerOpenDelegate { get; }
        internal MsQuicNativeMethods.ListenerCloseDelegate ListenerCloseDelegate { get; }
        internal MsQuicNativeMethods.ListenerStartDelegate ListenerStartDelegate { get; }
        internal MsQuicNativeMethods.ListenerStopDelegate ListenerStopDelegate { get; }

        internal MsQuicNativeMethods.ConnectionOpenDelegate ConnectionOpenDelegate { get; }
        internal MsQuicNativeMethods.ConnectionCloseDelegate ConnectionCloseDelegate { get; }
        internal MsQuicNativeMethods.ConnectionSetConfigurationDelegate ConnectionSetConfigurationDelegate { get; }
        internal MsQuicNativeMethods.ConnectionShutdownDelegate ConnectionShutdownDelegate { get; }
        internal MsQuicNativeMethods.ConnectionStartDelegate ConnectionStartDelegate { get; }

        internal MsQuicNativeMethods.StreamOpenDelegate StreamOpenDelegate { get; }
        internal MsQuicNativeMethods.StreamCloseDelegate StreamCloseDelegate { get; }
        internal MsQuicNativeMethods.StreamStartDelegate StreamStartDelegate { get; }
        internal MsQuicNativeMethods.StreamShutdownDelegate StreamShutdownDelegate { get; }
        internal MsQuicNativeMethods.StreamSendDelegate StreamSendDelegate { get; }
        internal MsQuicNativeMethods.StreamReceiveCompleteDelegate StreamReceiveCompleteDelegate { get; }
        internal MsQuicNativeMethods.StreamReceiveSetEnabledDelegate StreamReceiveSetEnabledDelegate { get; }

        internal MsQuicNativeMethods.SetCallbackHandlerDelegate SetCallbackHandlerDelegate { get; }

        internal MsQuicNativeMethods.SetParamDelegate SetParamDelegate { get; }
        internal MsQuicNativeMethods.GetParamDelegate GetParamDelegate { get; }
    }
}
