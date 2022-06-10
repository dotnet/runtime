// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace System.Transactions.Diagnostics
{
    internal enum EnlistmentType
    {
        Volatile = 0,
        Durable = 1,
        PromotableSinglePhase = 2
    }

    internal enum NotificationCall
    {
        // IEnlistmentNotification
        Prepare = 0,
        Commit = 1,
        Rollback = 2,
        InDoubt = 3,
        // ISinglePhaseNotification
        SinglePhaseCommit = 4,
        // IPromotableSinglePhaseNotification
        Promote = 5

    }

    internal enum EnlistmentCallback
    {
        Done = 0,
        Prepared = 1,
        ForceRollback = 2,
        Committed = 3,
        Aborted = 4,
        InDoubt = 5
    }

    internal enum TransactionScopeResult
    {
        CreatedTransaction = 0,
        UsingExistingCurrent = 1,
        TransactionPassed = 2,
        DependentTransactionPassed = 3,
        NoTransaction = 4
    }

    /// <summary>
    /// TraceHelper is an internal class that is used by TraceRecord classes to write
    /// TransactionTraceIdentifiers and EnlistmentTraceIdentifiers to XmlWriters.
    /// </summary>
    internal static class TraceHelper
    {
        internal static void WriteTxId(XmlWriter writer, TransactionTraceIdentifier txTraceId)
        {
            writer.WriteStartElement("TransactionTraceIdentifier");
            if ( null != txTraceId.TransactionIdentifier )
            {
                writer.WriteElementString("TransactionIdentifier", txTraceId.TransactionIdentifier);
            }
            else
            {
                writer.WriteElementString("TransactionIdentifier", "");
            }

            // Don't write out CloneIdentifiers of 0 it's confusing.
            int cloneId = txTraceId.CloneIdentifier;
            if ( cloneId != 0 )
            {
                writer.WriteElementString("CloneIdentifier", cloneId.ToString( CultureInfo.CurrentCulture ));
            }

            writer.WriteEndElement();
        }

        internal static void WriteEnId(XmlWriter writer, EnlistmentTraceIdentifier enId)
        {
            writer.WriteStartElement("EnlistmentTraceIdentifier");
            writer.WriteElementString("ResourceManagerId", enId.ResourceManagerIdentifier.ToString());
            TraceHelper.WriteTxId(writer, enId.TransactionTraceId);
            writer.WriteElementString("EnlistmentIdentifier", enId.EnlistmentIdentifier.ToString( CultureInfo.CurrentCulture ) );
            writer.WriteEndElement();
        }

        internal static void WriteTraceSource( XmlWriter writer, string traceSource )
        {
            writer.WriteElementString( "TraceSource", traceSource );
        }
    }

    #region one

    /// <summary>
    /// Trace record for the TransactionCreated trace code.
    /// </summary>
    internal class TransactionCreatedTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionCreatedTraceRecord"; } }

        private static TransactionCreatedTraceRecord record = new TransactionCreatedTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.TransactionCreated,
                    SR.GetString( SR.TraceTransactionCreated ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource( xml, traceSource );
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }

    /// <summary>
    /// Trace record for the TransactionPromoted trace code.
    /// </summary>
    internal class TransactionPromotedTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionPromotedTraceRecord"; } }

        private static TransactionPromotedTraceRecord record = new TransactionPromotedTraceRecord();
        private TransactionTraceIdentifier localTxTraceId;
        private TransactionTraceIdentifier distTxTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier localTxTraceId, TransactionTraceIdentifier distTxTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.localTxTraceId = localTxTraceId;
                record.distTxTraceId = distTxTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.TransactionPromoted,
                    SR.GetString( SR.TraceTransactionPromoted ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource( xml, traceSource );
            xml.WriteStartElement("LightweightTransaction");
            TraceHelper.WriteTxId(xml, localTxTraceId);
            xml.WriteEndElement();
            xml.WriteStartElement("PromotedTransaction");
            TraceHelper.WriteTxId(xml, distTxTraceId);
            xml.WriteEndElement();
        }
    }

    /// <summary>
    /// Trace record for the Enlistment trace code.
    /// </summary>
    internal class EnlistmentTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "EnlistmentTraceRecord"; } }

        private static EnlistmentTraceRecord record = new EnlistmentTraceRecord();
        private EnlistmentTraceIdentifier enTraceId;
        private EnlistmentType enType;
        private EnlistmentOptions enOptions;
        string traceSource;

        internal static void Trace(string traceSource, EnlistmentTraceIdentifier enTraceId, EnlistmentType enType,
            EnlistmentOptions enOptions)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.enTraceId = enTraceId;
                record.enType = enType;
                record.enOptions = enOptions;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.Enlistment,
                    SR.GetString( SR.TraceEnlistment ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteEnId(xml, enTraceId);
            xml.WriteElementString("EnlistmentType", enType.ToString());
            xml.WriteElementString("EnlistmentOptions", enOptions.ToString());
        }
    }

    /// <summary>
    /// Trace record for the EnlistmentNotificationCall trace code.
    /// </summary>
    internal class EnlistmentNotificationCallTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "EnlistmentNotificationCallTraceRecord"; } }

        private static EnlistmentNotificationCallTraceRecord record = new EnlistmentNotificationCallTraceRecord();
        private EnlistmentTraceIdentifier enTraceId;
        private NotificationCall notCall;
        private string traceSource;

        internal static void Trace(string traceSource, EnlistmentTraceIdentifier enTraceId, NotificationCall notCall)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.enTraceId = enTraceId;
                record.notCall = notCall;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.EnlistmentNotificationCall,
                    SR.GetString( SR.TraceEnlistmentNotificationCall ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteEnId(xml, enTraceId);
            xml.WriteElementString("NotificationCall", notCall.ToString());
        }
    }

    /// <summary>
    /// Trace record for the EnlistmentCallbackPositive trace code.
    /// </summary>
    internal class EnlistmentCallbackPositiveTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "EnlistmentCallbackPositiveTraceRecord"; } }

        private static EnlistmentCallbackPositiveTraceRecord record = new EnlistmentCallbackPositiveTraceRecord();
        private EnlistmentTraceIdentifier enTraceId;
        private EnlistmentCallback callback;
        private string traceSource;

        internal static void Trace(string traceSource, EnlistmentTraceIdentifier enTraceId, EnlistmentCallback callback)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.enTraceId = enTraceId;
                record.callback = callback;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.EnlistmentCallbackPositive,
                    SR.GetString( SR.TraceEnlistmentCallbackPositive ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteEnId(xml, enTraceId);
            xml.WriteElementString("EnlistmentCallback", callback.ToString());
        }
    }

    /// <summary>
    /// Trace record for the EnlistmentCallbackNegative trace code.
    /// </summary>
    internal class EnlistmentCallbackNegativeTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "EnlistmentCallbackNegativeTraceRecord"; } }

        private static EnlistmentCallbackNegativeTraceRecord record = new EnlistmentCallbackNegativeTraceRecord();
        private EnlistmentTraceIdentifier enTraceId;
        private EnlistmentCallback callback;
        private string traceSource;

        internal static void Trace(string traceSource, EnlistmentTraceIdentifier enTraceId, EnlistmentCallback callback)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.enTraceId = enTraceId;
                record.callback = callback;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.EnlistmentCallbackNegative,
                    SR.GetString( SR.TraceEnlistmentCallbackNegative ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteEnId(xml, enTraceId);
            xml.WriteElementString("EnlistmentCallback", callback.ToString());
        }
    }
    #endregion

    #region two
    /// <summary>
    /// Trace record for the TransactionCommitCalled trace code.
    /// </summary>
    internal class TransactionCommitCalledTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionCommitCalledTraceRecord"; } }

        private static TransactionCommitCalledTraceRecord record = new TransactionCommitCalledTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.TransactionCommitCalled,
                    SR.GetString( SR.TraceTransactionCommitCalled ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }

    /// <summary>
    /// Trace record for the TransactionRollbackCalled trace code.
    /// </summary>
    internal class TransactionRollbackCalledTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionRollbackCalledTraceRecord"; } }

        private static TransactionRollbackCalledTraceRecord record = new TransactionRollbackCalledTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionRollbackCalled,
                    SR.GetString( SR.TraceTransactionRollbackCalled ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }

    /// <summary>
    /// Trace record for the TransactionCommitted trace code.
    /// </summary>
    internal class TransactionCommittedTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionCommittedTraceRecord"; } }

        private static TransactionCommittedTraceRecord record = new TransactionCommittedTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.TransactionCommitted,
                    SR.GetString( SR.TraceTransactionCommitted ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }

    /// <summary>
    /// Trace record for the TransactionAborted trace code.
    /// </summary>
    internal class TransactionAbortedTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionAbortedTraceRecord"; } }

        private static TransactionAbortedTraceRecord record = new TransactionAbortedTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionAborted,
                    SR.GetString( SR.TraceTransactionAborted ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }

    /// <summary>
    /// Trace record for the TransactionInDoubt trace code.
    /// </summary>
    internal class TransactionInDoubtTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionInDoubtTraceRecord"; } }

        private static TransactionInDoubtTraceRecord record = new TransactionInDoubtTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionInDoubt,
                    SR.GetString( SR.TraceTransactionInDoubt ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }

    /// <summary>
    /// Trace record for the TransactionScopeCreated trace code.
    /// </summary>
    internal class TransactionScopeCreatedTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionScopeCreatedTraceRecord"; } }

        private static TransactionScopeCreatedTraceRecord record = new TransactionScopeCreatedTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private TransactionScopeResult txScopeResult;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId, TransactionScopeResult txScopeResult)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                record.txScopeResult = txScopeResult;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.TransactionScopeCreated,
                    SR.GetString( SR.TraceTransactionScopeCreated ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
            xml.WriteElementString("TransactionScopeResult", txScopeResult.ToString());
        }
    }

    /// <summary>
    /// Trace record for the TransactionScopeDisposed trace code.
    /// </summary>
    internal class TransactionScopeDisposedTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionScopeDisposedTraceRecord"; } }

        private static TransactionScopeDisposedTraceRecord record = new TransactionScopeDisposedTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.TransactionScopeDisposed,
                    SR.GetString( SR.TraceTransactionScopeDisposed ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }

    /// <summary>
    /// Trace record for the TransactionScopeIncomplete trace code.
    /// </summary>
    internal class TransactionScopeIncompleteTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionScopeIncompleteTraceRecord"; } }

        private static TransactionScopeIncompleteTraceRecord record = new TransactionScopeIncompleteTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionScopeIncomplete,
                    SR.GetString( SR.TraceTransactionScopeIncomplete ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }

    /// <summary>
    /// Trace record for the TransactionScopeNestedIncorrectly trace code.
    /// </summary>
    internal class TransactionScopeNestedIncorrectlyTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionScopeNestedIncorrectlyTraceRecord"; } }

        private static TransactionScopeNestedIncorrectlyTraceRecord record = new TransactionScopeNestedIncorrectlyTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionScopeNestedIncorrectly,
                    SR.GetString( SR.TraceTransactionScopeNestedIncorrectly ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }


    /// <summary>
    /// Trace record for the TransactionScopeCurrentChanged trace code.
    /// </summary>
    internal class TransactionScopeCurrentChangedTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionScopeCurrentChangedTraceRecord"; } }

        private static TransactionScopeCurrentChangedTraceRecord record = new TransactionScopeCurrentChangedTraceRecord();
        private TransactionTraceIdentifier scopeTxTraceId;
        private TransactionTraceIdentifier currentTxTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier scopeTxTraceId, TransactionTraceIdentifier currentTxTraceId )
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.scopeTxTraceId = scopeTxTraceId;
                record.currentTxTraceId = currentTxTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionScopeCurrentTransactionChanged,
                    SR.GetString( SR.TraceTransactionScopeCurrentTransactionChanged ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, scopeTxTraceId);
            TraceHelper.WriteTxId(xml, currentTxTraceId);
        }
    }

    /// <summary>
    /// Trace record for the TransactionScopeTimeoutTraceRecord trace code.
    /// </summary>
    internal class TransactionScopeTimeoutTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionScopeTimeoutTraceRecord"; } }

        private static TransactionScopeTimeoutTraceRecord record = new TransactionScopeTimeoutTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionScopeTimeout,
                    SR.GetString( SR.TraceTransactionScopeTimeout ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }


    /// <summary>
    /// Trace record for the TransactionTimeoutTraceRecord trace code.
    /// </summary>
    internal class TransactionTimeoutTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionTimeoutTraceRecord"; } }

        private static TransactionTimeoutTraceRecord record = new TransactionTimeoutTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionTimeout,
                    SR.GetString( SR.TraceTransactionTimeout ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }



    /// <summary>
    /// Trace record for the DependentCloneCreatedTraceRecord trace code.
    /// </summary>
    internal class DependentCloneCreatedTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "DependentCloneCreatedTraceRecord"; } }

        private static DependentCloneCreatedTraceRecord record = new DependentCloneCreatedTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private DependentCloneOption option;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId, DependentCloneOption option)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                record.option = option;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.DependentCloneCreated,
                    SR.GetString( SR.TraceDependentCloneCreated ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
            xml.WriteElementString("DependentCloneOption", option.ToString());
        }
    }

    /// <summary>
    /// Trace record for the DependentCloneComplete trace code.
    /// </summary>
    internal class DependentCloneCompleteTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "DependentCloneCompleteTraceRecord"; } }

        private static DependentCloneCompleteTraceRecord record = new DependentCloneCompleteTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.DependentCloneComplete,
                    SR.GetString( SR.TraceDependentCloneComplete ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }


    /// <summary>
    /// Trace record for the CloneCreated trace code.
    /// </summary>
    internal class CloneCreatedTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "CloneCreatedTraceRecord"; } }

        private static CloneCreatedTraceRecord record = new CloneCreatedTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.CloneCreated,
                    SR.GetString( SR.TraceCloneCreated ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }

    #endregion

    #region three
    /// <summary>
    /// Trace record for the RecoveryComplete trace code.
    /// </summary>
    internal class RecoveryCompleteTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "RecoveryCompleteTraceRecord"; } }

        private static RecoveryCompleteTraceRecord record = new RecoveryCompleteTraceRecord();
        private Guid rmId;
        private string traceSource;

        internal static void Trace(string traceSource, Guid rmId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.rmId = rmId;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.RecoveryComplete,
                    SR.GetString( SR.TraceRecoveryComplete ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            xml.WriteElementString("ResourceManagerId", rmId.ToString());
        }
    }



    /// <summary>
    /// Trace record for the Reenlist trace code.
    /// </summary>
    internal class ReenlistTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "ReenlistTraceRecord"; } }

        private static ReenlistTraceRecord record = new ReenlistTraceRecord();
        private Guid rmId;
        private string traceSource;

        internal static void Trace(string traceSource, Guid rmId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.rmId = rmId;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.Reenlist,
                    SR.GetString( SR.TraceReenlist ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            xml.WriteElementString("ResourceManagerId", rmId.ToString());
        }
    }

    /// <summary>
    /// </summary>
    internal class DistributedTransactionManagerCreatedTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionManagerCreatedTraceRecord"; } }

        private static DistributedTransactionManagerCreatedTraceRecord record = new DistributedTransactionManagerCreatedTraceRecord();
        private Type tmType;
        private string nodeName;
        private string traceSource;

        internal static void Trace(string traceSource, Type tmType, string nodeName)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.tmType = tmType;
                record.nodeName = nodeName;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.TransactionManagerCreated,
                    SR.GetString( SR.TraceTransactionManagerCreated ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            xml.WriteElementString("TransactionManagerType", tmType.ToString());
            xml.WriteStartElement("TransactionManagerProperties");
            xml.WriteElementString("DistributedTransactionManagerName", nodeName);

            xml.WriteEndElement();
        }
    }
    #endregion

    #region four
    /// <summary>
    /// Trace record for the TransactionSerialized trace code.
    /// </summary>
    internal class TransactionSerializedTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionSerializedTraceRecord"; } }

        private static TransactionSerializedTraceRecord record = new TransactionSerializedTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.TransactionSerialized,
                    SR.GetString( SR.TraceTransactionSerialized ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }

    /// <summary>
    /// Trace record for the TransactionDeserialized trace code.
    /// </summary>
    internal class TransactionDeserializedTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionDeserializedTraceRecord"; } }

        private static TransactionDeserializedTraceRecord record = new TransactionDeserializedTraceRecord();
        private TransactionTraceIdentifier txTraceId;
        private string traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.TransactionDeserialized,
                    SR.GetString( SR.TraceTransactionDeserialized ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            TraceHelper.WriteTxId(xml, txTraceId);
        }
    }

    /// <summary>
    /// Trace record for the TransactionException trace code.
    /// </summary>
    internal class TransactionExceptionTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "TransactionExceptionTraceRecord"; } }

        private static TransactionExceptionTraceRecord record = new TransactionExceptionTraceRecord();
        private string exceptionMessage;
        private string traceSource;

        internal static void Trace(string traceSource, string exceptionMessage)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.exceptionMessage = exceptionMessage;
                DiagnosticTrace.TraceEvent(TraceEventType.Error,
                    TransactionsTraceCode.TransactionException,
                    SR.GetString( SR.TraceTransactionException ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            xml.WriteElementString("ExceptionMessage", exceptionMessage);
        }
    }

    class DictionaryTraceRecord : TraceRecord
    {
        System.Collections.IDictionary dictionary;

        internal DictionaryTraceRecord(System.Collections.IDictionary dictionary)
        {
            this.dictionary = dictionary;
        }

        internal override string EventId { get { return TraceRecord.EventIdBase + "Dictionary" + TraceRecord.NamespaceSuffix; } }

        internal override void WriteTo(XmlWriter xml)
        {
            if (this.dictionary != null)
            {
                foreach (object key in this.dictionary.Keys)
                {
                    xml.WriteElementString(key.ToString(), this.dictionary[key].ToString());
                }
            }
        }

        public override string ToString()
        {
            string retval = null;
            if (this.dictionary != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (object key in this.dictionary.Keys)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", key, this.dictionary[key].ToString()));
                }
            }
            return retval;
        }


    }

    /// <summary>
    /// Trace record for the ExceptionConsumed trace code.
    /// </summary>
    internal class ExceptionConsumedTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "ExceptionConsumedTraceRecord"; } }

        private static ExceptionConsumedTraceRecord record = new ExceptionConsumedTraceRecord();
        private Exception exception;
        private string traceSource;

        internal static void Trace(string traceSource, Exception exception)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.exception = exception;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.ExceptionConsumed,
                    SR.GetString( SR.TraceExceptionConsumed ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            xml.WriteElementString("ExceptionMessage", this.exception.Message);
            xml.WriteElementString("ExceptionStack", this.exception.StackTrace );
        }
    }


    /// <summary>
    /// Trace record for the InvalidOperationException trace code.
    /// </summary>
    internal class InvalidOperationExceptionTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "InvalidOperationExceptionTraceRecord"; } }

        private static InvalidOperationExceptionTraceRecord record = new InvalidOperationExceptionTraceRecord();
        private string exceptionMessage;
        private string traceSource;

        internal static void Trace(string traceSource, string exceptionMessage)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.exceptionMessage = exceptionMessage;
                DiagnosticTrace.TraceEvent(TraceEventType.Error,
                    TransactionsTraceCode.InvalidOperationException,
                    SR.GetString( SR.TraceInvalidOperationException ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            xml.WriteElementString("ExceptionMessage", exceptionMessage);
        }
    }


    /// <summary>
    /// Trace record for the InternalError trace code.
    /// </summary>
    internal class InternalErrorTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "InternalErrorTraceRecord"; } }

        private static InternalErrorTraceRecord record = new InternalErrorTraceRecord();
        private string exceptionMessage;
        private string traceSource;

        internal static void Trace(string traceSource, string exceptionMessage)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.exceptionMessage = exceptionMessage;
                DiagnosticTrace.TraceEvent(TraceEventType.Critical,
                    TransactionsTraceCode.InternalError,
                    SR.GetString( SR.TraceInternalError ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            xml.WriteElementString("ExceptionMessage", exceptionMessage);
        }
    }

    /// <summary>
    /// Trace record for the MethodEntered trace code.
    /// </summary>
    internal class MethodEnteredTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "MethodEnteredTraceRecord"; } }

        private static MethodEnteredTraceRecord record = new MethodEnteredTraceRecord();
        private string methodName;
        private string traceSource;

        internal static void Trace(string traceSource, string methodName)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.methodName = methodName;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.MethodEntered,
                    SR.GetString( SR.TraceMethodEntered ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            xml.WriteElementString("MethodName", methodName);
        }
    }

    /// <summary>
    /// Trace record for the MethodExited trace code.
    /// </summary>
    internal class MethodExitedTraceRecord : TraceRecord
    {

        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "MethodExitedTraceRecord"; } }

        private static MethodExitedTraceRecord record = new MethodExitedTraceRecord();
        private string methodName;
        private string traceSource;

        internal static void Trace(string traceSource, string methodName)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                record.methodName = methodName;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.MethodExited,
                    SR.GetString( SR.TraceMethodExited ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
            xml.WriteElementString("MethodName", methodName);
        }
    }

    /// <summary>
    /// Trace record for the MethodEntered trace code.
    /// </summary>
    internal class ConfiguredDefaultTimeoutAdjustedTraceRecord : TraceRecord
    {
        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal override string EventId { get { return EventIdBase + "ConfiguredDefaultTimeoutAdjustedTraceRecord"; } }

        private static ConfiguredDefaultTimeoutAdjustedTraceRecord record = new ConfiguredDefaultTimeoutAdjustedTraceRecord();
        private string traceSource;

        internal static void Trace(string traceSource)
        {
            lock (record)
            {
                record.traceSource = traceSource;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.ConfiguredDefaultTimeoutAdjusted,
                    SR.GetString( SR.TraceConfiguredDefaultTimeoutAdjusted ),
                    record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, traceSource);
        }
    }

    #endregion
}
