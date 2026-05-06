// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System
{
    /// <summary>
    /// Represents the status of an asynchronous operation.
    /// </summary>
    public interface IAsyncResult
    {
        bool IsCompleted { get; }

        WaitHandle AsyncWaitHandle { get; }

        object? AsyncState { get; }

        bool CompletedSynchronously { get; }
    }
}
