// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace System.Transactions.DtcProxyShim;

internal static class Guids
{
    internal const string IID_ITransactionDispenser = "3A6AD9E1-23B9-11cf-AD60-00AA00A74CCD";
    internal const string IID_IResourceManager = "13741D21-87EB-11CE-8081-0080C758527E";
    internal const string IID_ITransactionOutcomeEvents = "3A6AD9E2-23B9-11cf-AD60-00AA00A74CCD";
    internal const string IID_ITransaction = "0fb15084-af41-11ce-bd2b-204c4f4f5020";

    internal static readonly Guid IID_ITransactionDispenser_Guid = Guid.Parse(IID_ITransactionDispenser);
    internal static readonly Guid IID_IResourceManager_Guid = Guid.Parse(IID_IResourceManager);
    internal static readonly Guid IID_ITransactionOutcomeEvents_Guid = Guid.Parse(IID_ITransactionOutcomeEvents);
    internal static readonly Guid IID_ITransaction_Guid = Guid.Parse(IID_ITransaction);
}
