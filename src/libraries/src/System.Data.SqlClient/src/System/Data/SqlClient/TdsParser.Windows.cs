// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Data.SqlClient.SNI;

namespace System.Data.SqlClient
{
    internal sealed partial class TdsParser
    {
        private static volatile bool s_fSSPILoaded = false; // bool to indicate whether library has been loaded

        internal void PostReadAsyncForMars()
        {
            if (TdsParserStateObjectFactory.UseManagedSNI)
                return;

            // HACK HACK HACK - for Async only
            // Have to post read to initialize MARS - will get callback on this when connection goes
            // down or is closed.

            PacketHandle temp = default;
            uint error = TdsEnums.SNI_SUCCESS;

            _pMarsPhysicalConObj.IncrementPendingCallbacks();
            SessionHandle handle = _pMarsPhysicalConObj.SessionHandle;
            temp = _pMarsPhysicalConObj.ReadAsync(handle, out error);

            Debug.Assert(temp.Type == PacketHandle.NativePointerType, "unexpected packet type when requiring NativePointer");

            if (temp.NativePointer != IntPtr.Zero)
            {
                // Be sure to release packet, otherwise it will be leaked by native.
                _pMarsPhysicalConObj.ReleasePacket(temp);
            }

            Debug.Assert(IntPtr.Zero == temp.NativePointer, "unexpected syncReadPacket without corresponding SNIPacketRelease");
            if (TdsEnums.SNI_SUCCESS_IO_PENDING != error)
            {
                Debug.Assert(TdsEnums.SNI_SUCCESS != error, "Unexpected successful read async on physical connection before enabling MARS!");
                _physicalStateObj.AddError(ProcessSNIError(_physicalStateObj));
                ThrowExceptionAndWarning(_physicalStateObj);
            }
        }

        private void LoadSSPILibrary()
        {
            if (TdsParserStateObjectFactory.UseManagedSNI)
                return;
            // Outer check so we don't acquire lock once it's loaded.
            if (!s_fSSPILoaded)
            {
                lock (s_tdsParserLock)
                {
                    // re-check inside lock
                    if (!s_fSSPILoaded)
                    {
                        // use local for ref param to defer setting s_maxSSPILength until we know the call succeeded.
                        uint maxLength = 0;

                        if (0 != SNINativeMethodWrapper.SNISecInitPackage(ref maxLength))
                            SSPIError(SQLMessage.SSPIInitializeError(), TdsEnums.INIT_SSPI_PACKAGE);

                        s_maxSSPILength = maxLength;
                        s_fSSPILoaded = true;
                    }
                }
            }

            if (s_maxSSPILength > int.MaxValue)
            {
                throw SQL.InvalidSSPIPacketSize();   // SqlBu 332503
            }
        }

        private void WaitForSSLHandShakeToComplete(ref uint error)
        {
            if (TdsParserStateObjectFactory.UseManagedSNI)
                return;
            // in the case where an async connection is made, encryption is used and Windows Authentication is used, 
            // wait for SSL handshake to complete, so that the SSL context is fully negotiated before we try to use its 
            // Channel Bindings as part of the Windows Authentication context build (SSL handshake must complete 
            // before calling SNISecGenClientContext).
            error = _physicalStateObj.WaitForSSLHandShakeToComplete();
            if (error != TdsEnums.SNI_SUCCESS)
            {
                _physicalStateObj.AddError(ProcessSNIError(_physicalStateObj));
                ThrowExceptionAndWarning(_physicalStateObj);
            }
        }

        private SNIErrorDetails GetSniErrorDetails()
        {
            SNIErrorDetails details = new SNIErrorDetails();

            if (TdsParserStateObjectFactory.UseManagedSNI)
            {
                SNIError sniError = SNIProxy.Singleton.GetLastError();
                details.sniErrorNumber = sniError.sniError;
                details.errorMessage = sniError.errorMessage;
                details.nativeError = sniError.nativeError;
                details.provider = (int)sniError.provider;
                details.lineNumber = sniError.lineNumber;
                details.function = sniError.function;
                details.exception = sniError.exception;
            }
            else
            {
                SNINativeMethodWrapper.SNI_Error sniError;
                SNINativeMethodWrapper.SNIGetLastError(out sniError);
                details.sniErrorNumber = sniError.sniError;
                details.errorMessage = sniError.errorMessage;
                details.nativeError = sniError.nativeError;
                details.provider = (int)sniError.provider;
                details.lineNumber = sniError.lineNumber;
                details.function = sniError.function;
            }
            return details;
        }

    }    // tdsparser
}//namespace
