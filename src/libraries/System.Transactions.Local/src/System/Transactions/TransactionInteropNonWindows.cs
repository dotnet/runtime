// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Transactions.Oletx;

namespace System.Transactions
{
    public static class TransactionInterop
    {
        internal static OletxTransaction ConvertToOletxTransaction(Transaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);

            ObjectDisposedException.ThrowIf(transaction.Disposed, transaction);

            if (transaction._complete)
            {
                throw TransactionException.CreateTransactionCompletedException(transaction.DistributedTxId);
            }

            OletxTransaction? distributedTx = transaction.Promote();
            if (distributedTx == null)
            {
                throw OletxTransaction.NotSupported();
            }
            return distributedTx;
        }

        /// <summary>
        /// This is the PromoterType value that indicates that the transaction is promoting to MSDTC.
        ///
        /// If using the variation of Transaction.EnlistPromotableSinglePhase that takes a PromoterType and the
        /// ITransactionPromoter being used promotes to MSDTC, then this is the value that should be
        /// specified for the PromoterType parameter to EnlistPromotableSinglePhase.
        ///
        /// If using the variation of Transaction.EnlistPromotableSinglePhase that assumes promotion to MSDTC and
        /// it that returns false, the caller can compare this value with Transaction.PromoterType to
        /// verify that the transaction promoted, or will promote, to MSDTC. If the Transaction.PromoterType
        /// matches this value, then the caller can continue with its enlistment with MSDTC. But if it
        /// does not match, the caller will not be able to enlist with MSDTC.
        /// </summary>
        public static readonly Guid PromoterTypeDtc = new Guid("14229753-FFE1-428D-82B7-DF73045CB8DA");

        public static byte[] GetExportCookie(Transaction transaction, byte[] whereabouts)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            ArgumentNullException.ThrowIfNull(whereabouts);

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetExportCookie");
            }

            // Copy the whereabouts so that it cannot be modified later.
            var whereaboutsCopy = new byte[whereabouts.Length];
            Buffer.BlockCopy(whereabouts, 0, whereaboutsCopy, 0, whereabouts.Length);

            ConvertToOletxTransaction(transaction);
            byte[] cookie = OletxTransaction.GetExportCookie(whereaboutsCopy);

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetExportCookie");
            }

            return cookie;
        }

        public static Transaction GetTransactionFromExportCookie(byte[] cookie)
        {
            ArgumentNullException.ThrowIfNull(cookie);

            if (cookie.Length < 32)
            {
                throw new ArgumentException(SR.InvalidArgument, nameof(cookie));
            }

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromExportCookie");
            }

            var cookieCopy = new byte[cookie.Length];
            Buffer.BlockCopy(cookie, 0, cookieCopy, 0, cookie.Length);
            cookie = cookieCopy;

            // Extract the transaction guid from the propagation token to see if we already have a
            // transaction object for the transaction.
            // In a cookie, the transaction guid is preceded by a signature guid.
            var txId = new Guid(cookie.AsSpan(16, 16));

            // First check to see if there is a promoted LTM transaction with the same ID.  If there
            // is, just return that.
            Transaction? transaction = TransactionManager.FindPromotedTransaction(txId);
            if (transaction != null)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromExportCookie");
                }

                return transaction;
            }

            // Find or create the promoted transaction.
            OletxTransaction dTx = OletxTransactionManager.GetTransactionFromExportCookie(cookieCopy, txId);
            transaction = TransactionManager.FindOrCreatePromotedTransaction(txId, dTx);

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromExportCookie");
            }

            return transaction;
        }

        public static byte[] GetTransmitterPropagationToken(Transaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransmitterPropagationToken");
            }

            ConvertToOletxTransaction(transaction);
            byte[] token = OletxTransaction.GetTransmitterPropagationToken();

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransmitterPropagationToken");
            }

            return token;
        }

        public static Transaction GetTransactionFromTransmitterPropagationToken(byte[] propagationToken)
        {
            ArgumentNullException.ThrowIfNull(propagationToken);

            if (propagationToken.Length < 24)
            {
                throw new ArgumentException(SR.InvalidArgument, nameof(propagationToken));
            }

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromTransmitterPropagationToken");
            }

            // Extract the transaction guid from the propagation token to see if we already have a
            // transaction object for the transaction.
            // In a propagation token, the transaction guid is preceded by two version DWORDs.
            var txId = new Guid(propagationToken.AsSpan(8, 16));

            // First check to see if there is a promoted LTM transaction with the same ID.  If there is, just return that.
            Transaction? tx = TransactionManager.FindPromotedTransaction(txId);
            if (null != tx)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromTransmitterPropagationToken");
                }

                return tx;
            }

            OletxTransaction dTx = GetOletxTransactionFromTransmitterPropagationToken(propagationToken);

            // If a transaction is found then FindOrCreate will Dispose the distributed transaction created.
            Transaction returnValue = TransactionManager.FindOrCreatePromotedTransaction(txId, dTx);

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromTransmitterPropagationToken");
            }
            return returnValue;
        }

        public static IDtcTransaction GetDtcTransaction(Transaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetDtcTransaction");
            }

            ConvertToOletxTransaction(transaction);
            IDtcTransaction transactionNative = OletxTransaction.GetDtcTransaction();

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetDtcTransaction");
            }

            return transactionNative;
        }

        public static Transaction GetTransactionFromDtcTransaction(IDtcTransaction transactionNative)
        {
            ArgumentNullException.ThrowIfNull(transactionNative);

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromDtcTransaction");
            }

            Transaction transaction = OletxTransactionManager.GetTransactionFromDtcTransaction(transactionNative);

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromDtcTransaction");
            }
            return transaction;
        }

        public static byte[] GetWhereabouts()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetWhereabouts");
            }

            byte[] returnValue = OletxTransactionManager.GetWhereabouts();

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetWhereabouts");
            }
            return returnValue;
        }

        internal static OletxTransaction GetOletxTransactionFromTransmitterPropagationToken(byte[] propagationToken)
        {
            ArgumentNullException.ThrowIfNull(propagationToken);

            if (propagationToken.Length < 24)
            {
                throw new ArgumentException(SR.InvalidArgument, nameof(propagationToken));
            }

            byte[] propagationTokenCopy = new byte[propagationToken.Length];
            Array.Copy(propagationToken, propagationTokenCopy, propagationToken.Length);

            return OletxTransactionManager.GetOletxTransactionFromTransmitterPropagationToken(propagationTokenCopy);
        }
    }
}
