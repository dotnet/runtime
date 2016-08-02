// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Diagnostics;
using System.Globalization;

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
            if (null != txTraceId.TransactionIdentifier)
            {
                writer.WriteElementString("TransactionIdentifier", txTraceId.TransactionIdentifier);
            }
            else
            {
                writer.WriteElementString("TransactionIdentifier", "");
            }

            // Don't write out CloneIdentifiers of 0 it's confusing.
            int cloneId = txTraceId.CloneIdentifier;
            if (cloneId != 0)
            {
                writer.WriteElementString("CloneIdentifier", cloneId.ToString(CultureInfo.CurrentCulture));
            }

            writer.WriteEndElement();
        }

        internal static void WriteEnId(XmlWriter writer, EnlistmentTraceIdentifier enId)
        {
            writer.WriteStartElement("EnlistmentTraceIdentifier");
            writer.WriteElementString("ResourceManagerId", enId.ResourceManagerIdentifier.ToString());
            TraceHelper.WriteTxId(writer, enId.TransactionTraceId);
            writer.WriteElementString("EnlistmentIdentifier", enId.EnlistmentIdentifier.ToString(CultureInfo.CurrentCulture));
            writer.WriteEndElement();
        }

        internal static void WriteTraceSource(XmlWriter writer, string traceSource)
        {
            writer.WriteElementString("TraceSource", traceSource);
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

        private static TransactionCreatedTraceRecord s_record = new TransactionCreatedTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.TransactionCreated,
                    SR.TraceTransactionCreated,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static TransactionPromotedTraceRecord s_record = new TransactionPromotedTraceRecord();
        private TransactionTraceIdentifier _localTxTraceId;
        private TransactionTraceIdentifier _distTxTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier localTxTraceId, TransactionTraceIdentifier distTxTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._localTxTraceId = localTxTraceId;
                s_record._distTxTraceId = distTxTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.TransactionPromoted,
                    SR.TraceTransactionPromoted,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            xml.WriteStartElement("LightweightTransaction");
            TraceHelper.WriteTxId(xml, _localTxTraceId);
            xml.WriteEndElement();
            xml.WriteStartElement("PromotedTransaction");
            TraceHelper.WriteTxId(xml, _distTxTraceId);
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

        private static EnlistmentTraceRecord s_record = new EnlistmentTraceRecord();
        private EnlistmentTraceIdentifier _enTraceId;
        private EnlistmentType _enType;
        private EnlistmentOptions _enOptions;
        private string _traceSource;

        internal static void Trace(string traceSource, EnlistmentTraceIdentifier enTraceId, EnlistmentType enType,
            EnlistmentOptions enOptions)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._enTraceId = enTraceId;
                s_record._enType = enType;
                s_record._enOptions = enOptions;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.Enlistment,
                    SR.TraceEnlistment,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteEnId(xml, _enTraceId);
            xml.WriteElementString("EnlistmentType", _enType.ToString());
            xml.WriteElementString("EnlistmentOptions", _enOptions.ToString());
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

        private static EnlistmentNotificationCallTraceRecord s_record = new EnlistmentNotificationCallTraceRecord();
        private EnlistmentTraceIdentifier _enTraceId;
        private NotificationCall _notCall;
        private string _traceSource;

        internal static void Trace(string traceSource, EnlistmentTraceIdentifier enTraceId, NotificationCall notCall)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._enTraceId = enTraceId;
                s_record._notCall = notCall;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.EnlistmentNotificationCall,
                    SR.TraceEnlistmentNotificationCall,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteEnId(xml, _enTraceId);
            xml.WriteElementString("NotificationCall", _notCall.ToString());
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

        private static EnlistmentCallbackPositiveTraceRecord s_record = new EnlistmentCallbackPositiveTraceRecord();
        private EnlistmentTraceIdentifier _enTraceId;
        private EnlistmentCallback _callback;
        private string _traceSource;

        internal static void Trace(string traceSource, EnlistmentTraceIdentifier enTraceId, EnlistmentCallback callback)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._enTraceId = enTraceId;
                s_record._callback = callback;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.EnlistmentCallbackPositive,
                    SR.TraceEnlistmentCallbackPositive,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteEnId(xml, _enTraceId);
            xml.WriteElementString("EnlistmentCallback", _callback.ToString());
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

        private static EnlistmentCallbackNegativeTraceRecord s_record = new EnlistmentCallbackNegativeTraceRecord();
        private EnlistmentTraceIdentifier _enTraceId;
        private EnlistmentCallback _callback;
        private string _traceSource;

        internal static void Trace(string traceSource, EnlistmentTraceIdentifier enTraceId, EnlistmentCallback callback)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._enTraceId = enTraceId;
                s_record._callback = callback;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.EnlistmentCallbackNegative,
                    SR.TraceEnlistmentCallbackNegative,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteEnId(xml, _enTraceId);
            xml.WriteElementString("EnlistmentCallback", _callback.ToString());
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

        private static TransactionCommitCalledTraceRecord s_record = new TransactionCommitCalledTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.TransactionCommitCalled,
                    SR.TraceTransactionCommitCalled,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static TransactionRollbackCalledTraceRecord s_record = new TransactionRollbackCalledTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionRollbackCalled,
                    SR.TraceTransactionRollbackCalled,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static TransactionCommittedTraceRecord s_record = new TransactionCommittedTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.TransactionCommitted,
                    SR.TraceTransactionCommitted,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static TransactionAbortedTraceRecord s_record = new TransactionAbortedTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionAborted,
                    SR.TraceTransactionAborted,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static TransactionInDoubtTraceRecord s_record = new TransactionInDoubtTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionInDoubt,
                    SR.TraceTransactionInDoubt,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static TransactionScopeCreatedTraceRecord s_record = new TransactionScopeCreatedTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private TransactionScopeResult _txScopeResult;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId, TransactionScopeResult txScopeResult)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                s_record._txScopeResult = txScopeResult;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.TransactionScopeCreated,
                    SR.TraceTransactionScopeCreated,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
            xml.WriteElementString("TransactionScopeResult", _txScopeResult.ToString());
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

        private static TransactionScopeDisposedTraceRecord s_record = new TransactionScopeDisposedTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.TransactionScopeDisposed,
                    SR.TraceTransactionScopeDisposed,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static TransactionScopeIncompleteTraceRecord s_record = new TransactionScopeIncompleteTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionScopeIncomplete,
                    SR.TraceTransactionScopeIncomplete,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static TransactionScopeNestedIncorrectlyTraceRecord s_record = new TransactionScopeNestedIncorrectlyTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionScopeNestedIncorrectly,
                    SR.TraceTransactionScopeNestedIncorrectly,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static TransactionScopeCurrentChangedTraceRecord s_record = new TransactionScopeCurrentChangedTraceRecord();
        private TransactionTraceIdentifier _scopeTxTraceId;
        private TransactionTraceIdentifier _currentTxTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier scopeTxTraceId, TransactionTraceIdentifier currentTxTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._scopeTxTraceId = scopeTxTraceId;
                s_record._currentTxTraceId = currentTxTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionScopeCurrentTransactionChanged,
                    SR.TraceTransactionScopeCurrentTransactionChanged,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _scopeTxTraceId);
            TraceHelper.WriteTxId(xml, _currentTxTraceId);
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

        private static TransactionScopeTimeoutTraceRecord s_record = new TransactionScopeTimeoutTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionScopeTimeout,
                    SR.TraceTransactionScopeTimeout,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static TransactionTimeoutTraceRecord s_record = new TransactionTimeoutTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.TransactionTimeout,
                    SR.TraceTransactionTimeout,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static DependentCloneCreatedTraceRecord s_record = new DependentCloneCreatedTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private DependentCloneOption _option;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId, DependentCloneOption option)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                s_record._option = option;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.DependentCloneCreated,
                    SR.TraceDependentCloneCreated,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
            xml.WriteElementString("DependentCloneOption", _option.ToString());
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

        private static DependentCloneCompleteTraceRecord s_record = new DependentCloneCompleteTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.DependentCloneComplete,
                    SR.TraceDependentCloneComplete,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static CloneCreatedTraceRecord s_record = new CloneCreatedTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.CloneCreated,
                    SR.TraceCloneCreated,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static RecoveryCompleteTraceRecord s_record = new RecoveryCompleteTraceRecord();
        private Guid _rmId;
        private string _traceSource;

        internal static void Trace(string traceSource, Guid rmId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._rmId = rmId;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.RecoveryComplete,
                    SR.TraceRecoveryComplete,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            xml.WriteElementString("ResourceManagerId", _rmId.ToString());
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

        private static ReenlistTraceRecord s_record = new ReenlistTraceRecord();
        private Guid _rmId;
        private string _traceSource;

        internal static void Trace(string traceSource, Guid rmId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._rmId = rmId;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.Reenlist,
                    SR.TraceReenlist,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            xml.WriteElementString("ResourceManagerId", _rmId.ToString());
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

        private static DistributedTransactionManagerCreatedTraceRecord s_record = new DistributedTransactionManagerCreatedTraceRecord();
        private Type _tmType;
        private string _nodeName;
        private string _traceSource;

        internal static void Trace(string traceSource, Type tmType, string nodeName)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._tmType = tmType;
                s_record._nodeName = nodeName;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.TransactionManagerCreated,
                    SR.TraceTransactionManagerCreated,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            xml.WriteElementString("TransactionManagerType", _tmType.ToString());
            xml.WriteStartElement("TransactionManagerProperties");
            xml.WriteElementString("DistributedTransactionManagerName", _nodeName);

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

        private static TransactionSerializedTraceRecord s_record = new TransactionSerializedTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Information,
                    TransactionsTraceCode.TransactionSerialized,
                    SR.TraceTransactionSerialized,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static TransactionDeserializedTraceRecord s_record = new TransactionDeserializedTraceRecord();
        private TransactionTraceIdentifier _txTraceId;
        private string _traceSource;

        internal static void Trace(string traceSource, TransactionTraceIdentifier txTraceId)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._txTraceId = txTraceId;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.TransactionDeserialized,
                    SR.TraceTransactionDeserialized,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            TraceHelper.WriteTxId(xml, _txTraceId);
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

        private static TransactionExceptionTraceRecord s_record = new TransactionExceptionTraceRecord();
        private string _exceptionMessage;
        private string _traceSource;

        internal static void Trace(string traceSource, string exceptionMessage)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._exceptionMessage = exceptionMessage;
                DiagnosticTrace.TraceEvent(TraceEventType.Error,
                    TransactionsTraceCode.TransactionException,
                    SR.TraceTransactionException,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            xml.WriteElementString("ExceptionMessage", _exceptionMessage);
        }
    }

    internal class DictionaryTraceRecord : TraceRecord
    {
        private System.Collections.IDictionary _dictionary;

        internal DictionaryTraceRecord(System.Collections.IDictionary dictionary)
        {
            _dictionary = dictionary;
        }

        internal override string EventId { get { return TraceRecord.EventIdBase + "Dictionary" + TraceRecord.NamespaceSuffix; } }

        internal override void WriteTo(XmlWriter xml)
        {
            if (_dictionary != null)
            {
                foreach (object key in _dictionary.Keys)
                {
                    xml.WriteElementString(key.ToString(), _dictionary[key].ToString());
                }
            }
        }

        public override string ToString()
        {
            string retval = null;
            if (_dictionary != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (object key in _dictionary.Keys)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", key, _dictionary[key].ToString()));
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

        private static ExceptionConsumedTraceRecord s_record = new ExceptionConsumedTraceRecord();
        private Exception _exception;
        private string _traceSource;

        internal static void Trace(string traceSource, Exception exception)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._exception = exception;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.ExceptionConsumed,
                    SR.TraceExceptionConsumed,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            xml.WriteElementString("ExceptionMessage", _exception.Message);
            xml.WriteElementString("ExceptionStack", _exception.StackTrace);
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

        private static InvalidOperationExceptionTraceRecord s_record = new InvalidOperationExceptionTraceRecord();
        private string _exceptionMessage;
        private string _traceSource;

        internal static void Trace(string traceSource, string exceptionMessage)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._exceptionMessage = exceptionMessage;
                DiagnosticTrace.TraceEvent(TraceEventType.Error,
                    TransactionsTraceCode.InvalidOperationException,
                    SR.TraceInvalidOperationException,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            xml.WriteElementString("ExceptionMessage", _exceptionMessage);
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

        private static InternalErrorTraceRecord s_record = new InternalErrorTraceRecord();
        private string _exceptionMessage;
        private string _traceSource;

        internal static void Trace(string traceSource, string exceptionMessage)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._exceptionMessage = exceptionMessage;
                DiagnosticTrace.TraceEvent(TraceEventType.Critical,
                    TransactionsTraceCode.InternalError,
                    SR.TraceInternalError,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            xml.WriteElementString("ExceptionMessage", _exceptionMessage);
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

        private static MethodEnteredTraceRecord s_record = new MethodEnteredTraceRecord();
        private string _methodName;
        private string _traceSource;

        internal static void Trace(string traceSource, string methodName)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._methodName = methodName;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.MethodEntered,
                    SR.TraceMethodEntered,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            xml.WriteElementString("MethodName", _methodName);
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

        private static MethodExitedTraceRecord s_record = new MethodExitedTraceRecord();
        private string _methodName;
        private string _traceSource;

        internal static void Trace(string traceSource, string methodName)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                s_record._methodName = methodName;
                DiagnosticTrace.TraceEvent(TraceEventType.Verbose,
                    TransactionsTraceCode.MethodExited,
                    SR.TraceMethodExited,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
            xml.WriteElementString("MethodName", _methodName);
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

        private static ConfiguredDefaultTimeoutAdjustedTraceRecord s_record = new ConfiguredDefaultTimeoutAdjustedTraceRecord();
        private string _traceSource;

        internal static void Trace(string traceSource)
        {
            lock (s_record)
            {
                s_record._traceSource = traceSource;
                DiagnosticTrace.TraceEvent(TraceEventType.Warning,
                    TransactionsTraceCode.ConfiguredDefaultTimeoutAdjusted,
                    SR.TraceConfiguredDefaultTimeoutAdjusted,
                    s_record);
            }
        }

        internal override void WriteTo(XmlWriter xml)
        {
            TraceHelper.WriteTraceSource(xml, _traceSource);
        }
    }

    #endregion
}
