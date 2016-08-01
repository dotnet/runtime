// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Transactions.Diagnostics;

namespace System.Transactions
{
    public class SinglePhaseEnlistment : Enlistment
    {
        internal SinglePhaseEnlistment(InternalEnlistment enlistment) : base(enlistment)
        {
        }

        public void Aborted()
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "SinglePhaseEnlistment.Aborted");
            }

            if (DiagnosticTrace.Warning)
            {
                EnlistmentCallbackNegativeTraceRecord.Trace(SR.TraceSourceLtm,
                    _internalEnlistment.EnlistmentTraceId, EnlistmentCallback.Aborted);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.Aborted(_internalEnlistment, null);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "SinglePhaseEnlistment.Aborted");
            }
        }

        public void Aborted(Exception e)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "SinglePhaseEnlistment.Aborted");
            }

            if (DiagnosticTrace.Warning)
            {
                EnlistmentCallbackNegativeTraceRecord.Trace(SR.TraceSourceLtm,
                    _internalEnlistment.EnlistmentTraceId, EnlistmentCallback.Aborted);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.Aborted(_internalEnlistment, e);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "SinglePhaseEnlistment.Aborted");
            }
        }


        public void Committed()
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "SinglePhaseEnlistment.Committed");
                EnlistmentCallbackPositiveTraceRecord.Trace(SR.TraceSourceLtm,
                    _internalEnlistment.EnlistmentTraceId, EnlistmentCallback.Committed);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.Committed(_internalEnlistment);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "SinglePhaseEnlistment.Committed");
            }
        }


        public void InDoubt()
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "SinglePhaseEnlistment.InDoubt");
            }


            lock (_internalEnlistment.SyncRoot)
            {
                if (DiagnosticTrace.Warning)
                {
                    EnlistmentCallbackNegativeTraceRecord.Trace(SR.TraceSourceLtm,
                        _internalEnlistment.EnlistmentTraceId, EnlistmentCallback.InDoubt);
                }

                _internalEnlistment.State.InDoubt(_internalEnlistment, null);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "SinglePhaseEnlistment.InDoubt");
            }
        }


        public void InDoubt(Exception e)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "SinglePhaseEnlistment.InDoubt");
            }

            lock (_internalEnlistment.SyncRoot)
            {
                if (DiagnosticTrace.Warning)
                {
                    EnlistmentCallbackNegativeTraceRecord.Trace(SR.TraceSourceLtm,
                        _internalEnlistment.EnlistmentTraceId, EnlistmentCallback.InDoubt);
                }

                _internalEnlistment.State.InDoubt(_internalEnlistment, e);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "SinglePhaseEnlistment.InDoubt");
            }
        }
    }
}

