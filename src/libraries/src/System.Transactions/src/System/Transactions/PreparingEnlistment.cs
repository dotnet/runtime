// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.EnlistmentStatus(_internalEnlistment, "Prepared, Method Enter");
                etwLog.EnlistmentPrepared(_internalEnlistment);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.Prepared(_internalEnlistment);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.EnlistmentStatus(_internalEnlistment, "Prepared, Method Enter");
            }
        }

        public void ForceRollback()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.EnlistmentStatus(_internalEnlistment, "ForceRollback, Method Enter");
                etwLog.EnlistmentForceRollback(_internalEnlistment);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.ForceRollback(_internalEnlistment, null);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit("PreparingEnlistment.ForceRollback");
            }
        }

        public void ForceRollback(Exception e)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter("PreparingEnlistment.ForceRollback");
                etwLog.EnlistmentForceRollback(_internalEnlistment);
            }
 
            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.ForceRollback(_internalEnlistment, e);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit("PreparingEnlistment.ForceRollback");
            }
        }

        public byte[] RecoveryInformation()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter("PreparingEnlisment.RecoveryInformation");
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
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentStatus(_internalEnlistment, "RecoveryInformation, Method Exit");
                }
            }
        }
    }
}
