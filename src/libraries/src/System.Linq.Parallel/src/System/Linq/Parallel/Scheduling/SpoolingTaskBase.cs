// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// SpoolingTaskBase.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A spooling task handles marshaling data from a producer to a consumer. It simply
    /// takes data from a producer and hands it off to a consumer. This class is the base
    /// class from which other concrete spooling tasks derive, encapsulating some common
    /// logic (such as capturing exceptions).
    /// </summary>
    internal abstract class SpoolingTaskBase : QueryTask
    {
        //-----------------------------------------------------------------------------------
        // Constructs a new spooling task.
        //
        // Arguments:
        //     taskIndex   - the unique index of this task
        //

        protected SpoolingTaskBase(int taskIndex, QueryTaskGroupState groupState) :
            base(taskIndex, groupState)
        {
        }

        //-----------------------------------------------------------------------------------
        // The implementation of the Work API just enumerates the producer's data, and
        // enqueues it into the consumer channel. Well, really, it just defers to extension
        // points (below) implemented by subclasses to do these things.
        //

        protected override void Work()
        {
            try
            {
                // Defer to the base class for the actual work.
                SpoolingWork();
            }
            catch (Exception ex)
            {
                OperationCanceledException oce = ex as OperationCanceledException;
                if (oce != null &&
                    oce.CancellationToken == _groupState.CancellationState.MergedCancellationToken
                    && _groupState.CancellationState.MergedCancellationToken.IsCancellationRequested)
                {
                    //an expected internal cancellation has occurred.  suppress this exception.
                }
                else
                {
                    // TPL will catch and store the exception on the task object. We'll then later
                    // turn around and wait on it, having the effect of propagating it. In the meantime,
                    // we want to cooperative cancel all workers.
                    _groupState.CancellationState.InternalCancellationTokenSource.Cancel();

                    // And then repropagate to let TPL catch it.
                    throw;
                }
            }
            finally
            {
                SpoolingFinally(); //dispose resources etc.
            }
        }

        //-----------------------------------------------------------------------------------
        // This method is responsible for enumerating results and enqueueing them to
        // the output channel(s) as appropriate.  Each base class implements its own.
        //

        protected abstract void SpoolingWork();

        //-----------------------------------------------------------------------------------
        // If the subclass needs to do something in the finally block of the main work loop,
        // it should override this and do it.  Purely optional.
        //

        protected virtual void SpoolingFinally()
        {
            // (Intentionally left blank.)
        }
    }
}
