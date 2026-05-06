// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed partial class UnixHandleAsyncContext
    {
        public abstract class Operation : IThreadPoolWorkItem
        {
            void IThreadPoolWorkItem.Execute() => ExecuteThreadPoolWorkItem();

            protected virtual void ExecuteThreadPoolWorkItem() { }

            protected internal abstract bool TryCompleteOperation(SafeHandle handle);
            protected internal abstract void OnCompleted(OnCompletedResult result);
        }
    }
}
