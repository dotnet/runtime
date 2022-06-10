// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Security.Permissions;
using System.Runtime.CompilerServices;
using System.Transactions.Diagnostics;

#nullable disable

namespace System.Transactions.Oletx
{
    internal class DtcTransactionManager
    {
        string nodeName;
        OletxTransactionManager oletxTm;
        IDtcProxyShimFactory proxyShimFactory;
        UInt32 whereaboutsSize;
        byte[] whereabouts;
        bool initialized;

        internal DtcTransactionManager( string nodeName, OletxTransactionManager oletxTm )
        {
            this.nodeName = nodeName;
            this.oletxTm = oletxTm;
            this.initialized = false;
            this.proxyShimFactory = OletxTransactionManager.proxyShimFactory;
        }

        // This is here for the DangerousGetHandle call.  We need to do it.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")]
        void Initialize()
        {
            if ( this.initialized )
            {
                return;
            }

            OletxInternalResourceManager internalRM = this.oletxTm.internalResourceManager;
            IntPtr handle = IntPtr.Zero;
            IResourceManagerShim resourceManagerShim = null;
            bool nodeNameMatches = false;

            CoTaskMemHandle whereaboutsBuffer = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                handle = HandleTable.AllocHandle( internalRM );

                this.proxyShimFactory.ConnectToProxy(
                    this.nodeName,
                    internalRM.Identifier,
                    handle,
                    out nodeNameMatches,
                    out this.whereaboutsSize,
                    out whereaboutsBuffer,
                    out resourceManagerShim
                    );

                // If the node name does not match, throw.
                if ( ! nodeNameMatches )
                {
                    throw new NotSupportedException( SR.ProxyCannotSupportMultipleNodeNames);
                }

                // Make a managed copy of the whereabouts.
                if ( ( null != whereaboutsBuffer ) && ( 0 != this.whereaboutsSize ) )
                {
                    this.whereabouts = new byte[this.whereaboutsSize];
                    Marshal.Copy( whereaboutsBuffer.DangerousGetHandle(), this.whereabouts, 0, Convert.ToInt32(this.whereaboutsSize) );
                }

                // Give the IResourceManagerShim to the internalRM and tell it to call ReenlistComplete.
                internalRM.resourceManagerShim = resourceManagerShim;
                internalRM.CallReenlistComplete();


                this.initialized = true;
            }
            catch ( COMException ex )
            {
                if ( NativeMethods.XACT_E_NOTSUPPORTED == ex.ErrorCode )
                {
                    throw new NotSupportedException( SR.CannotSupportNodeNameSpecification);
                }

                OletxTransactionManager.ProxyException( ex );

                // Unfortunately MSDTCPRX may return unknown error codes when attempting to connect to MSDTC
                // that error should be propagated back as a TransactionManagerCommunicationException.
                throw TransactionManagerCommunicationException.Create(
                    SR.TransactionManagerCommunicationException,
                    ex
                    );
            }
            finally
            {
                if ( null != whereaboutsBuffer )
                {
                    whereaboutsBuffer.Close();
                }

                // If we weren't successful at initializing ourself, clear things out
                // for next time around.
                if ( !this.initialized )
                {
                    if ( handle != IntPtr.Zero && null == resourceManagerShim )
                    {
                        HandleTable.FreeHandle( handle );
                    }

                    if ( null != this.whereabouts )
                    {
                        this.whereabouts = null;
                        this.whereaboutsSize = 0;
                    }
                }
            }
        }

        internal IDtcProxyShimFactory ProxyShimFactory
        {
            get
            {
                if ( !this.initialized )
                {
                    lock ( this )
                    {
                        Initialize();
                    }
                }
                return this.proxyShimFactory;
            }
        }

        internal void ReleaseProxy()
        {
            lock ( this )
            {
                this.whereabouts = null;
                this.whereaboutsSize = 0;
                this.initialized = false;
            }
        }

        internal byte[] Whereabouts
        {
            get
            {
                if ( !this.initialized )
                {
                    lock ( this )
                    {
                        Initialize();
                    }
                }
                return whereabouts;
            }
        }

        internal static UInt32 AdjustTimeout(
            TimeSpan timeout
            )
        {
            UInt32 returnTimeout = 0;

            try
            {
                returnTimeout = ( Convert.ToUInt32( timeout.TotalMilliseconds, CultureInfo.CurrentCulture ) );
            }
                // timeout.TotalMilliseconds might be negative, so let's catch overflow exceptions, just in case.
            catch ( OverflowException caughtEx )
            {
                if ( DiagnosticTrace.Verbose )
                {
                    ExceptionConsumedTraceRecord.Trace( SR.TraceSourceOletx,
                        caughtEx );
                }
                returnTimeout = UInt32.MaxValue;
            }
            return returnTimeout;
        }

    }
}
