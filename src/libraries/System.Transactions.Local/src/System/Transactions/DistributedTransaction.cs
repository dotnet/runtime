// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Transactions.Distributed
{
    internal sealed class DistributedTransactionManager
    {
        internal object? NodeName { get; set; }

        internal static IPromotedEnlistment ReenlistTransaction(Guid resourceManagerIdentifier, byte[] resourceManagerRecoveryInformation, RecoveringInternalEnlistment internalEnlistment)
        {
            throw DistributedTransaction.NotSupported();
        }

        internal static DistributedCommittableTransaction CreateTransaction(TransactionOptions options)
        {
            throw DistributedTransaction.NotSupported();
        }

        internal static void ResourceManagerRecoveryComplete(Guid resourceManagerIdentifier)
        {
            throw DistributedTransaction.NotSupported();
        }

        internal static byte[] GetWhereabouts()
        {
            throw DistributedTransaction.NotSupported();
        }

        internal static Transaction GetTransactionFromDtcTransaction(IDtcTransaction transactionNative)
        {
            throw DistributedTransaction.NotSupported();
        }

        internal static DistributedTransaction GetTransactionFromExportCookie(byte[] cookie, Guid txId)
        {
            throw DistributedTransaction.NotSupported();
        }

        internal static DistributedTransaction GetDistributedTransactionFromTransmitterPropagationToken(byte[] propagationToken)
        {
            throw DistributedTransaction.NotSupported();
        }
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
    internal class DistributedTransaction : ISerializable, IObjectReference
    {
        internal DistributedTransaction()
        {
        }

        protected DistributedTransaction(SerializationInfo serializationInfo, StreamingContext context)
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
        internal RealDistributedTransaction? RealTransaction { get; set; }
        internal TransactionTraceIdentifier TransactionTraceId { get; set; }
        internal IsolationLevel IsolationLevel { get; set; }
        internal Transaction? SavedLtmPromotedTransaction { get; set; }

        internal static IPromotedEnlistment EnlistVolatile(InternalEnlistment internalEnlistment, EnlistmentOptions enlistmentOptions)
        {
            throw NotSupported();
        }

        internal static IPromotedEnlistment EnlistDurable(Guid resourceManagerIdentifier, DurableInternalEnlistment internalEnlistment, bool v, EnlistmentOptions enlistmentOptions)
        {
            throw NotSupported();
        }

        internal static void Rollback()
        {
            throw NotSupported();
        }

        internal static DistributedDependentTransaction DependentClone(bool v)
        {
            throw NotSupported();
        }

        internal static IPromotedEnlistment EnlistVolatile(VolatileDemultiplexer volatileDemux, EnlistmentOptions enlistmentOptions)
        {
            throw NotSupported();
        }

        internal static byte[] GetExportCookie(byte[] whereaboutsCopy)
        {
            throw NotSupported();
        }

        public object GetRealObject(StreamingContext context)
        {
            throw NotSupported();
        }

        internal static byte[] GetTransmitterPropagationToken()
        {
            throw NotSupported();
        }

        internal static IDtcTransaction GetDtcTransaction()
        {
            throw NotSupported();
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
        {
            return new PlatformNotSupportedException(SR.DistributedNotSupported);
        }

        internal sealed class RealDistributedTransaction
        {
            internal InternalTransaction? InternalTransaction { get; set; }
        }
    }

    internal sealed class DistributedDependentTransaction : DistributedTransaction
    {
        internal static void Complete()
        {
            throw NotSupported();
        }
    }

    internal sealed class DistributedCommittableTransaction : DistributedTransaction
    {
        internal static void BeginCommit(InternalTransaction tx)
        {
            throw NotSupported();
        }
    }
}
