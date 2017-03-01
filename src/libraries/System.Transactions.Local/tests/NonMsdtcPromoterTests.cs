// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Transactions.Tests
{
    public class NonMsdtcPromoterTests
    {
        public static string PromotedTokenString1 = "Promoted Token String Number 1";
        public static byte[] PromotedToken1 = StringToByteArray(PromotedTokenString1);
        public static Guid PromoterType1 = new Guid("D9A34FDF-D02A-4EED-98C3-5AD092355E17");

        private static bool s_traceEnabled = true;

        private static MethodInfo s_enlistPromotableSinglePhaseMethodInfo;
        private static MethodInfo s_setDistributedTransactionIdentifierMethodInfo;
        private static MethodInfo s_getPromotedTokenMethodInfo;
        private static PropertyInfo s_promoterTypePropertyInfo;
        private static FieldInfo s_promoterTypeDtcFieldInfo;

        public NonMsdtcPromoterTests()
        {
            // reset the testFailures count back to 0 for each test case.
            VerifySoftDependencies();
        }

        private static void VerifySoftDependencies()
        {
            // Call test methods here if you need to run them without the TestHost/MsTest harness.
            // First, let's get the MethodInfo objects for the methods we are going to invoke thru reflection.
            // We can use this array for both EnlistPromotableSinglePhase and SetDistributedTransactionIdentifier
            if (s_enlistPromotableSinglePhaseMethodInfo == null)
            {
                Type[] parameterTypes = new Type[] { typeof(IPromotableSinglePhaseNotification), typeof(Guid) };
                s_enlistPromotableSinglePhaseMethodInfo = typeof(Transaction).GetTypeInfo().GetMethod("EnlistPromotableSinglePhase", parameterTypes);
                s_setDistributedTransactionIdentifierMethodInfo = typeof(Transaction).GetTypeInfo().GetMethod("SetDistributedTransactionIdentifier", parameterTypes);
                s_getPromotedTokenMethodInfo = typeof(Transaction).GetTypeInfo().GetMethod("GetPromotedToken");

                // And the PropertyInfo objects for PromoterType
                s_promoterTypePropertyInfo = typeof(Transaction).GetTypeInfo().GetProperty("PromoterType", typeof(Guid));

                // And the FieldInfo for TransactionInterop.PromoterTypeDtc
                s_promoterTypeDtcFieldInfo = typeof(TransactionInterop).GetTypeInfo().GetField("PromoterTypeDtc", BindingFlags.Public | BindingFlags.Static);
            }

            bool allMethodsAreThere = ((s_enlistPromotableSinglePhaseMethodInfo != null) &&
                (s_setDistributedTransactionIdentifierMethodInfo != null) &&
                (s_getPromotedTokenMethodInfo != null) &&
                (s_promoterTypePropertyInfo != null) &&
                (s_promoterTypeDtcFieldInfo != null)
                );
            Assert.True(allMethodsAreThere, "At least one of the expected new methods or properties is not implemented by the available System.Transactions.");
        }

        #region Helper Methods

        private static void CompleteDependentCloneThread(object stateObject)
        {
            DependentTransaction cloneToComplete = (DependentTransaction)stateObject;
            Trace("CompleteDependentCloneThread started - will sleep for 2 seconds");

            Task.Delay(TimeSpan.FromSeconds(2)).Wait();
            Trace("CompletedDependentCloneThread - completing the DependentTransaction...");
            cloneToComplete.Complete();
        }

        public static void Promote(string testCaseDescription, byte[] promotedTokenToCompare, Transaction txToPromote = null)
        {
            if (txToPromote == null)
            {
                txToPromote = Transaction.Current;
            }

            IPromotableSinglePhaseNotification shouldBeNull = null;
            shouldBeNull = CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                NonMsdtcPromoterTests.PromotedToken1,
                null,
                /*nonMSDTC = */ true,
                txToPromote,
                /*spcResponse=*/TransactionStatus.Committed,
                /*expectRejection = */ true);

            Assert.Null(shouldBeNull);

            byte[] promotedToken = TxPromotedToken(txToPromote);
            Assert.True(PromotedTokensMatch(promotedToken, NonMsdtcPromoterTests.PromotedToken1));
        }

        public static void Trace(string stringToTrace)
        {
            if (s_traceEnabled)
            {
                Debug.WriteLine(stringToTrace);
            }
        }

        public static void NoStressTrace(string stringToTrace)
        {
            //if (NonMsdtcPromoterTests.loopCount == 1)
            {
                Debug.WriteLine(stringToTrace);
            }
        }

        private static byte[] StringToByteArray(string stringToConvert)
        {
            byte[] bytes = new byte[stringToConvert.Length * sizeof(char)];
            System.Buffer.BlockCopy(stringToConvert.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static void TestPassed(bool displayTime = false)
        {
            if (displayTime)
            {
                NoStressTrace(string.Format("Pass: {0}", DateTime.Now.ToString()));
            }
            else
            {
                NoStressTrace("Pass");
            }
        }

        private static MyEnlistment CreateVolatileEnlistment(
            AutoResetEvent outcomeReceived,
            Transaction tx = null,
            EnlistmentOptions options = EnlistmentOptions.None,
            bool votePrepared = true)
        {
            MyEnlistment enlistment = new MyEnlistment(outcomeReceived, votePrepared);
            Transaction txToEnlist = Transaction.Current;
            if (tx != null)
            {
                txToEnlist = tx;
            }
            txToEnlist.EnlistVolatile(enlistment, options);
            return enlistment;
        }

        private static IPromotableSinglePhaseNotification CreatePSPEEnlistment(
            Guid promoterType,
            byte[] promotedToken,
            AutoResetEvent outcomeReceived,
            bool nonMSDTC = true,
            Transaction tx = null,
            TransactionStatus spcResponse = TransactionStatus.Committed,
            bool expectRejection = false,
            bool comparePromotedToken = false,
            bool failInitialize = false,
            bool failPromote = false,
            bool failSPC = false,
            bool failGetPromoterType = false,
            bool failGetId = false,
            bool incorrectNotificationObjectToSetDistributedTransactionId = false
           )
        {
            IPromotableSinglePhaseNotification enlistment = null;

            Transaction txToEnlist = Transaction.Current;
            if (tx != null)
            {
                txToEnlist = tx;
            }
            if (nonMSDTC)
            {
                NonMSDTCPromoterEnlistment nonMSDTCEnlistment = new NonMSDTCPromoterEnlistment(promoterType,
                    promotedToken,
                    outcomeReceived,
                    spcResponse,
                    failInitialize,
                    failPromote,
                    failSPC,
                    failGetPromoterType,
                    failGetId,
                    incorrectNotificationObjectToSetDistributedTransactionId);
                if (nonMSDTCEnlistment.Enlist(txToEnlist, expectRejection, comparePromotedToken))
                {
                    enlistment = nonMSDTCEnlistment;
                }

                TryProhibitedOperations(txToEnlist, promoterType);
            }
            else
            {
                throw new ApplicationException("normal PSPE not implemented yet.");
            }

            return enlistment;
        }

        private static DependentTransaction CreateDependentClone(bool blocking, Transaction tx = null)
        {
            DependentTransaction clone = null;
            if (tx == null)
            {
                tx = Transaction.Current;
            }

            clone = tx.DependentClone(blocking ? DependentCloneOption.BlockCommitUntilComplete : DependentCloneOption.RollbackIfNotComplete);

            return clone;
        }

        private static void TryProhibitedOperations(Transaction tx, Guid expectedPromoterType)
        {
            // First make sure that we can do a simple clone. This should be allowed.
            tx.Clone();

            try
            {
                Trace("Attempting TransactionInterop.GetDtcTransaction");
                TransactionInterop.GetDtcTransaction(tx);
                throw new ApplicationException("TransactionInterop.GetDtcTransaction unexpectedly succeeded.");
            }
            catch (TransactionPromotionException ex)
            {
                if (TxPromoterType(tx) != expectedPromoterType)
                {
                    Trace(string.Format("Exception {0} occurred, but transaction has an unexpected PromoterType of {1}", ex.ToString(), TxPromoterType(tx)));
                    throw new ApplicationException(string.Format("Exception {0} occurred, but transaction has an unexpected PromoterType of {1}", ex.ToString(), TxPromoterType(tx)));
                }
            }

            try
            {
                Trace("Attempting TransactionInterop.GetExportCookie");
                byte[] dummyWhereabouts = new byte[1];
                TransactionInterop.GetExportCookie(tx, dummyWhereabouts);
                throw new ApplicationException("TransactionInterop.GetExportCookie unexpectedly succeeded.");
            }
            catch (TransactionPromotionException ex)
            {
                if (TxPromoterType(tx) != expectedPromoterType)
                {
                    Trace(string.Format("Exception {0} occurred, but transaction has an unexpected PromoterType of {1}", ex.ToString(), TxPromoterType(tx)));
                    throw new ApplicationException(string.Format("Exception {0} occurred, but transaction has an unexpected PromoterType of {1}", ex.ToString(), TxPromoterType(tx)));
                }
            }

            try
            {
                Trace("Attempting TransactionInterop.GetTransmitterPropagationToken");
                byte[] dummyWhereabouts = new byte[1];
                TransactionInterop.GetTransmitterPropagationToken(tx);
                throw new ApplicationException("TransactionInterop.GetTransmitterPropagationToken unexpectedly succeeded.");
            }
            catch (TransactionPromotionException ex)
            {
                if (TxPromoterType(tx) != expectedPromoterType)
                {
                    Trace(string.Format("Exception {0} occurred, but transaction has an unexpected PromoterType of {1}", ex.ToString(), TxPromoterType(tx)));
                    throw new ApplicationException(string.Format("Exception {0} occurred, but transaction has an unexpected PromoterType of {1}", ex.ToString(), TxPromoterType(tx)));
                }
            }

            try
            {
                Trace("Attempting EnlistDurable");
                DummyDurableEnlistment enlistment = new DummyDurableEnlistment();
                tx.EnlistDurable(new Guid("611653C3-8536-4158-A990-00A8EE08B195"), enlistment, EnlistmentOptions.None);
                throw new ApplicationException("EnlistDurable unexpectedly succeeded.");
            }
            catch (TransactionPromotionException ex)
            {
                if (TxPromoterType(tx) != expectedPromoterType)
                {
                    Trace(string.Format("Exception {0} occurred, but transaction has an unexpected PromoterType of {1}", ex.ToString(), TxPromoterType(tx)));
                    throw new ApplicationException(string.Format("Exception {0} occurred, but transaction has an unexpected PromoterType of {1}", ex.ToString(), TxPromoterType(tx)));
                }
            }

            try
            {
                Trace("Attempting EnlistDurableSPC");
                DummyDurableEnlistmentSPC enlistment = new DummyDurableEnlistmentSPC();
                tx.EnlistDurable(new Guid("611653C3-8536-4158-A990-00A8EE08B195"), enlistment, EnlistmentOptions.None);
                throw new ApplicationException("EnlistDurableSPC unexpectedly succeeded.");
            }
            catch (TransactionPromotionException ex)
            {
                if (TxPromoterType(tx) != expectedPromoterType)
                {
                    Trace(string.Format("Exception {0} occurred, but transaction has an unexpected PromoterType of {1}", ex.ToString(), TxPromoterType(tx)));
                    throw new ApplicationException(string.Format("Exception {0} occurred, but transaction has an unexpected PromoterType of {1}", ex.ToString(), TxPromoterType(tx)));
                }
            }

            // TODO #9582: Uncomment once IFormatter and BinaryFormatter are available in .NET Core
            //try
            //{
            //    MemoryStream txStream = new MemoryStream();
            //    IFormatter formatter = new BinaryFormatter();
            //    formatter.Serialize(txStream, tx);
            //    throw new ApplicationException("Serialize of transaction unexpectedly succeeded.");
            //}
            //catch (TransactionPromotionException ex)
            //{
            //    if (TxPromoterType(tx) != expectedPromoterType)
            //    {
            //        Trace(string.Format("Exception {0} occurred, but transaction has an unexpected PromoterType of {1}", ex.ToString(), TxPromoterType(tx)));
            //        throw new ApplicationException(string.Format("Exception {0} occurred, but transaction has an unexpected PromoterType of {1}", ex.ToString(), TxPromoterType(tx)));
            //    }
            //}
        }

        private static bool PromotedTokensMatch(byte[] one, byte[] two)
        {
            if (one.Length != two.Length)
            {
                return false;
            }

            for (int i = 1; i < one.Length; i++)
            {
                if (one[i] != two[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static bool EnlistPromotable(IPromotableSinglePhaseNotification promotableNotification, Transaction txToEnlist, Guid promoterType)
        {
            object[] parameters = new object[] { promotableNotification, promoterType };
            bool returnVal = (bool)s_enlistPromotableSinglePhaseMethodInfo.Invoke(txToEnlist, parameters);
            return returnVal;
        }

        private static void SetDistributedTransactionId(IPromotableSinglePhaseNotification promotableNotification, Transaction txToSet, Guid distributedId)
        {
            object[] parameters = new object[] { promotableNotification, distributedId };
            s_setDistributedTransactionIdentifierMethodInfo.Invoke(txToSet, parameters);
        }

        private static Guid TxPromoterType(Transaction txToGet)
        {
            return (Guid)s_promoterTypePropertyInfo.GetValue(txToGet);
        }

        private static byte[] TxPromotedToken(Transaction txToGet)
        {
            return (byte[])s_getPromotedTokenMethodInfo.Invoke(txToGet, null);
        }

        private static Guid PromoterTypeDtc
        {
            get
            {
                return (Guid)s_promoterTypeDtcFieldInfo.GetValue(null);
            }
        }

        #endregion

        #region NonMSDTCPromoterEnlistment
        public class NonMSDTCPromoterEnlistment : IPromotableSinglePhaseNotification
        {
            private Guid _promoterType;
            private byte[] _promotedToken;
            private TransactionStatus _spcResponse;
            private bool _failPromote;
            private bool _failInitialize;
            private bool _failSPC;
            private bool _failGetPromoterType;
            private bool _failGetId;
            private bool _incorrectNotificationObjectToSetDistributedTransactionId;
            private AutoResetEvent _completionEvent;
            private Guid _distributedTxId;
            private Transaction _enlistedTransaction;

            public NonMSDTCPromoterEnlistment(Guid promoterType,
                byte[] promotedTokenToReturn,
                AutoResetEvent completionEvent,
                TransactionStatus spcResponse = TransactionStatus.Committed,
                bool failInitialize = false,
                bool failPromote = false,
                bool failSPC = false,
                bool failGetPromoterType = false,
                bool failGetId = false,
                bool incorrectNotificationObjectToSetDistributedTransactionId = false
                )
            {
                _promoterType = promoterType;
                _promotedToken = promotedTokenToReturn;
                _spcResponse = spcResponse;
                _completionEvent = completionEvent;
                _failInitialize = failInitialize;
                _failPromote = failPromote;
                _failSPC = failSPC;
                _failGetPromoterType = failGetPromoterType;
                _failGetId = failGetId;
                _incorrectNotificationObjectToSetDistributedTransactionId = incorrectNotificationObjectToSetDistributedTransactionId;
            }

            public bool Aborted
            {
                get;
                private set;
            }

            public bool Promoted
            {
                get;
                private set;
            }

            public void Initialize()
            {
                Trace("NonMSDTCPromoterEnlistment.Initialize");
                if (_failInitialize)
                {
                    Trace("NonMSDTCPromoterEnlistment.Initialize - Failing based on configuration");
                    throw new ApplicationException("Failing Initialize based on configuration");
                }
                return;
            }

            public bool Enlist(Transaction txToEnlist = null, bool expectRejection = false, bool comparePromotedToken = false)
            {
                if (txToEnlist == null)
                {
                    txToEnlist = Transaction.Current;
                }

                // invoke txToEnlist.EnlistPromotableSinglePhase(this, this.promoterType) via reflection.
                if (!EnlistPromotable(this, txToEnlist, _promoterType))
                {
                    if (expectRejection)
                    {
                        // invoke txToEnlist.PromoterType and txToEnlist.PromotedToken via reflection
                        if (comparePromotedToken && (TxPromoterType(txToEnlist) == _promoterType))
                        {
                            if (TxPromotedToken(txToEnlist) != _promotedToken)
                            {
                                throw new ApplicationException("The PromotedToken does not match");
                            }
                        }
                        return false;
                    }
                    else
                    {
                        throw new ApplicationException("EnlistPromotableSinglePhase failed when expected to succeed");
                    }
                }
                else if (expectRejection)
                {
                    throw new ApplicationException("EnlistPromotableSinglePhase succeeded when expected to fail");
                }

                _enlistedTransaction = txToEnlist;
                return true;
            }

            public void Rollback(SinglePhaseEnlistment singlePhaseEnlistment)
            {
                Trace("NonMSDTCPromoterEnlistment.Aborted");
                this.Aborted = true;
                _completionEvent.Set();
                singlePhaseEnlistment.Done();
            }

            public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
            {
                Trace("NonMSDTCPromoterEnlistment.SinglePhaseCommit");
                _completionEvent.Set();

                if (_failSPC)
                {
                    Trace("NonMSDTCPromoterEnlistment.SinglePhaseCommit - Failing based on configuration");
                    throw new ApplicationException("Failing SinglePhaseCommit based on configuration");
                }

                switch (_spcResponse)
                {
                    case TransactionStatus.Committed:
                        {
                            singlePhaseEnlistment.Committed();
                            break;
                        }

                    case TransactionStatus.Aborted:
                        {
                            singlePhaseEnlistment.Aborted(new ApplicationException("Aborted by NonMSDTCPromoterEnlistment.SinglePhaseCommit"));
                            break;
                        }

                    case TransactionStatus.InDoubt:
                        {
                            singlePhaseEnlistment.InDoubt(new ApplicationException("InDoubt by NonMSDTCPromoterEnlistment.SinglePhaseCommit"));
                            break;
                        }
                    default:
                        {
                            throw new ApplicationException("InDoubt by NonMSDTCPromoterEnlistment.SinglePhaseCommit because of invalid TransactionStatus outcome value.");
                        }
                }
            }

            public byte[] Promote()
            {
                Trace("NonMSDTCPromoterEnlistment.Promote");
                this.Promoted = true;
                _distributedTxId = Guid.NewGuid();
                if (_failPromote)
                {
                    Trace("NonMSDTCPromoterEnlistment.Promote - Failing based on configuration");
                    throw new ApplicationException("Failing promotion based on configuration");
                }

                // invoke this.enlistedTransaction.SetDistributedTransactionIdentifier(this, this.promoterType); via reflection
                if (_incorrectNotificationObjectToSetDistributedTransactionId)
                {
                    NonMSDTCPromoterEnlistment incorrectNotificationObject = new NonMSDTCPromoterEnlistment(_promoterType, _promotedToken, _completionEvent);
                    try
                    {
                        SetDistributedTransactionId(incorrectNotificationObject, _enlistedTransaction, _distributedTxId);
                        throw new ApplicationException("SetDistributedTransactionIdentifier did not throw the expected InvalidOperationException");
                    }
                    catch (TargetInvocationException ex)
                    {
                        if (!(ex.InnerException is InvalidOperationException))
                        {
                            throw new ApplicationException("SetDistributedTransactionIdentifier did not throw the expected InvalidOperationException");
                        }
                    }
                }
                else
                {
                    SetDistributedTransactionId(this, _enlistedTransaction, _distributedTxId);
                }

                return _promotedToken;
            }
        }
        #endregion

        #region MyEnlistment
        public class MyEnlistment : IEnlistmentNotification
        {
            private bool _committed;
            private bool _aborted;
            private bool _indoubt;
            private bool _votePrepared;
            private bool _enlistDuringPrepare;
            private EnlistmentOptions _enlistOptions;
            private bool _expectSuccessfulEnlist;
            private AutoResetEvent _secondEnlistmentCompleted;

            private AutoResetEvent _outcomeReceived;

            public MyEnlistment(
                AutoResetEvent outcomeReceived,
                bool votePrepared = true,
                bool enlistDuringPrepare = false,
                EnlistmentOptions enlistOptions = EnlistmentOptions.None,
                bool expectSuccessfulEnlist = true,
                AutoResetEvent secondEnlistmentCompleted = null
                )
            {
                _outcomeReceived = outcomeReceived;
                _votePrepared = votePrepared;
                _enlistDuringPrepare = enlistDuringPrepare;
                _enlistOptions = enlistOptions;
                _expectSuccessfulEnlist = expectSuccessfulEnlist;
                _secondEnlistmentCompleted = secondEnlistmentCompleted;
            }

            public Transaction TransactionToEnlist
            {
                get;
                set;
            }

            public bool CommittedOutcome
            {
                get
                {
                    return _committed;
                }
            }

            public bool AbortedOutcome
            {
                get
                {
                    return _aborted;
                }
            }

            public bool InDoubtOutcome
            {
                get
                {
                    return _indoubt;
                }
            }

            public void Commit(Enlistment enlistment)
            {
                Trace("MyEnlistment.Commit");
                _committed = true;
                enlistment.Done();
                _outcomeReceived.Set();
            }

            public void InDoubt(Enlistment enlistment)
            {
                Trace("MyEnlistment.InDoubt");
                _indoubt = true;
                enlistment.Done();
                _outcomeReceived.Set();
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                if (_enlistDuringPrepare)
                {
                    Trace(string.Format("MyEnlistment.Prepare - attempting another enlistment with options {0}", _enlistOptions.ToString()));
                    try
                    {
                        MyEnlistment enlist2 = new MyEnlistment(
                            _secondEnlistmentCompleted,
                            /*votePrepared=*/ true,
                            /*enlistDuringPrepare=*/ false,
                            _enlistOptions);
                        this.TransactionToEnlist.EnlistVolatile(enlist2, _enlistOptions);
                        if (!_expectSuccessfulEnlist)
                        {
                            // Force rollback of the transaction because the second enlistment was unsuccessful.
                            Trace("MyEnlistment.Prepare - Force Rollback because second enlistment succeeded unexpectedly");
                            _aborted = true;
                            _outcomeReceived.Set();
                            preparingEnlistment.ForceRollback(new ApplicationException("MyEnlistment voted ForceRollback"));
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_expectSuccessfulEnlist)
                        {
                            Trace(string.Format("MyEnlistment.Prepare - Force Rollback because second enlistment failed unexpectedly - {0}; {1}", ex.GetType().ToString(), ex.ToString()));
                            // Force rollback of the transaction because the second enlistment was unsuccessful.
                            _aborted = true;
                            _outcomeReceived.Set();
                            preparingEnlistment.ForceRollback(new ApplicationException("MyEnlistment voted ForceRollback"));
                            return;
                        }
                    }
                }

                if (_votePrepared)
                {
                    Trace("MyEnlistment.Prepare voting Prepared");
                    preparingEnlistment.Prepared();
                }
                else
                {
                    Trace("MyEnlistment.Prepare - Force Rollback");
                    _aborted = true;
                    _outcomeReceived.Set();
                    preparingEnlistment.ForceRollback(new ApplicationException("MyEnlistment voted ForceRollback"));
                }
            }

            public void Rollback(Enlistment enlistment)
            {
                Trace("MyEnlistment.Rollback");
                _aborted = true;
                enlistment.Done();
                _outcomeReceived.Set();
            }
        }
        #endregion

        #region DummyDurableEnlistment
        public class DummyDurableEnlistment : IEnlistmentNotification
        {
            public void Commit(Enlistment enlistment)
            {
                throw new NotImplementedException();
            }

            public void InDoubt(Enlistment enlistment)
            {
                throw new NotImplementedException();
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                throw new NotImplementedException();
            }

            public void Rollback(Enlistment enlistment)
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        #region DummyDurableEnlistmentSPC
        private class DummyDurableEnlistmentSPC : ISinglePhaseNotification
        {
            public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
            {
                throw new NotImplementedException();
            }

            public void Commit(Enlistment enlistment)
            {
                throw new NotImplementedException();
            }

            public void InDoubt(Enlistment enlistment)
            {
                throw new NotImplementedException();
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                throw new NotImplementedException();
            }

            public void Rollback(Enlistment enlistment)
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        // This class is used in conjunction with SubordinateTransaction. When asked via the Promote
        // method, it needs to create a DTC transaction and return the propagation token. Since we
        // can't just create another CommittableTransaction and promote it and return it's propagation
        // token in the same AppDomain, we spin up another AppDomain and do it there.
        private class MySimpleTransactionSuperior : ISimpleTransactionSuperior
        {
            private DtcTxCreator _dtcTxCreator = new DtcTxCreator() { TraceEnabled = false };
            private PromotedTx _promotedTx;

            public byte[] Promote()
            {
                byte[] propagationToken = null;

                Trace("MySimpleTransactionSuperior.Promote");
                propagationToken = _dtcTxCreator.CreatePromotedTx(ref _promotedTx);

                return propagationToken;
            }

            public void Rollback()
            {
                Trace("MySimpleTransactionSuperior.Rollback");
                _promotedTx.Rollback();
            }

            public void Commit()
            {
                Trace("MySimpleTransactionSuperior.Commit");
                _promotedTx.Commit();
            }
        }

        public class DtcTxCreator // : MarshalByRefObject
        {
            private static bool s_trace = false;

            public bool TraceEnabled
            {
                get { return s_trace; }
                set { s_trace = value; }
            }
            public static void Trace(string stringToTrace, params object[] args)
            {
                if (s_trace)
                {
                    Debug.WriteLine(stringToTrace, args);
                }
            }

            public byte[] CreatePromotedTx(ref PromotedTx promotedTx)
            {
                DtcTxCreator.Trace("DtcTxCreator.CreatePromotedTx");
                byte[] propagationToken;
                CommittableTransaction commitTx = new CommittableTransaction();
                promotedTx = new PromotedTx(commitTx);
                propagationToken = TransactionInterop.GetTransmitterPropagationToken(commitTx);
                return propagationToken;
            }
        }

        // This is the class that is created in the "other" AppDomain to create a
        // CommittableTransaction, promote it to DTC, and return the propagation token.
        // It also commits or aborts the transaction. Used by MySimpleTransactionSuperior
        // to create a DTC transaction when asked to promote.
        public class PromotedTx // : MarshalByRefObject
        {
            private CommittableTransaction _commitTx;

            public PromotedTx(CommittableTransaction commitTx)
            {
                DtcTxCreator.Trace("PromotedTx constructor");
                _commitTx = commitTx;
            }

            ~PromotedTx()
            {
                DtcTxCreator.Trace("PromotedTx destructor");
                if (_commitTx != null)
                {
                    DtcTxCreator.Trace("PromotedTx destructor calling Rollback");
                    _commitTx.Rollback();
                    _commitTx = null;
                }
            }

            public void Commit()
            {
                DtcTxCreator.Trace("PromotedTx.Commit");
                _commitTx.Commit();
                _commitTx = null;
            }

            public void Rollback()
            {
                DtcTxCreator.Trace("PromotedTx.Rollback");
                _commitTx.Rollback();
                _commitTx = null;
            }
        }

        #region TestCase_ methods
        private static void TestCase_VolatileEnlistments(
            int count,
            TransactionStatus expectedOutcome,
            EnlistmentOptions options = EnlistmentOptions.None,
            bool commitTx = true,
            bool votePrepared = true,
            Type expectedExceptionType = null)
        {
            string testCaseDescription = string.Format("TestCase_VolatileEnlistments; count = {0}; expectedOutcome = {1}; options = {2}; votePrepared = {3}, expectedExceptionType = {4}",
                        count,
                        expectedOutcome.ToString(),
                        options.ToString(),
                        votePrepared,
                        expectedExceptionType);

            Trace("**** " + testCaseDescription + " ****");

            AutoResetEvent[] enlistmentDoneEvts = new AutoResetEvent[count];
            MyEnlistment[] vols = new MyEnlistment[count];
            for (int i = 0; i < count; i++)
            {
                enlistmentDoneEvts[i] = new AutoResetEvent(false);
            }

            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    for (int i = 0; i < count; i++)
                    {
                        vols[i] = CreateVolatileEnlistment(enlistmentDoneEvts[i], null, options, votePrepared);
                    }
                    if (commitTx)
                    {
                        ts.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Equal(expectedExceptionType, ex.GetType());
            }

            for (int i = 0; i < count; i++)
            {
                Assert.True(enlistmentDoneEvts[i].WaitOne(TimeSpan.FromSeconds(5)));
            }

            int passCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (((expectedOutcome == TransactionStatus.Committed) && vols[i].CommittedOutcome) ||
                    ((expectedOutcome == TransactionStatus.Aborted) && vols[i].AbortedOutcome) ||
                    ((expectedOutcome == TransactionStatus.InDoubt) && vols[i].InDoubtOutcome)
                    )
                {
                    passCount++;
                }
            }
            Assert.Equal(count, passCount);

            TestPassed();
        }

        private static void TestCase_PSPENonMsdtc(bool commit,
            bool promote,
            TransactionStatus spcResponse,
            int p0BeforePSPE = 0,
            int p0AfterPSPE = 0,
            int p1BeforePSPE = 0,
            int p1AfterPSPE = 0,
            int p0AfterPromote = 0,
            int p1AfterPromote = 0
            )
        {
            string testCaseDescription = string.Format(
                "TestCase_PSPENonMsdtc commit={0}; promote={1}; spcResponse= {2}; p0BeforePSPE={3}; p0AfterPSPE={4}; p1BeforePSPE={5}; p1AfterPSPE={6}; p0AfterPromote={7}; p1AfterPromote={8}",
                commit,
                promote,
                spcResponse,
                p0BeforePSPE,
                p0AfterPSPE,
                p1BeforePSPE,
                p1AfterPSPE,
                p0AfterPromote,
                p1AfterPromote);

            Trace("**** " + testCaseDescription + " ****");

            // It doesn't make sense to have "AfterPromote" enlistments if we aren't going to promote the transaction.
            if (!promote)
            {
                if ((p0AfterPromote > 0) || (p1AfterPromote > 0))
                {
                    Trace("Not promoting - Resetting p0AfterPromote and p1AfterPromote to 0.");
                    p0AfterPromote = 0;
                    p1AfterPromote = 0;
                }
            }

            AutoResetEvent completedEvent = new AutoResetEvent(false);

            IPromotableSinglePhaseNotification enlistment = null;
            Transaction savedTransaction = null;

            int numVolatiles = p0BeforePSPE + p0AfterPSPE + p1BeforePSPE + p1AfterPSPE + p0AfterPromote + p1AfterPromote;

            AutoResetEvent[] enlistmentDoneEvts = new AutoResetEvent[numVolatiles];
            MyEnlistment[] vols = new MyEnlistment[numVolatiles];
            for (int i = 0; i < numVolatiles; i++)
            {
                enlistmentDoneEvts[i] = new AutoResetEvent(false);
            }

            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    // For checking status later, outside the scope.
                    savedTransaction = Transaction.Current.Clone();

                    if (p0BeforePSPE > 0)
                    {
                        for (int i = 0; i < p0BeforePSPE; i++)
                        {
                            vols[i] = CreateVolatileEnlistment(enlistmentDoneEvts[i], null, EnlistmentOptions.EnlistDuringPrepareRequired, /*votePrepared=*/ true);
                        }
                    }

                    if (p1BeforePSPE > 0)
                    {
                        for (int i = 0; i < p1BeforePSPE; i++)
                        {
                            vols[p0BeforePSPE + i] = CreateVolatileEnlistment(enlistmentDoneEvts[p0BeforePSPE + i], null, EnlistmentOptions.None, /*votePrepared=*/ true);
                        }
                    }

                    enlistment = CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                        NonMsdtcPromoterTests.PromotedToken1,
                        completedEvent,
                        /*nonMSDTC = */ true,
                        /*tx = */ null,
                        spcResponse,
                        /*expectRejection=*/ false
                        );

                    if (p0AfterPSPE > 0)
                    {
                        for (int i = 0; i < p0AfterPSPE; i++)
                        {
                            vols[p0BeforePSPE + p1BeforePSPE + i] = CreateVolatileEnlistment(
                                enlistmentDoneEvts[p0BeforePSPE + p1BeforePSPE + i],
                                null, EnlistmentOptions.EnlistDuringPrepareRequired, /*votePrepared=*/ true);
                        }
                    }

                    if (p1AfterPSPE > 0)
                    {
                        for (int i = 0; i < p1AfterPSPE; i++)
                        {
                            vols[p0BeforePSPE + p1BeforePSPE + p0AfterPSPE + i] = CreateVolatileEnlistment(
                                enlistmentDoneEvts[p0BeforePSPE + p1BeforePSPE + p0AfterPSPE + i],
                                null, EnlistmentOptions.None, /*votePrepared=*/ true);
                        }
                    }

                    if (promote)
                    {
                        Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1);

                        if (p0AfterPromote > 0)
                        {
                            for (int i = 0; i < p0AfterPromote; i++)
                            {
                                vols[p0BeforePSPE + p1BeforePSPE + p0AfterPSPE + p1AfterPSPE + i] = CreateVolatileEnlistment(
                                    enlistmentDoneEvts[p0BeforePSPE + p1BeforePSPE + p0AfterPSPE + p1AfterPSPE + i],
                                    null, EnlistmentOptions.EnlistDuringPrepareRequired, /*votePrepared=*/ true);
                            }
                        }

                        if (p1AfterPromote > 0)
                        {
                            for (int i = 0; i < p1AfterPromote; i++)
                            {
                                vols[p0BeforePSPE + p1BeforePSPE + p0AfterPSPE + p1AfterPSPE + p0AfterPromote + i] = CreateVolatileEnlistment(
                                    enlistmentDoneEvts[p0BeforePSPE + p1BeforePSPE + p0AfterPSPE + p1AfterPSPE + p0AfterPromote + i],
                                    null, EnlistmentOptions.None, /*votePrepared=*/ true);
                            }
                        }
                    }

                    if (commit)
                    {
                        ts.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                TransactionAbortedException abortedEx = ex as TransactionAbortedException;
                if ((abortedEx != null) && (spcResponse != TransactionStatus.Aborted))
                {
                    Assert.Equal(spcResponse, TransactionStatus.Aborted);
                }

                TransactionInDoubtException indoubtEx = ex as TransactionInDoubtException;
                if ((indoubtEx != null) && (spcResponse != TransactionStatus.InDoubt))
                {
                    Assert.Equal(spcResponse, TransactionStatus.InDoubt);
                }

                if (spcResponse == TransactionStatus.Committed)
                {
                    Trace(string.Format("Caught unexpected exception {0}:{1}", ex.GetType().ToString(), ex.ToString()));
                    return;
                }
            }

            NonMSDTCPromoterEnlistment nonDtcEnlistment = enlistment as NonMSDTCPromoterEnlistment;
            Assert.NotNull(nonDtcEnlistment);

            if (numVolatiles > 0)
            {
                for (int i = 0; i < numVolatiles; i++)
                {
                    Assert.True(enlistmentDoneEvts[i].WaitOne(TimeSpan.FromSeconds(5)));
                }

                int passCount = 0;
                for (int i = 0; i < numVolatiles; i++)
                {
                    if (commit)
                    {
                        if (((spcResponse == TransactionStatus.Committed) && vols[i].CommittedOutcome) ||
                            ((spcResponse == TransactionStatus.Aborted) && vols[i].AbortedOutcome) ||
                            ((spcResponse == TransactionStatus.InDoubt) && vols[i].InDoubtOutcome)
                           )
                        {
                            passCount++;
                        }
                    }
                    else
                    {
                        if (vols[i].AbortedOutcome)
                        {
                            passCount++;
                        }
                    }
                }
                Assert.Equal(numVolatiles, passCount);
            }

            Assert.True(completedEvent.WaitOne(TimeSpan.FromSeconds(5)));

            Assert.False(!promote && nonDtcEnlistment.Promoted);

            Assert.False(promote && !nonDtcEnlistment.Promoted);

            if (commit)
            {
                Assert.False((spcResponse == TransactionStatus.Committed) && (nonDtcEnlistment.Aborted));
                Assert.Equal(spcResponse, savedTransaction.TransactionInformation.Status);
            }
            else
            {
                Assert.True(nonDtcEnlistment.Aborted);
                Assert.Equal(TransactionStatus.Aborted, savedTransaction.TransactionInformation.Status);
            }

            TestPassed();
        }

        private static void TestCase_PSPENonMsdtcWithClones(
            bool commit,
            bool promote,
            TransactionStatus spcResponse,
            int abortingBeforePSPE = 0,
            int abortingAfterPSPE = 0,
            int blockingBeforePSPE = 0,
            int blockingAfterPSPE = 0,
            int abortingAfterPromote = 0,
            int blockingAfterPromote = 0)
        {
            string testCaseDescription = string.Format(
                "TestCase_PSPENonMsdtcWithClones commit={0}; promote={1}; spcResponse= {2}; abortingBeforePSPE={3}; abortingAfterPSPE={4}; blockingBeforePSPE={5}; blockingAfterPSPE={6}; abortingAfterPromote={7}; blockingAfterPromote={8}",
                commit,
                promote,
                spcResponse,
                abortingBeforePSPE,
                abortingAfterPSPE,
                blockingBeforePSPE,
                blockingAfterPSPE,
                abortingAfterPromote,
                blockingAfterPromote);

            Trace("**** " + testCaseDescription + " ****");

            // It doesn't make sense to have "AfterPromote" enlistments if we aren't going to promote the transaction.
            if (!promote)
            {
                if ((abortingAfterPromote > 0) || (blockingAfterPromote > 0))
                {
                    Trace("Not promoting - Resetting abortingAfterPromote and blockingAfterPromote to 0.");
                    abortingAfterPromote = 0;
                    blockingAfterPromote = 0;
                }
            }

            AutoResetEvent completedEvent = new AutoResetEvent(false);

            IPromotableSinglePhaseNotification enlistment = null;

            int numClones = abortingBeforePSPE + abortingAfterPSPE + blockingBeforePSPE + blockingAfterPSPE + abortingAfterPromote + blockingAfterPromote;


            DependentTransaction[] clones = new DependentTransaction[numClones];

            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    if (abortingBeforePSPE > 0)
                    {
                        for (int i = 0; i < abortingBeforePSPE; i++)
                        {
                            clones[i] = CreateDependentClone(/*blocking=*/false);
                        }
                    }

                    if (blockingBeforePSPE > 0)
                    {
                        for (int i = 0; i < blockingBeforePSPE; i++)
                        {
                            clones[abortingBeforePSPE + i] = CreateDependentClone(/*blocking=*/true);
                        }
                    }

                    enlistment = CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                        NonMsdtcPromoterTests.PromotedToken1,
                        completedEvent,
                        /*nonMSDTC = */ true,
                        /*tx = */ null,
                        spcResponse,
                        /*expectRejection=*/ false
                        );

                    if (abortingAfterPSPE > 0)
                    {
                        for (int i = 0; i < abortingAfterPSPE; i++)
                        {
                            clones[abortingBeforePSPE + blockingBeforePSPE + i] = CreateDependentClone(/*blocking=*/false);
                        }
                    }

                    if (blockingAfterPSPE > 0)
                    {
                        for (int i = 0; i < blockingAfterPSPE; i++)
                        {
                            clones[abortingBeforePSPE + blockingBeforePSPE + abortingAfterPSPE + i] = CreateDependentClone(/*blocking=*/true);
                        }
                    }

                    if (promote)
                    {
                        Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1);

                        if (abortingAfterPromote > 0)
                        {
                            for (int i = 0; i < abortingAfterPromote; i++)
                            {
                                clones[abortingBeforePSPE + blockingBeforePSPE + abortingAfterPSPE + blockingAfterPSPE + i] = CreateDependentClone(/*blocking=*/false);
                            }
                        }

                        if (blockingAfterPromote > 0)
                        {
                            for (int i = 0; i < blockingAfterPromote; i++)
                            {
                                clones[abortingBeforePSPE + blockingBeforePSPE + abortingAfterPSPE + blockingAfterPSPE + abortingAfterPromote + i] = CreateDependentClone(/*blocking=*/true);
                            }
                        }
                    }

                    // Complete all the clones
                    for (int i = 0; i < numClones; i++)
                    {
                        clones[i].Complete();
                    }

                    if (commit)
                    {
                        ts.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                TransactionAbortedException abortedEx = ex as TransactionAbortedException;
                if ((abortedEx != null) && (spcResponse != TransactionStatus.Aborted))
                {
                    Assert.Equal(spcResponse, TransactionStatus.Aborted);
                }

                TransactionInDoubtException indoubtEx = ex as TransactionInDoubtException;
                if ((indoubtEx != null) && (spcResponse != TransactionStatus.InDoubt))
                {
                    Assert.Equal(spcResponse, TransactionStatus.InDoubt);
                }

                Assert.NotEqual(spcResponse, TransactionStatus.Committed);
            }

            TestPassed();
        }

        public static void TestCase_AbortFromVolatile(bool promote, EnlistmentOptions enlistmentOptions = EnlistmentOptions.None)
        {
            string testCaseDescription = string.Format(
                "TestCase_AbortFromVolatile promote={0}; enlistmentOptions = {1}",
                promote,
                enlistmentOptions.ToString()
                );

            Trace("**** " + testCaseDescription + " ****");

            AutoResetEvent volCompleted = new AutoResetEvent(false);
            AutoResetEvent pspeCompleted = new AutoResetEvent(false);
            MyEnlistment vol = null;
            NonMSDTCPromoterEnlistment pspe = null;

            Assert.Throws<TransactionAbortedException>(() =>
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    vol = CreateVolatileEnlistment(volCompleted, null, enlistmentOptions, false);

                    pspe = (NonMSDTCPromoterEnlistment)CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                        NonMsdtcPromoterTests.PromotedToken1,
                        pspeCompleted,
                        /*nonMSDTC = */ true,
                        /*tx = */ null,
                        /*spcResponse=*/ TransactionStatus.Committed,
                        /*expectRejection=*/ false
                        );

                    if (promote)
                    {
                        Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1);
                    }

                    ts.Complete();
                }
            });

            Assert.True(volCompleted.WaitOne(TimeSpan.FromSeconds(5)) && pspeCompleted.WaitOne(TimeSpan.FromSeconds(5)));

            Assert.True(vol.AbortedOutcome);

            if (promote)
            {
                Assert.True(pspe.Promoted);
            }
            else
            {
                Assert.False(pspe.Promoted);
            }

            Assert.True(pspe.Aborted);

            TestPassed();
        }

        public static void TestCase_AbortingCloneNotCompleted(bool promote)
        {
            string testCaseDescription = string.Format(
                "TestCase_AbortingCloneNotCompleted promote={0}",
                promote
                );

            Trace("**** " + testCaseDescription + " ****");

            AutoResetEvent pspeCompleted = new AutoResetEvent(false);
            NonMSDTCPromoterEnlistment pspe = null;

            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    CreateDependentClone(/*blocking=*/false);
                    pspe = (NonMSDTCPromoterEnlistment)CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                        NonMsdtcPromoterTests.PromotedToken1,
                        pspeCompleted,
                        /*nonMSDTC = */ true,
                        /*tx = */ null,
                        /*spcResponse=*/ TransactionStatus.Committed,
                        /*expectRejection=*/ false
                        );

                    if (promote)
                    {
                        Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1);
                    }

                    ts.Complete();
                }
            }
            catch (Exception ex)
            {
                Assert.IsType<TransactionAbortedException>(ex);
            }

            Assert.True(pspeCompleted.WaitOne(TimeSpan.FromSeconds(5)));

            if (promote)
            {
                Assert.True(pspe.Promoted);
            }
            else
            {
                Assert.False(pspe.Promoted);
            }

            Assert.True(pspe.Aborted);

            TestPassed();
        }

        public static void TestCase_BlockingCloneCompletedAfterCommit(bool promote)
        {
            string testCaseDescription = string.Format(
                "TestCase_BlockingCloneCompletedAfterCommit promote={0}",
                promote
                );

            Trace("**** " + testCaseDescription + " ****");

            AutoResetEvent pspeCompleted = new AutoResetEvent(false);
            NonMSDTCPromoterEnlistment pspe = null;

            NoStressTrace(string.Format("There will be a 2 second delay here - {0}", DateTime.Now.ToString()));

            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    DependentTransaction clone = CreateDependentClone(/*blocking=*/true);

                    Task.Run(() => CompleteDependentCloneThread(clone));

                    pspe = (NonMSDTCPromoterEnlistment)CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                        NonMsdtcPromoterTests.PromotedToken1,
                        pspeCompleted,
                        /*nonMSDTC = */ true,
                        /*tx = */ null,
                        /*spcResponse=*/ TransactionStatus.Committed,
                        /*expectRejection=*/ false
                        );

                    if (promote)
                    {
                        Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1);
                    }

                    ts.Complete();
                }
            }
            catch (Exception ex)
            {
                Assert.Null(ex);
            }

            Assert.True(pspeCompleted.WaitOne(TimeSpan.FromSeconds(2)));

            if (promote)
            {
                Assert.True(pspe.Promoted);
            }
            else
            {
                Assert.False(pspe.Promoted);
            }

            Assert.False(pspe.Aborted);

            TestPassed(true);
        }

        public static void TestCase_TransactionTimeout(bool promote)
        {
            string testCaseDescription = string.Format(
                "TestCase_TransactionTimeout promote={0}",
                promote
                );

            Trace("**** " + testCaseDescription + " ****");

            AutoResetEvent pspeCompleted = new AutoResetEvent(false);
            NonMSDTCPromoterEnlistment pspe = null;

            Assert.Throws<TransactionAbortedException>(() =>
            {
                CommittableTransaction tx = new CommittableTransaction(TimeSpan.FromSeconds(1));

                pspe = (NonMSDTCPromoterEnlistment)CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                    NonMsdtcPromoterTests.PromotedToken1,
                    pspeCompleted,
                    /*nonMSDTC = */ true,
                    tx,
                    /*spcResponse=*/ TransactionStatus.Committed,
                    /*expectRejection=*/ false
                    );

                if (promote)
                {
                    Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1, tx);
                }

                NoStressTrace(string.Format("There will be a 3 second delay here - {0}", DateTime.Now.ToString()));

                Task.Delay(TimeSpan.FromSeconds(3)).Wait();

                NoStressTrace(string.Format("Woke up from sleep. Attempting Commit - {0}", DateTime.Now.ToString()));

                tx.Commit();
            });

            Assert.True(pspeCompleted.WaitOne(TimeSpan.FromSeconds(5)));

            if (promote)
            {
                Assert.True(pspe.Promoted);
            }
            else
            {
                Assert.False(pspe.Promoted);
            }

            Assert.True(pspe.Aborted);

            TestPassed(true);
        }

        public static void TestCase_EnlistDuringPrepare(bool promote,
            bool beforePromote,
            EnlistmentOptions firstOptions = EnlistmentOptions.None,
            EnlistmentOptions secondOptions = EnlistmentOptions.None,
            bool expectSecondEnlistSuccess = true
            )
        {
            string testCaseDescription = string.Format(
                "TestCase_EnlistDuringPrepare promote={0}; beforePromote={1}, firstOptions={2}, secondOptions={3}, expectSecondEnlistSuccess={4}",
                promote,
                beforePromote,
                firstOptions.ToString(),
                secondOptions.ToString(),
                expectSecondEnlistSuccess
                );

            Trace("**** " + testCaseDescription + " ****");

            AutoResetEvent volCompleted = new AutoResetEvent(false);
            AutoResetEvent vol2Completed = new AutoResetEvent(false);
            AutoResetEvent pspeCompleted = new AutoResetEvent(false);
            MyEnlistment vol = null;
            NonMSDTCPromoterEnlistment pspe = null;

            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    if (beforePromote)
                    {
                        vol = new MyEnlistment(
                            volCompleted,
                            true,
                            true,
                            secondOptions,
                            /*expectSuccessfulEnlist=*/ expectSecondEnlistSuccess,
                            vol2Completed);
                        vol.TransactionToEnlist = Transaction.Current;
                        Transaction.Current.EnlistVolatile(vol, firstOptions);
                    }

                    pspe = (NonMSDTCPromoterEnlistment)CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                        NonMsdtcPromoterTests.PromotedToken1,
                        pspeCompleted,
                        /*nonMSDTC = */ true,
                        /*tx = */ null,
                        /*spcResponse=*/ TransactionStatus.Committed,
                        /*expectRejection=*/ false
                        );

                    if (promote)
                    {
                        Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1);

                        if (!beforePromote)
                        {
                            vol = new MyEnlistment(
                                volCompleted,
                                true,
                                true,
                                secondOptions,
                                /*expectSuccessfulEnlist=*/ expectSecondEnlistSuccess,
                                vol2Completed);
                            vol.TransactionToEnlist = Transaction.Current;
                            Transaction.Current.EnlistVolatile(vol, firstOptions);
                        }
                    }

                    ts.Complete();
                }
            }
            catch (Exception ex)
            {
                Assert.Null(ex);
            }

            if (!expectSecondEnlistSuccess)
            {
                vol2Completed.Set();
            }

            Assert.True(volCompleted.WaitOne(TimeSpan.FromSeconds(5)) && vol2Completed.WaitOne(TimeSpan.FromSeconds(5)) &&
                pspeCompleted.WaitOne(TimeSpan.FromSeconds(5)));

            Assert.False(vol.AbortedOutcome);

            if (promote)
            {
                Assert.True(pspe.Promoted);
            }
            else
            {
                Assert.False(pspe.Promoted);
            }

            Assert.False(pspe.Aborted);

            TestPassed();
        }

        public static void TestCase_GetStatusAndDistributedId()
        {
            string testCaseDescription = "TestCase_GetDistributedId";

            Trace("**** " + testCaseDescription + " ****");

            AutoResetEvent pspeCompleted = new AutoResetEvent(false);
            NonMSDTCPromoterEnlistment pspe = null;

            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    TransactionStatus txStatus = Transaction.Current.TransactionInformation.Status;
                    Assert.Equal(TransactionStatus.Active, txStatus);

                    Guid distId = Transaction.Current.TransactionInformation.DistributedIdentifier;
                    Assert.Equal(Guid.Empty, distId);

                    pspe = (NonMSDTCPromoterEnlistment)CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                        NonMsdtcPromoterTests.PromotedToken1,
                        pspeCompleted,
                        /*nonMSDTC = */ true,
                        /*tx = */ null,
                        /*spcResponse=*/ TransactionStatus.Committed,
                        /*expectRejection=*/ false
                        );

                    txStatus = Transaction.Current.TransactionInformation.Status;
                    Assert.Equal(TransactionStatus.Active, txStatus);

                    distId = Transaction.Current.TransactionInformation.DistributedIdentifier;
                    Assert.Equal(Guid.Empty, distId);

                    Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1);

                    txStatus = Transaction.Current.TransactionInformation.Status;
                    Assert.Equal(TransactionStatus.Active, txStatus);

                    distId = Transaction.Current.TransactionInformation.DistributedIdentifier;
                    Assert.NotEqual(distId, Guid.Empty);
                    ts.Complete();
                }

                TestPassed();
            }
            catch (Exception ex)
            {
                Assert.Null(ex);
            }
        }

        public static void TestCase_DisposeCommittableTransaction(bool promote)
        {
            string testCaseDescription = string.Format(
                "TestCase_DisposeCommittableTransaction promote={0}",
                promote
                );

            Trace("**** " + testCaseDescription + " ****");

            AutoResetEvent pspeCompleted = new AutoResetEvent(false);
            NonMSDTCPromoterEnlistment pspe = null;
            Transaction savedTransaction = null;

            try
            {
                CommittableTransaction tx = new CommittableTransaction(TimeSpan.FromMinutes(1));
                savedTransaction = tx.Clone();

                pspe = (NonMSDTCPromoterEnlistment)CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                    NonMsdtcPromoterTests.PromotedToken1,
                    pspeCompleted,
                    /*nonMSDTC = */ true,
                    tx,
                    /*spcResponse=*/ TransactionStatus.Committed,
                    /*expectRejection=*/ false
                    );

                if (promote)
                {
                    Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1, tx);
                }

                tx.Dispose();

                tx.Commit();
            }
            catch (Exception ex)
            {
                Assert.IsType<ObjectDisposedException>(ex);
            }

            Assert.True(pspeCompleted.WaitOne(TimeSpan.FromSeconds(5)));

            if (promote)
            {
                Assert.True(pspe.Promoted);
            }
            else
            {
                Assert.False(pspe.Promoted);
            }

            Assert.True(pspe.Aborted);

            Assert.Equal(TransactionStatus.Aborted, savedTransaction.TransactionInformation.Status);

            TestPassed();
        }

        public static void TestCase_OutcomeRegistration(bool promote)
        {
            string testCaseDescription = string.Format(
                "TestCase_OutcomeRegistration promote={0}",
                promote
                );

            Trace("**** " + testCaseDescription + " ****");

            AutoResetEvent pspeCompleted = new AutoResetEvent(false);
            NonMSDTCPromoterEnlistment pspe = null;
            int numberOfCompletions = 0;
            CommittableTransaction tx = null;

            try
            {
                tx = new CommittableTransaction(TimeSpan.FromSeconds(5));

                tx.TransactionCompleted += delegate (object sender, TransactionEventArgs completedArgs)
                {
                    Trace("Completed event registered before PSPE");
                    numberOfCompletions++;
                    Assert.Equal(TransactionStatus.Committed, completedArgs.Transaction.TransactionInformation.Status);
                };

                pspe = (NonMSDTCPromoterEnlistment)CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                    NonMsdtcPromoterTests.PromotedToken1,
                    pspeCompleted,
                    /*nonMSDTC = */ true,
                    tx,
                    /*spcResponse=*/ TransactionStatus.Committed,
                    /*expectRejection=*/ false
                    );

                tx.TransactionCompleted += delegate (object sender, TransactionEventArgs completedArgs)
                {
                    Trace("Completed event registered after PSPE");
                    numberOfCompletions++;
                    Assert.Equal(TransactionStatus.Committed, completedArgs.Transaction.TransactionInformation.Status);
                };

                if (promote)
                {
                    Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1, tx);

                    tx.TransactionCompleted += delegate (object sender, TransactionEventArgs completedArgs)
                    {
                        Trace("Completed event registered after promote");
                        numberOfCompletions++;
                        Assert.Equal(TransactionStatus.Committed, completedArgs.Transaction.TransactionInformation.Status);
                    };
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                Assert.Null(ex);
            }

            tx.TransactionCompleted += delegate (object sender, TransactionEventArgs completedArgs)
            {
                Trace("Completed event registered after commit");
                numberOfCompletions++;
                Assert.Equal(TransactionStatus.Committed, completedArgs.Transaction.TransactionInformation.Status);
            };

            Assert.True(pspeCompleted.WaitOne(TimeSpan.FromSeconds(5)));

            if (promote)
            {
                Assert.True(pspe.Promoted);
            }
            else
            {
                Assert.False(pspe.Promoted);
            }

            Assert.Equal((promote ? 4 : 3), numberOfCompletions);

            TestPassed();
        }

        public static void TestCase_PromoterType()
        {
            string testCaseDescription = "TestCase_PromoterType";

            Trace("**** " + testCaseDescription + " ****");

            AutoResetEvent pspeCompleted = new AutoResetEvent(false);
            NonMSDTCPromoterEnlistment pspe = null;

            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    Assert.Equal(Guid.Empty, TxPromoterType(Transaction.Current));

                    pspe = (NonMSDTCPromoterEnlistment)CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                        NonMsdtcPromoterTests.PromotedToken1,
                        pspeCompleted,
                        /*nonMSDTC = */ true,
                        /*tx = */ null,
                        /*spcResponse=*/ TransactionStatus.Committed,
                        /*expectRejection=*/ false
                        );

                    Assert.Equal(NonMsdtcPromoterTests.PromoterType1, TxPromoterType(Transaction.Current));

                    Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1);

                    Assert.Equal(NonMsdtcPromoterTests.PromoterType1, TxPromoterType(Transaction.Current));

                    ts.Complete();
                }
            }
            catch (Exception ex)
            {
                Assert.Null(ex);
            }

            Assert.True(pspeCompleted.WaitOne(TimeSpan.FromSeconds(5)));

            Assert.True(pspe.Promoted);

            TestPassed();
        }

        public static void TestCase_PromoterTypeMSDTC()
        {
            string testCaseDescription = "TestCase_PromoterTypeMSDTC";

            Trace("**** " + testCaseDescription + " ****");

            AutoResetEvent volCompleted = new AutoResetEvent(false);
            MyEnlistment vol = null;

            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    Assert.Equal(Guid.Empty, TxPromoterType(Transaction.Current));

                    vol = CreateVolatileEnlistment(volCompleted);

                    // Force MSDTC promotion.
                    TransactionInterop.GetDtcTransaction(Transaction.Current);

                    // TransactionInterop.PromoterTypeDtc
                    Assert.Equal(PromoterTypeDtc, TxPromoterType(Transaction.Current));

                    ts.Complete();
                }
            }
            catch (Exception ex)
            {
                Trace(string.Format("Caught unexpected exception {0}:{1}", ex.GetType().ToString(), ex.ToString()));
                return;
            }

            Assert.True(volCompleted.WaitOne(TimeSpan.FromSeconds(5)));

            Assert.True(vol.CommittedOutcome);

            TestPassed();
        }

        public static void TestCase_FailPromotableSinglePhaseNotificationCalls()
        {
            string testCaseDescription = "TestCase_FailPromotableSinglePhaseNotificationCalls";

            Trace("**** " + testCaseDescription + " ****");

            AutoResetEvent pspeCompleted = new AutoResetEvent(false);
            NonMSDTCPromoterEnlistment pspe = null;

            Trace("Fail Initialize");
            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    pspe = (NonMSDTCPromoterEnlistment)CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                        NonMsdtcPromoterTests.PromotedToken1,
                        pspeCompleted,
                        /*nonMSDTC = */ true,
                        /*tx = */ null,
                        /*spcResponse=*/ TransactionStatus.Committed,
                        /*expectRejection=*/ false,
                        /*comparePromotedToken=*/ false,
                        /*failInitialize=*/ true,
                        /*failPromote=*/ false,
                        /*failSPC=*/ false,
                        /*failGetPromoterType=*/ false,
                        /*failGetId=*/ false
                        );
                    bool shouldNotBeExecuted = true;
                    Assert.False(shouldNotBeExecuted);
                }
            }
            catch (Exception ex)
            {
                Assert.True(ex is ApplicationException || (ex is TargetInvocationException && ex.InnerException is ApplicationException));
            }

            Trace("Fail Promote");
            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    pspe = (NonMSDTCPromoterEnlistment)CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                        NonMsdtcPromoterTests.PromotedToken1,
                        pspeCompleted,
                        /*nonMSDTC = */ true,
                        /*tx = */ null,
                        /*spcResponse=*/ TransactionStatus.Committed,
                        /*expectRejection=*/ false,
                        /*comparePromotedToken=*/ false,
                        /*failInitialize=*/ false,
                        /*failPromote=*/ true,
                        /*failSPC=*/ false,
                        /*failGetPromoterType=*/ false,
                        /*failGetId=*/ false
                        );

                    Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1);
                    ts.Complete();
                }
            }
            catch (Exception ex)
            {
                Assert.True(ex is ApplicationException || (ex is TargetInvocationException && ex.InnerException is ApplicationException));
            }

            // The NonMSDTCPromoterEnlistment is coded to set "Promoted" at the beginning of Promote, before
            // throwing.
            Assert.True(pspeCompleted.WaitOne(TimeSpan.FromSeconds(5)));

            Assert.True(pspe.Promoted);

            Trace("Fail SinglePhaseCommit");
            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    pspe = (NonMSDTCPromoterEnlistment)CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                        NonMsdtcPromoterTests.PromotedToken1,
                        pspeCompleted,
                        /*nonMSDTC = */ true,
                        /*tx = */ null,
                        /*spcResponse=*/ TransactionStatus.Committed,
                        /*expectRejection=*/ false,
                        /*comparePromotedToken=*/ false,
                        /*failInitialize=*/ false,
                        /*failPromote=*/ false,
                        /*failSPC=*/ true,
                        /*failGetPromoterType=*/ false,
                        /*failGetId=*/ false
                        );

                    Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1);
                    ts.Complete();
                }
            }
            catch (Exception ex)
            {
                Assert.True(ex is ApplicationException || (ex is TargetInvocationException && ex.InnerException is ApplicationException));
            }

            // The NonMSDTCPromoterEnlistment is coded to set "Promoted" at the beginning of Promote, before
            // throwing.
            Assert.True(pspeCompleted.WaitOne(TimeSpan.FromSeconds(5)));

            Assert.True(pspe.Promoted);

            TestPassed();
        }

        public static void TestCase_SetDistributedIdAtWrongTime()
        {
            string testCaseDescription = "TestCase_SetDistributedIdAtWrongTime";

            Trace("**** " + testCaseDescription + " ****");

            AutoResetEvent pspeCompleted = new AutoResetEvent(false);
            NonMSDTCPromoterEnlistment pspe = null;
            NonMSDTCPromoterEnlistment dummyPSPE = new NonMSDTCPromoterEnlistment(NonMsdtcPromoterTests.PromoterType1, NonMsdtcPromoterTests.PromotedToken1, pspeCompleted);

            Guid guidToSet = new Guid("236BC646-FE3B-41F9-99F7-08BF448D8420");

            using (TransactionScope ts = new TransactionScope())
            {
                Trace("Before EnlistPromotable");
                Exception ex = Assert.ThrowsAny<Exception>(() => SetDistributedTransactionId(dummyPSPE, Transaction.Current, guidToSet));
                Assert.True(ex is TransactionException || (ex is TargetInvocationException && ex.InnerException is TransactionException));

                pspe = (NonMSDTCPromoterEnlistment)CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                    NonMsdtcPromoterTests.PromotedToken1,
                    pspeCompleted,
                    /*nonMSDTC = */ true,
                    /*tx = */ null,
                    /*spcResponse=*/ TransactionStatus.Committed,
                    /*expectRejection=*/ false,
                    /*comparePromotedToken=*/ false,
                    /*failInitialize=*/ false,
                    /*failPromote=*/ false,
                    /*failSPC=*/ false,
                    /*failGetPromoterType=*/ false,
                    /*failGetId=*/ false
                    );

                Trace("After EnlistPromotable");
                ex = Assert.ThrowsAny<Exception>(() => SetDistributedTransactionId(dummyPSPE, Transaction.Current, guidToSet));
                Assert.True(ex is TransactionException || (ex is TargetInvocationException && ex.InnerException is TransactionException));

                Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1);

                Trace("After Promotion");
                ex = Assert.ThrowsAny<Exception>(() => SetDistributedTransactionId(dummyPSPE, Transaction.Current, guidToSet));
                Assert.True(ex is TransactionException || (ex is TargetInvocationException && ex.InnerException is TransactionException));

                ts.Complete();
            }

            // The NonMSDTCPromoterEnlistment is coded to set "Promoted" at the beginning of Promote, before
            // throwing.
            Assert.True(pspeCompleted.WaitOne(TimeSpan.FromSeconds(5)));

            Assert.True(pspe.Promoted);

            TestPassed();
        }

        public static void TestCase_SetDistributedIdWithWrongNotificationObject()
        {
            string testCaseDescription = "TestCase_SetDistributedIdWithWrongNotificationObject";

            Trace("**** " + testCaseDescription + " ****");

            AutoResetEvent pspeCompleted = new AutoResetEvent(false);
            NonMSDTCPromoterEnlistment pspe = null;

            Guid guidToSet = new Guid("236BC646-FE3B-41F9-99F7-08BF448D8420");

            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    pspe = (NonMSDTCPromoterEnlistment)CreatePSPEEnlistment(NonMsdtcPromoterTests.PromoterType1,
                        NonMsdtcPromoterTests.PromotedToken1,
                        pspeCompleted,
                        /*nonMSDTC = */ true,
                        /*tx = */ null,
                        /*spcResponse=*/ TransactionStatus.Committed,
                        /*expectRejection=*/ false,
                        /*comparePromotedToken=*/ false,
                        /*failInitialize=*/ false,
                        /*failPromote=*/ false,
                        /*failSPC=*/ false,
                        /*failGetPromoterType=*/ false,
                        /*failGetId=*/ false,
                        /*incorrectNotificationObjectToSetDistributedTransactionId=*/ true
                        );

                    Promote(testCaseDescription, NonMsdtcPromoterTests.PromotedToken1);

                    ts.Complete();
                }
            }
            catch (Exception ex)
            {
                Assert.Null(ex);
            }

            // The NonMSDTCPromoterEnlistment is coded to set "Promoted" at the beginning of Promote, before
            // throwing.
            Assert.True(pspeCompleted.WaitOne(TimeSpan.FromSeconds(5)));

            Assert.True(pspe.Promoted);

            TestPassed();
        }

        #endregion


        /// <summary>
        /// This test case is very basic Volatile Enlistment test.
        /// </summary>
        [Fact]
        public void VolatileEnlistments()
        {
            TestCase_VolatileEnlistments(1, TransactionStatus.Committed);
            TestCase_VolatileEnlistments(5, TransactionStatus.Committed);
            TestCase_VolatileEnlistments(1, TransactionStatus.Aborted, EnlistmentOptions.None, false);
            TestCase_VolatileEnlistments(5, TransactionStatus.Aborted, EnlistmentOptions.None, false);
            TestCase_VolatileEnlistments(1, TransactionStatus.Aborted, EnlistmentOptions.None, true, false, typeof(TransactionAbortedException));
        }

        /// <summary>
        /// Tests PSPE Non-MSDTC with volatile enlistments.
        /// </summary>
        [Theory]
        [InlineData(true, false, TransactionStatus.Committed)]
        [InlineData(true, true, TransactionStatus.Committed)]
        [InlineData(false, false, TransactionStatus.Committed)]
        [InlineData(false, true, TransactionStatus.Committed)]
        [InlineData(true, false, TransactionStatus.Aborted)]
        [InlineData(true, true, TransactionStatus.Aborted)]
        [InlineData(true, false, TransactionStatus.InDoubt)]
        [InlineData(true, true, TransactionStatus.InDoubt)]
        public void PSPENonMSDTCWithVolatileEnlistments(bool commit, bool promote, TransactionStatus spcResponse)
        {
            TestCase_PSPENonMsdtc(commit, promote, spcResponse);
            TestCase_PSPENonMsdtc(commit, promote, spcResponse, 1);
            TestCase_PSPENonMsdtc(commit, promote, spcResponse, 1, 1);
            TestCase_PSPENonMsdtc(commit, promote, spcResponse, 1, 1, 1);
            TestCase_PSPENonMsdtc(commit, promote, spcResponse, 1, 1, 1, 1);
        }

        /// <summary>
        /// Tests PSPE Non-MSDTC with dependent clones.
        /// </summary>
        [Theory]
        [InlineData(true, false, TransactionStatus.Committed)]
        [InlineData(true, true, TransactionStatus.Committed)]
        [InlineData(false, false, TransactionStatus.Committed)]
        [InlineData(false, true, TransactionStatus.Committed)]
        [InlineData(true, false, TransactionStatus.Aborted)]
        [InlineData(true, true, TransactionStatus.Aborted)]
        [InlineData(true, false, TransactionStatus.InDoubt)]
        [InlineData(true, true, TransactionStatus.InDoubt)]
        public void PSPENonMSDTCWithDependentClones(bool commit, bool promote, TransactionStatus spcResponse)
        {
            TestCase_PSPENonMsdtcWithClones(commit, promote, spcResponse);
            TestCase_PSPENonMsdtcWithClones(commit, promote, spcResponse, 1);
            TestCase_PSPENonMsdtcWithClones(commit, promote, spcResponse, 1, 1);
            TestCase_PSPENonMsdtcWithClones(commit, promote, spcResponse, 1, 1, 1);
            TestCase_PSPENonMsdtcWithClones(commit, promote, spcResponse, 1, 1, 1, 1);
        }

        /// <summary>
        /// PSPE Non-MSDTC Abort From Volatile.
        /// </summary>
        [Theory]
        [InlineData(false, EnlistmentOptions.EnlistDuringPrepareRequired)]
        [InlineData(true, EnlistmentOptions.EnlistDuringPrepareRequired)]
        [InlineData(false, EnlistmentOptions.None)]
        [InlineData(true, EnlistmentOptions.None)]
        public void PSPENonMsdtcAbortFromVolatile(bool promote, EnlistmentOptions options)
        {
            // Abort from p0 and p1 volatile, not promoted and promoted.
            TestCase_AbortFromVolatile(promote, options);
        }

        /// <summary>
        /// PSPE Non-MSDTC Aborting Clone Not Completed.
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PSPENonMsdtcAbortingCloneNotCompleted(bool promote)
        {
            // Aborting clone that isn't completed
            TestCase_AbortingCloneNotCompleted(promote);
        }

        /// <summary>
        /// PSPE Non-MSDTC Blocking Clone Completed After Commit.
        /// </summary>
        [OuterLoop] // long delay
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PSPENonMsdtcBlockingCloneCompletedAfterCommit(bool promote)
        {
            // Blocking clone that isn't completed before commit
            TestCase_BlockingCloneCompletedAfterCommit(promote);
        }

        /// <summary>
        /// PSPE Non-MSDTC Timeout.
        /// </summary>
        [OuterLoop] // long timeout
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PSPENonMsdtcTimeout(bool promote)
        {
            // tx timeout
            TestCase_TransactionTimeout(promote);
        }

        /// <summary>
        /// PSPE Non-MSDTC Enlist During Phase 0.
        /// </summary>
        [Theory]
        [InlineData(false, true, EnlistmentOptions.EnlistDuringPrepareRequired, EnlistmentOptions.EnlistDuringPrepareRequired, true)]
        [InlineData(false, true, EnlistmentOptions.EnlistDuringPrepareRequired, EnlistmentOptions.None, true)]
        [InlineData(true, true, EnlistmentOptions.EnlistDuringPrepareRequired, EnlistmentOptions.EnlistDuringPrepareRequired, true)]
        [InlineData(true, true, EnlistmentOptions.EnlistDuringPrepareRequired, EnlistmentOptions.None, true)]
        [InlineData(true, false, EnlistmentOptions.EnlistDuringPrepareRequired, EnlistmentOptions.EnlistDuringPrepareRequired, true)]
        [InlineData(true, false, EnlistmentOptions.EnlistDuringPrepareRequired, EnlistmentOptions.EnlistDuringPrepareRequired, true)]
        [InlineData(false, true, EnlistmentOptions.None, EnlistmentOptions.None, false)]
        [InlineData(false, true, EnlistmentOptions.None, EnlistmentOptions.None, false)]
        [InlineData(true, true, EnlistmentOptions.None, EnlistmentOptions.EnlistDuringPrepareRequired, false)]
        [InlineData(true, true, EnlistmentOptions.None, EnlistmentOptions.None, false)]
        [InlineData(true, false, EnlistmentOptions.None, EnlistmentOptions.EnlistDuringPrepareRequired, false)]
        [InlineData(true, false, EnlistmentOptions.None, EnlistmentOptions.None, false)]
        public void PSPENonMsdtcEnlistDuringPhase0(
            bool promote, bool beforePromote, EnlistmentOptions options, EnlistmentOptions secondOptions, bool expectSecondEnlistSuccess)
        {
            TestCase_EnlistDuringPrepare(promote, beforePromote, options, secondOptions, expectSecondEnlistSuccess);
        }

        /// <summary>
        /// PSPE Non-MSDTC Get Status and Distributed Id.
        /// </summary>
        [Fact]
        public void PSPENonMsdtcGetStatusAndDistributedId()
        {
            // Retrieve the DistributedId before PSPE, before Promote, and after Promote
            TestCase_GetStatusAndDistributedId();
        }

        /// <summary>
        /// PSPE Non-MSDTC Dispose Committable Transaction.
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PSPENonMsdtcDisposeCommittable(bool promote)
        {
            // Dispose a committable transaction early.
            TestCase_DisposeCommittableTransaction(promote);
        }

        /// <summary>
        /// PSPE Non-MSDTC Completed Event.
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PSPENonMsdtcCompletedEvent(bool promote)
        {
            // Registration for completed event
            TestCase_OutcomeRegistration(promote);
        }

        /// <summary>
        /// PSPE Non-MSDTC Get PromoterType.
        /// </summary>
        [Fact]
        public void PSPENonMsdtcGetPromoterType()
        {
            // get_PromoterType
            TestCase_PromoterType();
        }

        /// <summary>
        /// PSPE Non-MSDTC Get PromoterType.
        /// </summary>
        [Fact]
        public void PSPENonMsdtcGetPromoterTypeMSDTC()
        {
            // get_PromoterType
            TestCase_PromoterTypeMSDTC();
        }

        /// <summary>
        /// PSPE Non-MSDTC Fail PromotableSinglePhaseNotification Calls.
        /// </summary>
        [Fact]
        public void PSPENonMsdtcFailPromotableSinglePhaseNotificationCalls()
        {
            // Fail calls to the non-MSDTC Promotable Enlistment
            TestCase_FailPromotableSinglePhaseNotificationCalls();
        }

        /// <summary>
        /// Make SetDistributedTransactionIdentifier calls at the wrong time - negative test.
        /// </summary>
        [Fact]
        public void PSPENonMsdtcInCorrectSetDistributedTransactionIdentifierCalls()
        {
            // Call SetDistributedTransactionIdentifier at the wrong time.
            TestCase_SetDistributedIdAtWrongTime();
        }

        /// <summary>
        /// Call SetDistributedTransactionIdentifier with incorrect notification object - negative test.
        /// </summary>
        [Fact]
        public void PSPENonMsdtcSetDistributedTransactionIdentifierCallWithWrongNotificationObject()
        {
            // Call SetDistributedTransactionIdentifier at the wrong time.
            TestCase_SetDistributedIdWithWrongNotificationObject();
        }

        [Fact]
        public void SimpleTransactionSuperior()
        {
            MySimpleTransactionSuperior superior = new MySimpleTransactionSuperior();
            SubordinateTransaction subTx = new SubordinateTransaction(IsolationLevel.Serializable, superior);

            AutoResetEvent durableCompleted = new AutoResetEvent(false);
            MyEnlistment durable = null;

            durable = new MyEnlistment(
                durableCompleted,
                true,
                false,
                EnlistmentOptions.None,
                /*expectSuccessfulEnlist=*/ false,
                /*secondEnlistmentCompleted=*/ null);
            durable.TransactionToEnlist = Transaction.Current;

            Assert.Throws<PlatformNotSupportedException>(() => // SubordinateTransaction promotes to MSDTC
            {
                subTx.EnlistDurable(Guid.NewGuid(), durable, EnlistmentOptions.None);
            });
        }
    }
}
