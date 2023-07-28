// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Transactions.DtcProxyShim;

internal enum TransactionOutcome
{
    NotKnownYet                 = 0,
    Committed                   = 1,
    Aborted                     = 2
}
