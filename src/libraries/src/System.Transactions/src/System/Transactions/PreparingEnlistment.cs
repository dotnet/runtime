// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Transactions.Diagnostics;

namespace System.Transactions
{
    public class PreparingEnlistment : Enlistment
    {
        internal PreparingEnlistment(
            InternalEnlistment enlistment
            ) : base(enlistment)
        {
        }

        public void Prepared()
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "PreparingEnlistment.Prepared");
                EnlistmentCallbackPositiveTraceRecord.Trace(SR.TraceSourceLtm,
                    _internalEnlistment.EnlistmentTraceId, EnlistmentCallback.Prepared);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.Prepared(_internalEnlistment);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "PreparingEnlistment.Prepared");
            }
        }

        public void ForceRollback()
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "PreparingEnlistment.ForceRollback");
            }

            if (DiagnosticTrace.Warning)
            {
                EnlistmentCallbackNegativeTraceRecord.Trace(SR.TraceSourceLtm,
                    _internalEnlistment.EnlistmentTraceId, EnlistmentCallback.ForceRollback);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.ForceRollback(_internalEnlistment, null);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "PreparingEnlistment.ForceRollback");
            }
        }

        public void ForceRollback(Exception e)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "PreparingEnlistment.ForceRollback");
            }

            if (DiagnosticTrace.Warning)
            {
                EnlistmentCallbackNegativeTraceRecord.Trace(SR.TraceSourceLtm, _internalEnlistment.EnlistmentTraceId, EnlistmentCallback.ForceRollback);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.ForceRollback(_internalEnlistment, e);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "PreparingEnlistment.ForceRollback");
            }
        }

        public byte[] RecoveryInformation()
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "PreparingEnlistment.RecoveryInformation");
            }

            try
            {
                lock (_internalEnlistment.SyncRoot)
                {
                    return _internalEnlistment.State.RecoveryInformation(_internalEnlistment);
                }
            }
            finally
            {
                if (DiagnosticTrace.Verbose)
                {
                    MethodExitedTraceRecord.Trace(SR.TraceSourceLtm,
                        "PreparingEnlistment.RecoveryInformation"
                        );
                }
            }
        }
    }
}
