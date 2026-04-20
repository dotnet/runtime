// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Threading
{
    public abstract class PollTriggeredOperation : IThreadPoolWorkItem
    {
        void IThreadPoolWorkItem.Execute() { }

        protected internal abstract bool TryCompleteOperation(SafeHandle handle);
        protected internal abstract void OnCompleted(PollOperationOnCompletedResult result);
    }
}
