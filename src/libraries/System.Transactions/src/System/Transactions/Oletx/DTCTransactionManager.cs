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
        private string _nodeName;
        private OletxTransactionManager _oletxTm;
        private IDtcProxyShimFactory _proxyShimFactory;
        private uint _whereaboutsSize;
        private byte[] _whereabouts;
        private bool _initialized;

        internal DtcTransactionManager(string nodeName, OletxTransactionManager oletxTm)
        {
            _nodeName = nodeName;
            _oletxTm = oletxTm;
            _initialized = false;
            _proxyShimFactory = OletxTransactionManager.ProxyShimFactory;
        }

        // This is here for the DangerousGetHandle call.  We need to do it.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")]
        void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            OletxInternalResourceManager internalRM = _oletxTm.InternalResourceManager;
            IntPtr handle = IntPtr.Zero;
            IResourceManagerShim resourceManagerShim = null;
            bool nodeNameMatches;

            CoTaskMemHandle whereaboutsBuffer = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                handle = HandleTable.AllocHandle(internalRM);

                _proxyShimFactory.ConnectToProxy(
                    _nodeName,
                    internalRM.Identifier,
                    handle,
                    out nodeNameMatches,
                    out _whereaboutsSize,
                    out whereaboutsBuffer,
                    out resourceManagerShim);

                // If the node name does not match, throw.
                if (!nodeNameMatches)
                {
                    throw new NotSupportedException(SR.ProxyCannotSupportMultipleNodeNames);
                }

                // Make a managed copy of the whereabouts.
                if (whereaboutsBuffer != null && _whereaboutsSize != 0)
                {
                    _whereabouts = new byte[_whereaboutsSize];
                    Marshal.Copy(whereaboutsBuffer.DangerousGetHandle(), _whereabouts, 0, Convert.ToInt32(_whereaboutsSize));
                }

                // Give the IResourceManagerShim to the internalRM and tell it to call ReenlistComplete.
                internalRM.ResourceManagerShim = resourceManagerShim;
                internalRM.CallReenlistComplete();

                _initialized = true;
            }
            catch (COMException ex)
            {
                if (ex.ErrorCode == NativeMethods.XACT_E_NOTSUPPORTED)
                {
                    throw new NotSupportedException( SR.CannotSupportNodeNameSpecification);
                }

                OletxTransactionManager.ProxyException(ex);

                // Unfortunately MSDTCPRX may return unknown error codes when attempting to connect to MSDTC
                // that error should be propagated back as a TransactionManagerCommunicationException.
                throw TransactionManagerCommunicationException.Create(SR.TransactionManagerCommunicationException, ex);
            }
            finally
            {
                if (whereaboutsBuffer != null)
                {
                    whereaboutsBuffer.Close();
                }

                // If we weren't successful at initializing ourself, clear things out
                // for next time around.
                if (!_initialized)
                {
                    if (handle != IntPtr.Zero && resourceManagerShim == null)
                    {
                        HandleTable.FreeHandle(handle);
                    }

                    if (null != _whereabouts)
                    {
                        _whereabouts = null;
                        _whereaboutsSize = 0;
                    }
                }
            }
        }

        internal IDtcProxyShimFactory ProxyShimFactory
        {
            get
            {
                if (!_initialized)
                {
                    lock (this)
                    {
                        Initialize();
                    }
                }

                return _proxyShimFactory;
            }
        }

        internal void ReleaseProxy()
        {
            lock (this)
            {
                _whereabouts = null;
                _whereaboutsSize = 0;
                _initialized = false;
            }
        }

        internal byte[] Whereabouts
        {
            get
            {
                if (!_initialized)
                {
                    lock ( this )
                    {
                        Initialize();
                    }
                }

                return _whereabouts;
            }
        }

        internal static uint AdjustTimeout(TimeSpan timeout)
        {
            uint returnTimeout = 0;

            try
            {
                returnTimeout = Convert.ToUInt32(timeout.TotalMilliseconds, CultureInfo.CurrentCulture);
            }
                // timeout.TotalMilliseconds might be negative, so let's catch overflow exceptions, just in case.
            catch (OverflowException caughtEx)
            {
                if (DiagnosticTrace.Verbose)
                {
                    ExceptionConsumedTraceRecord.Trace(SR.TraceSourceOletx, caughtEx);
                }

                returnTimeout = uint.MaxValue;
            }
            return returnTimeout;
        }
    }
}
