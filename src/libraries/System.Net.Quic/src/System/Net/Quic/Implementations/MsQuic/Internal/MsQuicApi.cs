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
            uint status = (uint)MsQuicNativeMethods.MsQuicOpen(version: 1, out MsQuicNativeMethods.NativeApi* registration);
            MsQuicStatusException.ThrowIfFailed(status);

            NativeRegistration = *registration;

            RegistrationOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.RegistrationOpenDelegate>(
                    NativeRegistration.RegistrationOpen);
            RegistrationCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.RegistrationCloseDelegate>(
                    NativeRegistration.RegistrationClose);

            SecConfigCreateDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SecConfigCreateDelegate>(
                    NativeRegistration.SecConfigCreate);
            SecConfigDeleteDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SecConfigDeleteDelegate>(
                    NativeRegistration.SecConfigDelete);

            SessionOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SessionOpenDelegate>(
                    NativeRegistration.SessionOpen);
            SessionCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SessionCloseDelegate>(
                    NativeRegistration.SessionClose);
            SessionShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SessionShutdownDelegate>(
                    NativeRegistration.SessionShutdown);

            ListenerOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerOpenDelegate>(
                    NativeRegistration.ListenerOpen);
            ListenerCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerCloseDelegate>(
                    NativeRegistration.ListenerClose);
            ListenerStartDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerStartDelegate>(
                    NativeRegistration.ListenerStart);
            ListenerStopDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerStopDelegate>(
                    NativeRegistration.ListenerStop);

            ConnectionOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionOpenDelegate>(
                    NativeRegistration.ConnectionOpen);
            ConnectionCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionCloseDelegate>(
                    NativeRegistration.ConnectionClose);
            ConnectionShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionShutdownDelegate>(
                    NativeRegistration.ConnectionShutdown);
            ConnectionStartDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionStartDelegate>(
                    NativeRegistration.ConnectionStart);

            StreamOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamOpenDelegate>(
                    NativeRegistration.StreamOpen);
            StreamCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamCloseDelegate>(
                    NativeRegistration.StreamClose);
            StreamStartDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamStartDelegate>(
                   NativeRegistration.StreamStart);
            StreamShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamShutdownDelegate>(
                    NativeRegistration.StreamShutdown);
            StreamSendDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamSendDelegate>(
                    NativeRegistration.StreamSend);

            SetContextDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SetContextDelegate>(
                    NativeRegistration.SetContext);
            GetContextDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.GetContextDelegate>(
                    NativeRegistration.GetContext);
            SetCallbackHandlerDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SetCallbackHandlerDelegate>(
                    NativeRegistration.SetCallbackHandler);

            SetParamDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SetParamDelegate>(
                    NativeRegistration.SetParam);
            GetParamDelegate =
                Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.GetParamDelegate>(
                    NativeRegistration.GetParam);
        }

        internal MsQuicNativeMethods.NativeApi NativeRegistration { get; private set; }

        internal MsQuicNativeMethods.RegistrationOpenDelegate RegistrationOpenDelegate { get; private set; }
        internal MsQuicNativeMethods.RegistrationCloseDelegate RegistrationCloseDelegate { get; private set; }

        internal MsQuicNativeMethods.SecConfigCreateDelegate SecConfigCreateDelegate { get; private set; }
        internal MsQuicNativeMethods.SecConfigCreateCompleteDelegate SecConfigCreateCompleteDelegate { get; private set; }
        internal MsQuicNativeMethods.SecConfigDeleteDelegate SecConfigDeleteDelegate { get; private set; }

        internal MsQuicNativeMethods.SessionOpenDelegate SessionOpenDelegate { get; private set; }
        internal MsQuicNativeMethods.SessionCloseDelegate SessionCloseDelegate { get; private set; }
        internal MsQuicNativeMethods.SessionShutdownDelegate SessionShutdownDelegate { get; private set; }

        internal MsQuicNativeMethods.ListenerOpenDelegate ListenerOpenDelegate { get; private set; }
        internal MsQuicNativeMethods.ListenerCloseDelegate ListenerCloseDelegate { get; private set; }
        internal MsQuicNativeMethods.ListenerStartDelegate ListenerStartDelegate { get; private set; }
        internal MsQuicNativeMethods.ListenerStopDelegate ListenerStopDelegate { get; private set; }

        internal MsQuicNativeMethods.ConnectionOpenDelegate ConnectionOpenDelegate { get; private set; }
        internal MsQuicNativeMethods.ConnectionCloseDelegate ConnectionCloseDelegate { get; private set; }
        internal MsQuicNativeMethods.ConnectionShutdownDelegate ConnectionShutdownDelegate { get; private set; }
        internal MsQuicNativeMethods.ConnectionStartDelegate ConnectionStartDelegate { get; private set; }

        internal MsQuicNativeMethods.StreamOpenDelegate StreamOpenDelegate { get; private set; }
        internal MsQuicNativeMethods.StreamCloseDelegate StreamCloseDelegate { get; private set; }
        internal MsQuicNativeMethods.StreamStartDelegate StreamStartDelegate { get; private set; }
        internal MsQuicNativeMethods.StreamShutdownDelegate StreamShutdownDelegate { get; private set; }
        internal MsQuicNativeMethods.StreamSendDelegate StreamSendDelegate { get; private set; }
        internal MsQuicNativeMethods.StreamReceiveCompleteDelegate StreamReceiveComplete { get; private set; }

        internal MsQuicNativeMethods.SetContextDelegate SetContextDelegate { get; private set; }
        internal MsQuicNativeMethods.GetContextDelegate GetContextDelegate { get; private set; }
        internal MsQuicNativeMethods.SetCallbackHandlerDelegate SetCallbackHandlerDelegate { get; private set; }

        internal MsQuicNativeMethods.SetParamDelegate SetParamDelegate { get; private set; }
        internal MsQuicNativeMethods.GetParamDelegate GetParamDelegate { get; private set; }

        internal void RegistrationOpen(byte[] name)
        {
            MsQuicStatusException.ThrowIfFailed(RegistrationOpenDelegate(name, out IntPtr ctx));
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

            uint status = SecConfigCreateDelegate(
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
