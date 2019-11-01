// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Transactions
{
    internal interface IEnlistmentNotificationInternal
    {
        void Prepare(IPromotedEnlistment preparingEnlistment);

        void Commit(IPromotedEnlistment enlistment);

        void Rollback(IPromotedEnlistment enlistment);

        void InDoubt(IPromotedEnlistment enlistment);
    }

    public interface IEnlistmentNotification
    {
        void Prepare(PreparingEnlistment preparingEnlistment);

        void Commit(Enlistment enlistment);

        void Rollback(Enlistment enlistment);

        void InDoubt(Enlistment enlistment);
    }
}
