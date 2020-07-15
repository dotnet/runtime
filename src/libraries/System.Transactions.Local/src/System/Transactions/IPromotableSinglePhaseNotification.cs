// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Transactions
{
    public interface IPromotableSinglePhaseNotification : ITransactionPromoter
    {
        void Initialize();

        void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment);

        void Rollback(SinglePhaseEnlistment singlePhaseEnlistment);
    }
}
