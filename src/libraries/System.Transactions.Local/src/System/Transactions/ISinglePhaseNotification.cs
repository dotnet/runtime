// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Transactions
{
    internal interface ISinglePhaseNotificationInternal : IEnlistmentNotificationInternal
    {
        void SinglePhaseCommit(IPromotedEnlistment singlePhaseEnlistment);
    }

    public interface ISinglePhaseNotification : IEnlistmentNotification
    {
        void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment);
    }
}
