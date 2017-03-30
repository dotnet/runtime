// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Transactions.Tests
{
    // Ported from Mono

    public class EnlistTest
    {
        #region Vol1_Dur0

        /* Single volatile resource, SPC happens */
        [Fact]
        public void Vol1_Dur0()
        {
            IntResourceManager irm = new IntResourceManager(1);
            irm.UseSingle = true;
            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                scope.Complete();
            }
            irm.CheckSPC("irm");
        }

        [Fact]
        public void Vol1_Dur0_2PC()
        {
            IntResourceManager irm = new IntResourceManager(1);

            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                scope.Complete();
            }
            irm.Check2PC("irm");
        }

        /* Single volatile resource, SPC happens */
        [Fact]
        public void Vol1_Dur0_Fail1()
        {
            IntResourceManager irm = new IntResourceManager(1);
            irm.UseSingle = true;
            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                /* Not completing this..
				scope.Complete ();*/
            }

            irm.Check(0, 0, 0, 1, 0, 0, 0, "irm");
        }

        [Fact]
        public void Vol1_Dur0_Fail2()
        {
            Assert.Throws<TransactionAbortedException>(() =>
           {
               IntResourceManager irm = new IntResourceManager(1);

               irm.FailPrepare = true;

               using (TransactionScope scope = new TransactionScope())
               {
                   irm.Value = 2;

                   scope.Complete();
               }
           });
        }

        [Fact]
        public void Vol1_Dur0_Fail3()
        {
            Assert.Throws<TransactionAbortedException>(() =>
            {
                IntResourceManager irm = new IntResourceManager(1);
                irm.UseSingle = true;
                irm.FailSPC = true;

                using (TransactionScope scope = new TransactionScope())
                {
                    irm.Value = 2;

                    scope.Complete();
                }
            });
        }

        #endregion

        #region Vol2_Dur0

        /* >1 volatile, 2PC */
        [Fact]
        public void Vol2_Dur0_SPC()
        {
            IntResourceManager irm = new IntResourceManager(1);
            IntResourceManager irm2 = new IntResourceManager(3);

            irm.UseSingle = true;
            irm2.UseSingle = true;
            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;
                irm2.Value = 6;

                scope.Complete();
            }
            irm.Check2PC("irm");
            irm2.Check2PC("irm2");
        }

        #endregion

        #region Vol0_Dur1
        /* 1 durable */
        [Fact]
        public void Vol0_Dur1()
        {
            IntResourceManager irm = new IntResourceManager(1);
            irm.Type = ResourceManagerType.Durable;
            irm.UseSingle = true;

            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                scope.Complete();
            }

            irm.CheckSPC("irm");
        }

        /* We support only 1 durable with 2PC
		 * On .net, this becomes a distributed transaction
		 */
        [ActiveIssue(13532)] //Distributed transactions are not supported.
        [Fact]
        public void Vol0_Dur1_2PC()
        {
            IntResourceManager irm = new IntResourceManager(1);

            /* Durable resource enlisted with a IEnlistedNotification
			 * object
			 */
            irm.Type = ResourceManagerType.Durable;

            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                scope.Complete();
            }
        }

        [Fact]
        public void Vol0_Dur1_Fail()
        {
            IntResourceManager irm = new IntResourceManager(1);

            /* Durable resource enlisted with a IEnlistedNotification
			 * object
			 */
            irm.Type = ResourceManagerType.Durable;
            irm.FailSPC = true;
            irm.UseSingle = true;
            Assert.Throws<TransactionAbortedException>(() =>
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    irm.Value = 2;

                    scope.Complete();
                }
            });
            irm.Check(1, 0, 0, 0, 0, 0, 0, "irm");
        }
        #endregion

        #region Vol2_Dur1
        /* >1vol + 1 durable */
        [Fact]
        public void Vol2_Dur1()
        {
            IntResourceManager[] irm = new IntResourceManager[4];
            irm[0] = new IntResourceManager(1);
            irm[1] = new IntResourceManager(3);
            irm[2] = new IntResourceManager(5);
            irm[3] = new IntResourceManager(7);

            irm[0].Type = ResourceManagerType.Durable;
            for (int i = 0; i < 4; i++)
                irm[i].UseSingle = true;

            using (TransactionScope scope = new TransactionScope())
            {
                irm[0].Value = 2;
                irm[1].Value = 6;
                irm[2].Value = 10;
                irm[3].Value = 14;

                scope.Complete();
            }

            irm[0].CheckSPC("irm [0]");

            /* Volatile RMs get 2PC */
            for (int i = 1; i < 4; i++)
                irm[i].Check2PC("irm [" + i + "]");
        }

        /* >1vol + 1 durable
		 * Durable fails SPC
		 */
        [Fact]
        public void Vol2_Dur1_Fail1()
        {
            IntResourceManager[] irm = new IntResourceManager[4];
            irm[0] = new IntResourceManager(1);
            irm[1] = new IntResourceManager(3);
            irm[2] = new IntResourceManager(5);
            irm[3] = new IntResourceManager(7);

            irm[0].Type = ResourceManagerType.Durable;
            irm[0].FailSPC = true;

            for (int i = 0; i < 4; i++)
                irm[i].UseSingle = true;

            /* Durable RM irm[0] does Abort on SPC, so
			 * all volatile RMs get Rollback */
            Assert.Throws<TransactionAbortedException>(() =>
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    irm[0].Value = 2;
                    irm[1].Value = 6;
                    irm[2].Value = 10;
                    irm[3].Value = 14;

                    scope.Complete();
                }
            });
            irm[0].CheckSPC("irm [0]");
            /* Volatile RMs get 2PC Prepare, and then get rolled back */
            for (int i = 1; i < 4; i++)
                irm[i].Check(0, 1, 0, 1, 0, 0, 0, "irm [" + i + "]");
        }

        /* >1vol + 1 durable
		 * Volatile fails Prepare
		 */
        [Fact]
        public void Vol2_Dur1_Fail3()
        {
            IntResourceManager[] irm = new IntResourceManager[4];
            irm[0] = new IntResourceManager(1);
            irm[1] = new IntResourceManager(3);
            irm[2] = new IntResourceManager(5);
            irm[3] = new IntResourceManager(7);

            irm[0].Type = ResourceManagerType.Durable;
            irm[2].FailPrepare = true;

            for (int i = 0; i < 4; i++)
                irm[i].UseSingle = true;

            /* Durable RM irm[2] does on SPC, so
			 * all volatile RMs get Rollback */
            Assert.Throws<TransactionAbortedException>(() =>
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    irm[0].Value = 2;
                    irm[1].Value = 6;
                    irm[2].Value = 10;
                    irm[3].Value = 14;

                    scope.Complete();
                }
            });
            irm[0].Check(0, 0, 0, 1, 0, 0, 0, "irm [0]");

            /* irm [1] & [2] get prepare,
             * [2] -> ForceRollback,
             * [1] & [3] get rollback,
             * [0](durable) gets rollback */
            irm[1].Check(0, 1, 0, 1, 0, 0, 0, "irm [1]");
            irm[2].Check(0, 1, 0, 0, 0, 0, 0, "irm [2]");
            irm[3].Check(0, 0, 0, 1, 0, 0, 0, "irm [3]");
        }

        [Fact]
        public void Vol2_Dur1_Fail4()
        {
            IntResourceManager[] irm = new IntResourceManager[2];
            irm[0] = new IntResourceManager(1);
            irm[1] = new IntResourceManager(3);

            irm[0].Type = ResourceManagerType.Durable;
            irm[0].FailSPC = true;
            irm[0].FailWithException = true;

            for (int i = 0; i < 2; i++)
                irm[i].UseSingle = true;

            /* Durable RM irm[2] does on SPC, so
			 * all volatile RMs get Rollback */
            TransactionAbortedException e = Assert.Throws<TransactionAbortedException>(() =>
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    irm[0].Value = 2;
                    irm[1].Value = 6;

                    scope.Complete();
                }
            });
            Assert.IsType<NotSupportedException>(e.InnerException);

            irm[0].Check(1, 0, 0, 0, 0, 0, 0, "irm [0]");
            irm[1].Check(0, 1, 0, 1, 0, 0, 0, "irm [1]");
        }

        [Fact]
        public void Vol2_Dur1_Fail5()
        {
            CommittableTransaction ct = new CommittableTransaction();
            IntResourceManager[] irm = new IntResourceManager[2];
            irm[0] = new IntResourceManager(1);
            irm[1] = new IntResourceManager(3);

            Transaction.Current = ct;
            irm[0].Type = ResourceManagerType.Durable;
            irm[0].FailSPC = true;
            irm[0].FailWithException = true;

            for (int i = 0; i < 2; i++)
                irm[i].UseSingle = true;

            /* Durable RM irm[2] does on SPC, so
			 * all volatile RMs get Rollback */

            using (TransactionScope scope = new TransactionScope())
            {
                irm[0].Value = 2;
                irm[1].Value = 6;

                scope.Complete();
            }

            TransactionAbortedException tae = Assert.Throws<TransactionAbortedException>(() => ct.Commit());
            Assert.IsType<NotSupportedException>(tae.InnerException);

            irm[0].Check(1, 0, 0, 0, 0, 0, 0, "irm [0]");
            irm[1].Check(0, 1, 0, 1, 0, 0, 0, "irm [1]");

            InvalidOperationException ioe = Assert.Throws<InvalidOperationException>(() => ct.Commit());
            Assert.Null(ioe.InnerException);

            Transaction.Current = null;
        }

        #endregion

        #region Promotable Single Phase Enlistment
        [Fact]
        public void Vol0_Dur0_Pspe1()
        {
            IntResourceManager irm = new IntResourceManager(1);
            irm.Type = ResourceManagerType.Promotable;
            using (TransactionScope scope = new TransactionScope())
            {
                irm.Value = 2;

                scope.Complete();
            }
            irm.Check(1, 0, 0, 0, 0, 1, 0, "irm");
        }

        [Fact]
        public void Vol1_Dur0_Pspe1()
        {
            IntResourceManager irm0 = new IntResourceManager(1);
            IntResourceManager irm1 = new IntResourceManager(1);
            irm1.Type = ResourceManagerType.Promotable;
            using (TransactionScope scope = new TransactionScope())
            {
                irm0.Value = 2;
                irm1.Value = 8;

                scope.Complete();
            }
            irm1.Check(1, 0, 0, 0, 0, 1, 0, "irm1");
        }

        [Fact]
        public void Vol0_Dur1_Pspe1()
        {
            IntResourceManager irm0 = new IntResourceManager(1);
            IntResourceManager irm1 = new IntResourceManager(1);
            irm0.Type = ResourceManagerType.Durable;
            irm0.UseSingle = true;
            irm1.Type = ResourceManagerType.Promotable;
            using (TransactionScope scope = new TransactionScope())
            {
                irm0.Value = 8;
                irm1.Value = 2;
                Assert.Equal(0, irm1.NumEnlistFailed);
            }

            // TODO: Technically this is not correct. A call to EnlistPromotableSinglePhase is called AFTER a
            // DurableEnlist for a given transaction will return "false", which should probably be considered
            // an enlistment failure. An exception is not thrown, but the PSPE still "failed"
        }

        [Fact]
        public void Vol0_Dur0_Pspe2()
        {
            IntResourceManager irm0 = new IntResourceManager(1);
            IntResourceManager irm1 = new IntResourceManager(1);
            irm0.Type = ResourceManagerType.Promotable;
            irm1.Type = ResourceManagerType.Promotable;
            using (TransactionScope scope = new TransactionScope())
            {
                irm0.Value = 8;
                irm1.Value = 2;
                Assert.Equal(0, irm1.NumEnlistFailed);
            }

            // TODO: Technically this is not correct. A call to EnlistPromotableSinglePhase is called AFTER a
            // successful EnlistPromotableSinglePhase for a given transaction will return "false", which should
            // probably be considered an enlistment failure. An exception is not thrown, but the second PSPE still "failed".
        }
        #endregion

        #region Others
        /* >1vol  
		 * > 1 durable, On .net this becomes a distributed transaction
		 * We don't support this in mono yet. 
		 */
        [ActiveIssue(13532)] //Distributed transactions are not supported.
        [Fact]
        public void Vol0_Dur2()
        {
            IntResourceManager[] irm = new IntResourceManager[2];
            irm[0] = new IntResourceManager(1);
            irm[1] = new IntResourceManager(3);

            irm[0].Type = ResourceManagerType.Durable;
            irm[1].Type = ResourceManagerType.Durable;

            for (int i = 0; i < 2; i++)
                irm[i].UseSingle = true;

            using (TransactionScope scope = new TransactionScope())
            {
                irm[0].Value = 2;
                irm[1].Value = 6;

                scope.Complete();
            }
        }

        [Fact]
        public void TransactionDispose()
        {
            CommittableTransaction ct = new CommittableTransaction();
            IntResourceManager irm = new IntResourceManager(1);
            irm.Type = ResourceManagerType.Durable;

            ct.Dispose();
            irm.Check(0, 0, 0, 0, "Dispose transaction");
        }

        [Fact]
        public void TransactionDispose2()
        {
            CommittableTransaction ct = new CommittableTransaction();
            IntResourceManager irm = new IntResourceManager(1);

            Transaction.Current = ct;
            irm.Value = 5;

            try
            {
                ct.Dispose();
            }
            finally
            {
                Transaction.Current = null;
            }

            irm.Check(0, 0, 1, 0, "Dispose transaction");
            Assert.Equal(1, irm.Value);
        }

        [Fact]
        public void TransactionDispose3()
        {
            CommittableTransaction ct = new CommittableTransaction();
            IntResourceManager irm = new IntResourceManager(1);

            try
            {
                Transaction.Current = ct;
                irm.Value = 5;
                ct.Commit();
                ct.Dispose();
            }
            finally
            {
                Transaction.Current = null;
            }

            irm.Check(1, 1, 0, 0, "Dispose transaction");
            Assert.Equal(5, irm.Value);
        }
        #endregion

        #region TransactionCompleted
        [Fact]
        public void TransactionCompleted_Committed()
        {
            bool called = false;
            using (var ts = new TransactionScope())
            {
                var tr = Transaction.Current;
                tr.TransactionCompleted += (s, e) => called = true;
                ts.Complete();
            }

            Assert.True(called, "TransactionCompleted event handler not called!");
        }

        [Fact]
        public void TransactionCompleted_Rollback()
        {
            bool called = false;
            using (var ts = new TransactionScope())
            {
                var tr = Transaction.Current;
                tr.TransactionCompleted += (s, e) => called = true;
                // Not calling ts.Complete() on purpose..
            }

            Assert.True(called, "TransactionCompleted event handler not called!");
        }
        #endregion

        #region Success/Failure behavior tests
        #region Success/Failure behavior Vol1_Dur0 Cases
        [Fact]
        public void Vol1SPC_Committed()
        {
            bool called = false;
            TransactionStatus status = TransactionStatus.Active;
            var rm = new IntResourceManager(1)
            {
                UseSingle = true,
                Type = ResourceManagerType.Volatile
            };

            using (var ts = new TransactionScope())
            {
                rm.Value = 2;
                var tr = Transaction.Current;
                tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                ts.Complete();
            }

            rm.Check(1, 0, 0, 0, 0, 0, 0, "rm");
            Assert.True(called, "TransactionCompleted event handler not called!");
            Assert.Equal(TransactionStatus.Committed, status);
        }

        [Fact]
        public void Vol1_Committed()
        {
            bool called = false;
            TransactionStatus status = TransactionStatus.Active;
            var rm = new IntResourceManager(1)
            {
                Type = ResourceManagerType.Volatile,
            };

            using (var ts = new TransactionScope())
            {
                rm.Value = 2;
                var tr = Transaction.Current;
                tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                ts.Complete();
            }

            rm.Check(0, 1, 1, 0, 0, 0, 0, "rm");
            Assert.True(called, "TransactionCompleted event handler not called!");
            Assert.Equal(TransactionStatus.Committed, status);
        }

        [Fact]
        public void Vol1_Rollback()
        {
            bool called = false;
            TransactionStatus status = TransactionStatus.Active;
            var rm = new IntResourceManager(1)
            {
                Type = ResourceManagerType.Volatile,
            };

            using (var ts = new TransactionScope())
            {
                rm.Value = 2;
                var tr = Transaction.Current;
                tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                // Not calling ts.Complete() on purpose..
            }

            rm.Check(0, 0, 0, 1, 0, 0, 0, "rm");

            Assert.True(called, "TransactionCompleted event handler not called!");
            Assert.Equal(TransactionStatus.Aborted, status);
        }

        [Fact]
        public void Vol1SPC_Throwing_On_Commit()
        {
            bool called = false;
            Exception ex = null;
            TransactionStatus status = TransactionStatus.Active;
            var rm = new IntResourceManager(1)
            {
                UseSingle = true,
                FailSPC = true,
                FailWithException = true,
                Type = ResourceManagerType.Volatile
            };

            try
            {
                using (var ts = new TransactionScope())
                {
                    rm.Value = 2;
                    var tr = Transaction.Current;
                    tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                    ts.Complete();
                }
            }
            catch (Exception _ex)
            {
                ex = _ex;
            }

            rm.Check(1, 0, 0, 0, 0, 0, 0, "rm");

            Assert.True(called, "TransactionCompleted event handler not called!");
            Assert.Equal(TransactionStatus.Aborted, status);
            Assert.NotNull(ex);
            Assert.IsType<TransactionAbortedException>(ex);
            Assert.NotNull(ex.InnerException);
            Assert.IsType<NotSupportedException>(ex.InnerException);
        }

        [Fact]
        public void Vol1_Throwing_On_Commit()
        {
            bool called = false;
            TransactionStatus status = TransactionStatus.Active;
            Exception ex = null;
            var rm = new IntResourceManager(1)
            {
                FailCommit = true,
                FailWithException = true,
                Type = ResourceManagerType.Volatile
            };

            try
            {
                using (var ts = new TransactionScope())
                {
                    rm.Value = 2;
                    var tr = Transaction.Current;
                    tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                    ts.Complete();
                }
            }
            catch (Exception _ex)
            {
                ex = _ex;
            }

            rm.Check(0, 1, 1, 0, 0, 0, 0, "rm");

            // MS.NET won't call TransactionCompleted event in this particular case.
            Assert.False(called, "TransactionCompleted event handler _was_ called!?!?!");
            Assert.IsType<NotSupportedException>(ex);
        }

        [Fact]
        public void Vol1_Throwing_On_Rollback()
        {
            bool called = false;
            TransactionStatus status = TransactionStatus.Active;
            Exception ex = null;
            var rm = new IntResourceManager(1)
            {
                FailRollback = true,
                FailWithException = true,
                Type = ResourceManagerType.Volatile
            };

            try
            {
                using (var ts = new TransactionScope())
                {
                    rm.Value = 2;
                    var tr = Transaction.Current;
                    tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                    // Not calling ts.Complete() on purpose..
                }
            }
            catch (Exception _ex)
            {
                ex = _ex;
            }

            rm.Check(0, 0, 0, 1, 0, 0, 0, "rm");

            // MS.NET won't call TransactionCompleted event in this particular case.
            Assert.False(called, "TransactionCompleted event handler _was_ called!?!?!");
            Assert.NotNull(ex);
            // MS.NET will relay the exception thrown by RM instead of wrapping it on a TransactionAbortedException.
            Assert.IsType<NotSupportedException>(ex);
        }

        [Fact]
        public void Vol1_Throwing_On_Prepare()
        {
            bool called = false;
            TransactionStatus status = TransactionStatus.Active;
            Exception ex = null;
            var rm = new IntResourceManager(1)
            {
                FailPrepare = true,
                FailWithException = true,
                Type = ResourceManagerType.Volatile
            };

            try
            {
                using (var ts = new TransactionScope())
                {
                    rm.Value = 2;
                    var tr = Transaction.Current;
                    tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                    ts.Complete();
                }
            }
            catch (Exception _ex)
            {
                ex = _ex;
            }

            rm.Check(0, 1, 0, 0, 0, 0, 0, "rm");

            Assert.True(called, "TransactionCompleted event handler not called!");
            Assert.NotNull(ex);
            Assert.IsType<TransactionAbortedException>(ex);
            Assert.NotNull(ex.InnerException);
            Assert.IsType<NotSupportedException>(ex.InnerException);
            Assert.Equal(TransactionStatus.Aborted, status);
        }
        #endregion

        #region Success/Failure behavior Vol2_Dur0 Cases
        [Fact]
        public void Vol2SPC_Committed()
        {
            TransactionStatus status = TransactionStatus.Active;
            bool called = false;
            var rm1 = new IntResourceManager(1)
            {
                UseSingle = true,
                Type = ResourceManagerType.Volatile
            };
            var rm2 = new IntResourceManager(2)
            {
                UseSingle = true,
                Type = ResourceManagerType.Volatile
            };

            using (var ts = new TransactionScope())
            {
                rm1.Value = 11;
                rm2.Value = 22;
                var tr = Transaction.Current;
                tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                ts.Complete();
            }

            // There can be only one *Single* PC enlistment,
            // so TM will downgrade both to normal enlistments.
            rm1.Check(0, 1, 1, 0, 0, 0, 0, "rm1");
            rm2.Check(0, 1, 1, 0, 0, 0, 0, "rm2");

            Assert.True(called, "TransactionCompleted event handler not called!");
            Assert.Equal(TransactionStatus.Committed, status);
        }

        [Fact]
        public void Vol2_Committed()
        {
            TransactionStatus status = TransactionStatus.Active;
            bool called = false;
            var rm1 = new IntResourceManager(1)
            {
                Type = ResourceManagerType.Volatile
            };
            var rm2 = new IntResourceManager(1)
            {
                Type = ResourceManagerType.Volatile
            };

            using (var ts = new TransactionScope())
            {
                rm1.Value = 11;
                rm2.Value = 22;
                var tr = Transaction.Current;
                tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                ts.Complete();
            }
            rm1.Check(0, 1, 1, 0, 0, 0, 0, "rm1");
            rm2.Check(0, 1, 1, 0, 0, 0, 0, "rm2");

            Assert.True(called, "TransactionCompleted event handler not called!");
            Assert.Equal(TransactionStatus.Committed, status);
        }

        [Fact]
        public void Vol2_Rollback()
        {
            TransactionStatus status = TransactionStatus.Active;
            bool called = false;
            var rm1 = new IntResourceManager(1)
            {
                Type = ResourceManagerType.Volatile
            };
            var rm2 = new IntResourceManager(1)
            {
                Type = ResourceManagerType.Volatile
            };

            using (var ts = new TransactionScope())
            {
                rm1.Value = 11;
                rm2.Value = 22;
                var tr = Transaction.Current;
                tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                // Not calling ts.Complete() on purpose..
            }

            rm1.Check(0, 0, 0, 1, 0, 0, 0, "rm1");
            rm2.Check(0, 0, 0, 1, 0, 0, 0, "rm2");

            Assert.True(called, "TransactionCompleted event handler not called!");
            Assert.Equal(TransactionStatus.Aborted, status);
        }

        [Fact]
        public void Vol2SPC_Throwing_On_Commit()
        {
            TransactionStatus status = TransactionStatus.Active;
            bool called = false;
            Exception ex = null;
            var rm1 = new IntResourceManager(1)
            {
                UseSingle = true,
                FailCommit = true,
                FailWithException = true,
                ThrowThisException = new InvalidOperationException("rm1"),
                Type = ResourceManagerType.Volatile,
            };
            var rm2 = new IntResourceManager(2)
            {
                UseSingle = true,
                Type = ResourceManagerType.Volatile,
            };

            try
            {
                using (var ts = new TransactionScope())
                {
                    rm1.Value = 11;
                    rm2.Value = 22;

                    var tr = Transaction.Current;
                    tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                    ts.Complete();
                }
            }
            catch (Exception _ex)
            {
                ex = _ex;
            }

            // There can be only one *Single* PC enlistment,
            // so TM will downgrade both to normal enlistments.
            rm1.Check(0, 1, 1, 0, 0, 0, 0, "rm1");
            rm2.Check(0, 1, 0, 0, 0, 0, 0, "rm2");

            // MS.NET won't call TransactionCompleted event in this particular case.
            Assert.False(called, "TransactionCompleted event handler _was_ called!?!?!");
            Assert.NotNull(ex);
            Assert.Equal(rm1.ThrowThisException, ex);
        }

        [Fact]
        public void Vol2_Throwing_On_Commit()
        {
            bool called = false;
            Exception ex = null;
            var rm1 = new IntResourceManager(1)
            {
                FailCommit = true,
                FailWithException = true,
                ThrowThisException = new InvalidOperationException("rm1"),
                Type = ResourceManagerType.Volatile
            };
            var rm2 = new IntResourceManager(2)
            {
                Type = ResourceManagerType.Volatile
            };

            try
            {
                using (var ts = new TransactionScope())
                {
                    rm1.Value = 11;
                    rm2.Value = 22;

                    var tr = Transaction.Current;
                    tr.TransactionCompleted += (s, e) => called = true;
                    ts.Complete();
                }
            }
            catch (Exception _ex)
            {
                ex = _ex;
            }

            rm1.Check(0, 1, 1, 0, 0, 0, 0, "rm1");
            rm2.Check(0, 1, 0, 0, 0, 0, 0, "rm2");

            // MS.NET won't call TransactionCompleted event in this particular case.
            Assert.False(called, "TransactionCompleted event handler _was_ called!?!?!");
            Assert.NotNull(ex);
            Assert.Equal(rm1.ThrowThisException, ex);
        }

        [Fact]
        public void Vol2_Throwing_On_Rollback()
        {
            bool called = false;
            Exception ex = null;
            var rm1 = new IntResourceManager(1)
            {
                FailRollback = true,
                FailWithException = true,
                ThrowThisException = new InvalidOperationException("rm1"),
                Type = ResourceManagerType.Volatile
            };
            var rm2 = new IntResourceManager(2)
            {
                Type = ResourceManagerType.Volatile
            };

            try
            {
                using (var ts = new TransactionScope())
                {
                    rm1.Value = 11;
                    rm2.Value = 22;

                    var tr = Transaction.Current;
                    tr.TransactionCompleted += (s, e) => called = true;
                    // Not calling ts.Complete() on purpose..
                }
            }
            catch (Exception _ex)
            {
                ex = _ex;
            }

            rm1.Check(0, 0, 0, 1, 0, 0, 0, "rm1");
            rm2.Check(0, 0, 0, 0, 0, 0, 0, "rm2");

            // MS.NET won't call TransactionCompleted event in this particular case.
            Assert.False(called, "TransactionCompleted event handler _was_ called!?!?!");
            Assert.NotNull(ex);
            // MS.NET will relay the exception thrown by RM instead of wrapping it on a TransactionAbortedException.
            Assert.Equal(rm1.ThrowThisException, ex);
        }

        [Fact]
        public void Vol2_Throwing_On_First_Prepare()
        {
            TransactionStatus status = TransactionStatus.Active;
            bool called = false;
            Exception ex = null;
            var rm1 = new IntResourceManager(1)
            {
                FailPrepare = true,
                FailWithException = true,
                ThrowThisException = new InvalidOperationException("rm1"),
                Type = ResourceManagerType.Volatile
            };
            var rm2 = new IntResourceManager(2)
            {
                Type = ResourceManagerType.Volatile
            };

            try
            {
                using (var ts = new TransactionScope())
                {
                    rm1.Value = 11;
                    rm2.Value = 22;

                    var tr = Transaction.Current;
                    tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                    ts.Complete();
                }
            }
            catch (Exception _ex)
            {
                ex = _ex;
            }

            rm1.Check(0, 1, 0, 0, 0, 0, 0, "rm1");
            rm2.Check(0, 0, 0, 1, 0, 0, 0, "rm2");

            Assert.True(called, "TransactionCompleted event handler not called!");
            Assert.NotNull(ex);
            Assert.IsType<TransactionAbortedException>(ex);
            Assert.NotNull(ex.InnerException);
            Assert.IsType<InvalidOperationException>(ex.InnerException);
            Assert.Equal(TransactionStatus.Aborted, status);
        }

        [Fact]
        public void Vol2_Throwing_On_Second_Prepare()
        {
            TransactionStatus status = TransactionStatus.Active;
            bool called = false;
            Exception ex = null;
            var rm1 = new IntResourceManager(1)
            {
                Type = ResourceManagerType.Volatile
            };
            var rm2 = new IntResourceManager(2)
            {
                FailPrepare = true,
                FailWithException = true,
                Type = ResourceManagerType.Volatile
            };

            try
            {
                using (var ts = new TransactionScope())
                {
                    rm1.Value = 11;
                    rm2.Value = 22;

                    var tr = Transaction.Current;
                    tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                    ts.Complete();
                }
            }
            catch (Exception _ex)
            {
                ex = _ex;
            }

            rm1.Check(0, 1, 0, 1, 0, 0, 0, "rm1");
            rm2.Check(0, 1, 0, 0, 0, 0, 0, "rm2");

            Assert.True(called, "TransactionCompleted event handler not called!");
            Assert.NotNull(ex);
            Assert.IsType<TransactionAbortedException>(ex);
            Assert.NotNull(ex.InnerException);
            Assert.IsType<NotSupportedException>(ex.InnerException);
            Assert.Equal(TransactionStatus.Aborted, status);
        }

        [Fact]
        public void Vol2_Throwing_On_First_Prepare_And_Second_Rollback()
        {
            TransactionStatus status = TransactionStatus.Active;
            bool called = false;
            Exception ex = null;
            var rm1 = new IntResourceManager(1)
            {
                FailPrepare = true,
                FailWithException = true,
                ThrowThisException = new InvalidOperationException("rm1"),
                Type = ResourceManagerType.Volatile
            };
            var rm2 = new IntResourceManager(2)
            {
                FailRollback = true,
                FailWithException = true,
                ThrowThisException = new InvalidOperationException("rm2"),
                Type = ResourceManagerType.Volatile
            };

            try
            {
                using (var ts = new TransactionScope())
                {
                    rm1.Value = 11;
                    rm2.Value = 22;

                    var tr = Transaction.Current;
                    tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                    ts.Complete();
                }
            }
            catch (Exception _ex)
            {
                ex = _ex;
            }

            rm1.Check(0, 1, 0, 0, 0, 0, 0, "rm1");
            rm2.Check(0, 0, 0, 1, 0, 0, 0, "rm2");

            // MS.NET won't call TransactionCompleted event in this particular case.
            Assert.False(called, "TransactionCompleted event handler _was_ called!?!?!");
            Assert.NotNull(ex);
            Assert.Equal(rm2.ThrowThisException, ex);
        }

        [Fact]
        public void Vol2_Throwing_On_First_Rollback_And_Second_Prepare()
        {
            TransactionStatus status = TransactionStatus.Active;
            bool called = false;
            Exception ex = null;
            var rm1 = new IntResourceManager(1)
            {
                FailRollback = true,
                FailWithException = true,
                ThrowThisException = new InvalidOperationException("rm1"),
                Type = ResourceManagerType.Volatile
            };
            var rm2 = new IntResourceManager(2)
            {
                FailPrepare = true,
                FailWithException = true,
                ThrowThisException = new InvalidOperationException("rm2"),
                Type = ResourceManagerType.Volatile
            };

            try
            {
                using (var ts = new TransactionScope())
                {
                    rm1.Value = 11;
                    rm2.Value = 22;

                    var tr = Transaction.Current;
                    tr.TransactionCompleted += (s, e) => { called = true; status = e.Transaction.TransactionInformation.Status; };
                    ts.Complete();
                }
            }
            catch (Exception _ex)
            {
                ex = _ex;
            }

            rm1.Check(0, 1, 0, 1, 0, 0, 0, "rm1");
            rm2.Check(0, 1, 0, 0, 0, 0, 0, "rm2");

            // MS.NET won't call TransactionCompleted event in this particular case.
            Assert.False(called, "TransactionCompleted event handler _was_ called!?!?!");
            Assert.NotNull(ex);
            Assert.Equal(rm1.ThrowThisException, ex);
        }

        #endregion

        #endregion
    }
}

