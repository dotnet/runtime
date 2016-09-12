// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Transactions.Diagnostics
{
    internal sealed class Activity : IDisposable
    {
        private readonly Guid _oldGuid;
        private readonly Guid _newGuid;
        private readonly bool _emitTransfer = false;
        private bool _mustDispose = false;

        private Activity(ref Guid newGuid, bool emitTransfer)
        {
            _emitTransfer = emitTransfer;
            if (DiagnosticTrace.ShouldCorrelate && newGuid != Guid.Empty)
            {
                _newGuid = newGuid;
                _oldGuid = DiagnosticTrace.GetActivityId();
                if (_oldGuid != newGuid)
                {
                    _mustDispose = true;
                    if (_emitTransfer)
                    {
                        DiagnosticTrace.TraceTransfer(newGuid);
                    }
                    DiagnosticTrace.SetActivityId(newGuid);
                }
            }
        }

        internal static Activity CreateActivity(Guid newGuid, bool emitTransfer)
        {
            Activity retval = null;
            if (DiagnosticTrace.ShouldCorrelate &&
                (newGuid != Guid.Empty) &&
                (newGuid != DiagnosticTrace.GetActivityId()))
            {
                retval = new Activity(ref newGuid, emitTransfer);
            }
            return retval;
        }

        public void Dispose()
        {
            if (_mustDispose)
            {
                _mustDispose = false;
                if (_emitTransfer)
                {
                    DiagnosticTrace.TraceTransfer(_oldGuid);
                }
                DiagnosticTrace.SetActivityId(_oldGuid);
            }
        }
    }
}
