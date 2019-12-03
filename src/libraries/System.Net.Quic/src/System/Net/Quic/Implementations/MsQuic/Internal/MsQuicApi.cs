// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal class MsQuicApi : IDisposable
    {
        private bool _disposed;

        private IntPtr _registrationContext;

        internal unsafe MsQuicApi()
        {
            uint status = (uint)Interop.MsQuic.MsQuicOpen(version: 1, out MsQuicNativeMethods.NativeApi* registration);
            MsQuicStatusException.ThrowIfFailed(status);

            MsQuicNativeMethods.NativeApi nativeRegistration = *registration;

            _registrationOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.RegistrationOpenDelegate>(
                    nativeRegistration.RegistrationOpen);
            _registrationCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.RegistrationCloseDelegate>(
                    nativeRegistration.RegistrationClose);

            _secConfigCreateDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SecConfigCreateDelegate>(
                    nativeRegistration.SecConfigCreate);
            _secConfigDeleteDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SecConfigDeleteDelegate>(
                    nativeRegistration.SecConfigDelete);
            _sessionOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SessionOpenDelegate>(
                    nativeRegistration.SessionOpen);
            _sessionCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SessionCloseDelegate>(
                    nativeRegistration.SessionClose);
            _sessionShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SessionShutdownDelegate>(
                    nativeRegistration.SessionShutdown);

            _listenerOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerOpenDelegate>(
                    nativeRegistration.ListenerOpen);
            _listenerCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerCloseDelegate>(
                    nativeRegistration.ListenerClose);
            _listenerStartDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerStartDelegate>(
                    nativeRegistration.ListenerStart);
            _listenerStopDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerStopDelegate>(
                    nativeRegistration.ListenerStop);

            _connectionOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionOpenDelegate>(
                    nativeRegistration.ConnectionOpen);
            _connectionCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionCloseDelegate>(
                    nativeRegistration.ConnectionClose);
            _connectionShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionShutdownDelegate>(
                    nativeRegistration.ConnectionShutdown);
            _connectionStartDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionStartDelegate>(
                    nativeRegistration.ConnectionStart);

            _streamOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamOpenDelegate>(
                    nativeRegistration.StreamOpen);
            _streamCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamCloseDelegate>(
                    nativeRegistration.StreamClose);
            _streamStartDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamStartDelegate>(
                    nativeRegistration.StreamStart);
            _streamShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamShutdownDelegate>(
                    nativeRegistration.StreamShutdown);
            _streamSendDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamSendDelegate>(
                    nativeRegistration.StreamSend);
            _streamReceiveCompleteDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamReceiveCompleteDelegate>(
                    nativeRegistration.StreamReceiveComplete);
            _streamReceiveSetEnabledDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamReceiveSetEnabledDelegate>(
                    nativeRegistration.StreamReceiveSetEnabled);
            _setContextDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SetContextDelegate>(
                    nativeRegistration.SetContext);
            _getContextDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.GetContextDelegate>(
                    nativeRegistration.GetContext);
            _setCallbackHandlerDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SetCallbackHandlerDelegate>(
                    nativeRegistration.SetCallbackHandler);

            SetParamDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SetParamDelegate>(
                    nativeRegistration.SetParam);
            GetParamDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.GetParamDelegate>(
                    nativeRegistration.GetParam);
        }

        internal readonly MsQuicNativeMethods.RegistrationOpenDelegate _registrationOpenDelegate;
        internal readonly MsQuicNativeMethods.RegistrationCloseDelegate _registrationCloseDelegate;

        internal readonly MsQuicNativeMethods.SecConfigCreateDelegate _secConfigCreateDelegate;
        internal readonly MsQuicNativeMethods.SecConfigDeleteDelegate _secConfigDeleteDelegate;

        internal readonly MsQuicNativeMethods.SessionOpenDelegate _sessionOpenDelegate;
        internal readonly MsQuicNativeMethods.SessionCloseDelegate _sessionCloseDelegate;
        internal readonly MsQuicNativeMethods.SessionShutdownDelegate _sessionShutdownDelegate;

        internal readonly MsQuicNativeMethods.ListenerOpenDelegate _listenerOpenDelegate;
        internal readonly MsQuicNativeMethods.ListenerCloseDelegate _listenerCloseDelegate;
        internal readonly MsQuicNativeMethods.ListenerStartDelegate _listenerStartDelegate;
        internal readonly MsQuicNativeMethods.ListenerStopDelegate _listenerStopDelegate;

        internal readonly MsQuicNativeMethods.ConnectionOpenDelegate _connectionOpenDelegate;
        internal readonly MsQuicNativeMethods.ConnectionCloseDelegate _connectionCloseDelegate;
        internal readonly MsQuicNativeMethods.ConnectionShutdownDelegate _connectionShutdownDelegate;
        internal readonly MsQuicNativeMethods.ConnectionStartDelegate _connectionStartDelegate;

        internal readonly MsQuicNativeMethods.StreamOpenDelegate _streamOpenDelegate;
        internal readonly MsQuicNativeMethods.StreamCloseDelegate _streamCloseDelegate;
        internal readonly MsQuicNativeMethods.StreamStartDelegate _streamStartDelegate;
        internal readonly MsQuicNativeMethods.StreamShutdownDelegate _streamShutdownDelegate;
        internal readonly MsQuicNativeMethods.StreamSendDelegate _streamSendDelegate;
        internal readonly MsQuicNativeMethods.StreamReceiveCompleteDelegate _streamReceiveCompleteDelegate;
        internal readonly MsQuicNativeMethods.StreamReceiveSetEnabledDelegate _streamReceiveSetEnabledDelegate;

        internal readonly MsQuicNativeMethods.SetContextDelegate _setContextDelegate;
        internal readonly MsQuicNativeMethods.GetContextDelegate _getContextDelegate;
        internal readonly MsQuicNativeMethods.SetCallbackHandlerDelegate _setCallbackHandlerDelegate;

        internal readonly MsQuicNativeMethods.SetParamDelegate SetParamDelegate;
        internal readonly MsQuicNativeMethods.GetParamDelegate GetParamDelegate;

        internal void RegistrationOpen(byte[] name)
        {
            MsQuicStatusException.ThrowIfFailed(_registrationOpenDelegate(name, out IntPtr ctx));
            _registrationContext = ctx;
        }

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
            var tcs = new TaskCompletionSource<object>();
            uint secConfigCreateStatus = MsQuicConstants.InternalError;

            uint status = _secConfigCreateDelegate(
                _registrationContext,
                (uint)QUIC_SEC_CONFIG_FLAG.CERT_CONTEXT,
                certificate.Handle,
                null,
                IntPtr.Zero,
                SecCfgCreateCallbackHandler);

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

            uint status = _sessionOpenDelegate(
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

            _registrationCloseDelegate?.Invoke(_registrationContext);

            _disposed = true;
        }
    }
}
