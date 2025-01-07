// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Transactions.DtcProxyShim;

namespace System.Transactions.Oletx;

internal sealed class DtcTransactionManager
{
    private readonly string? _nodeName;
    private readonly OletxTransactionManager _oletxTm;
    private readonly DtcProxyShimFactory _proxyShimFactory;
    private byte[]? _whereabouts;

    internal DtcTransactionManager(string? nodeName, OletxTransactionManager oletxTm)
    {
        _nodeName = nodeName;
        _oletxTm = oletxTm;
        _proxyShimFactory = OletxTransactionManager.ProxyShimFactory;
    }

    [MemberNotNull(nameof(_whereabouts))]
    private void Initialize()
    {
        if (_whereabouts is not null)
        {
            return;
        }

        OletxInternalResourceManager internalRM = _oletxTm.InternalResourceManager;
        bool nodeNameMatches;

        try
        {
            _proxyShimFactory.ConnectToProxy(
                _nodeName,
                internalRM.Identifier,
                internalRM,
                out nodeNameMatches,
                out _whereabouts,
                out ResourceManagerShim resourceManagerShim);

            // If the node name does not match, throw.
            if (!nodeNameMatches)
            {
                throw new NotSupportedException(SR.ProxyCannotSupportMultipleNodeNames);
            }

            // Give the IResourceManagerShim to the internalRM and tell it to call ReenlistComplete.
            internalRM.ResourceManagerShim = resourceManagerShim;
            internalRM.CallReenlistComplete();
        }
        catch (COMException ex)
        {
            if (ex.ErrorCode == OletxHelper.XACT_E_NOTSUPPORTED)
            {
                throw new NotSupportedException(SR.CannotSupportNodeNameSpecification);
            }

            OletxTransactionManager.ProxyException(ex);

            // Unfortunately MSDTCPRX may return unknown error codes when attempting to connect to MSDTC
            // that error should be propagated back as a TransactionManagerCommunicationException.
            throw TransactionManagerCommunicationException.Create(SR.TransactionManagerCommunicationException, ex);
        }
    }

    internal DtcProxyShimFactory ProxyShimFactory
    {
        get
        {
            if (_whereabouts is null)
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
        }
    }

    internal byte[] Whereabouts
    {
        get
        {
            if (_whereabouts is null)
            {
                lock (this)
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
        catch (OverflowException caughtEx)
        {
            // timeout.TotalMilliseconds might be negative, so let's catch overflow exceptions, just in case.
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, caughtEx);
            }

            returnTimeout = uint.MaxValue;
        }
        return returnTimeout;
    }
}
