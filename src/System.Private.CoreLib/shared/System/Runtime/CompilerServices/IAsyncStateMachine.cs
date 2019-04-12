// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// Represents state machines generated for asynchronous methods.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

#nullable enable
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Represents state machines generated for asynchronous methods.
    /// This type is intended for compiler use only.
    /// </summary>
    public interface IAsyncStateMachine
    {
        /// <summary>Moves the state machine to its next state.</summary>
        void MoveNext();
        /// <summary>Configures the state machine with a heap-allocated replica.</summary>
        /// <param name="stateMachine">The heap-allocated replica.</param>
        void SetStateMachine(IAsyncStateMachine stateMachine);
    }
}
