// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// CancellationState.cs
//
// A bag of cancellation-related items that are passed around as a group.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel
{
    internal sealed class CancellationState
    {
        // a cancellation signal that can be set internally to prompt early query termination.
        internal CancellationTokenSource? InternalCancellationTokenSource;

        // the external cancellationToken that the user sets to ask for the query to terminate early.
        // this has to be tracked explicitly so that an OCE(externalToken) can be thrown as the query
        // execution unravels.
        internal CancellationToken ExternalCancellationToken;

        // A combined token Source for internal/external cancellation, defining the total cancellation state.
        internal CancellationTokenSource? MergedCancellationTokenSource;

        // A combined token for internal/external cancellation, defining the total cancellation state.
        internal CancellationToken MergedCancellationToken
        {
            get
            {
                if (MergedCancellationTokenSource != null)
                    return MergedCancellationTokenSource.Token;
                else
                    return new CancellationToken(false);
            }
        }

        // A shared boolean flag to track whether a query-opening-enumerator dispose has occurred.
        internal Shared<bool> TopLevelDisposedFlag;

        internal CancellationState(CancellationToken externalCancellationToken)
        {
            ExternalCancellationToken = externalCancellationToken;
            TopLevelDisposedFlag = new Shared<bool>(false); //it would always be initialized to false, so no harm doing it here and avoid #if around constructors.
        }

        /// <summary>
        /// Poll frequency (number of loops per cancellation check) for situations where per-1-loop testing is too high an overhead.
        /// </summary>
        internal const int POLL_INTERVAL = 63;  //must be of the form (2^n)-1.

        // The two main situations requiring POLL_INTERVAL are:
        //    1. inner loops of sorting/merging operations
        //    2. tight loops that perform very little work per MoveNext call.
        // Testing has shown both situations have similar requirements and can share the same constant for polling interval.
        //
        // Because the poll checks are per-N loops, if there are delays in user code, they may affect cancellation timeliness.
        // Guidance is that all user-delegates should perform cancellation checks at least every 1ms.
        //
        // Inner loop code should poll once per n loop, typically via:
        // if ((i++ & CancellationState.POLL_INTERVAL) == 0)
        //     _cancellationToken.ThrowIfCancellationRequested();
        // (Note, this only behaves as expected if FREQ is of the form (2^n)-1

        // Test if external cancellation was requested and occurred, and if so throw a standardize OCE with standardized message
        internal static void ThrowWithStandardMessageIfCanceled(CancellationToken externalCancellationToken)
        {
            if (externalCancellationToken.IsCancellationRequested)
            {
                string oceMessage = SR.PLINQ_ExternalCancellationRequested;
                throw new OperationCanceledException(oceMessage, externalCancellationToken);
            }
        }
    }
}
