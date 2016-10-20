// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Transactions.Tests
{
    // Ported from Mono

    public class TransactionScopeTest
    {
        [Fact]
        public void TransactionScopeWithInvalidTimeSpanThrows()
        {
            Assert.Throws<ArgumentNullException>("transactionToUse", () => new TransactionScope(null, TimeSpan.FromSeconds(-1)));
            Assert.Throws<ArgumentOutOfRangeException>("scopeTimeout", () => new TransactionScope(TransactionScopeOption.Required, TimeSpan.FromSeconds(-1)));
        }

        [Fact]
        public void TransactionScopeCommit()
        {
            Assert.Null(Transaction.Current);
            using (TransactionScope scope = new TransactionScope())
            {
                Assert.NotNull(Transaction.Current);
                Assert.Equal(TransactionStatus.Active, Transaction.Current.TransactionInformation.Status);
                scope.Complete();
            }
            Assert.Null(Transaction.Current);
        }

        [Fact]
        public void TransactionScopeAbort()
        {
            Assert.Null(Transaction.Current);
            IntResourceManager irm = new IntResourceManager(1);
            using (TransactionScope scope = new TransactionScope())
            {
                Assert.NotNull(Transaction.Current);
                Assert.Equal(TransactionStatus.Active, Transaction.Current.TransactionInformation.Status);

                irm.Value = 2;
                /* Not completing scope here */
            }
            irm.Check(0, 0, 1, 0, "irm");
            Assert.Equal(1, irm.Value);
            Assert.Null(Transaction.Current);
        }

        [Fact]
        public void TransactionScopeCompleted1()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    scope.Complete();
                    /* Can't access ambient transaction after scope.Complete */
                    TransactionStatus status = Transaction.Current.TransactionInformation.Status;
                }
            });
        }

        [Fact]
        public void TransactionScopeCompleted2()
        {
            using (TransactionScope scope = new TransactionScope())
            {
                scope.Complete();
                Assert.Throws<InvalidOperationException>(() =>
                {
                    Transaction.Current = Transaction.Current;
                });
            }
        }

        [Fact]
        public void TransactionScopeCompleted3()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    scope.Complete();
                    scope.Complete();
                }
            });
        }

        #region NestedTransactionScope tests
        [Fact]
        public void NestedTransactionScope1()
        {
            IntResourceManager irm = new IntResourceManager(1);

            Assert.Null(Transaction.Current);
            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                /* Complete this scope */
                scope.Complete();
            }

            Assert.Null(Transaction.Current);
            /* Value = 2, got committed */
            Assert.Equal(irm.Value, 2);
            irm.Check(1, 1, 0, 0, "irm");
        }

        [Fact]
        public void NestedTransactionScope2()
        {
            IntResourceManager irm = new IntResourceManager(1);
            Assert.Null(Transaction.Current);
            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                /* Not-Completing this scope */
            }

            Assert.Null(Transaction.Current);
            /* Value = 2, got rolledback */
            Assert.Equal(irm.Value, 1);
            irm.Check(0, 0, 1, 0, "irm");
        }

        [Fact]
        public void NestedTransactionScope3()
        {
            IntResourceManager irm = new IntResourceManager(1);
            IntResourceManager irm2 = new IntResourceManager(10);

            Assert.Null(Transaction.Current);
            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                using (TransactionScope scope2 = new TransactionScope())
                {
                    irm2.Value = 20;

                    scope2.Complete();
                }

                scope.Complete();
            }

            Assert.Null(Transaction.Current);
            /* Both got committed */
            Assert.Equal(irm.Value, 2);
            Assert.Equal(irm2.Value, 20);
            irm.Check(1, 1, 0, 0, "irm");
            irm2.Check(1, 1, 0, 0, "irm2");
        }

        [Fact]
        public void NestedTransactionScope4()
        {
            IntResourceManager irm = new IntResourceManager(1);
            IntResourceManager irm2 = new IntResourceManager(10);

            Assert.Null(Transaction.Current);
            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                using (TransactionScope scope2 = new TransactionScope())
                {
                    irm2.Value = 20;

                    /* Inner Tx not completed, Tx should get rolled back */
                    //scope2.Complete();
                }
                /* Both rolledback */
                irm.Check(0, 0, 1, 0, "irm");
                irm2.Check(0, 0, 1, 0, "irm2");
                Assert.Equal(TransactionStatus.Aborted, Transaction.Current.TransactionInformation.Status);
                //scope.Complete ();
            }

            Assert.Null(Transaction.Current);

            Assert.Equal(irm.Value, 1);
            Assert.Equal(irm2.Value, 10);
            irm.Check(0, 0, 1, 0, "irm");
        }

        [Fact]
        public void NestedTransactionScope5()
        {
            IntResourceManager irm = new IntResourceManager(1);
            IntResourceManager irm2 = new IntResourceManager(10);

            Assert.Null(Transaction.Current);
            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                using (TransactionScope scope2 = new TransactionScope())
                {
                    irm2.Value = 20;
                    scope2.Complete();
                }

                Assert.Equal(TransactionStatus.Active, Transaction.Current.TransactionInformation.Status);
                /* Not completing outer scope
				scope.Complete (); */
            }

            Assert.Null(Transaction.Current);

            Assert.Equal(irm.Value, 1);
            Assert.Equal(irm2.Value, 10);
            irm.Check(0, 0, 1, 0, "irm");
            irm2.Check(0, 0, 1, 0, "irm2");
        }

        [Fact]
        public void NestedTransactionScope6()
        {
            IntResourceManager irm = new IntResourceManager(1);
            IntResourceManager irm2 = new IntResourceManager(10);

            Assert.Null(Transaction.Current);
            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                using (TransactionScope scope2 = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    irm2.Value = 20;
                    scope2.Complete();
                }
                /* vr2, committed */
                irm2.Check(1, 1, 0, 0, "irm2");
                Assert.Equal(irm2.Value, 20);

                Assert.Equal(TransactionStatus.Active, Transaction.Current.TransactionInformation.Status);

                scope.Complete();
            }

            Assert.Null(Transaction.Current);
            Assert.Equal(irm.Value, 2);
            irm.Check(1, 1, 0, 0, "irm");
        }

        [Fact]
        public void NestedTransactionScope7()
        {
            IntResourceManager irm = new IntResourceManager(1);
            IntResourceManager irm2 = new IntResourceManager(10);

            Assert.Null(Transaction.Current);
            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                using (TransactionScope scope2 = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    irm2.Value = 20;
                    /* Not completing 
					 scope2.Complete();*/
                }

                /* irm2, rolled back*/
                irm2.Check(0, 0, 1, 0, "irm2");
                Assert.Equal(irm2.Value, 10);

                Assert.Equal(TransactionStatus.Active, Transaction.Current.TransactionInformation.Status);

                scope.Complete();
            }

            Assert.Null(Transaction.Current);
            /* ..But irm got committed */
            Assert.Equal(irm.Value, 2);
            irm.Check(1, 1, 0, 0, "irm");
        }

        [Fact]
        public void NestedTransactionScope8()
        {
            IntResourceManager irm = new IntResourceManager(1);
            IntResourceManager irm2 = new IntResourceManager(10);

            Assert.Null(Transaction.Current);
            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                using (TransactionScope scope2 = new TransactionScope(TransactionScopeOption.Suppress))
                {
                    /* Not transactional, so this WONT get committed */
                    irm2.Value = 20;
                    scope2.Complete();
                }
                irm2.Check(0, 0, 0, 0, "irm2");
                Assert.Equal(20, irm2.Value);
                Assert.Equal(TransactionStatus.Active, Transaction.Current.TransactionInformation.Status);

                scope.Complete();
            }

            Assert.Null(Transaction.Current);
            Assert.Equal(irm.Value, 2);
            irm.Check(1, 1, 0, 0, "irm");
        }

        [Fact]
        public void NestedTransactionScope8a()
        {
            IntResourceManager irm = new IntResourceManager(1);
            IntResourceManager irm2 = new IntResourceManager(10);

            Assert.Null(Transaction.Current);
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Suppress))
            {
                irm.Value = 2;

                using (TransactionScope scope2 = new TransactionScope())
                {
                    irm2.Value = 20;
                    scope2.Complete();
                }
                irm2.Check(1, 1, 0, 0, "irm2");
                Assert.Equal(20, irm2.Value);

                scope.Complete();
            }

            Assert.Null(Transaction.Current);
            Assert.Equal(2, irm.Value);
            irm.Check(0, 0, 0, 0, "irm");
        }

        [Fact]
        public void NestedTransactionScope9()
        {
            IntResourceManager irm = new IntResourceManager(1);
            IntResourceManager irm2 = new IntResourceManager(10);

            Assert.Null(Transaction.Current);
            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                using (TransactionScope scope2 = new TransactionScope(TransactionScopeOption.Suppress))
                {
                    /* Not transactional, so this WONT get committed */
                    irm2.Value = 4;
                    scope2.Complete();
                }
                irm2.Check(0, 0, 0, 0, "irm2");

                using (TransactionScope scope3 = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    irm.Value = 6;
                    scope3.Complete();
                }

                /* vr's value has changed as the inner scope committed = 6 */
                irm.Check(1, 1, 0, 0, "irm");
                Assert.Equal(irm.Value, 6);
                Assert.Equal(irm.Actual, 6);
                Assert.Equal(TransactionStatus.Active, Transaction.Current.TransactionInformation.Status);

                scope.Complete();
            }

            Assert.Null(Transaction.Current);
            Assert.Equal(irm.Value, 6);
            irm.Check(2, 2, 0, 0, "irm");
        }

        [Fact]
        public void NestedTransactionScope10()
        {
            Assert.Throws<TransactionAbortedException>(() =>
           {
               IntResourceManager irm = new IntResourceManager(1);

               Assert.Null(Transaction.Current);
               using (TransactionScope scope = new TransactionScope())
               {
                   irm.Value = 2;

                   using (TransactionScope scope2 = new TransactionScope())
                   {
                       irm.Value = 4;
                       /* Not completing this, so the transaction will
                        * get aborted 
                       scope2.Complete (); */
                   }

                   using (TransactionScope scope3 = new TransactionScope())
                   {
                       /* Aborted transaction cannot be used for another
                        * TransactionScope 
                        */
                   }
               }
           });
        }

        [Fact]
        public void NestedTransactionScope12()
        {
            IntResourceManager irm = new IntResourceManager(1);

            Assert.Null(Transaction.Current);
            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                using (TransactionScope scope2 = new TransactionScope())
                {
                    irm.Value = 4;
                    /* Not completing this, so the transaction will
					 * get aborted 
					scope2.Complete (); */
                }

                using (TransactionScope scope3 = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    /* Using RequiresNew here, so outer transaction
					 * being aborted doesn't matter
					 */
                    scope3.Complete();
                }
            }
        }

        [Fact]
        public void NestedTransactionScope13()
        {
            Assert.Throws<TransactionAbortedException>(() =>
           {
               IntResourceManager irm = new IntResourceManager(1);

               Assert.Null(Transaction.Current);
               using (TransactionScope scope = new TransactionScope())
               {
                   irm.Value = 2;

                   using (TransactionScope scope2 = new TransactionScope())
                   {
                       irm.Value = 4;
                       /* Not completing this, so the transaction will
                        * get aborted 
                       scope2.Complete (); */
                   }

                   scope.Complete();
               }
           });
        }
        #endregion

        /* Tests using IntResourceManager */

        [Fact]
        public void RMFail1()
        {
            IntResourceManager irm = new IntResourceManager(1);
            IntResourceManager irm2 = new IntResourceManager(10);
            IntResourceManager irm3 = new IntResourceManager(12);

            Assert.Null(Transaction.Current);
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    irm.Value = 2;
                    irm2.Value = 20;
                    irm3.Value = 24;

                    /* Make second RM fail to prepare, this should throw
					 * TransactionAbortedException when the scope ends 
					 */
                    irm2.FailPrepare = true;
                    scope.Complete();
                }
            }
            catch (TransactionAbortedException)
            {
                irm.Check(1, 0, 1, 0, "irm");
                irm2.Check(1, 0, 0, 0, "irm2");
                irm3.Check(0, 0, 1, 0, "irm3");
            }
            Assert.Null(Transaction.Current);
        }

        [Fact]
        [OuterLoop] // 30 second timeout
        public void RMFail2()
        {
            IntResourceManager irm = new IntResourceManager(1);
            IntResourceManager irm2 = new IntResourceManager(10);
            IntResourceManager irm3 = new IntResourceManager(12);

            Assert.Null(Transaction.Current);
            TransactionAbortedException e = Assert.Throws<TransactionAbortedException>(() =>
            {
                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TimeSpan(0, 0, 10)))
                {
                    irm.Value = 2;
                    irm2.Value = 20;
                    irm3.Value = 24;

                    /* irm2 wont call Prepared or ForceRollback in
					 * its Prepare (), so TransactionManager will timeout
					 * waiting for it 
					 */
                    irm2.IgnorePrepare = true;
                    scope.Complete();
                }
            });

            Assert.NotNull(e.InnerException);
            Assert.IsType<TimeoutException>(e.InnerException);
            Assert.Null(Transaction.Current);
        }

        #region Explicit Transaction Tests

        [Fact]
        public void ExplicitTransactionCommit()
        {
            Assert.Null(Transaction.Current);

            CommittableTransaction ct = new CommittableTransaction();
            Transaction oldTransaction = Transaction.Current;
            Transaction.Current = ct;

            IntResourceManager irm = new IntResourceManager(1);
            irm.Value = 2;
            ct.Commit();

            Assert.Equal(2, irm.Value);
            Assert.Equal(TransactionStatus.Committed, ct.TransactionInformation.Status);
            Transaction.Current = oldTransaction;
        }

        [Fact]
        public void ExplicitTransactionRollback()
        {
            Assert.Null(Transaction.Current);

            CommittableTransaction ct = new CommittableTransaction();
            Transaction oldTransaction = Transaction.Current;
            Transaction.Current = ct;
            try
            {
                IntResourceManager irm = new IntResourceManager(1);
                irm.Value = 2;
                Assert.Equal(TransactionStatus.Active, ct.TransactionInformation.Status);
                ct.Rollback();

                Assert.Equal(1, irm.Value);
                Assert.Equal(TransactionStatus.Aborted, ct.TransactionInformation.Status);
            }
            finally
            {
                Transaction.Current = oldTransaction;
            }
        }

        [Fact]
        public void ExplicitTransaction1()
        {
            Assert.Null(Transaction.Current);
            CommittableTransaction ct = new CommittableTransaction();
            Transaction oldTransaction = Transaction.Current;

            Transaction.Current = ct;
            try
            {
                IntResourceManager irm = new IntResourceManager(1);

                irm.Value = 2;

                using (TransactionScope scope = new TransactionScope())
                {
                    Assert.Equal(ct, Transaction.Current);
                    irm.Value = 4;
                    scope.Complete();
                }

                Assert.Equal(ct, Transaction.Current);
                Assert.Equal(TransactionStatus.Active, Transaction.Current.TransactionInformation.Status);
                Assert.Equal(1, irm.Actual);

                ct.Commit();
                Assert.Equal(4, irm.Actual);
                Assert.Equal(TransactionStatus.Committed, Transaction.Current.TransactionInformation.Status);
            }
            finally
            {
                Transaction.Current = oldTransaction;
            }
        }

        [Fact]
        public void ExplicitTransaction2()
        {
            Assert.Null(Transaction.Current);
            CommittableTransaction ct = new CommittableTransaction();
            Transaction oldTransaction = Transaction.Current;

            Transaction.Current = ct;
            try
            {
                IntResourceManager irm = new IntResourceManager(1);

                irm.Value = 2;
                using (TransactionScope scope = new TransactionScope())
                {
                    Assert.Equal(ct, Transaction.Current);

                    /* Not calling scope.Complete
                    scope.Complete ();*/
                }

                Assert.Equal(TransactionStatus.Aborted, ct.TransactionInformation.Status);
                Assert.Equal(ct, Transaction.Current);
                Assert.Equal(1, irm.Actual);
                Assert.Equal(1, irm.NumRollback);
                irm.Check(0, 0, 1, 0, "irm");
            }
            finally
            {
                Transaction.Current = oldTransaction;
            }
            Assert.Throws<TransactionAbortedException>(() => ct.Commit());
        }

        [Fact]
        public void ExplicitTransaction3()
        {
            Assert.Null(Transaction.Current);
            CommittableTransaction ct = new CommittableTransaction();
            Transaction oldTransaction = Transaction.Current;

            Transaction.Current = ct;
            try
            {
                IntResourceManager irm = new IntResourceManager(1);

                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    Assert.True(ct != Transaction.Current, "Scope with RequiresNew should have a new ambient transaction");

                    irm.Value = 3;
                    scope.Complete();
                }

                irm.Value = 2;

                Assert.Equal(3, irm.Actual);

                Assert.Equal(ct, Transaction.Current);
                ct.Commit();
                Assert.Equal(2, irm.Actual);
            }
            finally
            {
                Transaction.Current = oldTransaction;
            }
        }

        [Fact]
        public void ExplicitTransaction4()
        {
            Assert.Null(Transaction.Current);
            CommittableTransaction ct = new CommittableTransaction();
            Transaction oldTransaction = Transaction.Current;

            /* Not setting ambient transaction 
			 Transaction.Current = ct; 
			 */

            IntResourceManager irm = new IntResourceManager(1);

            using (TransactionScope scope = new TransactionScope(ct))
            {
                Assert.Equal(ct, Transaction.Current);

                irm.Value = 2;
                scope.Complete();
            }

            Assert.Equal(oldTransaction, Transaction.Current);
            Assert.Equal(TransactionStatus.Active, ct.TransactionInformation.Status);
            Assert.Equal(1, irm.Actual);

            ct.Commit();
            Assert.Equal(2, irm.Actual);
            Assert.Equal(TransactionStatus.Committed, ct.TransactionInformation.Status);

            irm.Check(1, 1, 0, 0, "irm");
        }

        [Fact]
        public void ExplicitTransaction5()
        {
            Assert.Null(Transaction.Current);
            CommittableTransaction ct = new CommittableTransaction();
            Transaction oldTransaction = Transaction.Current;

            /* Not setting ambient transaction 
			 Transaction.Current = ct; 
			 */

            IntResourceManager irm = new IntResourceManager(1);

            using (TransactionScope scope = new TransactionScope(ct))
            {
                Assert.Equal(ct, Transaction.Current);

                irm.Value = 2;

                /* Not completing this scope
				scope.Complete (); */
            }

            Assert.Equal(oldTransaction, Transaction.Current);
            Assert.Equal(TransactionStatus.Aborted, ct.TransactionInformation.Status);
            Assert.Equal(1, irm.Actual);

            irm.Check(0, 0, 1, 0, "irm");
        }

        [Fact]
        public void ExplicitTransaction6()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                CommittableTransaction ct = new CommittableTransaction();

                IntResourceManager irm = new IntResourceManager(1);
                irm.Value = 2;
                ct.Commit();

                ct.Commit();
            });
        }

        [Fact]
        public void ExplicitTransaction6a()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                CommittableTransaction ct = new CommittableTransaction();

                IntResourceManager irm = new IntResourceManager(1);
                irm.Value = 2;
                ct.Commit();

                /* Using a already committed transaction in a new 
                 * TransactionScope
                 */
                TransactionScope scope = new TransactionScope(ct);
            });
        }

        [Fact]
        public void ExplicitTransaction6b()
        {
            CommittableTransaction ct = new CommittableTransaction();

            IntResourceManager irm = new IntResourceManager(1);

            Transaction.Current = ct;
            try
            {
                TransactionScope scope1 = new TransactionScope();
                /* Enlist */
                irm.Value = 2;

                scope1.Complete();

                Assert.Throws<TransactionAbortedException>(() => ct.Commit());
                irm.Check(0, 0, 1, 0, "irm");

                scope1.Dispose();
            }
            finally
            {
                Transaction.Current = null;
            }
        }

        [Fact]
        public void ExplicitTransaction6c()
        {
            CommittableTransaction ct = new CommittableTransaction();

            IntResourceManager irm = new IntResourceManager(1);

            Transaction.Current = ct;
            try
            {
                TransactionScope scope1 = new TransactionScope(TransactionScopeOption.RequiresNew);
                /* Enlist */
                irm.Value = 2;

                TransactionScope scope2 = new TransactionScope();
                Assert.Throws<InvalidOperationException>(() => scope1.Dispose());
                irm.Check(0, 0, 1, 0, "irm");
                scope2.Dispose();
            }
            finally
            {
                Transaction.Current = null;
            }
        }

        [Fact]
        public void ExplicitTransaction6d()
        {
            CommittableTransaction ct = new CommittableTransaction();

            IntResourceManager irm = new IntResourceManager(1);

            Transaction.Current = ct;
            try
            {
                TransactionScope scope1 = new TransactionScope();
                /* Enlist */
                irm.Value = 2;

                TransactionScope scope2 = new TransactionScope(TransactionScopeOption.RequiresNew);
                Assert.Throws<InvalidOperationException>(() => scope1.Dispose());
                scope2.Dispose();
            }
            finally
            {
                Transaction.Current = null;
            }
        }

        [Fact]
        public void ExplicitTransaction6e()
        {
            CommittableTransaction ct = new CommittableTransaction();

            IntResourceManager irm = new IntResourceManager(1);

            Transaction.Current = ct;
            try
            {
                TransactionScope scope1 = new TransactionScope();
                /* Enlist */
                irm.Value = 2;

                TransactionScope scope2 = new TransactionScope(TransactionScopeOption.Suppress);
                Assert.Throws<InvalidOperationException>(() => scope1.Dispose());
                scope2.Dispose();
            }
            finally
            {
                Transaction.Current = null;
            }
        }

        [Fact]
        public void ExplicitTransaction7()
        {
            Assert.Throws<TransactionException>(() =>
            {
                CommittableTransaction ct = new CommittableTransaction();

                IntResourceManager irm = new IntResourceManager(1);
                irm.Value = 2;
                ct.Commit();
                /* Cannot accept any new work now, so TransactionException */
                ct.Rollback();
            });
        }

        [Fact]
        public void ExplicitTransaction8()
        {
            CommittableTransaction ct = new CommittableTransaction();

            IntResourceManager irm = new IntResourceManager(1);
            using (TransactionScope scope = new TransactionScope(ct))
            {
                irm.Value = 2;
                Assert.Throws<TransactionAbortedException>(() => ct.Commit()); /* FIXME: Why TransactionAbortedException ?? */
                irm.Check(0, 0, 1, 0, "irm");
            }
        }

        [Fact]
        public void ExplicitTransaction8a()
        {
            CommittableTransaction ct = new CommittableTransaction();

            IntResourceManager irm = new IntResourceManager(1);
            using (TransactionScope scope = new TransactionScope(ct))
            {
                irm.Value = 2;
                scope.Complete();
                Assert.Throws<TransactionAbortedException>(() => ct.Commit()); /* FIXME: Why TransactionAbortedException ?? */
                irm.Check(0, 0, 1, 0, "irm");
            }
        }

        [Fact]
        public void ExplicitTransaction9()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                CommittableTransaction ct = new CommittableTransaction();

                IntResourceManager irm = new IntResourceManager(1);
                ct.BeginCommit(null, null);
                ct.BeginCommit(null, null);
            });
        }

        [Fact]
        public void ExplicitTransaction10()
        {
            CommittableTransaction ct = new CommittableTransaction();

            IntResourceManager irm = new IntResourceManager(1);
            Transaction.Current = ct;
            try
            {
                irm.Value = 2;

                TransactionScope scope = new TransactionScope(ct);
                Assert.Equal(ct, Transaction.Current);
                Assert.Throws<TransactionAbortedException>(() => ct.Commit());
                irm.Check(0, 0, 1, 0, "irm");
            }
            finally
            {
                Transaction.Current = null;
            }
        }

        [Fact]
        public void ExplicitTransaction10a()
        {
            CommittableTransaction ct = new CommittableTransaction();

            IntResourceManager irm = new IntResourceManager(1);
            Transaction.Current = ct;
            try
            {
                irm.Value = 2;
                Transaction.Current = null;

                TransactionScope scope = new TransactionScope(ct);
                Assert.Equal(ct, Transaction.Current);
                Transaction.Current = null;

                Assert.Throws<TransactionAbortedException>(() => ct.Commit());
                irm.Check(0, 0, 1, 0, "irm");
            }
            finally
            {
                Transaction.Current = null;
            }
        }

        [Fact]
        public void ExplicitTransaction10b()
        {
            CommittableTransaction ct = new CommittableTransaction();

            IntResourceManager irm = new IntResourceManager(1);
            Transaction.Current = ct;
            try
            {
                irm.Value = 2;
                Transaction.Current = null;

                TransactionScope scope = new TransactionScope(ct);
                Assert.Equal(ct, Transaction.Current);
                IAsyncResult ar = ct.BeginCommit(null, null);
                Assert.Throws<TransactionAbortedException>(() => ct.EndCommit(ar));
                irm.Check(0, 0, 1, 0, "irm");
            }
            finally
            {
                Transaction.Current = null;
            }
        }

        [Fact]
        public void ExplicitTransaction12()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                CommittableTransaction ct = new CommittableTransaction();

                IntResourceManager irm = new IntResourceManager(1);
                irm.FailPrepare = true;
                ct.BeginCommit(null, null);
                ct.EndCommit(null);
            });
        }

        [Fact]
        public void ExplicitTransaction13()
        {
            CommittableTransaction ct = new CommittableTransaction();
            IntResourceManager irm = new IntResourceManager(1);

            Assert.Null(Transaction.Current);
            Transaction.Current = ct;
            try
            {
                irm.Value = 2;
                irm.FailPrepare = true;

                Assert.Throws<TransactionAbortedException>(() => ct.Commit());
                Assert.Equal(TransactionStatus.Aborted, ct.TransactionInformation.Status);
                Assert.Throws<InvalidOperationException>(() => ct.BeginCommit(null, null));
            }
            finally
            {
                Transaction.Current = null;
            }
        }

        [Fact]
        public void ExplicitTransaction14()
        {
            CommittableTransaction ct = new CommittableTransaction();
            IntResourceManager irm = new IntResourceManager(1);

            Assert.Null(Transaction.Current);
            Transaction.Current = ct;
            try
            {
                irm.Value = 2;

                ct.Commit();

                Assert.Equal(TransactionStatus.Committed, ct.TransactionInformation.Status);
                Assert.Throws<InvalidOperationException>(() => ct.BeginCommit(null, null));
            }
            finally
            {
                Transaction.Current = null;
            }
        }

        [Fact]
        public void ExplicitTransaction15()
        {
            CommittableTransaction ct = new CommittableTransaction();
            IntResourceManager irm = new IntResourceManager(1);
            IntResourceManager irm2 = new IntResourceManager(3);

            Assert.Null(Transaction.Current);
            Transaction.Current = ct;
            try
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    using (TransactionScope scope = new TransactionScope())
                    {
                        irm.Value = 2;
                        Transaction.Current = new CommittableTransaction();
                        irm2.Value = 6;
                    }
                });
                irm.Check(0, 0, 1, 0, "irm");
                irm2.Check(0, 0, 1, 0, "irm2");
            }
            finally
            {
                Transaction.Current = null;
            }
        }

        [Fact]
        public void ExplicitTransaction16()
        {
            CommittableTransaction ct = new CommittableTransaction();
            IntResourceManager irm0 = new IntResourceManager(3);
            IntResourceManager irm = new IntResourceManager(1);

            Assert.Null(Transaction.Current);

            Transaction.Current = ct;
            try
            {
                irm.FailPrepare = true;
                irm.FailWithException = true;
                irm.Value = 2;
                irm0.Value = 6;

                var e = Assert.Throws<TransactionAbortedException>(() => ct.Commit());
                Assert.NotNull(e.InnerException);
                Assert.IsType<NotSupportedException>(e.InnerException);
                irm.Check(1, 0, 0, 0, "irm");
                irm0.Check(0, 0, 1, 0, "irm0");
            }
            finally
            {
                Transaction.Current = null;
            }
        }

        #endregion
    }
}
