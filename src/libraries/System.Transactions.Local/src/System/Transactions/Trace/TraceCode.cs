// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Transactions.Diagnostics
{
    static internal class TransactionsTraceCode
           {
               const string Prefix = "http://msdn.microsoft.com/2004/06/System/";
               const string TransactionsFeature = "Transactions/";

               public const string CheckMemoryGatePassed    = Prefix + TransactionsFeature + "CheckMemoryGate/Passed";
               public const string TransactionCreated                  = Prefix + TransactionsFeature + "TransactionCreated";
               public const string TransactionPromoted                 = Prefix + TransactionsFeature + "TransactionPromoted";
               public const string Enlistment                          = Prefix + TransactionsFeature + "Enlistment";
               public const string EnlistmentNotificationCall          = Prefix + TransactionsFeature + "EnlistmentNotificationCall";
               public const string EnlistmentCallbackPositive          = Prefix + TransactionsFeature + "EnlistmentCallbackPositive";
               public const string EnlistmentCallbackNegative          = Prefix + TransactionsFeature + "EnlistmentCallbackNegative";
               public const string TransactionCommitCalled             = Prefix + TransactionsFeature + "TransactionCommitCalled";
               public const string TransactionRollbackCalled           = Prefix + TransactionsFeature + "TransactionRollbackCalled";
               public const string TransactionCommitted                = Prefix + TransactionsFeature + "TransactionCommitted";
               public const string TransactionAborted                  = Prefix + TransactionsFeature + "TransactionAborted";
               public const string TransactionInDoubt                  = Prefix + TransactionsFeature + "TransactionInDoubt";
               public const string TransactionScopeCreated             = Prefix + TransactionsFeature + "TransactionScopeCreated";
               public const string TransactionScopeDisposed            = Prefix + TransactionsFeature + "TransactionScopeDisposed";
               public const string TransactionScopeIncomplete          = Prefix + TransactionsFeature + "TransactionScopeIncomplete";
               public const string TransactionScopeNestedIncorrectly   = Prefix + TransactionsFeature + "TransactionScopeNestedIncorrectly";
               public const string TransactionScopeCurrentTransactionChanged      = Prefix + TransactionsFeature + "TransactionScopeCurrentTransactionChanged";
               public const string TransactionScopeTimeout             = Prefix + TransactionsFeature + "TransactionScopeTimeout";
               public const string TransactionTimeout                  = Prefix + TransactionsFeature + "TransactionTimeout";
               public const string DependentCloneCreated               = Prefix + TransactionsFeature + "DependentCloneCreated";
               public const string DependentCloneComplete              = Prefix + TransactionsFeature + "DependentCloneComplete";
               public const string CloneCreated                        = Prefix + TransactionsFeature + "CloneCreated";
               public const string PromotableSinglePhaseEnlistment     = Prefix + TransactionsFeature + "PromotableSinglePhaseEnlistment";
               //        no longer used                    = Prefix + TransactionsFeature + "";
               public const string RecoveryComplete                    = Prefix + TransactionsFeature + "RecoveryComplete";
               public const string ResourceManagerRecoveryComplete     = Prefix + TransactionsFeature + "ResourceManagerRecoveryComplete";
               public const string Reenlist                            = Prefix + TransactionsFeature + "Reenlist";
               public const string ReenlistTransaction                 = Prefix + TransactionsFeature + "ReenlistTransaction";
               public const string TransactionManagerCreated           = Prefix + TransactionsFeature + "TransactionManagerCreated";
               public const string TransactionSerialized               = Prefix + TransactionsFeature + "TransactionSerialized";
               public const string TransactionDeserialized             = Prefix + TransactionsFeature + "TransactionDeserialized";
               public const string TransactionException                = Prefix + TransactionsFeature + "TransactionException";
               public const string InvalidOperationException           = Prefix + TransactionsFeature + "InvalidOperationException";
               public const string InternalError                       = Prefix + TransactionsFeature + "InternalError";
               public const string MethodEntered                       = Prefix + TransactionsFeature + "MethodEntered";
               public const string MethodExited                        = Prefix + TransactionsFeature + "MethodExited";
               public const string FailedToLoadPerformanceCounter      = Prefix + TransactionsFeature + "FailedToLoadPerformanceCounter";
               public const string UnhandledException                  = Prefix + TransactionsFeature + "UnhandledException";
               public const string ConfiguredDefaultTimeoutAdjusted    = Prefix + TransactionsFeature + "ConfiguredDefaultTimeoutAdjusted";
               public const string ExceptionConsumed                   = Prefix + TransactionsFeature + "ExceptionConsumed";
               public const string ActivityIdSet                       = Prefix + TransactionsFeature + "ActivityIdSet";
               public const string NewActivityIdIssued                 = Prefix + TransactionsFeature + "NewActivityIdIssued";
               //more to come
           }
}
