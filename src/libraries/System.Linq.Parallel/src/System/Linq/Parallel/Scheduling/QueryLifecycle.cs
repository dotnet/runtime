// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// QueryLifecycle.cs
//
// A convenient place to put things associated with entire queries and their lifecycle events.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel
{
    internal static class QueryLifecycle
    {
        // This method is called once per execution of a logical query.
        // (It is not called multiple time if repartitionings occur)
        internal static void LogicalQueryExecutionBegin(int queryID)
        {
            //We call NOCTD to inform the debugger that multiple threads will most likely be required to 
            //execute this query.  We do not attempt to run the query even if we think we could, for simplicity and consistency.
            PlinqEtwProvider.Log.ParallelQueryBegin(queryID);
        }


        // This method is called once per execution of a logical query.
        // (It is not called multiple time if repartitionings occur)
        internal static void LogicalQueryExecutionEnd(int queryID)
        {
            PlinqEtwProvider.Log.ParallelQueryEnd(queryID);
        }
    }
}
