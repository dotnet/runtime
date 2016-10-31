// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Transactions
{
    public class SinglePhaseEnlistment : Enlistment
    {
        internal SinglePhaseEnlistment(InternalEnlistment enlistment) : base(enlistment)
        {
        }

        public void Aborted()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.EnlistmentStatus(_internalEnlistment, "SinglePhaseEnlistment.Aborted, Method Enter");
                etwLog.EnlistmentAborted(_internalEnlistment);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.Aborted(_internalEnlistment, null);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.EnlistmentStatus(_internalEnlistment, "SinglePhaseEnlistment.Aborted, Method Exit");
            }
        }

        public void Aborted(Exception e)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.EnlistmentStatus(_internalEnlistment, "SinglePhaseEnlistment.Aborted, Method Enter");
                etwLog.EnlistmentAborted(_internalEnlistment);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.Aborted(_internalEnlistment, e);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit("PreparingEnlistment.SinglePhaseEnlistment.Aborted");
            }
        }


        public void Committed()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter("SinglePhaseEnlistment.Commited");
                etwLog.EnlistmentCommited(_internalEnlistment);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.Committed(_internalEnlistment);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit("SinglePhaseEnlistment.Commited");
            }
        }


        public void InDoubt()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter("SinglePhaseEnlistment.InDoubt");
            }

            lock (_internalEnlistment.SyncRoot)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentInDoubt(_internalEnlistment);
                }

                _internalEnlistment.State.InDoubt(_internalEnlistment, null);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit("SinglePhaseEnlistment.InDoubt");
            }
        }


        public void InDoubt(Exception e)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter("SinglePhaseEnlistment.InDoubt");
            }

            lock (_internalEnlistment.SyncRoot)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentInDoubt(_internalEnlistment);
                }

                _internalEnlistment.State.InDoubt(_internalEnlistment, e);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit("SinglePhaseEnlistment.InDoubt");
            }
        }
    }
}

