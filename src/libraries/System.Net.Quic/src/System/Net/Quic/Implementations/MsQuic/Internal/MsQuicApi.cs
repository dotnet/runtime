// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal class MsQuicApi : IDisposable
    {
        private bool _disposed;

        private readonly IntPtr _registrationContext;

        private unsafe MsQuicApi()
        {
            MsQuicStatusException.ThrowIfFailed(Interop.MsQuic.MsQuicOpen(version: 1, out MsQuicNativeMethods.NativeApi* registration));

            MsQuicNativeMethods.NativeApi nativeRegistration = *registration;

            RegistrationOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.RegistrationOpenDelegate>(
                    nativeRegistration.RegistrationOpen);
            RegistrationCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.RegistrationCloseDelegate>(
                    nativeRegistration.RegistrationClose);

            SecConfigCreateDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SecConfigCreateDelegate>(
                    nativeRegistration.SecConfigCreate);
            SecConfigDeleteDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SecConfigDeleteDelegate>(
                    nativeRegistration.SecConfigDelete);
            SessionOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SessionOpenDelegate>(
                    nativeRegistration.SessionOpen);
            SessionCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SessionCloseDelegate>(
                    nativeRegistration.SessionClose);
            SessionShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SessionShutdownDelegate>(
                    nativeRegistration.SessionShutdown);

            ListenerOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerOpenDelegate>(
                    nativeRegistration.ListenerOpen);
            ListenerCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerCloseDelegate>(
                    nativeRegistration.ListenerClose);
            ListenerStartDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerStartDelegate>(
                    nativeRegistration.ListenerStart);
            ListenerStopDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerStopDelegate>(
                    nativeRegistration.ListenerStop);

            ConnectionOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionOpenDelegate>(
                    nativeRegistration.ConnectionOpen);
            ConnectionCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionCloseDelegate>(
                    nativeRegistration.ConnectionClose);
            ConnectionShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionShutdownDelegate>(
                    nativeRegistration.ConnectionShutdown);
            ConnectionStartDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionStartDelegate>(
                    nativeRegistration.ConnectionStart);

            StreamOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamOpenDelegate>(
                    nativeRegistration.StreamOpen);
            StreamCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamCloseDelegate>(
                    nativeRegistration.StreamClose);
            StreamStartDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamStartDelegate>(
                    nativeRegistration.StreamStart);
            StreamShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamShutdownDelegate>(
                    nativeRegistration.StreamShutdown);
            StreamSendDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamSendDelegate>(
                    nativeRegistration.StreamSend);
            StreamReceiveCompleteDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamReceiveCompleteDelegate>(
                    nativeRegistration.StreamReceiveComplete);
            StreamReceiveSetEnabledDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamReceiveSetEnabledDelegate>(
                    nativeRegistration.StreamReceiveSetEnabled);
            SetContextDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SetContextDelegate>(
                    nativeRegistration.SetContext);
            GetContextDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.GetContextDelegate>(
                    nativeRegistration.GetContext);
            SetCallbackHandlerDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SetCallbackHandlerDelegate>(
                    nativeRegistration.SetCallbackHandler);

            SetParamDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SetParamDelegate>(
                    nativeRegistration.SetParam);
            GetParamDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.GetParamDelegate>(
                    nativeRegistration.GetParam);

            RegistrationOpenDelegate(Encoding.UTF8.GetBytes("SystemNetQuic"), out IntPtr ctx);
            _registrationContext = ctx;
        }

        internal static MsQuicApi Api { get; } = new MsQuicApi();

        internal MsQuicNativeMethods.RegistrationOpenDelegate RegistrationOpenDelegate { get; }
        internal MsQuicNativeMethods.RegistrationCloseDelegate RegistrationCloseDelegate { get; }

        internal MsQuicNativeMethods.SecConfigCreateDelegate SecConfigCreateDelegate { get; }
        internal MsQuicNativeMethods.SecConfigDeleteDelegate SecConfigDeleteDelegate { get; }

        internal MsQuicNativeMethods.SessionOpenDelegate SessionOpenDelegate { get; }
        internal MsQuicNativeMethods.SessionCloseDelegate SessionCloseDelegate { get; }
        internal MsQuicNativeMethods.SessionShutdownDelegate SessionShutdownDelegate { get; }

        internal MsQuicNativeMethods.ListenerOpenDelegate ListenerOpenDelegate { get; }
        internal MsQuicNativeMethods.ListenerCloseDelegate ListenerCloseDelegate { get; }
        internal MsQuicNativeMethods.ListenerStartDelegate ListenerStartDelegate { get; }
        internal MsQuicNativeMethods.ListenerStopDelegate ListenerStopDelegate { get; }

        internal MsQuicNativeMethods.ConnectionOpenDelegate ConnectionOpenDelegate { get; }
        internal MsQuicNativeMethods.ConnectionCloseDelegate ConnectionCloseDelegate { get; }
        internal MsQuicNativeMethods.ConnectionShutdownDelegate ConnectionShutdownDelegate { get; }
        internal MsQuicNativeMethods.ConnectionStartDelegate ConnectionStartDelegate { get; }

        internal MsQuicNativeMethods.StreamOpenDelegate StreamOpenDelegate { get; }
        internal MsQuicNativeMethods.StreamCloseDelegate StreamCloseDelegate { get; }
        internal MsQuicNativeMethods.StreamStartDelegate StreamStartDelegate { get; }
        internal MsQuicNativeMethods.StreamShutdownDelegate StreamShutdownDelegate { get; }
        internal MsQuicNativeMethods.StreamSendDelegate StreamSendDelegate { get; }
        internal MsQuicNativeMethods.StreamReceiveCompleteDelegate StreamReceiveCompleteDelegate { get; }
        internal MsQuicNativeMethods.StreamReceiveSetEnabledDelegate StreamReceiveSetEnabledDelegate { get; }

        internal MsQuicNativeMethods.SetContextDelegate SetContextDelegate { get; }
        internal MsQuicNativeMethods.GetContextDelegate GetContextDelegate { get; }
        internal MsQuicNativeMethods.SetCallbackHandlerDelegate SetCallbackHandlerDelegate { get; }

        internal MsQuicNativeMethods.SetParamDelegate SetParamDelegate { get; }
        internal MsQuicNativeMethods.GetParamDelegate GetParamDelegate { get; }

        internal unsafe uint UnsafeSetParam(
            IntPtr Handle,
            uint Level,
            uint Param,
            MsQuicNativeMethods.QuicBuffer Buffer)
        {
            return SetParamDelegate(
                Handle,
                Level,
                Param,
                Buffer.Length,
                Buffer.Buffer);
        }

        internal unsafe uint UnsafeGetParam(
            IntPtr Handle,
            uint Level,
            uint Param,
            ref MsQuicNativeMethods.QuicBuffer Buffer)
        {
            uint bufferLength = Buffer.Length;
            byte* buf = Buffer.Buffer;
            return GetParamDelegate(
                Handle,
                Level,
                Param,
                &bufferLength,
                buf);
        }

        public async ValueTask<MsQuicSecurityConfig> CreateSecurityConfig(X509Certificate certificate)
        {
            MsQuicSecurityConfig secConfig = null;
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            uint secConfigCreateStatus = MsQuicStatusCodes.InternalError;
            uint status;
            if (certificate != null)
            {
                status = SecConfigCreateDelegate(
                    _registrationContext,
                    (uint)QUIC_SEC_CONFIG_FLAG.CERT_CONTEXT,
                    certificate.Handle,
                    null,
                    IntPtr.Zero,
                    SecCfgCreateCallbackHandler);
            }
            else
            {
                status = SecConfigCreateDelegate(
                    _registrationContext,
                    (uint)QUIC_SEC_CONFIG_FLAG.CERT_NULL,
                    IntPtr.Zero,
                    null,
                    IntPtr.Zero,
                    SecCfgCreateCallbackHandler);
            }

            MsQuicStatusException.ThrowIfFailed(status);

            void SecCfgCreateCallbackHandler(
                IntPtr context,
                uint status,
                IntPtr securityConfig)
            {
                secConfig = new MsQuicSecurityConfig(this, securityConfig);
                secConfigCreateStatus = status;
                tcs.SetResult(null);
            }

            await tcs.Task.ConfigureAwait(false);

            MsQuicStatusException.ThrowIfFailed(secConfigCreateStatus);

            return secConfig;
        }

        public IntPtr SessionOpen(byte[] alpn)
        {
            IntPtr sessionPtr = IntPtr.Zero;

            uint status = SessionOpenDelegate(
                _registrationContext,
                alpn,
                IntPtr.Zero,
                ref sessionPtr);
            MsQuicStatusException.ThrowIfFailed(status);

            return sessionPtr;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~MsQuicApi()
        {
            Dispose(disposing: false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            RegistrationCloseDelegate?.Invoke(_registrationContext);

            _disposed = true;
        }
    }
}
