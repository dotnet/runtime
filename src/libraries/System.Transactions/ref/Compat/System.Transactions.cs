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
        object System.IAsyncResult.AsyncState { get { return default(object); } }
        System.Threading.WaitHandle System.IAsyncResult.AsyncWaitHandle { get { return default(System.Threading.WaitHandle); } }
        bool System.IAsyncResult.CompletedSynchronously { get { return default(bool); } }
        bool System.IAsyncResult.IsCompleted { get { return default(bool); } }
        public System.IAsyncResult BeginCommit(System.AsyncCallback callback, object user_defined_state) { return default(System.IAsyncResult); }
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
        public byte[] RecoveryInformation() { return default(byte[]); }
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
        public static System.Transactions.Transaction Current { get { return default(System.Transactions.Transaction); } set { } }
        public System.Transactions.IsolationLevel IsolationLevel { get { return default(System.Transactions.IsolationLevel); } }
        public System.Transactions.TransactionInformation TransactionInformation { get { return default(System.Transactions.TransactionInformation); } }
        public event System.Transactions.TransactionCompletedEventHandler TransactionCompleted { add { } remove { } }
        protected System.IAsyncResult BeginCommitInternal(System.AsyncCallback callback) { return default(System.IAsyncResult); }
        public System.Transactions.Transaction Clone() { return default(System.Transactions.Transaction); }
        public System.Transactions.DependentTransaction DependentClone(System.Transactions.DependentCloneOption option) { return default(System.Transactions.DependentTransaction); }
        public void Dispose() { }
        protected void EndCommitInternal(System.IAsyncResult ar) { }
        [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand)]
        public System.Transactions.Enlistment EnlistDurable(System.Guid manager, System.Transactions.IEnlistmentNotification notification, System.Transactions.EnlistmentOptions options) { return default(System.Transactions.Enlistment); }
        [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand)]
        public System.Transactions.Enlistment EnlistDurable(System.Guid manager, System.Transactions.ISinglePhaseNotification notification, System.Transactions.EnlistmentOptions options) { return default(System.Transactions.Enlistment); }
        public bool EnlistPromotableSinglePhase(System.Transactions.IPromotableSinglePhaseNotification notification) { return default(bool); }
        public System.Transactions.Enlistment EnlistVolatile(System.Transactions.IEnlistmentNotification notification, System.Transactions.EnlistmentOptions options) { return default(System.Transactions.Enlistment); }
        public System.Transactions.Enlistment EnlistVolatile(System.Transactions.ISinglePhaseNotification notification, System.Transactions.EnlistmentOptions options) { return default(System.Transactions.Enlistment); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Transactions.Transaction x, System.Transactions.Transaction y) { return default(bool); }
        public static bool operator !=(System.Transactions.Transaction x, System.Transactions.Transaction y) { return default(bool); }
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
        public System.Transactions.Transaction Transaction { get { return default(System.Transactions.Transaction); } }
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
        public System.DateTime CreationTime { get { return default(System.DateTime); } }
        public System.Guid DistributedIdentifier { get { return default(System.Guid); } }
        public string LocalIdentifier { get { return default(string); } }
        public System.Transactions.TransactionStatus Status { get { return default(System.Transactions.TransactionStatus); } }
    }
    public static partial class TransactionInterop
    {
        public static System.Transactions.IDtcTransaction GetDtcTransaction(System.Transactions.Transaction transaction) { return default(System.Transactions.IDtcTransaction); }
        public static byte[] GetExportCookie(System.Transactions.Transaction transaction, byte[] exportCookie) { return default(byte[]); }
        public static System.Transactions.Transaction GetTransactionFromDtcTransaction(System.Transactions.IDtcTransaction dtc) { return default(System.Transactions.Transaction); }
        public static System.Transactions.Transaction GetTransactionFromExportCookie(byte[] exportCookie) { return default(System.Transactions.Transaction); }
        public static System.Transactions.Transaction GetTransactionFromTransmitterPropagationToken(byte[] token) { return default(System.Transactions.Transaction); }
        public static byte[] GetTransmitterPropagationToken(System.Transactions.Transaction transaction) { return default(byte[]); }
        public static byte[] GetWhereabouts() { return default(byte[]); }
    }
    public static partial class TransactionManager
    {
        public static System.TimeSpan DefaultTimeout { get { return default(System.TimeSpan); } }
        public static System.Transactions.HostCurrentTransactionCallback HostCurrentCallback { get { return default(System.Transactions.HostCurrentTransactionCallback); } set { } }
        public static System.TimeSpan MaximumTimeout { get { return default(System.TimeSpan); } }
        public static event System.Transactions.TransactionStartedEventHandler DistributedTransactionStarted { add { } remove { } }
        public static void RecoveryComplete(System.Guid manager) { }
        public static System.Transactions.Enlistment Reenlist(System.Guid manager, byte[] recoveryInfo, System.Transactions.IEnlistmentNotification notification) { return default(System.Transactions.Enlistment); }
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
        public System.Transactions.IsolationLevel IsolationLevel { get { return default(System.Transactions.IsolationLevel); } set { } }
        public System.TimeSpan Timeout { get { return default(System.TimeSpan); } set { } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Transactions.TransactionOptions o1, System.Transactions.TransactionOptions o2) { return default(bool); }
        public static bool operator !=(System.Transactions.TransactionOptions o1, System.Transactions.TransactionOptions o2) { return default(bool); }
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
