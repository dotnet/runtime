// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// An interface implemented by all <see cref="AsyncTaskMethodBuilder{TResult}.AsyncStateMachineBox{TStateMachine}"/> instances, regardless of generics.
    /// </summary>
    internal interface IAsyncStateMachineBox
    {
        /// <summary>Move the state machine forward.</summary>
        void MoveNext();

        /// <summary>
        /// Gets an action for moving forward the contained state machine.
        /// This will lazily-allocate the delegate as needed.
        /// </summary>
        Action MoveNextAction { get; }

        /// <summary>Gets the state machine as a boxed object.  This should only be used for debugging purposes.</summary>
        IAsyncStateMachine GetStateMachineObject();
    }
}
