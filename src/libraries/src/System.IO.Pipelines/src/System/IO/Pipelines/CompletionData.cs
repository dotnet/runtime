// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.IO.Pipelines
{
    internal readonly struct CompletionData
    {
        public Action<object?> Completion { get; }
        public object? CompletionState { get; }
        public ExecutionContext? ExecutionContext { get; }
        public SynchronizationContext? SynchronizationContext { get; }

        public CompletionData(Action<object?> completion, object? completionState, ExecutionContext? executionContext, SynchronizationContext? synchronizationContext)
        {
            Completion = completion;
            CompletionState = completionState;
            ExecutionContext = executionContext;
            SynchronizationContext = synchronizationContext;
        }
    }
}
