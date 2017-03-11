// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Transactions
{
    public class TransactionInformation
    {
        private InternalTransaction _internalTransaction;

        internal TransactionInformation(InternalTransaction internalTransaction)
        {
            _internalTransaction = internalTransaction;
        }

        public string LocalIdentifier
        {
            get
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                }
 
                try
                {
                    return _internalTransaction.TransactionTraceId.TransactionIdentifier;
                }
                finally
                {
                    if (etwLog.IsEnabled())
                    {
                        etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
                    }
                }
            }
        }


        public Guid DistributedIdentifier
        {
            get
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                }

                try
                {
                    // syncronize to avoid potential race between accessing the DistributerIdentifier
                    // and getting the transaction information entry populated...

                    lock (_internalTransaction)
                    {
                        return _internalTransaction.State.get_Identifier(_internalTransaction);
                    }
                }
                finally
                {
                    if (etwLog.IsEnabled())
                    {
                        etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
                    }
                }
            }
        }


        public DateTime CreationTime => new DateTime(_internalTransaction.CreationTime);

        public TransactionStatus Status
        {
            get
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                }

                try
                {
                    return _internalTransaction.State.get_Status(_internalTransaction);
                }
                finally
                {
                    if (etwLog.IsEnabled())
                    {
                        etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
                    }
                }
            }
        }
    }
}

