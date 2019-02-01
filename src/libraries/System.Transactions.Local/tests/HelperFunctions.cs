// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Transactions.Tests
{
    public class HelperFunctions
    {
        public static void PromoteTx(Transaction tx)
        {
            TransactionInterop.GetDtcTransaction(tx);
        }
    }
}
