// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
//
//
//
// Interfaces used to represent instances that notify listeners of their completion via continuations.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

#nullable enable
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Represents an operation that will schedule continuations when the operation completes.
    /// </summary>
    public interface INotifyCompletion
    {
        /// <summary>Schedules the continuation action to be invoked when the instance completes.</summary>
        /// <param name="continuation">The action to invoke when the operation completes.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="continuation"/> argument is null (Nothing in Visual Basic).</exception>
        void OnCompleted(Action continuation);
    }

    /// <summary>
    /// Represents an awaiter used to schedule continuations when an await operation completes.
    /// </summary>
    public interface ICriticalNotifyCompletion : INotifyCompletion
    {
        /// <summary>Schedules the continuation action to be invoked when the instance completes.</summary>
        /// <param name="continuation">The action to invoke when the operation completes.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="continuation"/> argument is null (Nothing in Visual Basic).</exception>
        /// <remarks>Unlike OnCompleted, UnsafeOnCompleted need not propagate ExecutionContext information.</remarks>
        void UnsafeOnCompleted(Action continuation);
    }
}
