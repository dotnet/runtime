// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    /// <summary>Indicates whether a method is an asynchronous iterator.</summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class AsyncIteratorStateMachineAttribute : StateMachineAttribute
    {
        /// <summary>Initializes a new instance of the <see cref="AsyncIteratorStateMachineAttribute"/> class.</summary>
        /// <param name="stateMachineType">The type object for the underlying state machine type that's used to implement a state machine method.</param>
        public AsyncIteratorStateMachineAttribute(Type stateMachineType)
            : base(stateMachineType)
        {
        }
    }
}
