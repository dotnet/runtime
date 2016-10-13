// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Transactions
{
    public sealed partial class CommittableTransaction : System.Transactions.Transaction, System.IAsyncResult, System.IDisposable, System.Runtime.Serialization.ISerializable
    {
        public CommittableTransaction() { }
        public CommittableTransaction(System.TimeSpan timeout) { }
        public CommittableTransaction(System.Transactions.TransactionOptions options) { }
        object System.IAsyncResult.AsyncState { get { throw null; } }
        System.Threading.WaitHandle System.IAsyncResult.AsyncWaitHandle { get { throw null; } }
        bool System.IAsyncResult.CompletedSynchronously { get { throw null; } }
        bool System.IAsyncResult.IsCompleted { get { throw null; } }
        public System.IAsyncResult BeginCommit(System.AsyncCallback callback, object user_defined_state) { throw null; }
        public void Commit() { }
        public void EndCommit(System.IAsyncResult ar) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public enum DependentCloneOption
    {
        BlockCommitUntilComplete = 0,
        RollbackIfNotComplete = 1,
    }
    public sealed partial class DependentTransaction : System.Transactions.Transaction, System.Runtime.Serialization.ISerializable
    {
        internal DependentTransaction() { }
        public void Complete() { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class Enlistment
    {
        internal Enlistment() { }
        public void Done() { }
    }
    [System.FlagsAttribute]
    public enum EnlistmentOptions
    {
        EnlistDuringPrepareRequired = 1,
        None = 0,
    }
    public enum EnterpriseServicesInteropOption
    {
        Automatic = 1,
        Full = 2,
        None = 0,
    }
    public delegate System.Transactions.Transaction HostCurrentTransactionCallback();
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface IDtcTransaction
    {
        void Abort(System.IntPtr manager, int whatever, int whatever2);
        void Commit(int whatever, int whatever2, int whatever3);
        void GetTransactionInfo(System.IntPtr whatever);
    }
    public partial interface IEnlistmentNotification
    {
        void Commit(System.Transactions.Enlistment enlistment);
        void InDoubt(System.Transactions.Enlistment enlistment);
        void Prepare(System.Transactions.PreparingEnlistment preparingEnlistment);
        void Rollback(System.Transactions.Enlistment enlistment);
    }
    public partial interface IPromotableSinglePhaseNotification : System.Transactions.ITransactionPromoter
    {
        void Initialize();
        void Rollback(System.Transactions.SinglePhaseEnlistment enlistment);
        void SinglePhaseCommit(System.Transactions.SinglePhaseEnlistment enlistment);
    }
    public partial interface ISimpleTransactionSuperior : System.Transactions.ITransactionPromoter
    {
        void Rollback();
    }
    public partial interface ISinglePhaseNotification : System.Transactions.IEnlistmentNotification
    {
        void SinglePhaseCommit(System.Transactions.SinglePhaseEnlistment enlistment);
    }
    public enum IsolationLevel
    {
        Chaos = 5,
        ReadCommitted = 2,
        ReadUncommitted = 3,
        RepeatableRead = 1,
        Serializable = 0,
        Snapshot = 4,
        Unspecified = 6,
    }
    public partial interface ITransactionPromoter
    {
        byte[] Promote();
    }
    public partial class PreparingEnlistment : System.Transactions.Enlistment
    {
        internal PreparingEnlistment() { }
        public void ForceRollback() { }
        public void ForceRollback(System.Exception ex) { }
        public void Prepared() { }
        public byte[] RecoveryInformation() { throw null; }
    }
    public partial class SinglePhaseEnlistment : System.Transactions.Enlistment
    {
        internal SinglePhaseEnlistment() { }
        public void Aborted() { }
        public void Aborted(System.Exception e) { }
        public void Committed() { }
        public void InDoubt() { }
        public void InDoubt(System.Exception e) { }
    }
    public sealed partial class SubordinateTransaction : System.Transactions.Transaction
    {
        public SubordinateTransaction(System.Transactions.IsolationLevel level, System.Transactions.ISimpleTransactionSuperior superior) { }
    }
    public partial class Transaction : System.IDisposable, System.Runtime.Serialization.ISerializable
    {
        internal Transaction() { }
        public static System.Transactions.Transaction Current { get { throw null; } set { } }
        public System.Transactions.IsolationLevel IsolationLevel { get { throw null; } }
        public System.Transactions.TransactionInformation TransactionInformation { get { throw null; } }
        public event System.Transactions.TransactionCompletedEventHandler TransactionCompleted { add { } remove { } }
        protected System.IAsyncResult BeginCommitInternal(System.AsyncCallback callback) { throw null; }
        public System.Transactions.Transaction Clone() { throw null; }
        public System.Transactions.DependentTransaction DependentClone(System.Transactions.DependentCloneOption option) { throw null; }
        public void Dispose() { }
        protected void EndCommitInternal(System.IAsyncResult ar) { }
        [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand)]
        public System.Transactions.Enlistment EnlistDurable(System.Guid manager, System.Transactions.IEnlistmentNotification notification, System.Transactions.EnlistmentOptions options) { throw null; }
        [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand)]
        public System.Transactions.Enlistment EnlistDurable(System.Guid manager, System.Transactions.ISinglePhaseNotification notification, System.Transactions.EnlistmentOptions options) { throw null; }
        public bool EnlistPromotableSinglePhase(System.Transactions.IPromotableSinglePhaseNotification notification) { throw null; }
        public System.Transactions.Enlistment EnlistVolatile(System.Transactions.IEnlistmentNotification notification, System.Transactions.EnlistmentOptions options) { throw null; }
        public System.Transactions.Enlistment EnlistVolatile(System.Transactions.ISinglePhaseNotification notification, System.Transactions.EnlistmentOptions options) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Transactions.Transaction x, System.Transactions.Transaction y) { throw null; }
        public static bool operator !=(System.Transactions.Transaction x, System.Transactions.Transaction y) { throw null; }
        public void Rollback() { }
        public void Rollback(System.Exception ex) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class TransactionAbortedException : System.Transactions.TransactionException
    {
        public TransactionAbortedException() { }
        protected TransactionAbortedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TransactionAbortedException(string message) { }
        public TransactionAbortedException(string message, System.Exception innerException) { }
    }
    public delegate void TransactionCompletedEventHandler(object o, System.Transactions.TransactionEventArgs e);
    public partial class TransactionEventArgs : System.EventArgs
    {
        public TransactionEventArgs() { }
        public System.Transactions.Transaction Transaction { get { throw null; } }
    }
    public partial class TransactionException : System.SystemException
    {
        protected TransactionException() { }
        protected TransactionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TransactionException(string message) { }
        public TransactionException(string message, System.Exception innerException) { }
    }
    public partial class TransactionInDoubtException : System.Transactions.TransactionException
    {
        protected TransactionInDoubtException() { }
        protected TransactionInDoubtException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TransactionInDoubtException(string message) { }
        public TransactionInDoubtException(string message, System.Exception innerException) { }
    }
    public partial class TransactionInformation
    {
        internal TransactionInformation() { }
        public System.DateTime CreationTime { get { throw null; } }
        public System.Guid DistributedIdentifier { get { throw null; } }
        public string LocalIdentifier { get { throw null; } }
        public System.Transactions.TransactionStatus Status { get { throw null; } }
    }
    public static partial class TransactionInterop
    {
        public static System.Transactions.IDtcTransaction GetDtcTransaction(System.Transactions.Transaction transaction) { throw null; }
        public static byte[] GetExportCookie(System.Transactions.Transaction transaction, byte[] exportCookie) { throw null; }
        public static System.Transactions.Transaction GetTransactionFromDtcTransaction(System.Transactions.IDtcTransaction dtc) { throw null; }
        public static System.Transactions.Transaction GetTransactionFromExportCookie(byte[] exportCookie) { throw null; }
        public static System.Transactions.Transaction GetTransactionFromTransmitterPropagationToken(byte[] token) { throw null; }
        public static byte[] GetTransmitterPropagationToken(System.Transactions.Transaction transaction) { throw null; }
        public static byte[] GetWhereabouts() { throw null; }
    }
    public static partial class TransactionManager
    {
        public static System.TimeSpan DefaultTimeout { get { throw null; } }
        public static System.Transactions.HostCurrentTransactionCallback HostCurrentCallback { get { throw null; } set { } }
        public static System.TimeSpan MaximumTimeout { get { throw null; } }
        public static event System.Transactions.TransactionStartedEventHandler DistributedTransactionStarted { add { } remove { } }
        public static void RecoveryComplete(System.Guid manager) { }
        public static System.Transactions.Enlistment Reenlist(System.Guid manager, byte[] recoveryInfo, System.Transactions.IEnlistmentNotification notification) { throw null; }
    }
    public partial class TransactionManagerCommunicationException : System.Transactions.TransactionException
    {
        protected TransactionManagerCommunicationException() { }
        protected TransactionManagerCommunicationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TransactionManagerCommunicationException(string message) { }
        public TransactionManagerCommunicationException(string message, System.Exception innerException) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TransactionOptions
    {
        public System.Transactions.IsolationLevel IsolationLevel { get { throw null; } set { } }
        public System.TimeSpan Timeout { get { throw null; } set { } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Transactions.TransactionOptions o1, System.Transactions.TransactionOptions o2) { throw null; }
        public static bool operator !=(System.Transactions.TransactionOptions o1, System.Transactions.TransactionOptions o2) { throw null; }
    }
    public partial class TransactionPromotionException : System.Transactions.TransactionException
    {
        protected TransactionPromotionException() { }
        protected TransactionPromotionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TransactionPromotionException(string message) { }
        public TransactionPromotionException(string message, System.Exception innerException) { }
    }
    public sealed partial class TransactionScope : System.IDisposable
    {
        public TransactionScope() { }
        public TransactionScope(System.Transactions.Transaction transaction) { }
        public TransactionScope(System.Transactions.Transaction transaction, System.TimeSpan timeout) { }
        public TransactionScope(System.Transactions.Transaction transaction, System.TimeSpan timeout, System.Transactions.EnterpriseServicesInteropOption opt) { }
        public TransactionScope(System.Transactions.TransactionScopeOption option) { }
        public TransactionScope(System.Transactions.TransactionScopeOption option, System.TimeSpan timeout) { }
        public TransactionScope(System.Transactions.TransactionScopeOption scopeOption, System.Transactions.TransactionOptions options) { }
        public TransactionScope(System.Transactions.TransactionScopeOption scopeOption, System.Transactions.TransactionOptions options, System.Transactions.EnterpriseServicesInteropOption opt) { }
        public void Complete() { }
        public void Dispose() { }
    }
    public enum TransactionScopeOption
    {
        Required = 0,
        RequiresNew = 1,
        Suppress = 2,
    }
    public delegate void TransactionStartedEventHandler(object o, System.Transactions.TransactionEventArgs e);
    public enum TransactionStatus
    {
        Aborted = 2,
        Active = 0,
        Committed = 1,
        InDoubt = 3,
    }
}
