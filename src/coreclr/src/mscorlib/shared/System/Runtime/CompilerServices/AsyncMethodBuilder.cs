// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;

namespace System.Runtime.CompilerServices
{
    internal static partial class AsyncMethodBuilder
    {
        /// <summary>Initiates the builder's execution with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [DebuggerStepThrough]
        public static void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            if (stateMachine == null) // TStateMachines are generally non-nullable value types, so this check will be elided
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stateMachine);
            }

            // enregistrer variables with 0 post-fix so they can be used in registers without EH forcing them to stack
            // Capture references to Thread Contexts
            Thread currentThread0 = Thread.CurrentThread;
            Thread currentThread = currentThread0;
            ExecutionContext previousExecutionCtx0 = currentThread0.ExecutionContext;

            // Store current ExecutionContext and SynchronizationContext as "previousXxx".
            // This allows us to restore them and undo any Context changes made in stateMachine.MoveNext
            // so that they won't "leak" out of the first await.
            ExecutionContext previousExecutionCtx = previousExecutionCtx0;
            SynchronizationContext previousSyncCtx = currentThread0.SynchronizationContext;

            try
            {
                stateMachine.MoveNext();
            }
            finally
            {
                // Re-enregistrer variables post EH with 1 post-fix so they can be used in registers rather than from stack
                SynchronizationContext previousSyncCtx1 = previousSyncCtx;
                Thread currentThread1 = currentThread;
                // The common case is that these have not changed, so avoid the cost of a write barrier if not needed.
                if (previousSyncCtx1 != currentThread1.SynchronizationContext)
                {
                    // Restore changed SynchronizationContext back to previous
                    currentThread1.SynchronizationContext = previousSyncCtx1;
                }

                ExecutionContext previousExecutionCtx1 = previousExecutionCtx;
                ExecutionContext currentExecutionCtx1 = currentThread1.ExecutionContext;
                if (previousExecutionCtx1 != currentExecutionCtx1)
                {
                    // Restore changed ExecutionContext back to previous
                    currentThread1.ExecutionContext = previousExecutionCtx1;
                    if ((currentExecutionCtx1 != null && currentExecutionCtx1.HasChangeNotifications) ||
                        (previousExecutionCtx1 != null && previousExecutionCtx1.HasChangeNotifications))
                    {
                        // There are change notifications; trigger any affected
                        ExecutionContext.OnValuesChanged(currentExecutionCtx1, previousExecutionCtx1);
                    }
                }
            }
        }
    }
}
