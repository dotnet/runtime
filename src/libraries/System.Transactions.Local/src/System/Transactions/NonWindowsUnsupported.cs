// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Transactions.Oletx;

// This files contains non-Windows stubs for Windows-only functionality, so that Sys.Tx can build. The APIs below
// are only ever called when a distributed transaction is needed, and throw PlatformNotSupportedException.

#pragma warning disable CA1822, IDE0060

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

        internal static byte[] GetWhereabouts()
            => throw NotSupported();

        internal static Transaction GetTransactionFromDtcTransaction(IDtcTransaction transactionNative)
            => throw NotSupported();

        internal static OletxTransaction GetTransactionFromExportCookie(byte[] cookie, Guid txId)
            => throw NotSupported();

        internal static OletxTransaction GetOletxTransactionFromTransmitterPropagationToken(byte[] propagationToken)
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
    internal class OletxTransaction : ISerializable
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

        internal static byte[] GetExportCookie(byte[] whereaboutsCopy)
            => throw NotSupported();

        internal static byte[] GetTransmitterPropagationToken()
            => throw NotSupported();

        internal static IDtcTransaction GetDtcTransaction()
            => throw NotSupported();

        public void GetObjectData(SerializationInfo serializationInfo, StreamingContext context)
        {
            //if (serializationInfo == null)
            //{
            //    throw new ArgumentNullException(nameof(serializationInfo));
            //}

            //throw NotSupported();

            throw new PlatformNotSupportedException();
        }

        internal void Dispose()
        {
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
