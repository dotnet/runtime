// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Fragile trick for getting the current state of a .NET Core async method state machine.
    /// To use, await FetchAsync to get back an object:
    ///     object box = await GetStateMachineData.FetchAsync();
    /// </summary>
    internal sealed class GetStateMachineData : ICriticalNotifyCompletion
    {
        private object _box;

        /// <summary>Returns an awaitable whose awaited result will be the boxed state machine for the async method.</summary>
        public static GetStateMachineData FetchAsync() => new GetStateMachineData();

        private GetStateMachineData() { }
        public GetStateMachineData GetAwaiter() => this;
        public bool IsCompleted => false;
        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation)
        {
            _box = continuation.Target;
            Task.Run(continuation);
        }
        public object GetResult() => _box;
    }
}
