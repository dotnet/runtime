// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Transactions.Oletx;

// This files contains non-Windows stubs for Windows-only functionality, so that Sys.Tx can build. The APIs below
// are only ever called when a distributed transaction is needed, and throw PlatformNotSupportedException.

namespace System.Transactions.Oletx
{
    internal sealed class OletxTransactionManager
    {
        internal object? NodeName { get; set; }

        internal OletxTransactionManager(string nodeName)
        {
        }

        internal IPromotedEnlistment ReenlistTransaction(
            Guid resourceManagerIdentifier,
            byte[] resourceManagerRecoveryInformation,
            RecoveringInternalEnlistment internalEnlistment)
            => throw NotSupported();

        internal OletxCommittableTransaction CreateTransaction(TransactionOptions options)
            => throw NotSupported();

        internal void ResourceManagerRecoveryComplete(Guid resourceManagerIdentifier)
            => throw NotSupported();

        internal byte[] GetWhereabouts()
            => throw NotSupported();

        internal Transaction GetTransactionFromDtcTransaction(IDtcTransaction transactionNative)
            => throw NotSupported();

        internal OletxTransaction GetTransactionFromExportCookie(byte[] cookie, Guid txId)
            => throw NotSupported();

        internal OletxTransaction GetOletxTransactionFromTransmitterPropagationToken(byte[] propagationToken)
            => throw NotSupported();

        internal static Exception NotSupported()
            => new PlatformNotSupportedException(SR.DistributedNotSupported);
    }

    /// <summary>
    /// A Transaction object represents a single transaction.  It is created by TransactionManager
    /// objects through CreateTransaction or through deserialization.  Alternatively, the static Create
    /// methods provided, which creates a "default" TransactionManager and requests that it create
    /// a new transaction with default values.  A transaction can only be committed by
    /// the client application that created the transaction.  If a client application wishes to allow
    /// access to the transaction by multiple threads, but wants to prevent those other threads from
    /// committing the transaction, the application can make a "clone" of the transaction.  Transaction
    /// clones have the same capabilities as the original transaction, except for the ability to commit
    /// the transaction.
    /// </summary>
    internal class OletxTransaction : ISerializable, IObjectReference
    {
        internal OletxTransaction()
        {
        }

        protected OletxTransaction(SerializationInfo serializationInfo, StreamingContext context)
        {
            //if (serializationInfo == null)
            //{
            //    throw new ArgumentNullException(nameof(serializationInfo));
            //}

            //throw NotSupported();
            throw new PlatformNotSupportedException();
        }

        internal Exception? InnerException { get; set; }
        internal Guid Identifier { get; set; }
        internal RealOletxTransaction? RealTransaction { get; set; }
        internal TransactionTraceIdentifier TransactionTraceId { get; set; }
        internal IsolationLevel IsolationLevel { get; set; }
        internal Transaction? SavedLtmPromotedTransaction { get; set; }

        internal IPromotedEnlistment EnlistVolatile(
            InternalEnlistment internalEnlistment,
            EnlistmentOptions enlistmentOptions)
            => throw NotSupported();

        internal IPromotedEnlistment EnlistDurable(
            Guid resourceManagerIdentifier,
            DurableInternalEnlistment internalEnlistment,
            bool v,
            EnlistmentOptions enlistmentOptions)
            => throw NotSupported();

        internal void Rollback()
            => throw NotSupported();

        internal OletxDependentTransaction DependentClone(bool delayCommit)
            => throw NotSupported();

        internal IPromotedEnlistment EnlistVolatile(
            VolatileDemultiplexer volatileDemux,
            EnlistmentOptions enlistmentOptions)
            => throw NotSupported();

        public object GetRealObject(StreamingContext context)
            => throw NotSupported();

        internal void Dispose()
        {
        }

        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext context)
        {
            //if (serializationInfo == null)
            //{
            //    throw new ArgumentNullException(nameof(serializationInfo));
            //}

            //throw NotSupported();

            throw new PlatformNotSupportedException();
        }

        internal static Exception NotSupported()
            => new PlatformNotSupportedException(SR.DistributedNotSupported);

        internal sealed class RealOletxTransaction
        {
            internal InternalTransaction? InternalTransaction { get; set; }
        }
    }

    internal sealed class OletxDependentTransaction : OletxTransaction
    {
        internal void Complete() => throw NotSupported();
    }

    internal sealed class OletxCommittableTransaction : OletxTransaction
    {
        internal void BeginCommit(InternalTransaction tx) => throw NotSupported();
    }
}

namespace System.Transactions
{
    public static class TransactionInterop
    {
        internal static OletxTransaction ConvertToOletxTransaction(Transaction transaction)
            => throw NotSupported();

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
            => throw NotSupported();

        public static Transaction GetTransactionFromExportCookie(byte[] cookie)
            => throw NotSupported();

        public static byte[] GetTransmitterPropagationToken(Transaction transaction)
            => throw NotSupported();

        internal static byte[] GetTransmitterPropagationToken(OletxTransaction oletxTx)
            => throw NotSupported();

        public static Transaction GetTransactionFromTransmitterPropagationToken(byte[] propagationToken)
            => throw NotSupported();

        public static IDtcTransaction GetDtcTransaction(Transaction transaction)
            => throw NotSupported();

        public static Transaction GetTransactionFromDtcTransaction(IDtcTransaction transactionNative)
            => throw NotSupported();

        public static byte[] GetWhereabouts()
            => throw NotSupported();

        internal static OletxTransaction GetOletxTransactionFromTransmitterPropagationToken(byte[] propagationToken)
            => throw NotSupported();

        internal static Exception NotSupported()
            => new PlatformNotSupportedException(SR.DistributedNotSupported);
    }
}

namespace System.Transactions.Diagnostics
{
}
