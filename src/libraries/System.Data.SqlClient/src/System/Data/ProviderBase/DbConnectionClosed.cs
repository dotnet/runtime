// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Transactions;

namespace System.Data.ProviderBase
{
    internal abstract partial class DbConnectionClosed : DbConnectionInternal
    {
        protected override void Activate(Transaction transaction) => throw ADP.ClosedConnectionError();

        public override void EnlistTransaction(Transaction transaction) => throw ADP.ClosedConnectionError();
    }
}
